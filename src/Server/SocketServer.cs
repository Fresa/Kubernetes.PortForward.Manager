﻿using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Log.It;

namespace Port.Server
{
    internal class SocketServer : INetworkServer
    {
        private readonly ConcurrentQueue<INetworkClient> _clients =
            new ConcurrentQueue<INetworkClient>();

        private readonly BufferBlock<INetworkClient> _waitingClients =
            new BufferBlock<INetworkClient>();

        private readonly CancellationTokenSource _cancellationSource =
            new CancellationTokenSource();

        private Task _acceptingClientsBackgroundTask = default!;
        private Socket _clientAcceptingSocket = default!;

        private static readonly ILogger Logger =
            LogFactory.Create<SocketServer>();

        internal int Port { get; private set; }
        internal IPAddress Address { get; private set; } = IPAddress.None;

        public async Task<INetworkClient> WaitForConnectedClientAsync(
            CancellationToken cancellationToken = default)
        {
            var client = await _waitingClients
                .ReceiveAsync(cancellationToken)
                .ConfigureAwait(false);
            Logger.Debug("Client accepted {@client}", client);
            _clients.Enqueue(client);
            return client;
        }

        private SocketServer()
        {
        }

        internal static SocketServer Start(
            IPAddress address,
            int port = 0,
            ProtocolType protocolType = ProtocolType.Tcp)
        {
            var server = new SocketServer();
            server.Connect(address, port, protocolType);
            server.StartAcceptingClients();
            return server;
        }

        private void StartAcceptingClients()
        {
            _acceptingClientsBackgroundTask = Task.Run(
                async () =>
                {
                    while (_cancellationSource.IsCancellationRequested == false)
                    {
                        try
                        {
                            var clientSocket = await _clientAcceptingSocket
                                .AcceptAsync()
                                .ConfigureAwait(false);
                            Logger.Debug(
                                "Client connected {@clientSocket}",
                                clientSocket);

                            await _waitingClients
                                .SendAsync(
                                    new SocketNetworkClient(clientSocket),
                                    _cancellationSource.Token)
                                .ConfigureAwait(false);
                        }
                        catch when (_cancellationSource.IsCancellationRequested)
                        {
                            // Shutdown in progress
                            return;
                        }
                    }
                });
        }

        private void Connect(
            IPAddress address,
            int port,
            ProtocolType protocolType)
        {
            var endPoint = new IPEndPoint(address, port);

            _clientAcceptingSocket = new Socket(
                address.AddressFamily,
                SocketType.Stream,
                protocolType);

            _clientAcceptingSocket.Bind(endPoint);
            var localEndPoint =
                (IPEndPoint) _clientAcceptingSocket.LocalEndPoint;
            Port = localEndPoint.Port;
            Address = localEndPoint.Address;
            _clientAcceptingSocket.Listen(100);
        }

        public async ValueTask DisposeAsync()
        {
            Logger.Trace("Disposing");
            _cancellationSource.Cancel(false);
            try
            {
                _clientAcceptingSocket.Shutdown(SocketShutdown.Both);
                _clientAcceptingSocket.Close();
            }
            catch
            {
            } // Ignore unhandled exceptions during shutdown 
            finally
            {
                _clientAcceptingSocket.Dispose();
            }

            await _acceptingClientsBackgroundTask
                .ConfigureAwait(false);
            while (_clients.TryDequeue(out var client))
            {
                await client
                    .DisposeAsync()
                    .ConfigureAwait(false);
            }
            Logger.Trace("Disposed");
        }
    }
}
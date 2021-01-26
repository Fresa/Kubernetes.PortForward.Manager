﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Port.Server.IntegrationTests.SocketTestFramework;
using Port.Server.IntegrationTests.SocketTestFramework.Collections;
using Port.Server.Spdy;
using Port.Server.Spdy.Configuration;
using Port.Server.Spdy.Frames;

namespace Port.Server.IntegrationTests.Spdy
{
    public sealed class SpdyTestServer : IAsyncDisposable
    {
        private readonly InMemorySocketTestFramework _testFramework =
            SocketTestFramework.SocketTestFramework.InMemory();

        private ISendingClient<Frame> _client = default!;
        private INetworkServer _server = default!;

        internal async Task<SpdySession> ConnectAsync(
            Configuration configuration,
            CancellationToken cancellationToken = default)
        {
            _server =
                _testFramework.NetworkServerFactory.CreateAndStart(
                    IPAddress.Any, 1,
                    ProtocolType.Tcp);
            _client = await _testFramework.ConnectAsync(
                                              new FrameClientFactory(),
                                              IPAddress.Any, 1,
                                              ProtocolType.Tcp,
                                              cancellationToken)
                                          .ConfigureAwait(false);

            return SpdySession.CreateClient(
                await _server.WaitForConnectedClientAsync(cancellationToken)
                             .ConfigureAwait(false), configuration);
        }

        internal async Task SendAsync(
            Frame frame,
            CancellationToken cancellationToken = default)
        {
            await _client.SendAsync(frame, cancellationToken)
                         .ConfigureAwait(false);
        }

        internal ISubscription<T> On<T>()
            where T : Frame
            => _testFramework.On<T>();

        public async ValueTask DisposeAsync()
        {
            await _testFramework.DisposeAsync()
                                .ConfigureAwait(false);
            await _server.DisposeAsync()
                         .ConfigureAwait(false);
        }
    }
}
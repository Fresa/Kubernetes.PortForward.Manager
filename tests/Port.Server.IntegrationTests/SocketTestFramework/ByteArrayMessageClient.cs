﻿using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Port.Server.IntegrationTests.SocketTestFramework
{
    internal sealed class ByteArrayMessageClient : IMessageClient<byte[]>
    {
        private readonly INetworkClient _networkClient;

        public ByteArrayMessageClient(INetworkClient networkClient)
        {
            _networkClient = networkClient;
        }

        public async ValueTask<byte[]> ReceiveAsync(
            CancellationToken cancellationToken = default)
        {
            using var memoryOwner = MemoryPool<byte>.Shared.Rent(65536);
            var memory = memoryOwner.Memory;

            await _networkClient.ReceiveAsync(memory, cancellationToken);
            return memory.ToArray();
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }

        public async ValueTask SendAsync(
            byte[] payload,
            CancellationToken cancellationToken = default)
        {
            await _networkClient.SendAsync(payload, cancellationToken);
        }
    }
}
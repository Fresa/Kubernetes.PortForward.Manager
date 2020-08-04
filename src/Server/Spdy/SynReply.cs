﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Port.Server.Spdy
{
    public class SynReply : Control
    {
        public SynReply(
            byte flags)
            : base(flags)
        {
        }
        
        public const ushort Type = 2;
        public bool IsFin => Flags == 1;
        public int StreamId { get; set; }

        internal static async ValueTask<SynReply> ReadAsync(
            IFrameReader frameReader,
            CancellationToken cancellation = default)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> Headers { get; set; } =
            new Dictionary<string, string>();
    }
}
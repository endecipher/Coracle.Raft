using System;

namespace Coracle.Raft.Engine.Helper
{
    internal class SystemClock : ISystemClock
    {
        public DateTimeOffset Epoch()
        {
            return DateTimeOffset.UnixEpoch;
        }

        public DateTimeOffset Now()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}

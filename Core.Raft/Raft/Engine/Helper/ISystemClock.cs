using System;

namespace Coracle.Raft.Engine.Helper
{
    internal interface ISystemClock 
    {
        DateTimeOffset Now();
        DateTimeOffset Epoch();
    }
}

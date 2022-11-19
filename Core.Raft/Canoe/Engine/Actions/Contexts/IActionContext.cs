using System;

namespace Core.Raft.Canoe.Engine.Actions.Contexts
{
    internal interface IActionContext : IDisposable
    {
        bool IsContextValid { get; }
    }
}

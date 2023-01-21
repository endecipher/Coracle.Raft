using System;

namespace Coracle.Raft.Engine.Actions.Contexts
{
    internal interface IActionContext : IDisposable
    {
        bool IsContextValid { get; }
    }
}

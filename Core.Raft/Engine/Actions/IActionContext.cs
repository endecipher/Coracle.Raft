using System;

namespace Coracle.Raft.Engine.Actions
{
    internal interface IActionContext
    {
        bool IsContextValid { get; }
    }
}

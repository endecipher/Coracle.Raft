using System;

namespace Coracle.Raft.Engine.Operational
{
    public abstract class OperationalResult
    {
        public bool IsOperationSuccessful { get; set; }

        public Exception Exception { get; set; }
    }
}

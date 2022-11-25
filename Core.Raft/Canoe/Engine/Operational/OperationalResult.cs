using System;

namespace Core.Raft.Canoe.Engine.Operational
{
    public abstract class OperationalResult
    {
        public bool IsOperationSuccessful { get; set; }
        
        public Exception Exception { get; set; }
    }
}

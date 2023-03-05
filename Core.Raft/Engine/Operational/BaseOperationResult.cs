using System;

namespace Coracle.Raft.Engine.Operational
{
    public abstract class BaseOperationResult
    {
        public bool IsSuccessful { get; set; }

        public Exception Exception { get; set; }
    }
}

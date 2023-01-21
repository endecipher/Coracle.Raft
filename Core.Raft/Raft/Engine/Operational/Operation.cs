using Coracle.Raft.Engine.Remoting.RPC;
using System;

namespace Coracle.Raft.Engine.Operational
{
    /// <summary>
    /// Instances of this class will hold the Resultant operations of any Remote Procedure Call Execution.
    /// </summary>
    public class Operation<T> where T : IRemoteResponse
    {
        public T Response { get; set; } = default;

        public Exception Exception { get; set; } = null;

        public bool HasResponse => Exception == null;
    }
}

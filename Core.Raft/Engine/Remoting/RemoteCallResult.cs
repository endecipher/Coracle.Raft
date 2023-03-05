using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting.RPC;

namespace Coracle.Raft.Engine.Remoting
{
    /// <summary>
    /// Instances of this class will hold the resultant operations of any Remote Procedure Call execution.
    /// </summary>
    public class RemoteCallResult<T> : BaseOperationResult where T : IRemoteResponse
    {
        public T Response { get; set; } = default;

        public new bool IsSuccessful => Exception == null;
    }
}

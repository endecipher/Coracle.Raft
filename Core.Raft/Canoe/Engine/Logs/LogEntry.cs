using Core.Raft.Canoe.Engine.Command;
using Newtonsoft.Json;

namespace Core.Raft.Canoe.Engine.Logs
{
    /// <summary>
    /// In Search of an Understandable Consensus Algorithm - (Extended Version) Diego Ongaro and John Ousterhout Stanford University <c>[Section 4 - End Para]</c>
    /// 
    /// <para>
    /// Our second approach was to simplify the state space by reducing the number of states to consider, making the system more coherent and 
    /// eliminating nondeterminism where possible. Specifically, logs are not allowed to have holes, and Raft limits the ways in which logs can become
    /// inconsistent with each other.
    /// </para>
    /// 
    /// <para>
    /// <c>Coracle.NET - </c>
    /// The log is a collection of individual <see cref="LogEntry"/>, containing a <see cref="LogEntry.Command"/>, <see cref="LogEntry.CurrentIndex"/> 
    /// and  <see cref="LogEntry.Term"/>. 
    /// 
    /// <see cref="IPersistentReplicatedLogHolder"/> which deals with LogEntries
    /// </para>
    /// </summary>
    public class LogEntry 
    {
        public ICommand Command { get; init; }

        public bool HasCommand => Command != null;

        public long Term { get; init; }

        public long CurrentIndex { get; init; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

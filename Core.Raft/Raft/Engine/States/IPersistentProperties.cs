using Coracle.Raft.Engine.Logs.CollectionStructure;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States
{
    /// <summary>
    /// 
    /// Terms act as a logical clock in Raft, and they allow servers to detect obsolete information such as stale leaders.
    /// Each server stores a current term number, which increases monotonically over time.
    /// <see cref="Section 5.1 Second-to-last Para"/>
    /// 
    /// 
    /// Holds the Persistent State actions of the Engine.
    /// 
    /// Current Term should be initialized to 0 and VotedFor should be cleared on initialization.
    /// </summary>
    public interface IPersistentProperties
    {
        Task<long> GetCurrentTerm();

        Task SetCurrentTerm(long termNumber);

        Task<long> IncrementCurrentTerm();

        Task<string> GetVotedFor();

        Task SetVotedFor(string serverUniqueId);

        Task ClearVotedFor();

        IPersistentReplicatedLogHolder LogEntries { get; }
    }
}

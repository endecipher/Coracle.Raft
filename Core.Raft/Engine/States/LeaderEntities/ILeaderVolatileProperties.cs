using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Snapshots;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States.LeaderEntities
{
    internal interface ILeaderVolatileProperties : IMembershipUpdate
    {
        bool AreMajorityOfServersHavingEntriesUpUntilIndexReplicated(long index);
        Task DecrementNextIndex(string externalServerId, long followerConflictTerm, long followerFirstIndexOfConflictingTerm);
        Task DecrementNextIndex(string externalServerId);
        void Initialize();
        bool TryGetMatchIndex(string externalServerId, out long matchIndex);
        bool TryGetNextIndex(string externalServerId, out long nextIndex);
        void UpdateIndices(string externalServerId, long maxIndexReplicated);
        Task<IDictionary<string, ISnapshotHeader>> RequiresSnapshot();
    }
}

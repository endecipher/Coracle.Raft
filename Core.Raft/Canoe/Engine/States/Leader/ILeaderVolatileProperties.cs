using Core.Raft.Canoe.Engine.Configuration;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.States
{
    internal interface ILeaderVolatileProperties : IHandleConfigurationChange
    {
        bool AreMajorityOfServersHavingEntriesUpUntilIndexReplicated(long index);
        Task DecrementNextIndex(string externalServerId, long followerConflictTerm, long followerFirstIndexOfConflictingTerm);
        Task DecrementNextIndex(string externalServerId);
        void Initialize();
        bool TryGetMatchIndex(string externalServerId, out long matchIndex);
        bool TryGetNextIndex(string externalServerId, out long nextIndex);
        void UpdateIndices(string externalServerId, long maxIndexReplicated);
    }
}

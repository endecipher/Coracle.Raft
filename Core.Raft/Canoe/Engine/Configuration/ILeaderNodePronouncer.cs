using Core.Raft.Canoe.Engine.Configuration.Cluster;

namespace Core.Raft.Canoe.Engine.Configuration
{
    internal interface ILeaderNodePronouncer
    {
        INodeConfiguration RecognizedLeaderConfiguration { get; }
        bool IsLeaderRecognized => RecognizedLeaderConfiguration != null;
        void SetNewLeader(string leaderServerId);
        void SetRunningNodeAsLeader();
    }
}

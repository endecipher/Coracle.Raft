using Coracle.Raft.Engine.Configuration.Cluster;

namespace Coracle.Raft.Engine.Configuration
{
    internal interface ILeaderNodePronouncer
    {
        INodeConfiguration RecognizedLeaderConfiguration { get; }
        bool IsLeaderRecognized => RecognizedLeaderConfiguration != null;
        void SetNewLeader(string leaderServerId);
        void SetRunningNodeAsLeader();
    }
}

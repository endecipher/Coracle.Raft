namespace Coracle.Web
{
    

    public class Constants
    {
        public class Routes
        {
            private const string Raft = nameof(Raft);
            
            public static string ConfigurationChangeEndpoint => Strings.Actions.ConfigurationChange + "/" + nameof(Controllers.ConfigurationChangeController.HandleChange);
            public static string CommandEndpoint => Strings.Actions.Command + "/" + nameof(Controllers.CommandController.HandleCommand);
            public static string AppendEntriesEndpoint => Raft + "/" + nameof(Controllers.RaftController.AppendEntries);
            public static string RequestVoteEndpoint => Raft + "/" + nameof(Controllers.RaftController.RequestVote);
            public static string InstallSnapshotEndpoint => Raft + "/" + nameof(Controllers.RaftController.InstallSnapshot);
        }

        public class Discovery
        {
            public const string Enroll = "discovery/enroll";
            public const string GetCluster = "discovery/get";
        }

        public class Hubs
        {
            public const string Log = "/logHub";
            public const string Raft = "/raftHub";
        }

        public class Strings
        {
            public class Actions
            {
                public const string Command = nameof(Command);
                public const string ConfigurationChange = nameof(ConfigurationChange);
                public const string Successful = $" successfully executed: ";
            }

            public class Errors
            {
                public const string NodeNotReady = $"{nameof(Coracle)} Node not started yet";
            }

            public class Configuration
            {
                public const string Development = nameof(Development);
                public const string EngineConfiguration = "Coracle:EngineConfiguration";
            }
        }
    }
}

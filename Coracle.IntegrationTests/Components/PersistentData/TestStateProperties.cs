using ActivityLogger.Logging;
using ActivityLogLevel = ActivityLogger.Logging.ActivityLogLevel;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.Raft.Engine.Logs.CollectionStructure;
using Coracle.Raft.Engine.States;

namespace Coracle.IntegrationTests.Components.PersistentData
{
    public class TestStateProperties : IPersistentProperties
    {
        #region Constants 
        public const string PersistentPropertiesEntity = nameof(TestStateProperties);
        public const string CurrentTermValue = nameof(CurrentTermValue);
        public const string VotedForValue = nameof(VotedForValue);
        public const string GetValue = nameof(GetValue);
        public const string ClearingVotedFor = nameof(ClearingVotedFor);
        public const string IncrementedCurrentTerm = nameof(IncrementedCurrentTerm);
        public const string SetCurrentTermExternally = nameof(SetCurrentTermExternally);
        public const string SetVotedForExternally = nameof(SetVotedForExternally);
        #endregion

        IActivityLogger ActivityLogger { get; }

        public long CurrentTerm { get; private set; }

        public string VotedFor { get; private set; }

        public IPersistentReplicatedLogHolder LogEntries { get; }

        public TestStateProperties(IActivityLogger activityLogger)
        {
            CurrentTerm = 0;
            ActivityLogger = activityLogger;
            LogEntries = new TestLogHolder(this);
        }

        private void Snap(string eventString = GetValue)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = PersistentPropertiesEntity,
                Event = eventString,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(CurrentTermValue, CurrentTerm))
            .With(ActivityParam.New(VotedForValue, VotedFor))
            .WithCallerInfo());
        }

        public Task ClearVotedFor()
        {
            VotedFor = null;
            Snap(ClearingVotedFor);
            return Task.CompletedTask;
        }

        public Task<long> GetCurrentTerm()
        {
            Snap();
            return Task.FromResult(CurrentTerm);
        }

        public Task<string> GetVotedFor()
        {
            Snap();
            return Task.FromResult(VotedFor);
        }

        public Task<long> IncrementCurrentTerm()
        {
            CurrentTerm++;
            Snap(IncrementedCurrentTerm);
            return Task.FromResult(CurrentTerm);
        }

        public Task SetCurrentTerm(long termNumber)
        {
            CurrentTerm = termNumber;
            Snap(SetCurrentTermExternally);
            return Task.CompletedTask;
        }

        public Task SetVotedFor(string serverUniqueId)
        {
            VotedFor = serverUniqueId;
            Snap(SetVotedForExternally);
            return Task.CompletedTask;
        }
    }
}

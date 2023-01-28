using ActivityLogger.Logging;
using Coracle.Raft.Engine.ActivityLogger;

namespace Coracle.Raft.Engine.States
{
    internal interface IVolatileProperties
    {
        long CommitIndex { get; set; }
        long LastApplied { get; set; }
    }

    internal class VolatileProperties : IVolatileProperties
    {
        #region Constants
        public const string commitIndex = nameof(commitIndex);
        public const string lastApplied = nameof(lastApplied);
        public const string Entity = nameof(VolatileProperties);
        public const string VolatileChange = nameof(VolatileChange);

        #endregion

        public VolatileProperties(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
        }

        private long _commitIndex = 0;

        public long CommitIndex
        {
            get
            {
                return _commitIndex;
            }

            set
            {
                _commitIndex = value;

                Snap();
            }
        }


        private long _lastApplied = 0;

        public long LastApplied
        {
            get
            {
                return _lastApplied;
            }

            set
            {
                _lastApplied = value;

                Snap();
            }
        }

        public IActivityLogger ActivityLogger { get; }

        private void Snap()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Entity,
                Event = VolatileChange,
                Level = ActivityLogLevel.Debug
            }
            .With(ActivityParam.New(lastApplied, _lastApplied))
            .With(ActivityParam.New(commitIndex, _commitIndex))
            .WithCallerInfo());
        }
    }
}

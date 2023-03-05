using ActivityLogger.Logging;
using Coracle.Raft.Engine.ActivityLogger;

namespace Coracle.Raft.Engine.States
{
    namespace Current
    {
        public class CurrentAcessorActivityConstants
        {
            #region Constants
            public const string Entity = nameof(CurrentStateAccessor);
            public const string StateChange = nameof(StateChange);
            public const string newState = nameof(newState);
            #endregion
        }
    }

    internal class CurrentStateAccessor : ICurrentStateAccessor
    {
        public IActivityLogger ActivityLogger { get; }

        public CurrentStateAccessor(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
        }


        #region Node State Holder

        private object _lock = new object();
        IStateDevelopment _state = null;

        IStateDevelopment State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"State changed to {value.StateValue}",
                    EntitySubject = Current.CurrentAcessorActivityConstants.Entity,
                    Event = Current.CurrentAcessorActivityConstants.StateChange,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(Current.CurrentAcessorActivityConstants.newState, value.StateValue.ToString()))
                .WithCallerInfo());
            }
        }

        public IStateDevelopment Get()
        {
            lock (_lock)
            {
                return State;
            }
        }

        public void UpdateWith(IStateDevelopment changingState)
        {
            lock (_lock)
            {
                State = changingState;
            }
        }

        #endregion
    }
}

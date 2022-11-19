using ActivityLogger.Logging;
using Core.Raft.Canoe.Engine.ActivityLogger;
using System;

namespace Core.Raft.Canoe.Engine.States
{
    internal class CurrentStateAccessor : ICurrentStateAccessor
    {
        #region Constants

        public const string StateHolderEntity = nameof(CurrentStateAccessor);
        public const string StateChange = nameof(StateChange);
        public const string NewState = nameof(NewState);

        #endregion

        public IActivityLogger ActivityLogger { get; }

        public CurrentStateAccessor(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
        }


        #region Node State Holder

        private object _lock = new object();
        IChangingState _state = null;

        IChangingState State
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
                    EntitySubject = StateHolderEntity,
                    Event = StateChange,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(NewState, value.StateValue.ToString()))
                .WithCallerInfo());
            }
        }

        public IChangingState Get()
        {
            lock (_lock)
            {
                return State;
            }
        }

        public void UpdateWith(IChangingState changingState)
        {
            lock (_lock)
            {
                State = changingState;
            }
        }

        #endregion
    }

}

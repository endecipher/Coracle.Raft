using ActivityLogger.Logging;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using EventGuidance.Structure;
using System;

namespace EventGuidance.Responsibilities
{
    public class ActionJetton : IDisposable, IActionJetton
    {
        #region Constants

        public const string SignalDoneCalledOnException = nameof(SignalDoneCalledOnException);
        public const string SignalDoneCalledOnSuccess = nameof(SignalDoneCalledOnSuccess);
        public const string GetResultCalled = nameof(GetResultCalled);
        public const string StatusChanged = nameof(StatusChanged);
        public const string JettonUniqueIdentifier = nameof(UniqueIdentifier);
        public const string JettonUniqueActionName = "UniqueActionName";
        public const string OldStatus = nameof(OldStatus);
        public const string NewStatus = nameof(NewStatus);
        public const string ActionJettonEntity = nameof(ActionJetton);

        #endregion

        IActivityLogger ActivityLogger { get; set; }
        public IEventAction EventAction { get; }
        public IActionLock Awaiter { get; private set; }
        public bool IsBlocking { get; set; }
        public ActionJetton(IEventAction eventAction)
        {
            Awaiter = new ActionLock();
            EventAction = eventAction;
        }

        public ActionJetton WithLogger(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
            return this;
        }

        public string EventKey => string.Concat(EventAction.UniqueName, UniqueIdentifier.ToString());

        private readonly Guid UniqueIdentifier = Guid.NewGuid();

        public void Dispose()
        {
            Exception = null;
            Result = null;
        }

        public object Result { private get; set; } = null;

        public Exception Exception { get; set; }

        public void SetResultIfAny<T>(T result, Exception encouteredException = null) where T : class
        {
            if (encouteredException != null)
            {
                Exception = encouteredException;

                ActivityLogger?.Log(new GuidanceActivity
                {
                    Description = $"SignalDone() called on Exception. Setting Exception",
                    EntitySubject = ActionJettonEntity,
                    Event = SignalDoneCalledOnException,
                    Level = ActivityLogLevel.Verbose,

                }
                .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
                .With(ActivityParam.New(JettonUniqueActionName, EventAction.UniqueName))
                .WithCallerInfo());
            }
            else
            {
                Result = result;

                ActivityLogger?.Log(new GuidanceActivity
                {
                    Description = $"SignalDone() called on Success, setting Result",
                    EntitySubject = ActionJettonEntity,
                    Event = SignalDoneCalledOnSuccess,
                    Level = ActivityLogLevel.Verbose,

                }
                .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
                .With(ActivityParam.New(JettonUniqueActionName, EventAction.UniqueName))
                .WithCallerInfo());
            }

            Awaiter?.SignalDone();
        }

        public T GetResult<T>() where T : class
        {
            ActivityLogger?.Log(new GuidanceActivity
            {
                Description = $"GetResult<{nameof(T)}> called",
                EntitySubject = ActionJettonEntity,
                Event = GetResultCalled,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
            .With(ActivityParam.New(JettonUniqueActionName, EventAction.UniqueName))
            .WithCallerInfo());

            Awaiter?.Wait();

            FreeBlockingResources();

            if (HasCompleted && Result is T result)
            {
                return result;
            }

            throw Exception ?? new InvalidOperationException("Cannot Get proper result type or result for a non completed and non blocking Event Action");
        }

        public void FreeBlockingResources()
        {
            if (!IsBlocking)
            {
                Awaiter?.Dispose();
                Awaiter = null;
            }
        }

        #region Status Manipulations

        private EventActionStatusValues Status = EventActionStatusValues.New;

        public bool HasFaulted => Status == EventActionStatusValues.Faulted;

        public bool HasCanceled => Status == EventActionStatusValues.Cancelled;

        public bool HasCompleted => Status == EventActionStatusValues.Completed;

        public bool HasTimedOut => Status == EventActionStatusValues.TimedOut;

        public bool IsProcessing => Status == EventActionStatusValues.Processing;


        public void MoveToReady()
        {
            Change(EventActionStatusValues.New);
        }

        public void MoveToCompleted()
        {
            Change(EventActionStatusValues.Completed);
        }

        public void MoveToFaulted()
        {
            Change(EventActionStatusValues.Faulted);
        }

        public void MoveToProcessing()
        {
            Change(EventActionStatusValues.Processing);
        }

        public void MoveToTimeOut()
        {
            Change(EventActionStatusValues.TimedOut);
        }

        public void MoveToCancelled()
        {
            Change(EventActionStatusValues.Cancelled);
        }

        public void MoveToStopped()
        {
            Change(EventActionStatusValues.Stopped);
            EventAction.CancellationManager.TriggerCancellation();
        }

        private void Change(EventActionStatusValues newValue)
        {
            ActivityLogger?.Log(new GuidanceActivity
            {
                EntitySubject = ActionJettonEntity,
                Event = StatusChanged,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
            .With(ActivityParam.New(JettonUniqueActionName, EventAction.UniqueName))
            .With(ActivityParam.New(OldStatus, Status.ToString()))
            .With(ActivityParam.New(NewStatus, newValue.ToString()))
            .WithCallerInfo());

            Status = newValue;
        }
        #endregion
    }
}

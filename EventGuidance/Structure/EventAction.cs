using ActivityLogger.Logging;
using EventGuidance.Cancellation;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using EventGuidance.Responsibilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventGuidance.Structure
{
    public abstract class EventAction<TInput, TOutput> : IEventAction, IJettonExecutor where TInput : class where TOutput : class
    {
        #region Constants

        private const string EventActionEntity = nameof(IEventAction);
        private const string Exception = nameof(Exception);
        private const string _New = nameof(_New);
        private const string UniqueActionName = nameof(UniqueActionName);
        private const string _Begin = nameof(_Begin);
        private const string _Proceeding = nameof(_Proceeding);
        private const string _CoreActionStarting = nameof(_CoreActionStarting);
        private const string _CoreActionEnded = nameof(_CoreActionEnded);
        private const string _SkipProceeding = nameof(_SkipProceeding);
        private const string _OnTimeOut = nameof(_OnTimeOut);
        private const string _OnCancellation = nameof(_OnCancellation);
        private const string _OnFailure = nameof(_OnFailure);
        private const string _OnException = nameof(_OnException);
        private const string _PostProcessSignalling = nameof(_PostProcessSignalling);

        #endregion

        #region Dependencies 
        /// <summary>
        /// Transient Cancellation Manager associated with this Action
        /// </summary>
        public ICancellationManager CancellationManager { get; protected set; }
        protected IActivityLogger ActivityLogger { get; set; }

        #endregion

        protected TInput Input = null;

        public abstract TimeSpan TimeOut { get; }
        protected virtual TimeSpan? ShouldProceedTimeOut { get; } = null;

        public virtual ActionPriorityValues PriorityValue { get; set; } = ActionPriorityValues.Medium;

        public abstract string UniqueName { get; }

        public EventAction(TInput input, IActivityLogger activityLogger = null)
        {
            Input = input;
            ActivityLogger = activityLogger;

            ActivityLogger?.Log(new GuidanceActivity
            {
                EntitySubject = EventActionEntity,
                Event = _New,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());
        }

        public void SupportCancellation(ICancellationManager cancellationManager = null)
        {
            CancellationManager = cancellationManager ?? new CancellationManager(ActivityLogger);
        }

        #region Workflow Actions

        /// <summary>
        /// Returns Default Output of the Action. Used to initialize the Output.
        /// In any event of failure, the defult Output would be considered for BlockingEventActions.
        /// </summary>
        /// <returns></returns>
        protected virtual TOutput DefaultOutput()
        {
            return default;
        }

        /// <summary>
        /// Precondition to check whether the Processing should continue.
        /// </summary>
        /// <returns></returns>
        protected abstract Task<bool> ShouldProceed();

        /// <summary>
        /// Core Action to perform. Called within a Timeout wrapper.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<TOutput> Action(CancellationToken cancellationToken);

        /// <summary>
        /// Fired when The Core Action successfully completes.
        /// </summary>
        /// <param name="outputObtained"></param>
        /// <returns></returns>
        protected virtual async Task PostAction(TOutput outputObtained)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fired when Timeout Exception is caught.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task OnTimeOut()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fired when the Action is cancelled externally and TaskCanceledException/OperationCanceledException is caught.
        /// TODO: Determine difference
        /// </summary>
        /// <returns></returns>
        protected virtual async Task OnCancellation()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fired when an Aggregate Exception/Fault occurs
        /// </summary>
        /// <returns></returns>
        protected virtual async Task OnFailure()
        {
            await Task.CompletedTask;
        }

        #endregion

        /// <summary>
        /// Core Task to be queued for Processing. 
        /// </summary>
        async Task IJettonExecutor.Perform(IActionJetton jetton)
        {
            TOutput output = DefaultOutput();
            Exception exception = null;

            try
            {
                Log(_Begin);

                bool canProceed = ShouldProceedTimeOut.HasValue ? await ShouldProceed().WithTimeOut(ShouldProceedTimeOut.Value, CancellationManager?.CoreToken) : await ShouldProceed();

                if (canProceed)
                {
                    Log(_Proceeding);

                    CancellationManager?.ThrowIfCancellationRequested();

                    jetton.MoveToProcessing();

                    Log(_CoreActionStarting);

                    output = await Action(CancellationManager?.CoreToken ?? CancellationToken.None).WithTimeOut(TimeOut, CancellationManager?.CoreToken);

                    Log(_CoreActionEnded);

                    //No Cancellation post action, since the action itself may trigger new Responsibilities, which could cancel EventProcessor.
                    //So, since it's expected, we would want this action to at least move on without throwing a cancellation exception

                    await PostAction(output);

                    jetton.MoveToCompleted();
                }
                else
                {
                    Log(_SkipProceeding);
                }
            }
            catch (TimeoutException t)
            {
                exception = t;

                jetton.MoveToTimeOut();

                Log(_OnTimeOut, t);

                await OnTimeOut();
            }
            catch (AggregateException ae)
            {
                exception = ae;

                var ex = ae.InnerExceptions.First();

                foreach (var e in ae.InnerExceptions)
                {
                    Log(_OnException, e);
                }

                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    jetton.MoveToCancelled();

                    Log(_OnCancellation, ex);

                    await OnCancellation();
                }
                else
                {
                    jetton.MoveToFaulted();

                    Log(_OnFailure, ex);

                    await OnFailure();
                }
            }
            catch (Exception e)
            {
                exception = e;

                jetton.MoveToFaulted();

                Log(_OnFailure, e);

                await OnFailure();
            }

            Log(_PostProcessSignalling);

            jetton.SetResultIfAny<TOutput>(output, exception);
        }

        private void Log(string ev, Exception ex = null)
        {
            bool hasException = ex != null;

            Activity e = new GuidanceActivity
            {
                EntitySubject = UniqueName,
                Event = ev,
                Level = !hasException ? ActivityLogLevel.Verbose : ActivityLogLevel.Error,
            };

            if (hasException)
            {
                e = e.With(ActivityParam.New(Exception, ex));
            }

            ActivityLogger?.Log(e.WithCallerInfo());
        }

        IActionJetton IJettonExecutor.ReturnJetton()
        {
            return new ActionJetton(eventAction: this).WithLogger(ActivityLogger);
        }
    }
}

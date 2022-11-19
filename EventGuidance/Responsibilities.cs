using ActivityLogger.Logging;
using EventGuidance.Cancellation;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using EventGuidance.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EventGuidance.Responsibilities
{
    public class Responsibilities : IResponsibilities
    {
        #region Constants

        public const string ResponsibilitiesEntity = nameof(Responsibilities);
        public const string ConfiguringNew = nameof(ConfiguringNew);
        public const string QueuingEvent = nameof(QueuingEvent);
        public const string InvocableActions = nameof(InvocableActions);
        public const string ShouldExecuteSeparately = nameof(ShouldExecuteSeparately);
        public const string EventKey = nameof(EventKey);

        #endregion

        ICancellationManager GlobalCancellationManager { get; }
        IActivityLogger ActivityLogger { get; }
        IEventProcessor EventProcessor { get; }


        private bool IsConfigured { get; set; } = false;
        public string SystemId { get; private set; }
        HashSet<string> ConfiguredEvents { get; set; }
        public CancellationToken GlobalCancellationToken => GlobalCancellationManager.CoreToken;


        public Responsibilities(ICancellationManager manager, IEventProcessor eventProcessor, IActivityLogger activityLogger)
        {
            GlobalCancellationManager = manager;
            EventProcessor = eventProcessor;
            ActivityLogger = activityLogger;
        }

        /// <summary>
        /// Configure new Event Actions possible
        /// </summary>
        /// <param name="invocableEventActionNames"></param>
        /// <param name="eventLoopingActionsToQueue"></param>
        public void ConfigureNew(HashSet<string> invocableEventActionNames = null, string assigneeId = null)
        {
            SystemId = assigneeId;

            ActivityLogger?.Log(new GuidanceActivity
            {
                Description = $"Configuring New Responsibilities",
                EntitySubject = ResponsibilitiesEntity,
                Event = ConfiguringNew,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(InvocableActions, invocableEventActionNames))
            .WithCallerInfo());

            //Cancel All Tasks linked to Global Source
            GlobalCancellationManager.TriggerCancellation();

            //Clear Event Procesor Queue of any still Processing Tasks marked Separate/Information disposal
            EventProcessor.Stop();

            //Configure Possible Events To Fire
            ConfiguredEvents = invocableEventActionNames ?? new HashSet<string>();

            //Refresh Cancellation Manager Source for new State -- Check Refresh Code working Properly
            GlobalCancellationManager.Refresh();

            //Start background thread to queue up Tasks and fire them
            EventProcessor.StartProcessing(GlobalCancellationManager.CoreToken);
            
            IsConfigured = true;
        }

        public TOutput QueueBlockingEventAction<TOutput>(IEventAction action, bool executeSeparately = false) where TOutput : class
        {
            CheckIfConfigured();

            var jetton = (action as IJettonExecutor).ReturnJetton();

            jetton.IsBlocking = true;

            ActivityLogger?.Log(new GuidanceActivity
            {
                Description = $"Queueing {jetton.EventKey} | isSeparate: {executeSeparately}",
                EntitySubject = ResponsibilitiesEntity,
                Event = QueuingEvent,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(EventKey, jetton.EventKey))
            .With(ActivityParam.New(ShouldExecuteSeparately, executeSeparately))
            .WithCallerInfo());


            if (ConfiguredEvents.Any() && !ConfiguredEvents.TryGetValue(jetton.EventAction.UniqueName, out var value))
            {
                throw new ArgumentException($"Key:{jetton.EventAction.UniqueName} not registered for configured events");
            }

            if (!executeSeparately)
                action.CancellationManager.Bind(GlobalCancellationManager.CoreToken);

            jetton.MoveToReady();

            EventProcessor.Enqueue(jetton);

            return jetton.GetResult<TOutput>();
        }

        public void QueueEventAction(IEventAction action, bool executeSeparately = false)
        {
            CheckIfConfigured();

            var jetton = (action as IJettonExecutor).ReturnJetton();

            jetton.IsBlocking = false;

            ActivityLogger?.Log(new GuidanceActivity
            {
                Description = $"Queueing {jetton.EventKey} | isSeparate: {executeSeparately}",
                EntitySubject = ResponsibilitiesEntity,
                Event = QueuingEvent,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(EventKey, jetton.EventKey))
            .With(ActivityParam.New(ShouldExecuteSeparately, executeSeparately))
            .WithCallerInfo());

            if (ConfiguredEvents.Any() && !ConfiguredEvents.TryGetValue(jetton.EventAction.UniqueName, out var value))
            {
                throw new ArgumentException($"Key:{jetton.EventAction.UniqueName} not registered for configured events");
            }

            if (!executeSeparately)
                action.CancellationManager.Bind(GlobalCancellationManager.CoreToken);

            jetton.MoveToReady();

            jetton.FreeBlockingResources();

            EventProcessor.Enqueue(jetton);
        } 

        [Obsolete("Looping action to be handled externally")]
        public void QueueEventLoopingAction(IEventAction action, bool isSeparate)
        {
            
        }

        private void CheckIfConfigured()
        {
            if (!IsConfigured) throw new InvalidOperationException($"{nameof(Responsibilities)} not configured. Please call {nameof(ConfigureNew)}");
        }
    }
}

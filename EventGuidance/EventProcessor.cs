using ActivityLogger.Logging;
using EventGuidance.DataStructures;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using EventGuidance.Structure;
using System.Threading;
using System.Threading.Tasks;

namespace EventGuidance.Responsibilities
{
    public class EventProcessor : IEventProcessor
    {
        #region Constants

        public const string EventProcessorEntity = nameof(EventProcessor);
        public const string Starting = nameof(Starting);
        public const string Waiting = nameof(Waiting);
        public const string Stopping = nameof(Stopping);
        public const string Dequeued = nameof(Dequeued);
        public const string ForceStopping = nameof(ForceStopping);
        public const string EventKey = nameof(EventKey);

        #endregion

        IEventProcessorConfiguration EventProcessorConfiguration { get; }

        ConcurrentPriorityQueue<IActionJetton, ActionPriorityValues> ConcurrentPriorityQueue { get; }

        IActivityLogger ActivityLogger { get; }

        public EventProcessor(IEventProcessorConfiguration eventProcessorConfiguration, IActivityLogger activityLogger)
        {
            EventProcessorConfiguration = eventProcessorConfiguration;
            ActivityLogger = activityLogger;

            ConcurrentPriorityQueue = new ConcurrentPriorityQueue<IActionJetton, ActionPriorityValues>(
                EventProcessorConfiguration.EventProcessorQueueSize,
                new PriorityComparer()
            );
        }

        public void StartProcessing(CancellationToken token)
        {
            ActivityLogger?.Log(new GuidanceActivity
            {
                EntitySubject = EventProcessorEntity,
                Event = Starting,
                Level = ActivityLogLevel.Verbose,

            }
            .WithCallerInfo());

            var process = new Task(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (ConcurrentPriorityQueue.TryDequeue(out var info, out var priority))
                    {
                        ActivityLogger?.Log(new GuidanceActivity
                        {
                            Description = $"Dequeued and processing {info.EventKey}",
                            EntitySubject = EventProcessorEntity,
                            Event = Dequeued,
                            Level = ActivityLogLevel.Verbose,

                        }
                        .With(ActivityParam.New(EventKey, info.EventKey))
                        .WithCallerInfo());

                        new Task(() => (info.EventAction as IJettonExecutor).Perform(info), token, TaskCreationOptions.AttachedToParent).Start();
                    }
                    else
                    {
                        ActivityLogger?.Log(new GuidanceActivity
                        {
                            EntitySubject = EventProcessorEntity,
                            Event = Waiting,
                            Level = ActivityLogLevel.Verbose,

                        }
                        .WithCallerInfo());

                        Task.Delay(EventProcessorConfiguration.EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds, token).Wait();
                    }
                }
            },
            cancellationToken: token,
            creationOptions: TaskCreationOptions.DenyChildAttach);

            process.Start();
        }        

        public void Stop()
        {
            ActivityLogger?.Log(new GuidanceActivity
            {
                EntitySubject = EventProcessorEntity,
                Event = Stopping,
                Level = ActivityLogLevel.Verbose,

            }
            .WithCallerInfo());

            while (ConcurrentPriorityQueue.TryDequeue(out var info, out var priority))
            {
                info.MoveToStopped();

                ActivityLogger?.Log(new GuidanceActivity
                {
                    Description = $"Dequeued and force-stopping {info.EventKey}",
                    EntitySubject = EventProcessorEntity,
                    Event = ForceStopping,
                    Level = ActivityLogLevel.Verbose,

                }
                .With(ActivityParam.New(EventKey, info.EventKey))
                .WithCallerInfo());

                info.Dispose();
            }
        }

        public void Enqueue(IActionJetton info)
        {
            ConcurrentPriorityQueue.Enqueue(info, info.EventAction.PriorityValue);
        }
    }
}

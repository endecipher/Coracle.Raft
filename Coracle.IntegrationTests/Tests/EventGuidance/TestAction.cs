using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Logging;
using EventGuidance.Cancellation;
using EventGuidance.Responsibilities;
using EventGuidance.Structure;

namespace Coracle.IntegrationTests.Tests.EventGuidance
{
    internal abstract class TestAction : IEventAction, IJettonExecutor
    {
        #region Constants

        public const string Entity = nameof(TestAction);
        public const string StartingEvent = nameof(StartingEvent);
        public const string EndingEvent = nameof(EndingEvent);
        public const string UniqueActionName = nameof(UniqueActionName);
        #endregion

        public TestAction(IActivityLogger activityLogger, TimeSpan performTime)
        {
            ActivityLogger = activityLogger;
            PerformTime = performTime;
            CancellationManager = new CancellationManager(ActivityLogger);
        }

        public ICancellationManager CancellationManager { get; }

        public ActionPriorityValues PriorityValue { get; init; } 

        public TimeSpan TimeOut { get; init; }

        public string UniqueName { get; init; }

        public IActivityLogger ActivityLogger { get; }
        public TimeSpan PerformTime { get; }

        async Task IJettonExecutor.Perform(IActionJetton jetton)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = StartingEvent,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());

            await Task.Delay(PerformTime);

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = EndingEvent,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());
        }

        IActionJetton IJettonExecutor.ReturnJetton()
        {
            return new ActionJetton(eventAction: this).WithLogger(ActivityLogger);
        }
    }

    internal class NonBlockingAction : TestAction
    {
        public NonBlockingAction(IActivityLogger activityLogger, TimeSpan performTime) : base(activityLogger, performTime)
        {
        }
    }


    public class TestInput
    {
        public int Num1 { get; set; } = 3;
        public int Num2 { get; set; } = 4;

        public TimeSpan TimeOut { get; set; }
        public string ActionName { get; set; }
        public TimeSpan AdditionalPerformTime { get; set; }
        public bool ShouldProceed { get; set; }
        public TimeSpan ShouldProceedPerformTime { get; set; }
    }

    public class TestOutput
    {
        public int Sum { get; set; }
    }

    internal class BlockingAction : EventAction<TestInput, TestOutput>
    {
        #region Constants

        public const string Entity = nameof(BlockingAction);
        public const string Starting = nameof(Starting);
        public const string Ending = nameof(Ending);
        public const string StartingProceeds = nameof(StartingProceeds);
        public const string EndingProceeds = nameof(EndingProceeds);
        public const string UniqueActionName = nameof(UniqueActionName);

        #endregion
        public BlockingAction(TestInput input, IActivityLogger activityLogger) : base(input, activityLogger)
        {
            TimeOut = input.TimeOut;
            UniqueName = input.ActionName;
        }

        public override TimeSpan TimeOut { get; }

        public override string UniqueName { get; }

        protected override async Task<TestOutput> Action(CancellationToken cancellationToken)
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = Starting,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());

            await Task.Delay(Input.AdditionalPerformTime);

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = Ending,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());

            return new TestOutput
            {
                Sum = Input.Num1 + Input.Num2
            };
        }

        protected override async Task<bool> ShouldProceed()
        {
            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = StartingProceeds,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());

            await Task.Delay(Input.ShouldProceedPerformTime);

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = Entity,
                Event = EndingProceeds,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());

            return Input.ShouldProceed;
        }
    }
}

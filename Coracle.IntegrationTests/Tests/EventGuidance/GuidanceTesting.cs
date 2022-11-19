using ActivityLogger.Logging;
using ActivityMonitoring.Assertions.Core;
using Coracle.IntegrationTests.Components.ClientHandling.NoteCommand;
using Coracle.IntegrationTests.Components.ClientHandling.Notes;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Node;
using Coracle.IntegrationTests.Components.PersistentData;
using Coracle.IntegrationTests.Components.Remoting;
using Core.Raft.Canoe.Engine.Actions;
using Core.Raft.Canoe.Engine.ClientHandling;
using Core.Raft.Canoe.Engine.Configuration;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Discovery;
using Core.Raft.Canoe.Engine.Discovery.Registrar;
using Core.Raft.Canoe.Engine.Helper;
using Core.Raft.Canoe.Engine.Node;
using Core.Raft.Canoe.Engine.Remoting.RPC;
using Core.Raft.Canoe.Engine.States;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using EventGuidance.Responsibilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Xunit;


namespace Coracle.IntegrationTests.Framework
{
    public class ActionContext : IDisposable
    {
        public INodeContext NodeContext => ComponentContainer.Provider.GetRequiredService<INodeContext>();

        public ActionContext()
        {
            ComponentContainer.Configure();
            ComponentContainer.ServiceDescriptors.AddSingleton<IActivityLogger, TestActivityLogger>();
            ComponentContainer.ServiceDescriptors.AddSingleton<IActivityMonitorSettings, ActivityMonitorSettings>();
            ComponentContainer.ServiceDescriptors.AddSingleton<IActivityMonitor<Activity>, ActivityMonitor<Activity>>();

            //Final step
            ComponentContainer.Build();
        }

        public T GetService<T>() => ComponentContainer.Provider.GetRequiredService<T>();



        public void Dispose()
        {

        }


        internal static class ComponentContainer
        {
            public static ServiceProvider Provider { get; set; }
            public static IServiceCollection ServiceDescriptors { get; set; }

            public static void Configure()
            {
                var serviceCollection = new ServiceCollection()
                    .AddLogging(l => l.AddConsole())
                    .AddOptions()
                    .Configure<LoggerFilterOptions>(c => c.MinLevel = LogLevel.Trace);

                var dotNetContainer = new DotNetDependencyContainer(serviceCollection);

                new GuidanceDependencyRegistration().Register(dotNetContainer);

                ServiceDescriptors = serviceCollection;
            }

            public static void Build()
            {
                Provider = ServiceDescriptors.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true
                });
            }

            public static void Remove<T>()
            {
                var descriptor = ServiceDescriptors.SingleOrDefault(d => d.ServiceType == typeof(T));

                if (descriptor != null)
                    ServiceDescriptors.Remove(descriptor);
            }
        }

        public class DotNetDependencyContainer : IDependencyContainer
        {
            public DotNetDependencyContainer(IServiceCollection serviceDescriptors)
            {
                ServiceDescriptors = serviceDescriptors;
            }

            public IServiceCollection ServiceDescriptors { get; }

            void IDependencyContainer.RegisterSingleton<T1, T2>()
            {
                ServiceDescriptors.AddSingleton<T1, T2>();
            }

            void IDependencyContainer.RegisterTransient<T1, T2>()
            {
                ServiceDescriptors.AddTransient<T1, T2>();
            }
        }
    }

    //[TestCaseOrderer($"Coracle.IntegrationTests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.IntegrationTests")]
    public class GuidanceTesting : IClassFixture<ActionContext>
    {
        const string SystemId = "TestSystem";
        const int Seconds = 1000;
        const int MilliSeconds = 1;
        const int DecidedEventProcessorQueueSize = 49;
        const int DecidedEventProcessorWait = 100 * MilliSeconds;
        
        public GuidanceTesting(ActionContext context)
        {
            Context = context;

            var config = (Context.GetService<IEventProcessorConfiguration>() as EventProcessorConfiguration);
            config.EventProcessorQueueSize = DecidedEventProcessorQueueSize;
            config.EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds = DecidedEventProcessorWait;
        }

        public ActionContext Context { get; }

//        [Fact]
//        [Order(1)]
//        public void IsInitializationDoneProperly()
//        {
//            //Arrange
//            var monitor = Context.GetService<IActivityMonitor<Activity>>();

//            monitor.Start();

//            var queue = monitor.Capture().All();

//            var isEventProcessorStarting =  queue.AttachNotifier(x => x.Is(EventProcessor.EventProcessorEntity, EventProcessor.Starting));

//            IAssertableQueue<Activity> assertableQueue;

//            Exception caughtException = null;

//            //Act
//            try
//            {
//                Context.GetService<IResponsibilities>().ConfigureNew(assigneeId: SystemId);

//                isEventProcessorStarting.Wait();
//            }
//            catch (Exception e)
//            {
//                caughtException = e;
//            }

//            assertableQueue = monitor.EndCapture(queue.Id);

//#region Assertions
//            caughtException
//                .Should().Be(null, $"- Initialization should occur successfully and not throw {caughtException}");

//            Context
//                .GetService<IEventProcessorConfiguration>()
//                .EventProcessorQueueSize
//                .Should().Be(DecidedEventProcessorQueueSize, "- during Initialization, the EventProcessorConfiguration should be updated to the QueueSize supplied");

//            Context
//                .GetService<IEventProcessorConfiguration>()
//                .EventProcessorWaitTimeWhenQueueEmpty_InMilliseconds
//                .Should().Be(DecidedEventProcessorWait, "- during Initialization, the EventProcessorConfiguration should be updated to the WaitTime supplied");

//            assertableQueue
//                .Dig()
//                .UntilItSatisfies(x => x.Is(Responsibilities.ResponsibilitiesEntity, Responsibilities.ConfiguringNew));

//            isEventProcessorStarting.IsConditionMatched.Should().BeTrue();

//            assertableQueue
//                .Dig()
//                .UntilItSatisfies(x => x.Is(EventProcessor.EventProcessorEntity, EventProcessor.Waiting));

//#endregion
//        }


        /*
         * Test ConcurrentPrioQueue all operations
         * Test ConcurrentAwaiter
         * Test RecentPropertyCache
         * Test any new decorated Sync Primitives used by Core.Raft and this project such that they should reside in Event Guidance and tested
         */


        /*
         * - Enqueue Non Blocking action and check if Log has starting and ending.
         *      Also check if there's only one Log of starting and ending each.
         *      Also check if EventProcessor is waiting for more
         *      Also capture memory from start and end, and see if it doesn't grow
         *  - Enqueue Non Blocking action and wait for result.
         *      Check if Log has single startingproceeds and endingproceeds
         *      Check if Log has single starting starting and ending each.
         *      Check if Sum is proper
         *      Also check if EventProcessor is waiting for more
         *      Also capture memory from start and end, and see if it doesn't grow
         * - Enqueue Blocking Action with total timeout X, but ShouldProceedPerformTime 2X
         *      Check if log has single startproceeds
         *      Check that log has no endproceeds
         *      Check that log has no Starting and Ending each
         *      Check that log has Timeout logged internally
         *      Assert Jetton Result Exception should be not null and Timeout
         *      Also check if EventProcessor is waiting for more
         *      Also capture memory from start and end, and see if it doesn't grow
         * - Enqueue Blocking Action with total timeout X, but ActionPerformTime 2X
         *      Check if log has single startproceeds and endproceeds
         *      Check that log has no Ending, but single Starting
         *      Check that log has Timeout logged internally
         *      Assert Jetton Result Exception should be not null and Timeout
         *      Also check if EventProcessor is waiting for more
         *      Also capture memory from start and end, and see if it doesn't grow
         * - Enqueue NonBlocking Action with total timeout 2X, and Enqueue NonBlocking with X.
         *  Bind with Responsibilities coretoken for both. But call actioncancellationManager.Cancel() for X.
         *      Check that log has Cancelled logged internally for X
         *      Assert Jetton Result Exception should be not null and that of Cancellation for X
         *      Also check if EventProcessor is waiting for more
         *      Also check if Responsibilties core token is not canceled
         *      Also check if 2X has Ending logged
         *      Also capture memory from start and end, and see if it doesn't grow
         * - Enqueue Non-Blocking Action in a separate task with total timeout X, but in main thread, call Responsibilities.ConfigureNew()
         *      Check that log has Cancelled logged internally for that action
         *      Check that log has CancellationTriggeredViaBinding from CancMgr
         *      Assert Jetton Result Exception should be not null and that of Cancellation
         *      Check new Responsibilities configured
         *      Also check if EventProcessor has restarted and in waiting
         *      Also capture memory from start and end, and see if it doesn't grow
         *      
         *      //Ideally, this functionality is specific to what Core.Raft does. These cases are extreme. They should not be tested.
         *      //As part of CoracleInteg, we have to test these separately along with the functionality
         *      ALSO, MOVE ONCLIENTCOMMANDRECEIVE SHOULD PROCEED INSIDE CORE ACTION, SO THAT TIMEOUT JHAMELA IS AVOIDED
         * - Enqueue NonBlocking Action with total timeout X, and internally in action, register itself again post completion using retry counters
         *      Check if log has single startproceeds
         *      Check that log has no endproceeds
         *      Check that log has no Starting and Ending each
         *      Check that log has Timeout logged internally
         *      Assert Jetton Result Exception should be not null and Timeout
         *      Also check if EventProcessor is waiting for more
         *      Also capture memory from start and end, and see if it doesn't grow
         */



        

        ///// <summary>
        ///// All of the above tests should be focused before the ElectionTimer runs off
        ///// </summary>
        ///// <returns></returns>
        //[Fact]
        //public async Task IsFollowerTurningToCandidate()
        //{
        //    //LogAssert.Ready();
        //    //TestElectionTimer.EventLock.SignalDone() //For TimerCallback to execute for OnElectionTimeout so that this node becomes Candidate
        //    //LogAssert.Wait(x=> x.Event is StartingElection CandidateEntity) - 
        //    //
        //    //LogAssert.Complete();
        //}

        [Fact]
        [Order(int.MaxValue)]
        public async Task TestTemplateMethod()
        {
#region Arrange



#endregion

#region Act

            Exception caughtException = null;

            try
            {
            }
            catch (Exception e)
            {
                caughtException = e;
            }

#endregion

#region Assert

            caughtException
                .Should().Be(null, $" ");

#endregion
        }
    }

}

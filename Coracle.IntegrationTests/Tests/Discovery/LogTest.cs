using ActivityLogger.Logging;
using Coracle.Samples.ClientHandling.Notes;
using Coracle.IntegrationTests.Components.PersistentData;
using Coracle.IntegrationTests.Components.Remoting;
using Coracle.Raft.Engine.Actions.Awaiters;
using Coracle.Raft.Engine.Actions.Core;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.ClientHandling.Command;
using Coracle.Raft.Engine.Configuration;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Discovery;
using Coracle.Raft.Engine.Discovery.Registrar;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.Node;
using Coracle.Raft.Engine.Operational;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.States.LeaderEntities;
using EntityMonitoring.FluentAssertions.Extensions;
using EntityMonitoring.FluentAssertions.Structure;
using FluentAssertions;
using TaskGuidance.BackgroundProcessing.Core;
using Xunit;
using Coracle.Samples.ClientHandling.NoteCommand;
using Coracle.Raft.Engine.Logs;

namespace Coracle.IntegrationTests.Framework
{
    [TestCaseOrderer($"Coracle.IntegrationTests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.IntegrationTests")]
    public class LogTest : BaseTest, IClassFixture<TestContext>
    {
        public LogTest(TestContext context) : base(context)
        {
        }

        [Fact]
        [Order(1)]
        public async Task IsLogHavingSingleEntryInitially()
        {
#region Arrange

            Exception caughtException = null;
            InitializeEngineConfiguration();

            Context.GetService<IActivityMonitor<Activity>>().Start();

            var queue = CaptureActivities();

#endregion

#region Act
            List<LogEntry> allEntries = null;
            LogEntry entry = null;

            try
            {
                entry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();
                allEntries = Context.GetService<IPersistentProperties>().LogEntries.GetAll().ToList();

            }
            catch (Exception e)
            {
                caughtException = e;
            }

#endregion

#region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            allEntries
                .Count
                .Should().Be(1, "AS there should be only one entry present");

            entry
                .Term
                .Should().Be(0, "As it's the initialized entry for the log holder");

            entry
                .CurrentIndex
                .Should().Be(0, "As it's the initialized entry for the log holder");

            entry
                .Type
                .Should().Be(Raft.Engine.Logs.LogEntry.Types.None, "As this is the initialized entry type");

#endregion
        }


        [Fact]
        [Order(2)]
        public async Task IsLogOverwritingEntriesSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            var queue = CaptureActivities();

            #endregion

            #region Act
            List<LogEntry> allEntries = null;
            LogEntry lastEntry = null;

            try
            {

                LogEntry initialEntry = new LogEntry
                {
                    CurrentIndex = 0,
                    Content = null,
                    Term = 0,
                    Type = LogEntry.Types.None,
                };

                LogEntry noOpEntry = new LogEntry
                {
                    CurrentIndex = 1,
                    Content = null,
                    Term = 1,
                    Type = LogEntry.Types.NoOperation,
                };

                await Context.GetService<IPersistentProperties>().LogEntries.OverwriteEntries(new List<LogEntry>
                {
                    initialEntry,
                    noOpEntry
                });

                lastEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

                allEntries = Context.GetService<IPersistentProperties>().LogEntries.GetAll().ToList();

            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            allEntries
                .Count
                .Should().Be(2, "As it's the initialized entry for the log holder and the new one added");

            lastEntry
                .Term
                .Should().Be(1, "As this is the new entry's property value");

            lastEntry
                .CurrentIndex
                .Should().Be(1, "As this is the new entry's property value");

            lastEntry
                .Type
                .Should().Be(Raft.Engine.Logs.LogEntry.Types.NoOperation, "As this is the new entry's type");

            #endregion
        }

        [Fact]
        [Order(3)]
        public async Task IsLogOverwritingOldEntriesSuccessfully()
        {
            #region Arrange

            Exception caughtException = null;

            var queue = CaptureActivities();

            #endregion

            #region Act
            List<LogEntry> allEntries = null;
            LogEntry lastEntry = null;
            LogEntry initialEntry = new LogEntry
            {
                CurrentIndex = 0,
                Content = null,
                Term = 0,
                Type = LogEntry.Types.None,
            };

            NoteCommand testCommand = TestAddCommand().TestCommand;

            LogEntry commandEntry = new LogEntry
            {
                CurrentIndex = 2,
                Content = testCommand,
                Term = 2,
                Type = LogEntry.Types.Command,
            };

            try
            {
                await Context.GetService<IPersistentProperties>().LogEntries.OverwriteEntries(new List<LogEntry>
                {
                    initialEntry,
                    commandEntry
                });

                lastEntry = await Context.GetService<IPersistentProperties>().LogEntries.TryGetValueAtLastIndex();

                allEntries = Context.GetService<IPersistentProperties>().LogEntries.GetAll().ToList();

            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assertions

            var assertableQueue = StartAssertions(queue);

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            allEntries
                .Count
                .Should().Be(2, "As it's the initialized entry for the log holder and the new Command entry added");

            lastEntry
                .Term
                .Should().Be(commandEntry.Term, "As this is the new entry's property value");

            lastEntry
                .CurrentIndex
                .Should().Be(commandEntry.CurrentIndex, "As this is the new entry's property value");

            lastEntry
                .Type
                .Should().Be(commandEntry.Type, "As this is the new entry's property value");

            allEntries
                .First()
                .Type
                .Should().Be(LogEntry.Types.None, "As this is it the initial entry's property");

            #endregion
        }


        [Fact]
        [Order(4)]
        public void IsMajorityWorkingFine()
        {
            #region Arrange

            Exception caughtException = null;

            #endregion

            #region Act

            bool _2OutOf3Majority = false;
            bool _3OutOf4Majority = false;
            bool _2OutOf4Majority = false;
            bool _1OutOf3Majority = false;

            try
            {
                _2OutOf3Majority = Majority.IsAttained(2, 3);
                _3OutOf4Majority = Majority.IsAttained(3, 4);
                _2OutOf4Majority = Majority.IsAttained(2, 4);
                _1OutOf3Majority = Majority.IsAttained(1, 3);

            }
            catch (Exception e)
            {
                caughtException = e;
            }

            #endregion

            #region Assertions

            caughtException
                .Should().Be(null, $"Act should occur successfully");

            _2OutOf3Majority
                .Should().Be(true);

            _1OutOf3Majority
                .Should().Be(false);

            _2OutOf4Majority
                .Should().Be(false);

            _3OutOf4Majority
                .Should().Be(true);

            #endregion
        }
    }
}

#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using ActivityLogger.Logging;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.States;
using EntityMonitoring.FluentAssertions.Structure;
using FluentAssertions;
using Xunit;
using Coracle.Raft.Engine.Logs;
using Newtonsoft.Json;
using Coracle.Raft.Examples.Data;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Tests.Framework;

namespace Coracle.Raft.Tests.Integration
{
    /// <summary>
    /// Tests basic characteristics of the log chain
    /// </summary>
    [TestCaseOrderer($"Coracle.Raft.Tests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.Raft.Tests")]
    public class LogHolderWorkflowTest : BaseTest, IClassFixture<TestContext>
    {
        public LogHolderWorkflowTest(TestContext context) : base(context)
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
                entry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();
                allEntries = (Context.GetService<IPersistentStateHandler>() as SampleVolatileStateHandler).GetAll().ToList();

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
                .Should().Be(LogEntry.Types.None, "As this is the initialized entry type");

            Cleanup();
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

                await Context.GetService<IPersistentStateHandler>().OverwriteEntries(new List<LogEntry>
                {
                    initialEntry,
                    noOpEntry
                });

                lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

                allEntries = (Context.GetService<IPersistentStateHandler>() as SampleVolatileStateHandler).GetAll().ToList();

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
                .Should().Be(LogEntry.Types.NoOperation, "As this is the new entry's type");

            Cleanup();
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
                await Context.GetService<IPersistentStateHandler>().OverwriteEntries(new List<LogEntry>
                {
                    initialEntry,
                    commandEntry
                });

                lastEntry = await Context.GetService<IPersistentStateHandler>().TryGetValueAtLastIndex();

                allEntries = (Context.GetService<IPersistentStateHandler>() as SampleVolatileStateHandler).GetAll().ToList();

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

            Cleanup();
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
                _2OutOf3Majority = Majority.HasAttained(2, 3);
                _3OutOf4Majority = Majority.HasAttained(3, 4);
                _2OutOf4Majority = Majority.HasAttained(2, 4);
                _1OutOf3Majority = Majority.HasAttained(1, 3);

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

            Cleanup();
            #endregion
        }
    }
}

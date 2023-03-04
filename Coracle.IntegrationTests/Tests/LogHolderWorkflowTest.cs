using ActivityLogger.Logging;
using Coracle.Raft.Engine.Helper;
using Coracle.Raft.Engine.States;
using EntityMonitoring.FluentAssertions.Structure;
using FluentAssertions;
using Xunit;
using Coracle.Raft.Engine.Logs;
using Coracle.Samples.ClientHandling;
using Coracle.Samples.Data;
using Coracle.IntegrationTests.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Coracle.IntegrationTests.Tests
{
    public static class JsonExtensions
    {
        public static JToken RemoveFromLowestPossibleParent(this JToken node)
        {
            if (node == null)
                return null;
            var contained = node.AncestorsAndSelf().Where(t => t.Parent is JContainer && t.Parent.Type != JTokenType.Property).FirstOrDefault();
            if (contained != null)
                contained.Remove();
            // Also detach the node from its immediate containing property -- Remove() does not do this even though it seems like it should
            if (node.Parent is JProperty)
                ((JProperty)node.Parent).Value = null;
            return node;
        }
    }

    /// <summary>
    /// Tests basic characteristics of the log chain
    /// </summary>
    [TestCaseOrderer($"Coracle.IntegrationTests.Framework.{nameof(ExecutionOrderer)}", $"Coracle.IntegrationTests")]
    public class LogHolderWorkflowTest : BaseTest, IClassFixture<TestContext>
    {
        public LogHolderWorkflowTest(TestContext context) : base(context)
        {
        }

        [JsonConverter((typeof(ActionConverter)))]
        public class Item
        {
            public IEnumerable<Entry> Entries { get; set; }

            public long Id { get; set; }

            public string Name { get; set; }
        }

        public class Entry
        {
            public object Data { get; set; }

            public string Name { get; set; }
        }

        class ActionConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                var obj = JObject.Load(reader);
                var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(objectType);
                var item = existingValue as Item ?? (Item)contract.DefaultCreator();

                // Remove the Result property for manual deserialization
                var objProperty = obj.GetValue(nameof(Item.Entries), StringComparison.OrdinalIgnoreCase).RemoveFromLowestPossibleParent();

                // Populate the remaining properties.
                using (var subReader = obj.CreateReader())
                {
                    serializer.Populate(subReader, item);
                }

                item.Entries = new List<Entry>();

                // Process the Result property
                if (objProperty != null)
                {
                    foreach (var child in objProperty.Children())
                    {
                        var entry = new Entry();

                        var data = child.SelectToken(nameof(Entry.Data));

                        using (var childReader = child.CreateReader())
                        {
                            serializer.Populate(childReader, entry);
                        }

                        if (entry.Name.Equals(nameof(Command)))
                        {
                            entry.Data = data.ToObject<Command>();
                        }
                        else if (entry.Name.Equals(nameof(Config)))
                        {
                            entry.Data = data.ToObject<Config>();
                        }
                    }
                }

                return item;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                
            }
        }

        public class Command
        {
            public string CommandId = Guid.NewGuid().ToString();
        }

        public class Config
        {
            public long Type { get; set; }
        }

        public class Writer : ITraceWriter
        {
            public System.Diagnostics.TraceLevel LevelFilter => System.Diagnostics.TraceLevel.Verbose;

            public List<string> Messages = new List<string>();

            public void Trace(System.Diagnostics.TraceLevel level, string message, Exception ex)
            {
                Messages.Add(message ?? ex.Message);
            }
        }

        [Fact]
        [Order(1)]
        public async Task IsLogHavingSingleEntryInitially()
        {

            var item = new Item { Entries = new List<Entry>
            {
                new Entry
                {
                    Data = new Command(),
                    Name = nameof(Command),
                },

                new Entry
                {
                    Data = new Config()
                    {
                        Type = 12
                    }
                }
            }, Id = 1, Name = "Hello" };


            Writer writer = new Writer();
            var s1 = JsonConvert.SerializeObject(item, new JsonSerializerSettings
            {
                TraceWriter = writer,

            });


            Writer writer1 = new Writer();
            var obj1 = JsonConvert.DeserializeObject<Item>(s1, new JsonSerializerSettings
            {

                TraceWriter = writer1,

            });



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

            #endregion
        }
    }
}

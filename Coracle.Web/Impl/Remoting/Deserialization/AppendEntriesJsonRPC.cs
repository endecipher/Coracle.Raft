using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.Logs;
using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Examples.ClientHandling;
using Coracle.Raft.Examples.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coracle.Web.Impl.Remoting.Deserialization
{
    [JsonConverter(typeof(AppendEntriesConverter))]
    public class AppendEntriesJsonRPC : AppendEntriesRPC
    {

    }

    class AppendEntriesConverter : JsonConverter
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
            AppendEntriesJsonRPC item = new AppendEntriesJsonRPC();

            var objProperty = obj.GetValue(nameof(AppendEntriesRPC.Entries), StringComparison.OrdinalIgnoreCase);

            using (var subReader = obj.CreateReader())
            {
                serializer.Populate(subReader, item);
            }

            var entries = new List<LogEntry>();

            if (objProperty != null)
            {
                foreach (var child in objProperty.Children())
                {
                    var entry = new LogEntry();
                    entries.Add(entry);

                    using (var childReader = child.CreateReader())
                    {
                        serializer.Populate(childReader, entry);
                    }

                    var data = child.SelectToken(nameof(LogEntry.Content).ToLower());

                    switch (entry.Type)
                    {
                        case LogEntry.Types.None:
                        case LogEntry.Types.NoOperation:
                            entry.Content = null;
                            break;
                        case LogEntry.Types.Command:
                            entry.Content = data.ToObject<NoteCommand>();
                            break;
                        case LogEntry.Types.Configuration:
                            entry.Content = data.ToObject<NodeConfiguration[]>();
                            break;
                        case LogEntry.Types.Snapshot:
                            entry.Content = data.ToObject<SnapshotHeader>();
                            break;
                        default:
                            throw new InvalidCastException($"{entry.Type} is not supported for {nameof(AppendEntriesJsonRPC)} conversion");
                    }
                }
            }

            item.Entries = entries;
            return item;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

        }
    }
}

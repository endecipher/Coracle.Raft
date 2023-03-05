using Coracle.Raft.Engine.Remoting.RPC;
using Coracle.Raft.Examples.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coracle.Web.Impl.Remoting.Deserialization
{
    [JsonConverter(typeof(InstallSnapshotConverter))]
    public class InstallSnapshotJsonRPC : InstallSnapshotRPC
    {
    }

    class InstallSnapshotConverter : JsonConverter
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
            InstallSnapshotJsonRPC item = new InstallSnapshotJsonRPC();

            var objProperty = obj.GetValue(nameof(InstallSnapshotJsonRPC.Data).ToLower(), StringComparison.OrdinalIgnoreCase);

            using (var subReader = obj.CreateReader())
            {
                serializer.Populate(subReader, item);
            }

            item.Data = objProperty.ToObject<SnapshotDataChunk>();
            return item;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

        }
    }
}

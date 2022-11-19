using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Newtonsoft.Json;

namespace Core.Raft.Canoe.Engine.Helper
{
    public static class Extensions
    {
        public static string Stringify(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}

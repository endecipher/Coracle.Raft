using Newtonsoft.Json;

namespace ActivityLogger.Logging
{
    [Serializable]
    public class ActivityParam
    {
        static JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public string Name { get; set; }
        public string Value { get; set; }
        public string Message { get; set; }

        public static ActivityParam New<T>(string name, T value, string Message = null) where T : class
        {
            return new ActivityParam
            {
                Name = name,
                Value = JsonConvert.SerializeObject(value, Settings),
                Message = Message
            };
        }

        public static ActivityParam New(string name, Guid value, string Message = null)
        {
            return new ActivityParam
            {
                Name = name,
                Value = Convert.ToString(value),
                Message = Message
            };
        }

        public static ActivityParam New(string name, string value, string Message = null)
        {
            return new ActivityParam
            {
                Name = name,
                Value = value is string ? Convert.ToString(value) : JsonConvert.SerializeObject(value, Settings),
                Message = Message
            };
        }

        public static ActivityParam New(string name, int? value, string Message = null)
        {
            return new ActivityParam
            {
                Name = name,
                Value = value.HasValue ? Convert.ToString(value) : null,
                Message = Message
            };
        }

        public static ActivityParam New(string name, long? value, string Message = null)
        {
            return new ActivityParam
            {
                Name = name,
                Value = value.HasValue ? Convert.ToString(value) : null,
                Message = Message
            };
        }

        public static ActivityParam New(string name, bool value, string Message = null)
        {
            return new ActivityParam
            {
                Name = name,
                Value = Convert.ToString(value),
                Message = Message
            };
        }
    }
}

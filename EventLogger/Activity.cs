using System.Runtime.CompilerServices;

namespace ActivityLogger.Logging
{
    [Serializable]
    public abstract class Activity : IActivity
    {
        public Activity()
        {
            EventTime = DateTimeOffset.UtcNow;
        }

        public ActivityLogLevel Level { get; init; }
        public string EntitySubject { get; init; }
        public string Event { get; init; }
        public string Description { get; set; } = null;
        public DateTimeOffset EventTime { get; private set; }
        public ICollection<ActivityParam> Parameters { get; private set; }
        public IActivityContext Context { get; set; }

        public ActivityCallerInfo CallerInfo { get; private set; }

        public virtual Activity With(params ActivityParam[] logParams)
        {
            foreach (var p in logParams) AddParam(p);
            return this;
        }

        public Activity WithCallerInfo([CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            CallerInfo = new ActivityCallerInfo
            {
                CallerMethod = memberName,
                CallerLineNumber = sourceLineNumber,
                CallerFilePath = sourceFilePath
            };

            return this;
        }

        public virtual Activity WithContext(IActivityContext ctx)
        {
            Context = ctx;

            return this;
        }

        private void AddParam(ActivityParam param)
        {
            if (Parameters == null) 
                Parameters = new List<ActivityParam>(); 

            Parameters.Add(param);
        }
    }
}

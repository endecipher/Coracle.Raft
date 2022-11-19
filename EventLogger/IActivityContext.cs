namespace ActivityLogger.Logging
{
    public interface IActivityContext
    {
        public string SystemId { get; set; }
        public string CallerFilePath { get; set; }
        public string CallerMethod { get; set; }
        public int CallerLineNumber { get; set; }
        public string CorrelationId { get; set; }
    }
}

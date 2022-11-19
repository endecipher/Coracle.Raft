namespace ActivityLogger.Logging
{
    [Serializable]
    public class ActivityCallerInfo
    {
        public string CallerFilePath { get; init; } = string.Empty;
        public string CallerMethod { get; init; } = string.Empty;
        public int CallerLineNumber { get; init; } = 0;
    }
}

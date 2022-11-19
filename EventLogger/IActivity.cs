using System.Runtime.CompilerServices;

namespace ActivityLogger.Logging
{
    /// <summary>
    /// To be used by intermediate libraries for injecting Context
    /// </summary>
    public interface IActivity
    {
        Activity WithCallerInfo([CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0);
    }
}

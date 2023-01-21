using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Coracle.Web.Hubs;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Coracle.Web.Impl.Logging
{
    //public class SimpleEventLogger : IEventLogger
    //{
    //    public enum LogLevel
    //    {
    //        Information = 2,
    //        Debug = 1,
    //        Verbose = 0,
    //        Exception = 3,
    //    }

    //    Guid Guid { get; }

    //    public SimpleEventLogger()
    //    {
    //        Guid = Guid.NewGuid();
    //    }

    //    public IHubContext<LogHub> LogHubContext { get; set; }

    //    public LogLevel Level { get; set; } = LogLevel.Exception;

    //    internal DateTimeOffset Now => DateTimeOffset.UtcNow;

    //    internal string UniqueNodeId => ComponentContainer.Instance.GetInstance<IClusterConfiguration>().ThisNode.UniqueNodeId;

    //    public void LogVerbose(string _event, string _uniqueEventIdentifier = null, string _message = null, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
    //    {
    //        if (!CanProceed(LogLevel.Verbose)) return;

    //        string message = $"{UniqueNodeId}: DEBUG {Now} {_event} {_uniqueEventIdentifier} {_message} {Path.GetFileName(sourceFilePath) ?? sourceFilePath} {memberName} {sourceLineNumber}";
    //        //Debug.WriteLine(message);
    //        LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);
    //    }

    //    public void LogDebug(string _event, string _uniqueEventIdentifier = null, string _message = null, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
    //    {
    //        if (!CanProceed(LogLevel.Debug)) return;

    //        string message = $"{UniqueNodeId}: DEBUG {Now} {_event} {_uniqueEventIdentifier} {_message} {Path.GetFileName(sourceFilePath) ?? sourceFilePath} {memberName} {sourceLineNumber}";
    //        //Debug.WriteLine(message);
    //        LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);
    //    }

    //    public void LogException(string _event, Exception exception, string _uniqueEventIdentifier = null, string _message = null, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
    //    {
    //        if (!CanProceed(LogLevel.Exception)) return;

    //        string message = $"{UniqueNodeId}: EXCEPTION {Now} {_event} {_uniqueEventIdentifier} {_message} {exception} {sourceFilePath} {memberName} {sourceLineNumber}";
    //        //Debug.WriteLine(message);
    //        LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);
    //    }

    //    public void LogInformation(string _event, string _uniqueEventIdentifier = null, string _message = null, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
    //    {
    //        if (!CanProceed(LogLevel.Information)) return;

    //        string message = $"{UniqueNodeId}: INFO {Now} {_event} {_uniqueEventIdentifier} {_message} {sourceFilePath} {memberName} {sourceLineNumber}";
    //        //Debug.WriteLine(message);
    //        LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);
    //    }

    //    public bool CanProceed(LogLevel level)
    //    {
    //        return (int) level >= (int) Level;
    //    }
    //}
}

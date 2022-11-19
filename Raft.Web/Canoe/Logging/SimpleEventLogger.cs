using Core.Raft.Canoe.Engine.Configuration;
using EventGuidance.Dependency;
using EventGuidance.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Raft.Web.Hubs;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Raft.Web.Canoe.Logging
{
    public class SimpleEventLogger : IEventLogger
    {
        Guid Guid { get; }

        public SimpleEventLogger()
        {
            Guid = Guid.NewGuid();
        }

        public IHubContext<LogHub> LogHubContext { get; set; }

        internal DateTimeOffset Now => DateTimeOffset.UtcNow;

        internal string UniqueNodeId => ComponentContainer.Instance.GetInstance<IClusterConfiguration>().ThisNode.UniqueNodeId;

        public void LogDebug(string _event, string _uniqueEventIdentifier = null, string _message = null, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string message = $"{UniqueNodeId}: DEBUG {Now} {_event} {_uniqueEventIdentifier} {_message} {sourceFilePath} {memberName} {sourceLineNumber}";
            Debug.WriteLine(message);
            LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message).RunSynchronously();
        }

        public void LogException(string _event, Exception exception, string _uniqueEventIdentifier = null, string _message = null, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string message = $"{UniqueNodeId}: EXCEPTION {Now} {_event} {_uniqueEventIdentifier} {_message} {exception} {sourceFilePath} {memberName} {sourceLineNumber}";
            Debug.WriteLine(message);
            LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message).RunSynchronously();
        }

        public void LogInformation(string _event, string _uniqueEventIdentifier = null, string _message = null, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string message = $"{UniqueNodeId}: INFO {Now} {_event} {_uniqueEventIdentifier} {_message} {sourceFilePath} {memberName} {sourceLineNumber}";
            Debug.WriteLine(message);
            LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message).RunSynchronously();
        }
    }
}

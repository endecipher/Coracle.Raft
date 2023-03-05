#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

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

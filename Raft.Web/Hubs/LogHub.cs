using Microsoft.AspNetCore.SignalR;

namespace Raft.Web.Hubs
{
    /// <summary>
    /// The Hub class manages connections, groups, and messaging.
    /// The SendLog method can be called by a connected client to send a log to all clients.
    /// JavaScript client code that calls the method is present in wwwroot.
    /// SignalR code is asynchronous to provide maximum scalability.
    /// </summary>
    public class LogHub : Hub
    {
        public const string ReceiveLog = nameof(ReceiveLog);

        public async Task SendLog(string log)
        {
            await Clients.All.SendAsync(ReceiveLog, log);
        }
    }
}

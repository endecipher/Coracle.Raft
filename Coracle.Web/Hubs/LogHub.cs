using Microsoft.AspNetCore.SignalR;

namespace Coracle.Web.Hubs
{
    public class LogHub : Hub
    {
        public const string ReceiveLog = nameof(ReceiveLog);

        public async Task SendLog(string log)
        {
            await Clients.All.SendAsync(ReceiveLog, log);
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
    }
}

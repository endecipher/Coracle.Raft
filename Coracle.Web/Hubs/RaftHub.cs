using Microsoft.AspNetCore.SignalR;

namespace Coracle.Web.Hubs
{
    public class RaftHub : Hub
    {
        public const string ReceiveEntries = nameof(ReceiveEntries);

        public async Task AppendEntries(string entry)
        {
            await Clients.All.SendAsync(ReceiveEntries, entry);
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
    }
}

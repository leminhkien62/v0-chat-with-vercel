using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace WmsSystem.Hubs
{
    [Authorize]
    public class WmsHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.Add(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.Remove(Context.ConnectionId, groupName);
        }

        public override Task OnConnected()
        {
            // Add user to appropriate groups based on their role/warehouse access
            var userName = Context.User.Identity.Name;
            Groups.Add(Context.ConnectionId, "AllUsers");
            
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            return base.OnDisconnected(stopCalled);
        }
    }
}

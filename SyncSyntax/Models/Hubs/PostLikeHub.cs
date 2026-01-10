using Microsoft.AspNetCore.SignalR;

namespace SyncSyntax.Models.Hubs
{
    public class PostLikeHub : Hub
    {
        public async Task BroadcastLike(int postId, int likesCount, bool userLiked)
        {
            await Clients.OthersInGroup(postId.ToString())
                .SendAsync("ReceiveLike", postId, likesCount);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var postId = httpContext.Request.Query["postId"];

            await Groups.AddToGroupAsync(Context.ConnectionId, postId);

            await base.OnConnectedAsync();

        }

        public async Task JoinGroup(string postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, postId);
        }

    }

}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ToDoAPI.Tool;

[Route("/ws/todoSync")]
public class TodoSyncHub : Hub
{
    public async Task JoinList(string listId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"list:{listId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task BroadcastDragMove(string listId, string cardId, double x, double y)
    {
        await Clients.OthersInGroup($"list:{listId}")
            .SendAsync("message", new
            {
                type = "DRAG_MOVE",
                payload = new { id = cardId, x, y }
            });
    }

    public async Task BroadcastDragStart(string listId, string cardId)
    {
        await Clients.OthersInGroup($"list:{listId}")
            .SendAsync("message", new { type = "DRAG_START", payload = new { id = cardId } });
    }

    public async Task BroadcastDragEnd(string listId, string cardId)
    {
        await Clients.OthersInGroup($"list:{listId}")
            .SendAsync("message", new { type = "DRAG_END", payload = new { id = cardId } });
    }
}
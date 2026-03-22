using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PokerProject.DTOs;

namespace PokerProject.Hubs;

[Authorize]
public class GameHub : Hub
{
    public async Task JoinGameGroup(int gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Game-{gameId}");
    }

    public async Task LeaveGameGroup(int gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Game-{gameId}");
    }

    public async Task BroadcastRoundStarted(int gameId, RoundDto round)
    {
        await Clients.Group($"Game-{gameId}").SendAsync("RoundStarted", round);
    }
   
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ADDRez.Api.Hubs;

[Authorize]
public class ReservationHub : Hub
{
    public async Task JoinOutlet(string outletId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"outlet-{outletId}");
    }

    public async Task LeaveOutlet(string outletId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"outlet-{outletId}");
    }
}

public static class ReservationHubExtensions
{
    public static async Task NotifyReservationCreated(this IHubContext<ReservationHub> hub, int outletId, object reservation)
    {
        await hub.Clients.Group($"outlet-{outletId}").SendAsync("ReservationCreated", reservation);
    }

    public static async Task NotifyReservationUpdated(this IHubContext<ReservationHub> hub, int outletId, object reservation)
    {
        await hub.Clients.Group($"outlet-{outletId}").SendAsync("ReservationUpdated", reservation);
    }

    public static async Task NotifyStatusChanged(this IHubContext<ReservationHub> hub, int outletId, int reservationId, string status)
    {
        await hub.Clients.Group($"outlet-{outletId}").SendAsync("StatusChanged", new { reservationId, status });
    }
}

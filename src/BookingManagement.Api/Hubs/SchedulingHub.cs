using Microsoft.AspNetCore.SignalR;

namespace BookingManagement.Api.Hubs;

/// <summary>
/// Real-time hub for the appointment calendar. Clients join a per-doctor group so
/// only viewers of that doctor's schedule receive slot-availability updates —
/// avoids broadcasting every booking to every connected client.
/// </summary>
public class SchedulingHub : Hub
{
    public static string DoctorGroupName(Guid doctorId) => $"doctor-{doctorId}";

    public Task JoinDoctorSchedule(Guid doctorId)
        => Groups.AddToGroupAsync(Context.ConnectionId, DoctorGroupName(doctorId));

    public Task LeaveDoctorSchedule(Guid doctorId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, DoctorGroupName(doctorId));
}

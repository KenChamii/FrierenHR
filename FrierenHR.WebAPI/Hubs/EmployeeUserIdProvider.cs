using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.SignalR;

namespace FrierenHR.WebAPI.Hubs;

/// <summary>
/// Tells SignalR that a connection belongs to an employee, so Clients.Users(...) can reach
/// somebody by employee id without the hub tracking connection ids itself.
///
/// The default provider happens to resolve the same claim, but only because inbound JWT
/// claims are remapped by default. Stating it here means turning that mapping off would not
/// silently stop every notification.
/// </summary>
public class EmployeeUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.GetEmployeeId()?.ToString();
}

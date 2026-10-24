using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.SignalR;

namespace FrierenHR.WebAPI.Hubs;

/// <summary>Tells SignalR that a connection belongs to an employee.</summary>
public class EmployeeUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.GetEmployeeId()?.ToString();
}

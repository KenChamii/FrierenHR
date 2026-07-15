using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace FrierenHR.WebAPI.Hubs;

public class ChatHub : Hub
{
    private readonly IMessagingService _messagingService;
    public ChatHub(IMessagingService messagingService) => _messagingService = messagingService;

    public async Task JoinConversation(string conversationId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

    public async Task LeaveConversation(string conversationId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);

    public async Task SendMessage(string conversationId, string senderEmployeeId, string body)
    {
        var dto = new SendMessageDto(Guid.Parse(conversationId), Guid.Parse(senderEmployeeId), body);
        var saved = await _messagingService.SendMessageAsync(dto); // persist FIRST
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", saved); // then broadcast the saved DTO (has real Id/SentAt)
    }
}
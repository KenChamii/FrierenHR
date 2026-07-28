using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Messaging;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FrierenHR.WebAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessagingService _messagingService;
    public ChatHub(IMessagingService messagingService) => _messagingService = messagingService;

    public async Task JoinConversation(string conversationId)
    {
        var callerId = Context.User?.GetEmployeeId();
        if (callerId is null) return;

        // Same membership check as MessagingController.GetHistory: don't let a connected
        // client join (and start receiving live messages from) a conversation they aren't
        // actually part of just by knowing/guessing its id.
        var myConversations = await _messagingService.GetConversationsAsync(callerId.Value);
        if (!Guid.TryParse(conversationId, out var parsedId) || !myConversations.Any(c => c.Id == parsedId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public async Task LeaveConversation(string conversationId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);

    public async Task SendMessage(string conversationId, string senderEmployeeId, string body,
        string? attachmentUrl = null, string? attachmentFileName = null, string? attachmentContentType = null, long? attachmentSizeBytes = null)
    {
        var callerId = Context.User?.GetEmployeeId();
        // You can only send as yourself — the client still passes senderEmployeeId explicitly
        // (kept for backwards compatibility with the DTO shape), but the hub now enforces it
        // matches who's actually authenticated on this connection, instead of trusting it blindly.
        if (callerId is null || !Guid.TryParse(senderEmployeeId, out var claimedId) || claimedId != callerId.Value) return;

        // A message needs text or an attachment (or both) — reject the fully-empty case rather
        // than persisting a blank bubble.
        if (string.IsNullOrWhiteSpace(body) && string.IsNullOrWhiteSpace(attachmentUrl)) return;

        var dto = new SendMessageDto(Guid.Parse(conversationId), callerId.Value, body, attachmentUrl, attachmentFileName, attachmentContentType, attachmentSizeBytes);
        var saved = await _messagingService.SendMessageAsync(dto); // persist FIRST
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", saved); // then broadcast the saved DTO (has real Id/SentAt)
    }
}

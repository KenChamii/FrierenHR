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
        var callerId = RequireCaller();

        if (!Guid.TryParse(conversationId, out var parsedId))
            throw new HubException("That conversation id is not valid.");

        // Same membership check as MessagingController.GetHistory.
        var myConversations = await _messagingService.GetConversationsAsync(callerId);
        if (!myConversations.Any(c => c.Id == parsedId))
            throw new HubException("You are not part of that conversation.");

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

        // Opening a conversation is reading it.
        await _messagingService.MarkReadAsync(parsedId, callerId);
    }

    public Task LeaveConversation(string conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);

    public async Task SendMessage(
        string conversationId, string senderEmployeeId, string body,
        string? attachmentUrl = null, string? attachmentFileName = null,
        string? attachmentContentType = null, long? attachmentSizeBytes = null)
    {
        var callerId = RequireCaller();

        // You can only send as yourself.
        if (!Guid.TryParse(senderEmployeeId, out var claimedId) || claimedId != callerId)
            throw new HubException("You can only send messages as yourself.");

        if (!Guid.TryParse(conversationId, out var parsedId))
            throw new HubException("That conversation id is not valid.");

        if (string.IsNullOrWhiteSpace(body) && string.IsNullOrWhiteSpace(attachmentUrl))
            throw new HubException("A message needs text or an attachment.");

        var participants = await _messagingService.GetParticipantIdsAsync(parsedId);
        if (!participants.Contains(callerId))
            throw new HubException("You are not part of that conversation.");

        var dto = new SendMessageDto(
            parsedId, callerId, body,
            attachmentUrl, attachmentFileName, attachmentContentType, attachmentSizeBytes);

        var saved = await _messagingService.SendMessageAsync(dto);

        // Anyone with this conversation open, including the sender.
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", saved);

        // Everyone else in the conversation is signed in somewhere but in no group for it.
        var recipients = participants.Where(id => id != callerId).Select(id => id.ToString()).ToList();
        if (recipients.Count > 0)
            await Clients.Users(recipients).SendAsync("ConversationUpdated", saved);
    }

    /// <summary>Marks a conversation read without opening it, for the unread badge.</summary>
    public async Task MarkRead(string conversationId)
    {
        var callerId = RequireCaller();
        if (!Guid.TryParse(conversationId, out var parsedId)) return;
        await _messagingService.MarkReadAsync(parsedId, callerId);
    }

    /// <summary>The employee id from the connection's own claims.</summary>
    private Guid RequireCaller() =>
        Context.User?.GetEmployeeId()
        ?? throw new HubException("Your session is not valid. Please sign in again.");
}

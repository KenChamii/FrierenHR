using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/messaging")]
public class MessagingController : ControllerBase
{
    private readonly IMessagingService _messagingService;
    public MessagingController(IMessagingService messagingService) => _messagingService = messagingService;

    [HttpGet("conversations/{employeeId:guid}")]
    public async Task<ActionResult<List<ConversationDto>>> GetConversations(Guid employeeId, CancellationToken ct) =>
        Ok(await _messagingService.GetConversationsAsync(employeeId, ct));

    [HttpGet("conversations/{conversationId:guid}/history")]
    public async Task<ActionResult<List<MessageDto>>> GetHistory(Guid conversationId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await _messagingService.GetHistoryAsync(conversationId, skip, take, ct));

    [HttpPost("conversations/direct")]
    public async Task<ActionResult<ConversationDto>> GetOrCreateDirect(CreateDirectConversationDto dto, CancellationToken ct) =>
        Ok(await _messagingService.GetOrCreateDirectAsync(dto, ct));

    [HttpPost("conversations/group")]
    public async Task<ActionResult<ConversationDto>> CreateGroup(CreateGroupConversationDto dto, CancellationToken ct) =>
        Ok(await _messagingService.CreateGroupAsync(dto, ct));
}
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Messaging;
using FrierenHR.WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrierenHR.WebAPI.Controllers;

[ApiController]
[Route("api/messaging")]
[Authorize]
public class MessagingController : ControllerBase
{
    private readonly IMessagingService _messagingService;
    public MessagingController(IMessagingService messagingService) => _messagingService = messagingService;

    [HttpGet("conversations/{employeeId:guid}")]
    public async Task<ActionResult<List<ConversationDto>>> GetConversations(Guid employeeId, CancellationToken ct)
    {
        // Your own inbox only — there's no legitimate reason to browse someone else's.
        if (!User.IsSelfOrRole(employeeId, "HRAdmin")) return Forbid();
        return Ok(await _messagingService.GetConversationsAsync(employeeId, ct));
    }

    [HttpGet("conversations/{conversationId:guid}/history")]
    public async Task<ActionResult<List<MessageDto>>> GetHistory(Guid conversationId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var callerId = User.GetEmployeeId();
        if (callerId is null) return Forbid();

        // No dedicated "is this employee a participant" repo method yet, so this reuses the
        // caller's own conversation list as a membership check. A bit more work per request
        // than a direct lookup would be, but avoids trusting conversationId blindly and
        // avoids a repository/schema change to get there.
        var myConversations = await _messagingService.GetConversationsAsync(callerId.Value, ct);
        if (!myConversations.Any(c => c.Id == conversationId)) return Forbid();

        return Ok(await _messagingService.GetHistoryAsync(conversationId, skip, take, ct));
    }

    [HttpPost("conversations/direct")]
    public async Task<ActionResult<ConversationDto>> GetOrCreateDirect(CreateDirectConversationDto dto, CancellationToken ct)
    {
        // One side of the direct conversation must be you — otherwise anyone could wire up
        // (and silently read) a DM thread between two other employees.
        var callerId = User.GetEmployeeId();
        if (callerId != dto.EmployeeAId && callerId != dto.EmployeeBId && User.GetRole() != "HRAdmin") return Forbid();
        return Ok(await _messagingService.GetOrCreateDirectAsync(dto, ct));
    }

    [HttpPost("conversations/group")]
    public async Task<ActionResult<ConversationDto>> CreateGroup(CreateGroupConversationDto dto, CancellationToken ct)
    {
        // You have to be a member of the group you're creating.
        var callerId = User.GetEmployeeId();
        if (callerId is null || (!dto.MemberEmployeeIds.Contains(callerId.Value) && User.GetRole() != "HRAdmin")) return Forbid();
        return Ok(await _messagingService.CreateGroupAsync(dto, ct));
    }
}

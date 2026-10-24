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
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    public MessagingController(IMessagingService messagingService, IWebHostEnvironment env, IConfiguration configuration)
    {
        _messagingService = messagingService;
        _env = env;
        _configuration = configuration;
    }

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

        // No dedicated "is this employee a participant" repo method yet.
        var myConversations = await _messagingService.GetConversationsAsync(callerId.Value, ct);
        if (!myConversations.Any(c => c.Id == conversationId)) return Forbid();

        return Ok(await _messagingService.GetHistoryAsync(conversationId, skip, take, ct));
    }

    [HttpPost("conversations/direct")]
    public async Task<ActionResult<ConversationDto>> GetOrCreateDirect(CreateDirectConversationDto dto, CancellationToken ct)
    {
        // One side of the direct conversation must be you.
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

    // 10MB default via config (Messaging:MaxAttachmentSizeBytes).
    [HttpPost("attachments")]
    [RequestSizeLimit(25_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AttachmentUploadResultDto>> UploadAttachment(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file was uploaded.");

        var maxBytes = _configuration.GetValue<long?>("Messaging:MaxAttachmentSizeBytes") ?? 10_485_760;
        if (file.Length > maxBytes)
            return BadRequest($"File is too large. Maximum size is {maxBytes / 1024 / 1024}MB.");

        var allowedTypes = _configuration.GetSection("Messaging:AllowedAttachmentContentTypes").Get<string[]>() ?? Array.Empty<string>();
        if (allowedTypes.Length > 0 && !allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest($"File type '{file.ContentType}' isn't allowed.");

        // Random filename on disk (never the original name) so a malicious filename can't do anything odd with the filesystem.
        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "messaging");
        Directory.CreateDirectory(uploadsRoot);

        var fullPath = Path.Combine(uploadsRoot, storedName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        // Served back as a plain static file under /uploads — see app.UseStaticFiles() in Program.cs.
        var url = $"/uploads/messaging/{storedName}";
        return Ok(new AttachmentUploadResultDto(url, file.FileName, file.ContentType, file.Length));
    }
}

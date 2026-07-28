using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

public record ConversationDto(Guid Id, ConversationType Type, string? Name, string? LastMessagePreview, DateTime? LastMessageAt, int UnreadCount);
public record MessageDto(
    Guid Id, Guid ConversationId, Guid SenderEmployeeId, string SenderName, string Body, DateTime SentAt,
    string? AttachmentUrl = null, string? AttachmentFileName = null, string? AttachmentContentType = null, long? AttachmentSizeBytes = null);
public record CreateDirectConversationDto(Guid EmployeeAId, Guid EmployeeBId);
public record CreateGroupConversationDto(Guid CompanyId, string Name, List<Guid> MemberEmployeeIds);
public record SendMessageDto(
    Guid ConversationId, Guid SenderEmployeeId, string Body,
    string? AttachmentUrl = null, string? AttachmentFileName = null, string? AttachmentContentType = null, long? AttachmentSizeBytes = null);

// Returned by the attachment upload endpoint so the client has everything it needs to
// immediately follow up with SendMessage (over the hub) without a second round trip.
public record AttachmentUploadResultDto(string Url, string FileName, string ContentType, long SizeBytes);

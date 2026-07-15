using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Common.DTOs;

public record ConversationDto(Guid Id, ConversationType Type, string? Name, string? LastMessagePreview, DateTime? LastMessageAt, int UnreadCount);
public record MessageDto(Guid Id, Guid ConversationId, Guid SenderEmployeeId, string SenderName, string Body, DateTime SentAt);
public record CreateDirectConversationDto(Guid EmployeeAId, Guid EmployeeBId);
public record CreateGroupConversationDto(Guid CompanyId, string Name, List<Guid> MemberEmployeeIds);
public record SendMessageDto(Guid ConversationId, Guid SenderEmployeeId, string Body);
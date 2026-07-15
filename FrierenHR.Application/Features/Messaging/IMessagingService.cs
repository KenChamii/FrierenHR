using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Common.Interfaces;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

namespace FrierenHR.Application.Features.Messaging;

public interface IMessagingService
{
    Task<ConversationDto> GetOrCreateDirectAsync(CreateDirectConversationDto dto, CancellationToken ct = default);
    Task<ConversationDto> CreateGroupAsync(CreateGroupConversationDto dto, CancellationToken ct = default);
    Task<ConversationDto> GetOrCreateBroadcastAsync(Guid companyId, CancellationToken ct = default);
    Task<List<ConversationDto>> GetConversationsAsync(Guid employeeId, CancellationToken ct = default);
    Task<MessageDto> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default);
    Task<List<MessageDto>> GetHistoryAsync(Guid conversationId, int skip, int take, CancellationToken ct = default);
}
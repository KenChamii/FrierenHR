using FrierenHR.Application.Common.DTOs;
using FrierenHR.Application.Features.Messaging;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;

public class MessagingService : IMessagingService
{
    private readonly IMessagingRepository _messagingRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public MessagingService(IMessagingRepository messagingRepository, IEmployeeRepository employeeRepository)
    {
        _messagingRepository = messagingRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<ConversationDto> GetOrCreateDirectAsync(CreateDirectConversationDto dto, CancellationToken ct = default)
    {
        var existing = await _messagingRepository.GetDirectConversationAsync(dto.EmployeeAId, dto.EmployeeBId, ct);
        if (existing is not null) return new ConversationDto(existing.Id, existing.Type, existing.Name, null, null, 0);

        var employeeA = await _employeeRepository.GetByIdAsync(dto.EmployeeAId, ct)
            ?? throw new InvalidOperationException("Employee A not found.");

        var conversation = new Conversation
        {
            CompanyId = employeeA.CompanyId,
            Type = ConversationType.Direct,
            Participants = new List<ConversationParticipant>
            {
                new() { EmployeeId = dto.EmployeeAId },
                new() { EmployeeId = dto.EmployeeBId }
            }
        };
        var created = await _messagingRepository.CreateConversationAsync(conversation, ct);
        return new ConversationDto(created.Id, created.Type, created.Name, null, null, 0);
    }

    public async Task<ConversationDto> CreateGroupAsync(CreateGroupConversationDto dto, CancellationToken ct = default)
    {
        var conversation = new Conversation
        {
            CompanyId = dto.CompanyId,
            Type = ConversationType.Group,
            Name = dto.Name,
            Participants = dto.MemberEmployeeIds.Select(id => new ConversationParticipant { EmployeeId = id }).ToList()
        };
        var created = await _messagingRepository.CreateConversationAsync(conversation, ct);
        return new ConversationDto(created.Id, created.Type, created.Name, null, null, 0);
    }

    public async Task<ConversationDto> GetOrCreateBroadcastAsync(Guid companyId, CancellationToken ct = default)
    {
        // Auto-subscribe-all: every active employee in the company becomes a participant.
        var employees = await _employeeRepository.GetByCompanyAsync(companyId, ct);
        var conversation = new Conversation
        {
            CompanyId = companyId,
            Type = ConversationType.Broadcast,
            Name = "Company Announcements",
            Participants = employees.Select(e => new ConversationParticipant { EmployeeId = e.Id }).ToList()
        };
        var created = await _messagingRepository.CreateConversationAsync(conversation, ct);
        return new ConversationDto(created.Id, created.Type, created.Name, null, null, 0);
    }

    public Task<List<ConversationDto>> GetConversationsAsync(Guid employeeId, CancellationToken ct = default) =>
        _messagingRepository.GetConversationsForEmployeeAsync(employeeId, ct);

    public async Task<MessageDto> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default)
    {
        // Broadcast role-gating: check sender is Manager/HRAdmin before allowing a send here
        // if conversation.Type == Broadcast (fetch conversation + sender role first).
        var employee = await _employeeRepository.GetByIdAsync(dto.SenderEmployeeId, ct)
            ?? throw new InvalidOperationException("Sender not found.");

        var message = new Message { ConversationId = dto.ConversationId, SenderEmployeeId = dto.SenderEmployeeId, Body = dto.Body };
        var saved = await _messagingRepository.AddMessageAsync(message, ct);
        return new MessageDto(saved.Id, saved.ConversationId, saved.SenderEmployeeId, $"{employee.FirstName} {employee.LastName}", saved.Body, saved.SentAt);
    }

    public Task<List<MessageDto>> GetHistoryAsync(Guid conversationId, int skip, int take, CancellationToken ct = default) =>
        _messagingRepository.GetHistoryAsync(conversationId, skip, take, ct);
}
using FrierenHR.Application.Common.DTOs;
using FrierenHR.Core.Entities;
using FrierenHR.Core.Enums;
using FrierenHR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FrierenHR.Infrastructure.Repositories;

public class MessagingRepository : IMessagingRepository
{
    private readonly FrierenHRDbContext _context;
    public MessagingRepository(FrierenHRDbContext context) => _context = context;

    public async Task<Conversation?> GetDirectConversationAsync(Guid a, Guid b, CancellationToken ct = default) =>
        await _context.Conversations
            .Where(c => c.Type == ConversationType.Direct)
            .Where(c => c.Participants.Any(p => p.EmployeeId == a) && c.Participants.Any(p => p.EmployeeId == b))
            .FirstOrDefaultAsync(ct);

    public async Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken ct = default)
    {
        await _context.Conversations.AddAsync(conversation, ct);
        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    // Single grouped query for "conversation list + last message + unread count" —
    // NOT a per-conversation query loop. This is the line that matters most in this file.
    public async Task<List<ConversationDto>> GetConversationsForEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        var conversationIds = await _context.ConversationParticipants.AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .Select(p => new { p.ConversationId, p.LastReadAt })
            .ToListAsync(ct);

        var lastMessages = await _context.Messages.AsNoTracking()
            .Where(m => conversationIds.Select(c => c.ConversationId).Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Last = g.OrderByDescending(m => m.SentAt).First() })
            .ToListAsync(ct);

        var conversations = await _context.Conversations.AsNoTracking()
            .Where(c => conversationIds.Select(x => x.ConversationId).Contains(c.Id))
            .ToListAsync(ct);

        return conversations.Select(c =>
        {
            var last = lastMessages.FirstOrDefault(m => m.ConversationId == c.Id);
            var participant = conversationIds.First(p => p.ConversationId == c.Id);
            var unread = last is not null && (participant.LastReadAt is null || last.Last.SentAt > participant.LastReadAt)
                ? 1 : 0; // simplified to a boolean-ish flag; swap for a real COUNT query if you need exact numbers
            return new ConversationDto(c.Id, c.Type, c.Name, last?.Last.Body, last?.Last.SentAt, unread);
        }).OrderByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task<Message> AddMessageAsync(Message message, CancellationToken ct = default)
    {
        await _context.Messages.AddAsync(message, ct);
        await _context.SaveChangesAsync(ct);
        return message;
    }

    public async Task<List<MessageDto>> GetHistoryAsync(Guid conversationId, int skip, int take, CancellationToken ct = default) =>
        await _context.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.SenderEmployee)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip).Take(take)
            .Select(m => new MessageDto(m.Id, m.ConversationId, m.SenderEmployeeId,
                m.SenderEmployee == null ? "" : $"{m.SenderEmployee.FirstName} {m.SenderEmployee.LastName}", m.Body, m.SentAt))
            .ToListAsync(ct);
}
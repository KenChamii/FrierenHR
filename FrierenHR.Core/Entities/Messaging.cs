using FrierenHR.Core.Common;
using FrierenHR.Core.Enums;

namespace FrierenHR.Core.Entities;

public class Conversation : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public ConversationType Type { get; set; }
    public string? Name { get; set; } // used for Group/Broadcast
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
}

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public Guid SenderEmployeeId { get; set; }
    public Employee? SenderEmployee { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
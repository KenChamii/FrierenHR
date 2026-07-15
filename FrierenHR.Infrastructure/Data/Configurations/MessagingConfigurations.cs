using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200);

        builder.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ConversationId, x.EmployeeId }).IsUnique();

        builder.HasOne(x => x.Conversation).WithMany(c => c.Participants)
            .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Employee).WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).IsRequired();
        builder.HasIndex(x => new { x.ConversationId, x.SentAt });

        builder.HasOne(x => x.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SenderEmployee).WithMany()
            .HasForeignKey(x => x.SenderEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
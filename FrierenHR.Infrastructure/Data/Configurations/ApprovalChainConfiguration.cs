using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class ApprovalChainConfiguration : IEntityTypeConfiguration<ApprovalChain>
{
    public void Configure(EntityTypeBuilder<ApprovalChain> builder)
    {
        builder.ToTable("ApprovalChains");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);

        builder.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Steps).WithOne(s => s.ApprovalChain)
            .HasForeignKey(s => s.ApprovalChainId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApprovalChainStepConfiguration : IEntityTypeConfiguration<ApprovalChainStep>
{
    public void Configure(EntityTypeBuilder<ApprovalChainStep> builder)
    {
        builder.ToTable("ApprovalChainSteps");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ApprovalChainId, x.StepOrder }).IsUnique();
    }
}

public class ApprovalInstanceConfiguration : IEntityTypeConfiguration<ApprovalInstance>
{
    public void Configure(EntityTypeBuilder<ApprovalInstance> builder)
    {
        builder.ToTable("ApprovalInstances");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ApprovalChain).WithMany()
            .HasForeignKey(x => x.ApprovalChainId).OnDelete(DeleteBehavior.Restrict);
        // Restrict, not Cascade — LeaveRequestId isn't a nav-mapped FK here (no LeaveRequest?
        // property), so there's no relationship for EF to cascade through on that side.
    }
}
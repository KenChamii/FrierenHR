using FrierenHR.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrierenHR.Infrastructure.Data.Configurations;

public class CompanyRuleConfigConfiguration : IEntityTypeConfiguration<CompanyRuleConfig>
{
    public void Configure(EntityTypeBuilder<CompanyRuleConfig> builder)
    {
        builder.ToTable("CompanyRuleConfigs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RuleJson).IsRequired();
        builder.Property(x => x.RuleType).IsRequired();

        builder.HasOne(x => x.Company)
            .WithMany(c => c.RuleConfigs)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CompanyId, x.RuleType, x.IsActive });
    }
}
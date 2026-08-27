using CateringSaaS.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Ordering.Data;

public sealed class EmployeeMealRequestConfiguration : IEntityTypeConfiguration<EmployeeMealRequest>
{
    public void Configure(EntityTypeBuilder<EmployeeMealRequest> builder)
    {
        builder.ToTable("employee_meal_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.WorkspaceId).IsRequired();
        builder.Property(r => r.ClientCompanyId).IsRequired();
        builder.Property(r => r.EmployeeId).IsRequired();
        builder.Property(r => r.TargetDate).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(r => r.WorkspaceId);
        builder.HasIndex(r => new { r.WorkspaceId, r.ClientCompanyId, r.TargetDate, r.Status });
        builder.HasIndex(r => new { r.WorkspaceId, r.EmployeeId, r.TargetDate });
        builder.HasIndex(r => r.EmployeeId);

        // Composition: cascade items with request. No FKs to User/Menu (Guid refs only) —
        // so historical rows are not wiped by external entity deletes.
        builder.HasMany(r => r.Items)
            .WithOne(i => i.Request)
            .HasForeignKey(i => i.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

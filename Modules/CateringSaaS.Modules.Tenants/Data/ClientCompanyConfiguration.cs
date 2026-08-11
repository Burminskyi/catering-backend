using CateringSaaS.Modules.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CateringSaaS.Modules.Tenants.Data;

public sealed class ClientCompanyConfiguration : IEntityTypeConfiguration<ClientCompany>
{
    public void Configure(EntityTypeBuilder<ClientCompany> builder)
    {
        builder.ToTable("client_companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.WorkspaceId)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => new { c.WorkspaceId, c.Name });

        builder.HasOne(c => c.Workspace)
            .WithMany()
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

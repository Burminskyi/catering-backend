using System.Linq.Expressions;
using System.Reflection;
using CateringSaaS.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Shared.Data;

/// <summary>
/// Global EF Core context for the modular monolith.
/// Module entities are accessed via <c>Set&lt;T&gt;()</c> (no DbSet properties here)
/// so Shared does not take a dependency on module assemblies.
/// Inventory examples: <c>db.Set&lt;Ingredient&gt;()</c>, <c>db.Set&lt;StockBatch&gt;()</c>, <c>db.Set&lt;Inventory&gt;()</c>.
/// </summary>
public class AppDbContext : DbContext
{
    private static readonly MethodInfo SetWorkspaceFilterMethod =
        typeof(AppDbContext).GetMethod(
            nameof(SetWorkspaceFilter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var assembly in ModuleConfigurationRegistry.GetAssemblies())
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        ApplyWorkspaceQueryFilters(modelBuilder);
    }

    private void ApplyWorkspaceQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var workspaceIdProperty = clrType.GetProperty(
                "WorkspaceId",
                BindingFlags.Public | BindingFlags.Instance);

            // Only non-nullable Guid — nullable WorkspaceId (e.g. User) is intentionally excluded
            // so SuperAdmin / cross-tenant auth lookups are not filtered out.
            if (workspaceIdProperty is null || workspaceIdProperty.PropertyType != typeof(Guid))
            {
                continue;
            }

            SetWorkspaceFilterMethod
                .MakeGenericMethod(clrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void SetWorkspaceFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        Expression<Func<TEntity, bool>> filter =
            entity => EF.Property<Guid>(entity, "WorkspaceId") == _tenantContext.WorkspaceId;

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}

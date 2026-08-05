using System.Reflection;

namespace CateringSaaS.Shared.Data;

/// <summary>
/// Allows modules to register assemblies containing <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/>
/// that <see cref="AppDbContext"/> applies during model creation.
/// </summary>
public static class ModuleConfigurationRegistry
{
    private static readonly List<Assembly> Assemblies = [];
    private static readonly object Sync = new();

    public static void Register(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        lock (Sync)
        {
            if (!Assemblies.Contains(assembly))
            {
                Assemblies.Add(assembly);
            }
        }
    }

    internal static IReadOnlyList<Assembly> GetAssemblies()
    {
        lock (Sync)
        {
            return Assemblies.ToArray();
        }
    }
}

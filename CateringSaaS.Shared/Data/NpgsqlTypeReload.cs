using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CateringSaaS.Shared.Data;

public static class NpgsqlTypeReload
{
    /// <summary>
    /// After <c>DROP SCHEMA public CASCADE</c>, pgvector may remain registered while the
    /// <c>vector</c> type is gone. Recreate the extension when the type is missing, then
    /// flush Npgsql's OID cache.
    /// </summary>
    public static async Task EnsurePgVectorAsync(
        this AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DO $vector$
            BEGIN
                IF to_regtype('vector') IS NULL THEN
                    DROP EXTENSION IF EXISTS vector CASCADE;
                    CREATE EXTENSION vector;
                END IF;
            END
            $vector$;
            """,
            cancellationToken);

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            if (dbContext.Database.GetDbConnection() is NpgsqlConnection npgsql)
            {
                await npgsql.ReloadTypesAsync();
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}

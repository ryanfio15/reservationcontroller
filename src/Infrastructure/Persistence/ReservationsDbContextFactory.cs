using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the model without starting the web host.
/// </summary>
/// <remarks>
/// <para>
/// Adding a migration needs the model, not a database, so the connection string here is never
/// connected to. It therefore carries no credential — if someone does point it at a real server,
/// it will fail to authenticate rather than quietly succeed (A3: never commit a connection
/// string with credentials).
/// </para>
/// <para>
/// Set <c>GATE_SQL_CONNECTION</c> to use a real server for commands that do need one, such as
/// <c>dotnet ef database update</c> against the local container.
/// </para>
/// </remarks>
public sealed class ReservationsDbContextFactory : IDesignTimeDbContextFactory<ReservationsDbContext>
{
    private const string NoCredentialPlaceholder =
        "Server=localhost,1433;Database=Reservations;Integrated Security=True;"
        + "Encrypt=True;TrustServerCertificate=True";

    public ReservationsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("GATE_SQL_CONNECTION") ?? NoCredentialPlaceholder;

        var options = new DbContextOptionsBuilder<ReservationsDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(
                typeof(ReservationsDbContextFactory).Assembly.FullName))
            .Options;

        // Design time is never inside a request, so there is no tenant to scope to.
        return new ReservationsDbContext(options, new UnscopedTenantScope());
    }
}

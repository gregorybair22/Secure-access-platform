using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecureAccess.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so EF Core tooling (migrations) can construct the context
/// without booting the web host. The runtime connection string comes from app
/// configuration; this value is only used by the CLI.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = SqlConnectionStringHelper.Resolve(
            Environment.GetEnvironmentVariable("SECUREACCESS_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=SecureAccess;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(conn, sql => sql.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}

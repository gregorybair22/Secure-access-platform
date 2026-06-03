using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SecureAccess.Infrastructure.Persistence;

/// <summary>
/// Normalizes SQL Server connection strings for local development (LocalDB).
/// </summary>
public static class SqlConnectionStringHelper
{
    private static readonly Regex LocalDbServerRegex = new(
        @"\(localdb\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Applies LocalDB-safe settings. When combined with
    /// <see cref="SqlClientWindowsBootstrap"/> (managed networking on Windows),
    /// the default <c>(localdb)\MSSQLLocalDB</c> connection string works without
    /// rewriting to a named pipe.
    /// </summary>
    public static string Resolve(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString ?? string.Empty;

        if (!LocalDbServerRegex.IsMatch(connectionString))
            return connectionString;

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            Encrypt = SqlConnectionEncryptOption.Optional,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }
}

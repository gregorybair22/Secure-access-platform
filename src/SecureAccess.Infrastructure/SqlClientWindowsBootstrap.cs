using System.Runtime.CompilerServices;

namespace SecureAccess.Infrastructure;

/// <summary>
/// Must run before any <see cref="Microsoft.Data.SqlClient"/> connection is opened.
/// On some Windows hosts the native SNI layer throws SEHException when connecting to
/// LocalDB; managed networking avoids that native code path.
/// </summary>
public static class SqlClientWindowsBootstrap
{
    private static int _initialized;

#pragma warning disable CA2255 // ModuleInitializer is required so the switch is set when this assembly loads
    [ModuleInitializer]
    internal static void ModuleInit() => Initialize();
#pragma warning restore CA2255

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
            return;

        if (!OperatingSystem.IsWindows())
            return;

        AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);
    }
}

using System.Net;

namespace SecureAccess.Api.Middleware;

/// <summary>
/// Optional defense-in-depth restriction: rejects requests whose remote IP is not
/// within the configured allow-list (Security:AllowedIpRanges as CIDR or exact IPs).
/// Documented as one of the access-restriction options in the spec.
/// </summary>
public class IpAllowListMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpAllowListMiddleware> _logger;
    private readonly List<(IPAddress Network, int Prefix)> _ranges = new();

    public IpAllowListMiddleware(RequestDelegate next, IConfiguration config, ILogger<IpAllowListMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        foreach (var entry in config.GetSection("Security:AllowedIpRanges").Get<string[]>() ?? Array.Empty<string>())
        {
            var parts = entry.Split('/');
            if (IPAddress.TryParse(parts[0], out var ip))
            {
                var prefix = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : ip.GetAddressBytes().Length * 8;
                _ranges.Add((ip, prefix));
            }
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Always allow health checks.
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var remote = context.Connection.RemoteIpAddress;
        if (remote is not null && (_ranges.Count == 0 || _ranges.Any(r => InRange(remote, r.Network, r.Prefix))))
        {
            await _next(context);
            return;
        }

        _logger.LogWarning("Blocked request from disallowed IP {Ip}", remote);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Access from this network is not permitted.");
    }

    private static bool InRange(IPAddress address, IPAddress network, int prefixLength)
    {
        if (address.AddressFamily != network.AddressFamily) return false;
        var addrBytes = address.GetAddressBytes();
        var netBytes = network.GetAddressBytes();

        var fullBytes = prefixLength / 8;
        for (var i = 0; i < fullBytes; i++)
            if (addrBytes[i] != netBytes[i]) return false;

        var remainingBits = prefixLength % 8;
        if (remainingBits == 0) return true;
        var mask = (byte)(0xFF << (8 - remainingBits));
        return (addrBytes[fullBytes] & mask) == (netBytes[fullBytes] & mask);
    }
}

using Microsoft.AspNetCore.Authorization;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.ImportExport;

namespace SecureAccess.Web.Auth;

/// <summary>Authenticated file-download endpoints for exports.</summary>
public static class DataEndpoints
{
    public static void MapDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/download/clients", [Authorize(Policy = "perm:data.export")]
            async (IExportService export, ExportFormat format) =>
        {
            var file = await export.ExportClientsAsync(format);
            return Results.File(file.Content, file.ContentType, file.FileName);
        });

        app.MapGet("/download/machines", [Authorize(Policy = "perm:data.export")]
            async (IExportService export, ExportFormat format, int? clientId) =>
        {
            var file = await export.ExportMachinesAsync(format, clientId);
            return Results.File(file.Content, file.ContentType, file.FileName);
        });
    }
}

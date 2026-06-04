using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.ImportExport;
using SecureAccess.Web.Services;

namespace SecureAccess.Web.Components.Pages;

public partial class ImportExport
{
    [Inject] private IImportService Import { get; set; } = default!;
    [Inject] private ICurrentUserService Current { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private string _entity = "Clients";
    private string? _selectedFileName;
    private bool _importing;

    private bool CanImport => Current.HasPermission(Permissions.ImportData);

    private MarkupString EntityIcon => _entity == "Machines"
        ? new("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"2\" y=\"3\" width=\"20\" height=\"14\" rx=\"2\"/><line x1=\"8\" y1=\"21\" x2=\"16\" y2=\"21\"/><line x1=\"12\" y1=\"17\" x2=\"12\" y2=\"21\"/></svg>")
        : new("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/></svg>");

    private async Task OnFile(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null)
            return;

        _selectedFileName = file.Name;
        _importing = true;

        try
        {
            var entity = Enum.Parse<ImportEntity>(_entity);
            await using var ms = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
            ms.Position = 0;
            var result = await Import.ImportAsync(entity, ms, file.Name);
            if (!result.Succeeded)
            {
                await Toast.ErrorAsync(result.Error ?? "Import failed.");
                return;
            }

            await Toast.SuccessAsync($"Import complete: {result.Value!.Created} created, {result.Value.Updated} updated, {result.Value.Skipped} skipped.");
        }
        catch (Exception ex)
        {
            await Toast.ErrorAsync($"Import failed: {ex.Message}");
        }
        finally
        {
            _importing = false;
        }
    }
}

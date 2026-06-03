using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using SecureAccess.Application.Features.ImportExport;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _db;

    public ExportService(AppDbContext db) => _db = db;

    public async Task<ExportFile> ExportClientsAsync(ExportFormat format, CancellationToken ct = default)
    {
        var rows = await _db.Clients.AsNoTracking().OrderBy(c => c.Name).Select(c => new[]
        {
            c.CustomerCode, c.Name, c.ContactPerson ?? "", c.PhoneNumber ?? "",
            c.Email ?? "", c.Address ?? "", c.Status.ToString()
        }).ToListAsync(ct);

        var headers = new[] { "Customer Code", "Name", "Contact", "Phone", "Email", "Address", "Status" };
        return ExportTable("Clients", headers, rows.Select(r => (IReadOnlyList<string?>)r).ToList(), format);
    }

    public async Task<ExportFile> ExportMachinesAsync(ExportFormat format, int? clientId, CancellationToken ct = default)
    {
        var q = _db.Machines.AsNoTracking().Include(m => m.Client).AsQueryable();
        if (clientId.HasValue) q = q.Where(m => m.ClientId == clientId.Value);

        var rows = await q.OrderBy(m => m.Client.Name).ThenBy(m => m.Name).Select(m => new[]
        {
            m.Client.Name, m.Name, m.SerialNumber ?? "", m.Model ?? "",
            m.Location ?? "", m.Status.ToString(),
            m.InstallationDate.HasValue ? m.InstallationDate.Value.ToString("yyyy-MM-dd") : ""
        }).ToListAsync(ct);

        var headers = new[] { "Client", "Machine", "Serial Number", "Model", "Location", "Status", "Installation Date" };
        return ExportTable("Machines", headers, rows.Select(r => (IReadOnlyList<string?>)r).ToList(), format);
    }

    public ExportFile ExportTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows, ExportFormat format)
        => format switch
        {
            ExportFormat.Csv => BuildCsv(title, headers, rows),
            ExportFormat.Pdf => BuildPdf(title, headers, rows),
            _ => BuildXlsx(title, headers, rows)
        };

    private static ExportFile BuildXlsx(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(Sanitize(title));
        for (var c = 0; c < headers.Count; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;

        for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < rows[r].Count; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c] ?? "";

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ExportFile
        {
            FileName = $"{Sanitize(title)}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Content = ms.ToArray()
        };
    }

    private static ExportFile BuildCsv(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, new UTF8Encoding(true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var h in headers) csv.WriteField(h);
            csv.NextRecord();
            foreach (var row in rows)
            {
                foreach (var cell in row) csv.WriteField(cell ?? "");
                csv.NextRecord();
            }
        }
        return new ExportFile
        {
            FileName = $"{Sanitize(title)}.csv",
            ContentType = "text/csv",
            Content = ms.ToArray()
        };
    }

    private static ExportFile BuildPdf(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.Header().Text(title).FontSize(16).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (var i = 0; i < headers.Count; i++) cols.RelativeColumn();
                    });
                    foreach (var h in headers)
                        table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).Bold();
                    foreach (var row in rows)
                        foreach (var cell in row)
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell ?? "");
                });
                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Generated ");
                    x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"));
                });
            });
        }).GeneratePdf();

        return new ExportFile
        {
            FileName = $"{Sanitize(title)}.pdf",
            ContentType = "application/pdf",
            Content = bytes
        };
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Length > 31 ? name[..31] : name; // worksheet name limit
    }
}

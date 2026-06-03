using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using SecureAccess.Application.Abstractions;
using SecureAccess.Application.Common;
using SecureAccess.Application.Features.ImportExport;
using SecureAccess.Domain.Entities;
using SecureAccess.Domain.Enums;
using SecureAccess.Infrastructure.Persistence;

namespace SecureAccess.Infrastructure.Services;

/// <summary>
/// Imports Clients and Machines from .xlsx or .csv. Rows are matched on their
/// natural key (customer code / serial number) so re-import updates in place.
/// </summary>
public class ImportService : IImportService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ImportService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Result<ImportResult>> ImportAsync(ImportEntity entity, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        List<Dictionary<string, string>> records;
        try
        {
            records = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? ReadCsv(fileStream)
                : ReadXlsx(fileStream);
        }
        catch (Exception ex)
        {
            return Result<ImportResult>.Fail($"Could not read file: {ex.Message}");
        }

        var result = entity switch
        {
            ImportEntity.Clients => await ImportClients(records, ct),
            ImportEntity.Machines => await ImportMachines(records, ct),
            _ => null
        };

        if (result is null)
            return Result<ImportResult>.Fail($"Import for {entity} is not supported via this endpoint.");

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(new AuditEntry
        {
            Action = AuditAction.Import,
            Details = $"Imported {entity}: {result.Created} created, {result.Updated} updated, {result.Skipped} skipped"
        }, ct);
        return Result<ImportResult>.Success(result);
    }

    private async Task<ImportResult> ImportClients(List<Dictionary<string, string>> records, CancellationToken ct)
    {
        var result = new ImportResult();
        foreach (var r in records)
        {
            var code = Get(r, "Customer Code", "CustomerCode", "Code");
            var name = Get(r, "Name", "Client Name");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                result.Skipped++;
                result.Errors.Add("Row skipped: missing customer code or name.");
                continue;
            }

            var existing = await _db.Clients.FirstOrDefaultAsync(c => c.CustomerCode == code, ct);
            if (existing is null)
            {
                _db.Clients.Add(new Client
                {
                    CustomerCode = code,
                    Name = name,
                    ContactPerson = Get(r, "Contact", "Contact Person"),
                    PhoneNumber = Get(r, "Phone", "Phone Number"),
                    Email = Get(r, "Email"),
                    Address = Get(r, "Address"),
                    Status = EntityStatus.Active
                });
                result.Created++;
            }
            else
            {
                existing.Name = name;
                existing.ContactPerson = Get(r, "Contact", "Contact Person") ?? existing.ContactPerson;
                existing.PhoneNumber = Get(r, "Phone", "Phone Number") ?? existing.PhoneNumber;
                existing.Email = Get(r, "Email") ?? existing.Email;
                existing.Address = Get(r, "Address") ?? existing.Address;
                result.Updated++;
            }
        }
        return result;
    }

    private async Task<ImportResult> ImportMachines(List<Dictionary<string, string>> records, CancellationToken ct)
    {
        var result = new ImportResult();
        foreach (var r in records)
        {
            var clientCode = Get(r, "Customer Code", "Client Code", "CustomerCode");
            var name = Get(r, "Machine", "Machine Name", "Name");
            if (string.IsNullOrWhiteSpace(clientCode) || string.IsNullOrWhiteSpace(name))
            {
                result.Skipped++;
                result.Errors.Add("Row skipped: missing client code or machine name.");
                continue;
            }

            var client = await _db.Clients.FirstOrDefaultAsync(c => c.CustomerCode == clientCode, ct);
            if (client is null)
            {
                result.Skipped++;
                result.Errors.Add($"Row skipped: client '{clientCode}' not found.");
                continue;
            }

            var serial = Get(r, "Serial Number", "Serial", "SerialNumber");
            var existing = serial is null ? null :
                await _db.Machines.FirstOrDefaultAsync(m => m.ClientId == client.Id && m.SerialNumber == serial, ct);

            if (existing is null)
            {
                _db.Machines.Add(new Machine
                {
                    ClientId = client.Id,
                    Name = name,
                    SerialNumber = serial,
                    Model = Get(r, "Model"),
                    Location = Get(r, "Location"),
                    Status = EntityStatus.Active
                });
                result.Created++;
            }
            else
            {
                existing.Name = name;
                existing.Model = Get(r, "Model") ?? existing.Model;
                existing.Location = Get(r, "Location") ?? existing.Location;
                result.Updated++;
            }
        }
        return result;
    }

    private static string? Get(Dictionary<string, string> row, params string[] keys)
    {
        foreach (var k in keys)
        {
            var match = row.FirstOrDefault(kv => string.Equals(kv.Key, k, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.Value)) return match.Value.Trim();
        }
        return null;
    }

    private static List<Dictionary<string, string>> ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        var list = new List<Dictionary<string, string>>();
        while (csv.Read())
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in headers) dict[h] = csv.GetField(h) ?? "";
            list.Add(dict);
        }
        return list;
    }

    private static List<Dictionary<string, string>> ReadXlsx(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var range = ws.RangeUsed();
        var list = new List<Dictionary<string, string>>();
        if (range is null) return list;

        var headerRow = range.FirstRow();
        var headers = headerRow.Cells().Select(c => c.GetString()).ToList();

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
                dict[headers[i]] = row.Cell(i + 1).GetString();
            list.Add(dict);
        }
        return list;
    }
}

namespace SecureAccess.Domain.Entities;

/// <summary>
/// Field-level history of modifications, backing the Change History report
/// (previous value, new value, modified by, modification date, reason).
/// Secret values are stored masked, never in plain text.
/// </summary>
public class ChangeLog
{
    public long Id { get; set; }

    /// <summary>Logical entity type, for example "Credential" or "Client".</summary>
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string? PreviousValue { get; set; }

    public string? NewValue { get; set; }

    public int? ModifiedByUserId { get; set; }
    public User? ModifiedByUser { get; set; }

    public string? ModifiedByUserName { get; set; }

    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }

    public bool IsArchived { get; set; }
}

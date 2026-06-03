namespace SecureAccess.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Carries the surrogate key and
/// the standard creation/modification audit columns used across the schema.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? UpdatedByUserId { get; set; }
}

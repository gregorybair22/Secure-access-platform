using SecureAccess.Domain.Common;

namespace SecureAccess.Domain.Entities;

/// <summary>
/// Defines the shape of a credential. Predefined system types ship in the seed
/// data; administrators may add unlimited custom types. The field schema is
/// stored as JSON in <see cref="FieldsJson"/> (a list of
/// <see cref="CredentialFieldDefinition"/>).
/// </summary>
public class CredentialType : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Optional icon/category hint for the UI.</summary>
    public string? Icon { get; set; }

    /// <summary>Predefined types cannot be deleted, only deactivated.</summary>
    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Serialized list of <see cref="CredentialFieldDefinition"/>.</summary>
    public string FieldsJson { get; set; } = "[]";

    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();

    public ICollection<CredentialTemplate> Templates { get; set; } = new List<CredentialTemplate>();
}

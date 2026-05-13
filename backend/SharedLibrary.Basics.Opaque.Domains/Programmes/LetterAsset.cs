using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

/// <summary>
/// Shared image archive for letter templates. Admins upload once and reference
/// the same asset across any letter on any programme. Persisted via
/// <c>IFileStorage</c>; rows hold metadata + the storage key.
/// </summary>
public class LetterAsset : IDeletedAtEntity
{
    public Guid LetterAssetId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? UploadedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
}

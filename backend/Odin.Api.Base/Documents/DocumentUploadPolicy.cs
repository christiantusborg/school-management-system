namespace Odin.Api.Base.Documents;

/// <summary>
/// Shared upload constraints for student documents. Centralises the MIME
/// allow list, the max file size, and the user-facing error message so
/// every upload entry point (student self-upload, admin-on-behalf,
/// partner-on-behalf, public signup wizard) stays in lockstep.
/// </summary>
public static class DocumentUploadPolicy
{
    public const long MaxBytes = 10 * 1024 * 1024;

    public static readonly HashSet<string> AllowedMimes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public const string AllowedHumanReadable = "PDF, JPG, PNG, or DOCX";

    public static bool IsAllowed(string? contentType)
        => !string.IsNullOrEmpty(contentType) && AllowedMimes.Contains(contentType);
}

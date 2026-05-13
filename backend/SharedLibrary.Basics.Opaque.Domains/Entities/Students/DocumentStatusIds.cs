namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Stable Guids for the seeded <see cref="DocumentStatus"/> lookup rows.
/// Referenced by the migration that creates the table, the seeder that
/// keeps it in sync, and any handler that needs to write a known status
/// without a database round-trip to resolve the Guid by code.
/// </summary>
public static class DocumentStatusIds
{
    public static readonly Guid Submitted           = Guid.Parse("11111111-1111-1111-1111-100000000001");
    public static readonly Guid VerifiedByPartner   = Guid.Parse("11111111-1111-1111-1111-100000000002");
    public static readonly Guid VerifiedByEnrolment = Guid.Parse("11111111-1111-1111-1111-100000000003");
    public static readonly Guid RejectedByPartner   = Guid.Parse("11111111-1111-1111-1111-100000000004");
    public static readonly Guid RejectedByEnrolment = Guid.Parse("11111111-1111-1111-1111-100000000005");

    public sealed record Seed(Guid Id, string Code, string Name);

    public static readonly Seed[] All =
    {
        new(Submitted,           "Submitted",           "Submitted"),
        new(VerifiedByPartner,   "VerifiedByPartner",   "Verified by Partner"),
        new(VerifiedByEnrolment, "VerifiedByEnrolment", "Verified by IBSS"),
        new(RejectedByPartner,   "RejectedByPartner",   "Rejected by Partner"),
        new(RejectedByEnrolment, "RejectedByEnrolment", "Rejected by IBSS"),
    };
}

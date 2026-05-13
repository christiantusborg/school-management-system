namespace School.PartnerAdminApi.Partner.V1.MyPrograms;

internal static class MyProgramsHelpers
{
    public const int StatusDraft = 0;
    public const int StatusPending = 1;
    public const int StatusApproved = 2;
    public const int StatusRejected = 3;

    public static string StatusLabel(int s) => s switch
    {
        StatusDraft => "Draft",
        StatusPending => "Pending",
        StatusApproved => "Approved",
        StatusRejected => "Rejected",
        _ => "Draft",
    };

    public static async Task<bool> HasEnrolmentsAsync(OdinDbContext db, Guid programmeId, CancellationToken ct) =>
        await db.Enrollments
            .AnyAsync(e => e.DeletedAt == null && e.Specialization.ProgrammeId == programmeId, ct);
}

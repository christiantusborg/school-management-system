namespace SharedLibrary.Basics.Opaque.Domains;

/// <summary>
/// Stable Guids for the seeded <see cref="EnrollmentStatus"/> lookup
/// rows. Referenced by the migration that creates the table, the seeder
/// that keeps it in sync, and any handler that needs to write a known
/// status without a database round-trip to resolve the Guid by code.
/// </summary>
public static class EnrollmentStatusIds
{
    public static readonly Guid Draft                                = Guid.Parse("22222222-2222-2222-2222-200000000001");
    public static readonly Guid ApplicationAwaitingReviewByPartner   = Guid.Parse("22222222-2222-2222-2222-200000000002");
    public static readonly Guid ApplicationRejectedByPartner         = Guid.Parse("22222222-2222-2222-2222-200000000003");
    public static readonly Guid ApplicationAwaitingReviewByAdmission = Guid.Parse("22222222-2222-2222-2222-200000000004");
    public static readonly Guid ApplicationRejectedByAdmission       = Guid.Parse("22222222-2222-2222-2222-200000000005");
    public static readonly Guid ApplicationApprovedAdmission         = Guid.Parse("22222222-2222-2222-2222-200000000006");
    public static readonly Guid AcceptAdmission                      = Guid.Parse("22222222-2222-2222-2222-200000000007");
    public static readonly Guid GradesApproved                       = Guid.Parse("22222222-2222-2222-2222-200000000008");
    // Newly minted Guids for the four states added when the workflow was formalised.
    public static readonly Guid ApplicationSubmitted                 = Guid.Parse("22222222-2222-2222-2222-200000000009");
    public static readonly Guid AcceptOffer                          = Guid.Parse("22222222-2222-2222-2222-20000000000a");
    public static readonly Guid AwaitingGradesSubmit                 = Guid.Parse("22222222-2222-2222-2222-20000000000b");
    public static readonly Guid AwaitingGradesApproval               = Guid.Parse("22222222-2222-2222-2222-20000000000c");

    public const string RoleStudent   = "Student";
    public const string RolePartner   = "Partner";
    public const string RoleAdmission = "Admission";

    public sealed record Seed(
        Guid Id,
        string Code,
        string Name,
        int Level,
        int LevelDown,
        string? NextActionRole,
        Guid? NextStatusOnCompleteId);

    public static readonly Seed[] All =
    {
        new(Draft,                                "Draft",                                "Draft",                                  0,   0, RoleStudent,   ApplicationSubmitted),
        new(ApplicationSubmitted,                 "ApplicationSubmitted",                 "Application Submitted",                 50,   0, RoleStudent,   ApplicationAwaitingReviewByPartner),
        new(ApplicationRejectedByPartner,         "ApplicationRejectedByPartner",         "Application Rejected by Partner",       75,   0, RoleStudent,   ApplicationAwaitingReviewByPartner),
        new(ApplicationRejectedByAdmission,       "ApplicationRejectedByAdmission",       "Application Rejected by Admission",     85,   0, RoleStudent,   ApplicationAwaitingReviewByPartner),
        new(ApplicationAwaitingReviewByPartner,   "ApplicationAwaitingReviewByPartner",   "Awaiting Review by Partner",           100,  50, RolePartner,   ApplicationAwaitingReviewByAdmission),
        new(ApplicationAwaitingReviewByAdmission, "ApplicationAwaitingReviewByAdmission", "Awaiting Review by Admission",         200, 100, RoleAdmission, AcceptOffer),
        new(AcceptOffer,                          "AcceptOffer",                          "Accept Offer",                         250, 200, RoleStudent,   ApplicationApprovedAdmission),
        new(ApplicationApprovedAdmission,         "ApplicationApprovedAdmission",         "Approved by Admission",                300, 250, RoleAdmission, AcceptAdmission),
        new(AcceptAdmission,                      "AcceptAdmission",                      "Accept Admission",                     350, 300, RoleStudent,   AwaitingGradesSubmit),
        new(AwaitingGradesSubmit,                 "AwaitingGradesSubmit",                 "Awaiting Grades Submit",               400, 350, RolePartner,   AwaitingGradesApproval),
        new(AwaitingGradesApproval,               "AwaitingGradesApproval",               "Awaiting Grades Approval",             450, 400, RoleAdmission, GradesApproved),
        new(GradesApproved,                       "GradesApproved",                       "Grades Approved",                      500, 450, null,          null),
    };
}

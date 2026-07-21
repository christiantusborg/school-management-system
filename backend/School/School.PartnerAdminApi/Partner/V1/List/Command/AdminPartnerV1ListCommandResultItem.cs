namespace School.PartnerAdminApi.Partner.V1.List.Command;

public sealed class AdminPartnerV1ListCommandResultItem : IAdminPartnerV1ListCommandResultItemQueue
{
    public required Guid PartnerId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string PartnerNumber { get; init; }
    public required int UserCount { get; init; }
    public int StudentCount { get; init; }
    public int TeacherCount { get; init; }
    public int CohortCount { get; init; }
    public required bool IsEnabled { get; init; }
    public DateTime? DisabledAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}

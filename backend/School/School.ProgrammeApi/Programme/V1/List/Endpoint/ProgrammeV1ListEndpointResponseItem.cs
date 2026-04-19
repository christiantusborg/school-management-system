namespace School.ProgrammeApi.Programme.V1.List.Endpoint;

public sealed class ProgrammeV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public DateTime? DeletedAt { get; init; }
    public Guid? PartnerId { get; init; }
    public string? PartnerName { get; init; }
    public required string Status { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDisabledByAdmin { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public required bool HasEnrolments { get; init; }
    public required IReadOnlyList<int> PathwayIds { get; init; }
}

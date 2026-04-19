namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Endpoint;

public sealed class FinalProjectStatusV1ListEndpointResponseItem : HateoasBaseResponse
{
    public required int FinalProjectStatusId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool AllowSetByPartner { get; init; }
}

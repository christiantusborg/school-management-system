namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Endpoint;

public sealed class FinalProjectStatusV1GetEndpointResponse : HateoasBaseResponse
{
    public required int FinalProjectStatusId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool AllowSetByPartner { get; init; }
    public DateTime? DeletedAt { get; init; }
}

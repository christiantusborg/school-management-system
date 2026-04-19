namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint;

public sealed class FinalProjectStatusV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool AllowSetByPartner { get; init; }
}

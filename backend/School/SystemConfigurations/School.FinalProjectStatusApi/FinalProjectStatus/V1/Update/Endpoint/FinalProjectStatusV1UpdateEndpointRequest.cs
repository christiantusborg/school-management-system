namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint;

public sealed class FinalProjectStatusV1UpdateEndpointRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool AllowSetByPartner { get; init; }
}

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Endpoint;

public class ChangePasswordV1InitEndpointResponse : HateoasBaseResponse
{
    public required Guid ChangeId { get; init; }
    public required string OldEvaluatedElement { get; init; }
    public required string Challenge { get; init; }
    public required string EvaluatedElement { get; init; }
}

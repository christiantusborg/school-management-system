namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Endpoint;

public class ChangePasswordV1FinalizeEndpointResponse : HateoasBaseResponse
{
    public required Guid ChangeId { get; init; }
}

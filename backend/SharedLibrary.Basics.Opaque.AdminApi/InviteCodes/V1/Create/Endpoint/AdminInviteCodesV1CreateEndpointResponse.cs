using QuVian.SharedLibrary.Basics.Endpoints.Hateoas;

namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Endpoint;

public sealed class AdminInviteCodesV1CreateEndpointResponse : HateoasBaseResponse
{
    public required string Code { get; init; }
    public required string AssignedRole { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Endpoint.Mappers;

public class ChangePasswordV1FinalizeCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ChangePasswordV1FinalizeCommandResult, ChangePasswordV1FinalizeEndpointResponse>
{
    public ChangePasswordV1FinalizeEndpointResponse MapFrom(ChangePasswordV1FinalizeCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ChangePasswordV1FinalizeEndpointResponse
        {
            ChangeId = input.ChangeId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.ChangeId)
        };
    }
}

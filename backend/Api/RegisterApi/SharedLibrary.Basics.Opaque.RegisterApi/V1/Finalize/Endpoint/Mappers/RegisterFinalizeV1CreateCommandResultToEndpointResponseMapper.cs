using SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Endpoint.Mappers;

public class RegisterFinalizeV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<RegisterFinalizeV1CreateCommandResult, RegisterFinalizeV1CreateEndpointResponse>
{
    public RegisterFinalizeV1CreateEndpointResponse MapFrom(RegisterFinalizeV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RegisterFinalizeV1CreateEndpointResponse
        {
            Token = input.Token,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Parse(input.UserId))
        };
    }
}

using SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Endpoint.Mappers;

public class RegisterInitV1CreateCommandResultToResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<RegisterInitV1CreateCommandResult,
        RegisterInitV1CreateEndpointResponse>
{
    public RegisterInitV1CreateEndpointResponse MapFrom(RegisterInitV1CreateCommandResult input)
    {
        var result = new RegisterInitV1CreateEndpointResponse
        {
            EvaluatedElement = input.EvaluatedElement,
            RegistrationId = input.RegistrationId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.RegistrationId)
        };
        return result;
    }
}
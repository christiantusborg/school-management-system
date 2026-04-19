using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Endpoint.Mappers;

public class ChangePasswordV1InitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ChangePasswordV1InitCommandResult, ChangePasswordV1InitEndpointResponse>
{
    public ChangePasswordV1InitEndpointResponse MapFrom(ChangePasswordV1InitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ChangePasswordV1InitEndpointResponse
        {
            ChangeId = input.ChangeId,
            OldEvaluatedElement = input.OldEvaluatedElement,
            Challenge = input.Challenge,
            EvaluatedElement = input.EvaluatedElement,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.ChangeId)
        };
    }
}

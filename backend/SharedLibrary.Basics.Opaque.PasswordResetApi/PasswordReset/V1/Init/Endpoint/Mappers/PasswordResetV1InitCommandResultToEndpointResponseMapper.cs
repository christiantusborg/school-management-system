using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Endpoint.Mappers;

public sealed class PasswordResetV1InitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PasswordResetV1InitCommandResult, PasswordResetV1InitEndpointResponse>
{
    public PasswordResetV1InitEndpointResponse MapFrom(PasswordResetV1InitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PasswordResetV1InitEndpointResponse
        {
            ResetId = input.ResetId,
            EvaluatedElement = input.EvaluatedElement,
            Links = []
        };
    }
}

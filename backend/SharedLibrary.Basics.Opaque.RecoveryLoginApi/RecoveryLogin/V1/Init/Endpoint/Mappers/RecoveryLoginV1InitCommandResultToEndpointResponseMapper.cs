using SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Endpoint.Mappers;

public sealed class RecoveryLoginV1InitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<RecoveryLoginV1InitCommandResult, RecoveryLoginV1InitEndpointResponse>
{
    public RecoveryLoginV1InitEndpointResponse MapFrom(RecoveryLoginV1InitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RecoveryLoginV1InitEndpointResponse
        {
            LoginId = input.LoginId,
            EvaluatedElement = input.EvaluatedElement,
            Challenge = input.Challenge,
            EncryptedPrivateKey = input.EncryptedPrivateKey,
            Nonce = input.Nonce,
            Links = []
        };
    }
}

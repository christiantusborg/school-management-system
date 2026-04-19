using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Endpoint.Mappers;

public sealed class PasswordResetV1FinalizeEndpointRequestToCommandMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PasswordResetV1FinalizeEndpointRequest, PasswordResetV1FinalizeCommand>
{
    public PasswordResetV1FinalizeCommand MapFrom(PasswordResetV1FinalizeEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PasswordResetV1FinalizeCommand
        {
            ResetId = input.ResetId,
            ClientPublicKey = input.ClientPublicKey
        };
    }
}

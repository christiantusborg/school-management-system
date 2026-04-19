using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Endpoint.Mappers;

public sealed class PasswordResetV1InitEndpointRequestToCommandMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PasswordResetV1InitEndpointRequest, PasswordResetV1InitCommand>
{
    public PasswordResetV1InitCommand MapFrom(PasswordResetV1InitEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PasswordResetV1InitCommand
        {
            ResetToken = input.ResetToken,
            BlindedElement = input.BlindedElement
        };
    }
}

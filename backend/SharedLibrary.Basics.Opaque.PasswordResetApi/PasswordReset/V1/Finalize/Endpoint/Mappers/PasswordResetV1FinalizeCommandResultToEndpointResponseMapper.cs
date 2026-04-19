using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Endpoint.Mappers;

public sealed class PasswordResetV1FinalizeCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PasswordResetV1FinalizeCommandResult, PasswordResetV1FinalizeEndpointResponse>
{
    public PasswordResetV1FinalizeEndpointResponse MapFrom(PasswordResetV1FinalizeCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PasswordResetV1FinalizeEndpointResponse
        {
            Token = input.Token,
            ExpiresAt = input.ExpiresAt,
            Links = []
        };
    }
}

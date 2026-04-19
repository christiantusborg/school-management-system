using SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Endpoint.Mappers;

public class ChangePasswordV1FinalizeEndpointRequestToCommandMapper
    : IMapper<ChangePasswordV1FinalizeEndpointRequest, ChangePasswordV1FinalizeCommand>
{
    public ChangePasswordV1FinalizeCommand MapFrom(ChangePasswordV1FinalizeEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ChangePasswordV1FinalizeCommand
        {
            ChangeId = input.ChangeId,
            Signature = input.Signature,
            ClientPublicKey = input.ClientPublicKey,
        };
    }
}

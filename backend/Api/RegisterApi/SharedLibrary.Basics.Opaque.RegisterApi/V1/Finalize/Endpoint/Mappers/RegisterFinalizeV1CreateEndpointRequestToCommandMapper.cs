using SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Endpoint.Mappers;

public class RegisterFinalizeV1CreateEndpointRequestToCommandMapper
    : IMapper<RegisterFinalizeV1CreateEndpointRequest, RegisterFinalizeV1CreateCommand>
{
    public RegisterFinalizeV1CreateCommand MapFrom(RegisterFinalizeV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RegisterFinalizeV1CreateCommand
        {
            RegistrationId = input.RegistrationId,
            ClientPublicKey = input.ClientPublicKey,
        };
    }
}

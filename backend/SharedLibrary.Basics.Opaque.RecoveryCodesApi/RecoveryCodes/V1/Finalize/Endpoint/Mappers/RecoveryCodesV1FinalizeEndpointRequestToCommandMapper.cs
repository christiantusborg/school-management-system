using SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Endpoint.Mappers;

public class RecoveryCodesV1FinalizeEndpointRequestToCommandMapper
    : IMapper<RecoveryCodesV1FinalizeEndpointRequest, RecoveryCodesV1FinalizeCommand>
{
    public RecoveryCodesV1FinalizeCommand MapFrom(RecoveryCodesV1FinalizeEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new RecoveryCodesV1FinalizeCommand
        {
            BatchId = input.BatchId,
            ClientPublicKeys = input.ClientPublicKeys,
            EncryptedPrivateKeys = input.EncryptedPrivateKeys,
            Nonces = input.Nonces,
        };
    }
}

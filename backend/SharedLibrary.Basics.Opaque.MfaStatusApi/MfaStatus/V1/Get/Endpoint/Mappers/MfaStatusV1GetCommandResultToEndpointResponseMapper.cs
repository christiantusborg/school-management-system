using SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Endpoint.Mappers;

public sealed class MfaStatusV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaStatusV1GetCommandResult, MfaStatusV1GetEndpointResponse>
{
    public MfaStatusV1GetEndpointResponse MapFrom(MfaStatusV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaStatusV1GetEndpointResponse
        {
            EnabledMethods = input.EnabledMethods
                .Select(m => new MfaStatusV1GetEndpointMethodInfo { Method = m.Method })
                .ToArray(),
            Fido2Credentials = input.Fido2Credentials
                .Select(c => new MfaStatusV1GetEndpointFido2Info
                {
                    Fido2CredentialId = c.Fido2CredentialId,
                    Label = c.Label,
                    CreatedAt = c.CreatedAt,
                    LastUsedAt = c.LastUsedAt
                })
                .ToArray(),
            Links = []
        };
    }
}

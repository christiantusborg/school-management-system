using QuVian.CaseApi.CaseFiles.V1.GetKey.Command;

namespace QuVian.CaseApi.CaseFiles.V1.GetKey.Endpoint.Mappers;

public class CaseFilesV1GetKeyCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseFilesV1GetKeyCommandResult, CaseFilesV1GetKeyEndpointResponse>
{
    public CaseFilesV1GetKeyEndpointResponse MapFrom(CaseFilesV1GetKeyCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseFilesV1GetKeyEndpointResponse
        {
            Level            = input.Level,
            KemCiphertext    = input.KemCiphertext,
            EncryptedFileKey = input.EncryptedFileKey,
            Nonce            = input.Nonce,
            StoragePath      = input.StoragePath,
            Links            = []
        };
    }
}

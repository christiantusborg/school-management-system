using QuVian.CaseApi.CaseFiles.V1.Create.Command;

namespace QuVian.CaseApi.CaseFiles.V1.Create.Endpoint.Mappers;

public class CaseFilesV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseFilesV1CreateCommandResult, CaseFilesV1CreateEndpointResponse>
{
    public CaseFilesV1CreateEndpointResponse MapFrom(CaseFilesV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseFilesV1CreateEndpointResponse { CaseFileId = input.CaseFileId, Links = [] };
    }
}

using QuVian.CaseApi.CaseFiles.V1.List.Command;

namespace QuVian.CaseApi.CaseFiles.V1.List.Endpoint.Mappers;

public class CaseFilesV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseFilesV1ListCommandResult, CaseFilesV1ListEndpointResponse>
{
    public CaseFilesV1ListEndpointResponse MapFrom(CaseFilesV1ListCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseFilesV1ListEndpointResponse { Items = input.Items, Links = [] };
    }
}

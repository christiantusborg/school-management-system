using QuVian.CaseApi.CaseFiles.V1.List.Command;

namespace QuVian.CaseApi.CaseFiles.V1.List.Endpoint;

public class CaseFilesV1ListEndpointResponse : HateoasBaseResponse
{
    public required List<CaseFileItem> Items { get; init; }
}

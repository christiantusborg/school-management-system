namespace QuVian.CaseApi.CaseFiles.V1.Create.Endpoint;

public class CaseFilesV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid CaseFileId { get; init; }
}

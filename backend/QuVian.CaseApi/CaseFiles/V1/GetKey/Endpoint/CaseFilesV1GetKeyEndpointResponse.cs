namespace QuVian.CaseApi.CaseFiles.V1.GetKey.Endpoint;

public class CaseFilesV1GetKeyEndpointResponse : HateoasBaseResponse
{
    public int Level { get; init; }
    public required string KemCiphertext { get; init; }
    public required string EncryptedFileKey { get; init; }
    public required string Nonce { get; init; }
    public required string StoragePath { get; init; }
}

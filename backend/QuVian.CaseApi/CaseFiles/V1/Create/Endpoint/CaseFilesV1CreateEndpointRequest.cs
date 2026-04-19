namespace QuVian.CaseApi.CaseFiles.V1.Create.Endpoint;

public class CaseFilesV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public required string StoragePath { get; init; }
    public int MinLevel { get; init; }
    public CaseFileAccessMode AccessMode { get; init; } = CaseFileAccessMode.Hierarchical;
    public required List<FileLevelKeyDto> LevelKeys { get; init; }
}

public class FileLevelKeyDto
{
    public int Level { get; init; }
    public required string KemCiphertext { get; init; }   // base64
    public required string EncryptedFileKey { get; init; } // base64
    public required string Nonce { get; init; }            // base64
}

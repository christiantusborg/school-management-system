namespace QuVian.CaseApi.CaseFiles.V1.GetKey.Command;

public sealed class CaseFilesV1GetKeyCommandResult : ICaseFilesV1GetKeyCommandResultQueue
{
    /// <summary>The user's access level that matched.</summary>
    public int Level { get; init; }

    /// <summary>ML-KEM-768 ciphertext for the file-level key — base64, decapsulate with the level private key.</summary>
    public required string KemCiphertext { get; init; }

    /// <summary>Encrypted 32-byte file key — base64.</summary>
    public required string EncryptedFileKey { get; init; }

    /// <summary>12-byte AES-GCM nonce — base64.</summary>
    public required string Nonce { get; init; }

    /// <summary>The file's storage path so the frontend can fetch the encrypted blob.</summary>
    public required string StoragePath { get; init; }
}

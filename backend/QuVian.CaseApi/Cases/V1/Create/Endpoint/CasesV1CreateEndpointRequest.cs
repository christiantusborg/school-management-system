namespace QuVian.CaseApi.Cases.V1.Create.Endpoint;

public class CasesV1CreateEndpointRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public CasePriority Priority { get; init; } = CasePriority.Medium;
    public DateTime? DueDate { get; init; }

    /// <summary>One entry per access level (levels 1–6). Each contains the level's public key and the creator's encrypted copy of the level private key.</summary>
    public required List<CaseLevelKeyDto> LevelKeyPairs { get; init; }
}

public class CaseLevelKeyDto
{
    public int Level { get; init; }

    /// <summary>ML-KEM-768 public key — base64, 1184 bytes.</summary>
    public required string PublicKey { get; init; }

    /// <summary>Level private key wrapped to the creator's personal ML-KEM public key.</summary>
    public required CaseWrappedKeyDto WrappedPrivateKey { get; init; }
}

public class CaseWrappedKeyDto
{
    /// <summary>ML-KEM-768 ciphertext — base64, 1088 bytes.</summary>
    public required string KemCiphertext { get; init; }

    /// <summary>Encrypted level private key — base64.</summary>
    public required string EncryptedLevelPrivKey { get; init; }

    /// <summary>12-byte AES-GCM nonce — base64.</summary>
    public required string Nonce { get; init; }
}

using System.Security.Cryptography;
using NSec.Cryptography;

namespace Odin.Api.Base.Crypto;

/// <summary>
/// Server-side OPAQUE OPRF and challenge operations using Ristretto255.
/// </summary>
public class OpaqueServer
{
    /// <summary>
    /// Generate a fresh random 32-byte OPRF seed (the server's per-user secret scalar).
    /// </summary>
    public byte[] GenerateOprfSeed()
    {
        var seed = new byte[32];
        Ristretto255Interop.crypto_core_ristretto255_scalar_random(seed);
        return seed;
    }

    /// <summary>
    /// Compute BlindEvaluate: point = seed * blindedElement (Ristretto255 scalar mult).
    /// </summary>
    public byte[] BlindEvaluate(byte[] oprfSeed, byte[] blindedElement)
    {
        var result = new byte[32];
        var rc = Ristretto255Interop.crypto_scalarmult_ristretto255(result, oprfSeed, blindedElement);
        if (rc != 0)
            throw new InvalidOperationException("Ristretto255 scalar multiplication failed.");
        return result;
    }

    /// <summary>
    /// Generate a random 32-byte challenge for the login finish step.
    /// </summary>
    public byte[] GenerateChallenge() => RandomNumberGenerator.GetBytes(32);

    /// <summary>
    /// Verify an Ed25519 signature over the challenge using the client's public key.
    /// </summary>
    public bool VerifySignature(byte[] signature, byte[] challenge, byte[] publicKey)
    {
        try
        {
            var algorithm = SignatureAlgorithm.Ed25519;
            var key = NSec.Cryptography.PublicKey.Import(algorithm, publicKey, KeyBlobFormat.RawPublicKey);
            return algorithm.Verify(key, challenge, signature);
        }
        catch
        {
            return false;
        }
    }
}

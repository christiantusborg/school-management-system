using System.Security.Cryptography;
using Sodium;

namespace SharedLibrary.Basics.OpaqueService;

/// <summary>
/// Server-side OPAQUE operations: OPRF blind-evaluate, challenge generation, Ed25519 verification.
/// </summary>
public class OpaqueServer
{
    /// <summary>
    /// Generate a random OPRF seed (Ristretto255 scalar, 32 bytes).
    /// </summary>
    public byte[] GenerateSeed()
    {
        return Ristretto255Interop.RandomScalar();
    }

    /// <summary>
    /// OPRF BlindEvaluate: multiply the client's blinded point by the server's OPRF seed.
    /// Returns the evaluated Ristretto255 point (32 bytes).
    /// </summary>
    public byte[] BlindEvaluate(byte[] oprfSeed, byte[] blindedElement)
    {
        return Ristretto255Interop.ScalarMult(oprfSeed, blindedElement);
    }

    /// <summary>
    /// Generate a random 32-byte challenge for Ed25519 signature verification.
    /// </summary>
    public byte[] GenerateChallenge()
    {
        return RandomNumberGenerator.GetBytes(32);
    }

    /// <summary>
    /// Verify an Ed25519 detached signature over a message using the client's public key.
    /// </summary>
    public bool VerifySignature(byte[] signature, byte[] message, byte[] publicKey)
    {
        if (signature.Length != 64) return false;
        if (publicKey.Length != 32) return false;

        try
        {
            return PublicKeyAuth.VerifyDetached(signature, message, publicKey);
        }
        catch
        {
            return false;
        }
    }

    public bool TryConvertToBlindedElement(string input, out byte[] blindedElement, out string error)
    {
        try
        {
            blindedElement = Convert.FromBase64String(input);
            if (blindedElement.Length != 32)
            {
                error = "Invalid blinded element.";
                return false;
            }
            error = string.Empty;
            return true;
        }
        catch
        {
            blindedElement = [];
            error = "Invalid blinded element encoding.";
            return false;
        }
    }

    public bool TryGenerateSeed(out byte[] seed, out string error)
    {
        try
        {
            seed = GenerateSeed();
            error = string.Empty;
            return true;
        }
        catch
        {
            seed = [];
            error = "Failed to generate OPRF seed.";
            return false;
        }
    }

    public bool TryConvertToClientPublicKey(string input, out byte[] clientPublicKey, out string error)
    {
        try
        {
            clientPublicKey = Convert.FromBase64String(input);
            if (clientPublicKey.Length != 32)
            {
                error = "Invalid client public key.";
                return false;
            }
            error = string.Empty;
            return true;
        }
        catch
        {
            clientPublicKey = [];
            error = "Invalid client public key encoding.";
            return false;
        }
    }

    public bool TryBlindEvaluate(byte[] seed, byte[] blindedElement, out byte[] evaluatedElement, out string error)
    {
        try
        {
            evaluatedElement = BlindEvaluate(seed, blindedElement);
            error = string.Empty;
            return true;
        }
        catch
        {
            evaluatedElement = [];
            error = "Failed to evaluate blinded element.";
            return false;
        }
    }
}

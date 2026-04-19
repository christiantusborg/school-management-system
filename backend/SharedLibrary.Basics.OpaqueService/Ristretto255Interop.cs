using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SharedLibrary.Basics.OpaqueService;

/// <summary>
/// P/Invoke bindings to libsodium for Ristretto255 operations not wrapped by Sodium.Core.
/// </summary>
public static class Ristretto255Interop
{
    private const string LibSodium = "libsodium";

    [DllImport(LibSodium, CallingConvention = CallingConvention.Cdecl)]
    private static extern void crypto_core_ristretto255_scalar_random(byte[] x);

    [DllImport(LibSodium, CallingConvention = CallingConvention.Cdecl)]
    private static extern int crypto_scalarmult_ristretto255(byte[] q, byte[] n, byte[] p);

    /// <summary>
    /// Generate a random Ristretto255 scalar (32 bytes). Used as OPRF seed.
    /// </summary>
    public static byte[] RandomScalar()
    {
        var scalar = new byte[32];
        crypto_core_ristretto255_scalar_random(scalar);
        return scalar;
    }

    /// <summary>
    /// Scalar multiplication on Ristretto255: q = n * p.
    /// Used for OPRF BlindEvaluate (oprfSeed * blindedPoint).
    /// </summary>
    public static byte[] ScalarMult(byte[] scalar, byte[] point)
    {
        if (scalar.Length != 32) throw new ArgumentException("Scalar must be 32 bytes.", nameof(scalar));
        if (point.Length != 32) throw new ArgumentException("Point must be 32 bytes.", nameof(point));

        var result = new byte[32];
        var rc = crypto_scalarmult_ristretto255(result, scalar, point);
        if (rc != 0)
            throw new CryptographicException("Ristretto255 scalar multiplication failed (identity point).");

        return result;
    }
}

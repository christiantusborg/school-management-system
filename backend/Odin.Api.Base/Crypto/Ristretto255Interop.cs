using System.Runtime.InteropServices;

namespace Odin.Api.Base.Crypto;

/// <summary>
/// P/Invoke bindings for libsodium's Ristretto255 group operations.
/// </summary>
internal static class Ristretto255Interop
{
    private const string LibName = "libsodium";

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int crypto_core_ristretto255_scalar_reduce(
        byte[] r, byte[] s);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int crypto_scalarmult_ristretto255(
        byte[] q, byte[] n, byte[] p);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int crypto_core_ristretto255_scalar_random(
        byte[] r);
}

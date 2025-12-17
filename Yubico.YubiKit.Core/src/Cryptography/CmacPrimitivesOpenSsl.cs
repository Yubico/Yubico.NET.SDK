// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.PlatformInterop;

namespace Yubico.YubiKit.Core.Cryptography;

internal static class NativeMethods
{
    // We're calling on OpenSSL to perform CMAC.
    // Starting with OpenSSL 3.x, the way to perform CMAC is to use the
    // EVP_MAC API:
    //   unsigned char iv[16] = { 0 };
    //   OSSL_PARAM params[3] = {
    //       OSSL_PARAM_construct_utf8_string(OSSL_MAC_PARAM_CIPHER, "aes-128-cbc", 11);
    //       OSSL_PARAM_construct_octet_string(OSSL_MAC_PARAM_IV, iv, 16);
    //       OSSL_PARAM_construct_end();
    //   };
    //   EVP_MAC *evpMac = EVP_MAC_fetch(NULL, "CMAC", "provider=default");
    //   EVP_MAC_CTX *evpMacCtx = EVP_MAC_CTX_new(evpMac);
    //   EVP_MAC_init(evpMacCtx, keyData, 16, params);
    //   EVP_MAC_update(evpMacCtx, dataToMac, dataToMacLen);
    //   EVP_MAC_final(evpMacCtx, result, &outputLen, 16);

    [DllImport(Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_CTX_new", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern IntPtr CmacEvpMacCtxNewIntPtr();

    public static SafeEvpCmacCtx CmacEvpMacCtxNew() => new(CmacEvpMacCtxNewIntPtr(), true);

    // void EVP_MAC_CTX_free(EVP_MAC_CTX* c);
    [DllImport(Libraries.NativeShims, EntryPoint = "Native_EVP_MAC_CTX_free", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern void EvpMacCtxFree(IntPtr ctx);

    // This returns 1 for success, or 0 for an error.
    // In the Csharp object, the algorithm is an Enum. We're going to pass
    // that enum value as an int. In the C code, we just have to know what
    // those ints mean.
    //   1 - AES-128-CBC
    //   2 - AES-192-CBC
    //   3 - AES-256-CBC
    [DllImport(Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_init", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int CmacEvpMacInit(IntPtr ctx, int algorithm, byte[] key, int keyLength);

    public static int CmacEvpMacInit(SafeEvpCmacCtx ctx, int algorithm, byte[] key, int keyLength) =>
        CmacEvpMacInit(ctx.DangerousGetHandle(), algorithm, key, keyLength);

    // int EVP_MAC_update(
    //     EVP_MAC_CTX* c, const void *data, size_t dataLen);
    // This returns 1 for success, or 0 for an error.
    [DllImport(Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_update", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int CmacEvpMacUpdate(IntPtr ctx, byte[] input, int inLen);

    // The input begins at offset 0 and if the input.Length is not big enough
    // for inLen, it will return an error code.
    public static int CmacEvpMacUpdate(SafeEvpCmacCtx ctx, byte[] input, int inLen) =>
        CmacEvpMacUpdate(ctx.DangerousGetHandle(), input, inLen);

    // int EVP_MAC_final(
    //     EVP_MAC_CTX *c, unsigned char *out, size_t *outLen);
    // This returns 1 for success, or 0 for an error.
    [DllImport(Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_final", ExactSpelling = true,
        CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int CmacEvpMacFinal(IntPtr ctx, byte[] output, int outputSize, out int outLen);

    public static int CmacEvpMacFinal(SafeEvpCmacCtx ctx, byte[] output, int outputSize, out int outLen) =>
        CmacEvpMacFinal(ctx.DangerousGetHandle(), output, outputSize, out outLen);
}

public enum CmacBlockCipherAlgorithm
{
    /// <summary>
    ///     Use this enum value in order to specify CMAC with AES-128.
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     Use this enum value in order to specify CMAC with AES-128.
    /// </summary>
    Aes128 = 1,

    /// <summary>
    ///     Use this enum value in order to specify CMAC with AES-192.
    /// </summary>
    Aes192 = 2,

    /// <summary>
    ///     Use this enum value in order to specify CMAC with AES-256.
    /// </summary>
    Aes256 = 3
}

internal class SafeEvpCmacCtx : SafeHandle
{
    /// <summary>
    ///     Create a new <c>SafeEvpCmacCtx</c>. This constructor will initialize
    ///     the <c>BlockCipherAlgorithm</c> to <c>Aes128</c>.
    /// </summary>
    public SafeEvpCmacCtx() : base(IntPtr.Zero, true)
    {
        BlockCipherAlgorithm = CmacBlockCipherAlgorithm.Aes128;
    }

    /// <summary>
    ///     Create a new <c>SafeEvpCmacCtx</c>. This constructor will initialize
    ///     the <c>BlockCipherAlgorithm</c> to <c>Aes128</c>.
    /// </summary>
    public SafeEvpCmacCtx(IntPtr invalidHandleValue, bool ownsHandle) : base(invalidHandleValue, ownsHandle)
    {
        BlockCipherAlgorithm = CmacBlockCipherAlgorithm.Aes128;
    }

    /// <summary>
    ///     This specifies which algorithm the CMAC will use as the underlying
    ///     block cipher algorithm. The constructors will initialize this to
    ///     <c>Aes128</c>. If you want to use a different algorithm, set this
    ///     property.
    /// </summary>
    public CmacBlockCipherAlgorithm BlockCipherAlgorithm { get; set; }

    /// <inheritdoc />
    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <inheritdoc />
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid) NativeMethods.EvpMacCtxFree(handle);

        return true;
    }
}

/// <summary>
///     An OpenSSL implementation of the ICmacPrimitives interface, exposing CMAC
///     primitives to the SDK.
/// </summary>
internal sealed class CmacPrimitivesOpenSsl : IDisposable
{
    private readonly SafeEvpCmacCtx _cmacCtx;
    private bool _disposed;

    /// <summary>
    ///     Build a new OpenSSL CMAC Primitives object setting the algorithm to
    ///     the default: AES-128.
    /// </summary>
    public CmacPrimitivesOpenSsl()
        : this(CmacBlockCipherAlgorithm.Aes128)
    {
    }

    /// <summary>
    ///     Build a new OpenSSL CMAC Primitives object that uses the specified
    ///     algorithm.
    /// </summary>
    public CmacPrimitivesOpenSsl(CmacBlockCipherAlgorithm algorithm)
    {
        _cmacCtx = NativeMethods.CmacEvpMacCtxNew();
        _cmacCtx.BlockCipherAlgorithm = algorithm;
    }

    #region IDisposable Members

    public void Dispose()
    {
        if (_disposed) return;

        _cmacCtx.Dispose();
        _disposed = true;
    }

    #endregion

    /// <inheritdoc />
    public void CmacInit(ReadOnlySpan<byte> keyData)
    {
        if (keyData.Length != _cmacCtx.BlockCipherAlgorithm.KeyLength())
            throw new ArgumentException("ExceptionMessages.InvalidCmacInput");

        var keyBytes = keyData.ToArray();

        try
        {
            if (NativeMethods.CmacEvpMacInit(
                    _cmacCtx,
                    (int)_cmacCtx.BlockCipherAlgorithm,
                    keyBytes,
                    keyBytes.Length) == 0)
                throw new SecurityException("ExceptionMessages.CmacFailed");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    /// <inheritdoc />
    public void CmacUpdate(ReadOnlySpan<byte> dataToMac)
    {
        var dataBytes = dataToMac.ToArray();

        try
        {
            if (NativeMethods.CmacEvpMacUpdate(_cmacCtx, dataBytes, dataBytes.Length) == 0)
                throw new SecurityException("ExceptionMessages.CmacFailed");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataBytes);
        }
    }

    /// <inheritdoc />
    public void CmacFinal(Span<byte> macBuffer)
    {
        if (macBuffer.Length != _cmacCtx.BlockCipherAlgorithm.MacLength())
            throw new ArgumentException("ExceptionMessages.InvalidCmacInput");
        var outputBuffer = new byte[macBuffer.Length];

        if (NativeMethods.CmacEvpMacFinal(_cmacCtx, outputBuffer, outputBuffer.Length, out var outputLength) == 0)
            throw new SecurityException("ExceptionMessages.CmacFailed");

        if (outputLength != outputBuffer.Length) throw new SecurityException("ExceptionMessages.CmacFailed");

        outputBuffer.AsSpan().CopyTo(macBuffer);
    }
}

public static class CmacBlockCipherAlgorithmExtensions
{
    /// <summary>
    ///     Returns the length, in bytes, of the key to be used in the operations
    ///     given the specified underlying block cipher algorithm.
    /// </summary>
    /// <param name="algorithm">
    ///     The algorithm name to check.
    /// </param>
    /// <returns>
    ///     An int, the length, in bytes, of the key for the specified block
    ///     cipher algorithm.
    /// </returns>
    public static int KeyLength(this CmacBlockCipherAlgorithm algorithm) => algorithm switch
    {
        CmacBlockCipherAlgorithm.Aes128 => 16,
        CmacBlockCipherAlgorithm.Aes192 => 24,
        CmacBlockCipherAlgorithm.Aes256 => 32,
        _ => throw new ArgumentOutOfRangeException("ExceptionMessages.InvalidCmacInput")
    };

    /// <summary>
    ///     Returns the size, in bytes, of the resulting CMAC given the specified
    ///     underlying block cipher algorithm.
    /// </summary>
    /// <param name="algorithm">
    ///     The algorithm name to check.
    /// </param>
    /// <returns>
    ///     An int, the size, in bytes, of the CMAC result for the specified
    ///     block cipher algorithm.
    /// </returns>
    public static int MacLength(this CmacBlockCipherAlgorithm algorithm) => algorithm switch
    {
        CmacBlockCipherAlgorithm.Aes128 => 16,
        CmacBlockCipherAlgorithm.Aes192 => 16,
        CmacBlockCipherAlgorithm.Aes256 => 16,
        _ => throw new ArgumentOutOfRangeException("ExceptionMessages.InvalidCmacInput")
    };
}
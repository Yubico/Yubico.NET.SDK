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

using System;
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop;

internal static partial class NativeMethods
{
    #region CtrlFlag enum

    // Do not change these numbers. They are indeed magic. We need to pass
    // information to the C code indicating whether we want to get or set the
    // tag. We can either create multiple functions or send args. But how can
    // we send an arg that both the C# and C code know? A shared .h file? No,
    // that is not possible. So we simply have to write our code in both C#
    // and C with specified numbers. That is, because there is no way to
    // programmatically share definitions between C# and C code, we have to
    // write both versions simply "knowing" that these are the definitions of
    // these flags.
    public enum CtrlFlag
    {
        Unknown = 0,
        GetTag = 16,
        SetTag = 17
    }

    #endregion

    // EVP_CIPHER_CTX* EVP_CIPHER_CTX_new(void);
    [DllImport(
        Libraries.NativeShims, EntryPoint = "Native_EVP_CIPHER_CTX_new", ExactSpelling = true, CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern IntPtr EvpCipherCtxNewIntPtr();

    public static SafeEvpCipherCtx EvpCipherCtxNew() => new(EvpCipherCtxNewIntPtr(), true);

    // void EVP_CIPHER_CTX_free(EVP_CIPHER_CTX* c);
    [DllImport(
        Libraries.NativeShims, EntryPoint = "Native_EVP_CIPHER_CTX_free", ExactSpelling = true, CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static extern void EvpCipherCtxFree(IntPtr ctx);

    // int EVP_EncryptInit_ex(
    //     EVP_CIPHER_CTX* c, const EVP_CIPHER *type,
    //     ENGINE *impl, const unsigned char *key, const unsigned char *iv);
    // Also DecryptInit. Set isEncrypt to true in order to call EncryptInit.
    // We'll set the type in the C code, the impl will be null, so the C#
    // call just needs the ctx, key, and nonce.
    // This returns 1 for success, or 0 for an error.
    [DllImport(
        Libraries.NativeShims, EntryPoint = "Native_EVP_Aes256Gcm_Init", ExactSpelling = true, CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int EvpAes256GcmInit(int isEncrypt, IntPtr ctx, byte[] key, byte[] nonce);

    public static int EvpAes256GcmInit(bool isEncrypt, SafeEvpCipherCtx ctx, byte[] key, byte[] nonce) =>
        EvpAes256GcmInit(
            isEncrypt
                ? 1
                : 0, ctx.DangerousGetHandle(), key, nonce);

    // int EVP_EncryptUpdate(
    //     EVP_CIPHER_CTX* c, unsigned char* out, int* outLen,
    //     const unsigned char *in, int inLen);
    // Also DecryptUpdate. The C code will check the CTX to see if it was
    // init to encrypt or decrypt.
    // The OpenSSL Wiki says that if this is AES-GCM and the output is null,
    // then the input will be considered the AAD.
    // This returns 1 for success, or 0 for an error.
    [DllImport(Libraries.NativeShims, EntryPoint = "Native_EVP_Update", ExactSpelling = true, CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int EvpUpdate(IntPtr ctx, byte[]? output, out int outLen, byte[] input, int inLen);

    // The input begins at offset 0 and if the input.Length is not big enough
    // for inLen, it will return an error code.
    public static int EvpUpdate(SafeEvpCipherCtx ctx, byte[]? output, out int outLen, byte[] input, int inLen) =>
        EvpUpdate(ctx.DangerousGetHandle(), output, out outLen, input, inLen);

    // int EVP_EncryptFinal_ex(
    //     EVP_CIPHER_CTX* c, unsigned char* out, int* outLen);
    // Also DecryptFinal_ex. The C code will check the CTX to see if it was
    // init to encrypt or decrypt.
    // This returns 1 for success, or 0 for an error.
    // When performing AES-GCM, this is when the auth tag is computed. There
    // should be no data returned in the output buffer.
    [DllImport(Libraries.NativeShims, EntryPoint = "Native_EVP_Final_ex", ExactSpelling = true, CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int EvpFinal(IntPtr ctx, byte[] output, out int outLen);

    public static int EvpFinal(SafeEvpCipherCtx ctx, byte[] output, out int outLen) =>
        EvpFinal(ctx.DangerousGetHandle(), output, out outLen);

    // int EVP_CIPHER_CTX_ctrl(EVP_CIPHER_CTX* c, int cmd, int p1, void* p2);
    // The second argument to this function is the "command". For now, it
    // looks like all we need are GET_TAG and SET_TAG. In openssl/evp.h these
    // values are 16 and 17 respectively. However, this C# code does not have
    // access to the .h file. Although it is very unlikely that these values
    // will change in the future, or that they might be different values on
    // different platforms, it is possible, so it is not strictly proper to
    // simply pass numbers that we got from some #defines out of a .h file.
    // (Side note: we have encountered cases where a #define on Windows is
    // different than the #define on Linux, same flag, different values.)
    // So we'll declare some enum values (see above) and use them. We'll
    // pass these values to the C code (the Native_ function) and that code
    // will have to know what those numbers mean. Strictly speaking, we
    // should have some way to share information between C# and C, but
    // P/Invoke does not have anything. An alternative is to have separate
    // functions for each flag, and if we only implement get and set tag that
    // means increasing from one function to two. But this "ctrl" function
    // can be called with at least 20 different flags, so if we ever need to
    // use more flags, we would just keep adding functions.
    [DllImport(
        Libraries.NativeShims, EntryPoint = "Native_EVP_CIPHER_CTX_ctrl", ExactSpelling = true, CharSet = CharSet.Ansi)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int EvpCipherCtxCtrl(IntPtr ctx, int cmd, int p1, byte[] p2);

    public static int EvpCipherCtxCtrl(SafeEvpCipherCtx ctx, CtrlFlag cmd, int p1, byte[] p2) =>
        EvpCipherCtxCtrl(ctx.DangerousGetHandle(), (int)cmd, p1, p2);
}

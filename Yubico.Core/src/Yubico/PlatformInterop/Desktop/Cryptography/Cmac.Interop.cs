// Copyright 2023 Yubico AB
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

namespace Yubico.PlatformInterop
{
    internal static partial class NativeMethods
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

        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_CTX_new", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr CmacEvpMacCtxNewIntPtr();

        public static SafeEvpCmacCtx CmacEvpMacCtxNew() => new SafeEvpCmacCtx(CmacEvpMacCtxNewIntPtr(), true);

        // void EVP_MAC_CTX_free(EVP_MAC_CTX* c);
        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_EVP_MAC_CTX_free", ExactSpelling = true,
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
        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_init", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int CmacEvpMacInit(IntPtr ctx, int algorithm, byte[] key, int keyLength);

        public static int CmacEvpMacInit(SafeEvpCmacCtx ctx, int algorithm, byte[] key, int keyLength) =>
            CmacEvpMacInit(ctx.DangerousGetHandle(), algorithm, key, keyLength);

        // int EVP_MAC_update(
        //     EVP_MAC_CTX* c, const void *data, size_t dataLen);
        // This returns 1 for success, or 0 for an error.
        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_update", ExactSpelling = true,
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
        [DllImport(
            Libraries.NativeShims, EntryPoint = "Native_CMAC_EVP_MAC_final", ExactSpelling = true,
            CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int CmacEvpMacFinal(IntPtr ctx, byte[] output, int outputSize, out int outLen);

        public static int CmacEvpMacFinal(SafeEvpCmacCtx ctx, byte[] output, int outputSize, out int outLen) =>
            CmacEvpMacFinal(ctx.DangerousGetHandle(), output, outputSize, out outLen);
    }
}

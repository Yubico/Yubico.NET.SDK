// Copyright 2022 Yubico AB
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
        // BIGNUM* BN_bin2bn(const unsigned char* s, int len, BIGNUM* ret);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_bin2bn", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr BnBinaryToBigNum(byte[] buffer, int length, IntPtr ret);

        public static SafeBigNum BnBinaryToBigNum(byte[] buffer) =>
            new SafeBigNum(BnBinaryToBigNum(buffer, buffer.Length, IntPtr.Zero), true);

        // BIGNUM* BN_new();
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_new", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr BnNewIntPtr();

        public static SafeBigNum BnNew() => new SafeBigNum(BnNewIntPtr(), true);

        // void BN_clear_free(BIGNUM* a);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_clear_free", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void BnClearFree(IntPtr bignum);

        // int BN_num_bytes(const BIGNUM* a);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_num_bytes", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern void BnNumBytes(IntPtr bignum);

        public static void BnNumBytes(SafeBigNum bigNum) => BnNumBytes(bigNum.DangerousGetHandle());

        // int BN_bn2bin(const BIGNUM* a, unsigned char* to);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_bn2bin", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int BnBigNumToBinary(IntPtr bignum, byte[] buffer);

        public static int BnBigNumToBinary(SafeBigNum bigNum, byte[] buffer) =>
            BnBigNumToBinary(bigNum.DangerousGetHandle(), buffer);

        // int BN_bn2binpad(const BIGNUM* a, unsigned char* to, int tolen);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BN_bn2binpad", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern int BnBigNumToBinaryWithPadding(IntPtr bignum, byte[] buffer, int bufferSize);

        public static int BnBigNumToBinaryWithPadding(SafeBigNum bigNum, byte[] buffer) =>
            BnBigNumToBinaryWithPadding(bigNum.DangerousGetHandle(), buffer, buffer.Length);
    }
}

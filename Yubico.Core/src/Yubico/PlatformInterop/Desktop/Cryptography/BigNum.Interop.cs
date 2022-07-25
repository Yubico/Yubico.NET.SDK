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
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BnBinaryToBigNum", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr BnBinaryToBigNum(byte[] buffer, int length, IntPtr ret);

        // void BN_clear_free(BIGNUM* a);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BnClearFree", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern void BnClearFree(IntPtr bignum);

        // int BN_num_bytes(const BIGNUM* a);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BnNumBytes", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern void BnNumBytes(IntPtr bignum);

        // int BN_bn2bin(const BIGNUM* a, unsigned char* to);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_BnBigNumToBinary", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern int BnBigNumToBinary(IntPtr bignum, byte[] buffer);

        // BN_CTX_new
        // BN_CTX_start
        // BN_CTX_get
        // BN_CTX_end
        // BN_CTX_free

    }
}

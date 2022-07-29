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
        // EC_KEY* EC_KEY_new_by_curve_name(int nid);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_KEY_new_by_curve_name", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr EcKeyNewByCurveName(int curveId);

        // void EC_KEY_free(EC_KEY* key);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_KEY_free", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr EcKeyFree(IntPtr key);

        // const BIGNUM* EC_KEY_get0_private_key(EC_KEY* key);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_KEY_get0_private_key", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr EcKeyGetPrivateKey(IntPtr key);

        // int EC_KEY_set_private_key(EC_KEY* key, const BIGNUM* prv);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_KEY_set_private_key", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr EcKeySetPrivateKey(IntPtr key, IntPtr prv);
    }
}

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
        // int ECDH_compute_key(void* out, size_t outlen, const EC_POINT* public_key, EC_KEY* ecdh, void* kdf);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_ECDH_compute_key", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr EcdhComputeKey(IntPtr @out, ulong outlen, IntPtr public_key, IntPtr ecdh, IntPtr kdf);
    }
}

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
        // EC_POINT* EC_POINT_new(const EC_GROUP* group);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EcPointNew", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr EcPointNew(IntPtr ecGroup);

        // void EC_POINT_free(EC_POINT* point);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EcPointFree", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern void EcPointFree(IntPtr ecPoint);

        // int EC_POINT_set_affine_coordinates_GFp(const EC_GROUP* group, EC_POINT* p, const BIGNUM* x, const BIGNUM* y, BN_CTX* ctx);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EcPointSetAffineCoordinatesGfp", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern int EcPointSetAffineCoordinatesGfp(IntPtr group, IntPtr point, IntPtr x, IntPtr y, IntPtr ctx);

        // int EC_POINT_get_affine_coordinates_GFp(const EC_GROUP* group, EC_POINT* p, BIGNUM* x, BIGNUM* y, BN_CTX* ctx);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EcPointGetAffineCoordinatesGfp", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern int EcPointGetAffineCoordinatesGfp(IntPtr group, IntPtr point, IntPtr x, IntPtr y, IntPtr ctx);

        // EC_POINT_mul

    }
}

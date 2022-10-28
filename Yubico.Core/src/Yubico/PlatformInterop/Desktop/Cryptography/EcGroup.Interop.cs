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
        // EC_GROUP* EC_GROUP_new_by_curve_name(int nid);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_GROUP_new_by_curve_name", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr EcGroupNewByCurveNameIntPtr(int curveId);

        public static SafeEcGroup EcGroupNewByCurveName(int curveId) =>
            new SafeEcGroup(EcGroupNewByCurveNameIntPtr(curveId), true);

        // void EC_GROUP_free(EC_GROUP* group);
        [DllImport(Libraries.NativeShims, EntryPoint = "Native_EC_GROUP_free", ExactSpelling = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void EcGroupFree(IntPtr group);
    }
}

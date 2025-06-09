// Copyright 2024 Yubico AB
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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Registry of platform-specific libraries that need to be tested.
    /// </summary>
    internal static class PlatformLibraryRegistry
    {
        /// <summary>
        /// Gets platform-specific libraries based on the current operating system.
        /// </summary>
        public static IReadOnlyList<string> GetPlatformLibraries()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return
                [
                    Libraries.CfgMgr,
                    Libraries.Hid,
                    Libraries.Kernel32
                ];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return
                [
                    Libraries.LinuxKernelLib,
                    Libraries.LinuxUdevLib
                ];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return
                [
                    Libraries.CoreFoundation,
                    Libraries.IOKitFramework,
                    Libraries.WinSCard
                ];
            }

            return [];
        }

        /// <summary>
        /// Tests all platform-specific libraries to ensure they can be loaded.
        /// </summary>
        /// <returns>A dictionary of library names to success/failure results.</returns>
        public static Dictionary<string, (bool success, string message)> TestPlatformLibraries()
        {
            var results = new Dictionary<string, (bool success, string message)>();
            var libraries = GetPlatformLibraries();

            foreach (var library in libraries)
            {
                results[library] = PlatformLibraryLoader.TryLoadLibrary(library);
            }

            return results;
        }
    }
}

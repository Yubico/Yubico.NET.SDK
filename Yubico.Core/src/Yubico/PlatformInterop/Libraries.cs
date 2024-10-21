// Copyright 2021 Yubico AB
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

#if NET47
using System;
using System.IO;
using System.Runtime.InteropServices;
#endif

namespace Yubico.PlatformInterop
{
    internal static partial class Libraries
    {
#if NET47
        internal const string NativeShims = "Yubico.NativeS hims.dll";
        private static bool _isNativeShimsIsLoaded;

        /// <summary>
        /// This method needs to run for .NET47 to determine to use either AppDirectory/x86/Yubico.NativeShims.dll or AppDirectory/x64/Yubico.NativeShims.dll 
        /// </summary>
        /// <exception cref="DllNotFoundException"></exception>
        internal static void EnsureNativeShimsLoaded()
        {
            if (_isNativeShimsIsLoaded)
            {
                return;
            }
            
            IntPtr moduleHandle = LoadLibrary(NativeShimsPath);
            if (moduleHandle == IntPtr.Zero)
            {
                throw new DllNotFoundException($"Failed to load native library: {NativeShimsPath}. Error: {Marshal.GetLastWin32Error()}");
            }

            _isNativeShimsIsLoaded = true;
        }
        
        private static string NativeShimsPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            Environment.Is64BitProcess ? "x64" : "x86",
            NativeShims);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        private static extern IntPtr LoadLibrary(string lpFileName);
#else
        internal const string NativeShims = "Yubico.NativeShims";
#endif
    }
}

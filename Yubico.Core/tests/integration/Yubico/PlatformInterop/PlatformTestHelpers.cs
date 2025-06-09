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
using System.Runtime.InteropServices;
using Xunit;

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Helper class providing common platform-specific test functionality.
    /// </summary>
    internal static class PlatformTestHelpers
    {
        /// <summary>
        /// Skips a test if running on a platform other than Windows.
        /// </summary>
        public static void SkipIfNotWindows()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "This test only runs on Windows");
        }
        
        /// <summary>
        /// Skips a test if running on a platform other than Linux.
        /// </summary>
        public static void SkipIfNotLinux()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "This test only runs on Linux");
        }
        
        /// <summary>
        /// Skips a test if running on a platform other than macOS.
        /// </summary>
        public static void SkipIfNotMacOS()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), "This test only runs on macOS");
        }
        
        /// <summary>
        /// Gets the architecture-specific subdirectory name based on the current runtime architecture.
        /// </summary>
        public static string GetArchSpecificDirectoryName()
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new ArgumentOutOfRangeException($"Architecture {RuntimeInformation.OSArchitecture} is not supported!")
            };
        }
        
#if NET47
        /// <summary>
        /// Gets the expected path for a native library in .NET Framework 4.7 based on architecture.
        /// </summary>
        public static string GetNet47NativeLibraryPath(string libraryName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string archDir = GetArchSpecificDirectoryName();
            
            return Path.Combine(baseDir, archDir, libraryName + ".dll");
        }
#endif
    }
}

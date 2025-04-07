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
    /// Registry of native libraries that need to be tested across different platforms.
    /// This provides an extensible way to add more libraries in the future.
    /// </summary>
    internal static class NativeLibraryRegistry
    {
        /// <summary>
        /// Represents a native library with verification methods for each platform.
        /// </summary>
        internal class LibraryEntry
        {
            /// <summary>
            /// The name of the native library.
            /// </summary>
            public string LibraryName { get; }

            /// <summary>
            /// A method to verify the library functions correctly on Windows.
            /// </summary>
            public Func<(bool success, string message)> WindowsVerificationMethod { get; }

            /// <summary>
            /// A method to verify the library functions correctly on Linux.
            /// </summary>
            public Func<(bool success, string message)> LinuxVerificationMethod { get; }

            /// <summary>
            /// A method to verify the library functions correctly on macOS.
            /// </summary>
            public Func<(bool success, string message)> MacOSVerificationMethod { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="LibraryEntry"/> class.
            /// </summary>
            public LibraryEntry(
                string libraryName,
                Func<(bool success, string message)> windowsVerificationMethod,
                Func<(bool success, string message)> linuxVerificationMethod,
                Func<(bool success, string message)> macOSVerificationMethod)
            {
                LibraryName = libraryName;
                WindowsVerificationMethod = windowsVerificationMethod;
                LinuxVerificationMethod = linuxVerificationMethod;
                MacOSVerificationMethod = macOSVerificationMethod;
            }

            /// <summary>
            /// Verifies the library works correctly on the current platform.
            /// </summary>
            public (bool success, string message) VerifyOnCurrentPlatform()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return WindowsVerificationMethod();
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return LinuxVerificationMethod();
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return MacOSVerificationMethod();
                }

                return (false, $"Unsupported OS platform: {RuntimeInformation.OSDescription}");
            }
        }

        /// <summary>
        /// The registry of native libraries to be tested.
        /// </summary>
        private static readonly List<LibraryEntry> _libraries = new List<LibraryEntry>
        {
            // Yubico.NativeShims library
            new LibraryEntry(
                "Yubico.NativeShims",
                // Windows verification
                () =>
                {
                    try
                    {
                        // Verify platform
                        bool isWindows = SdkPlatformInfo.OperatingSystem == SdkPlatform.Windows;
                        if (!isWindows)
                        {
                            return (false, "SdkPlatformInfo.OperatingSystem did not indicate Windows platform");
                        }

                        // Actually call a native function to verify DLL loading
                        var bigNum = NativeMethods.BnNew();
                        if (bigNum == null || bigNum.IsInvalid)
                        {
                            return (false, "Failed to create BigNum instance on Windows");
                        }

                        // Clean up
                        bigNum.Dispose();

                        return (true, "Successfully called native library function on Windows");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Error accessing Windows native library: {ex.Message}");
                    }
                },
                // Linux verification
                () =>
                {
                    try
                    {
                        bool isLinux = SdkPlatformInfo.OperatingSystem == SdkPlatform.Linux;
                        if (!isLinux)
                        {
                            return (false, "SdkPlatformInfo.OperatingSystem did not indicate Linux platform");
                        }

                        // Actually call a native function to verify DLL loading
                        var bigNum = NativeMethods.BnNew();
                        if (bigNum == null || bigNum.IsInvalid)
                        {
                            return (false, "Failed to create BigNum instance on Linux");
                        }

                        // Clean up
                        bigNum.Dispose();

                        return (true, "Successfully called native library function on Linux");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Error accessing Linux native library: {ex.Message}");
                    }
                },
                // macOS verification
                () =>
                {
                    try
                    {
                        // Verify platform
                        bool isMacOS = SdkPlatformInfo.OperatingSystem == SdkPlatform.MacOS;

                        if (!isMacOS)
                        {
                            return (false, "SdkPlatformInfo.OperatingSystem did not indicate macOS platform");
                        }

                        // Actually call a native function to verify DLL loading
                        var bigNum = NativeMethods.BnNew();
                        if (bigNum == null || bigNum.IsInvalid)
                        {
                            return (false, "Failed to create BigNum instance on macOS");
                        }

                        // Clean up
                        bigNum.Dispose();

                        return (true, "Successfully called native library function on macOS");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Error accessing macOS native library: {ex.Message}");
                    }
                })

            // Add more libraries here in the future when needed
            // new LibraryEntry("AnotherLibrary", windowsVerification, linuxVerification, macosVerification)
        };

        /// <summary>
        /// Gets all registered libraries.
        /// </summary>
        public static IReadOnlyList<LibraryEntry> GetAllLibraries() => _libraries;
    }
}

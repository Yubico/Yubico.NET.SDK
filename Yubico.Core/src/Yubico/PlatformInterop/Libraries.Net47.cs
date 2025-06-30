// Copyright 2025 Yubico AB
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

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// .NET Framework 4.7 specific implementation for native library management.
    /// </summary>
    internal static partial class Libraries
    {
        /// <summary>
        /// The filename of the native shims library for .NET Framework 4.7.
        /// </summary>
        /// <remarks>
        /// For .NET Framework 4.7, the DLL must be placed in an architecture-specific subdirectory:
        /// - x86/Yubico.NativeShims.dll for 32-bit processes
        /// - x64/Yubico.NativeShims.dll for 64-bit processes
        /// The correct version is loaded at runtime based on the process architecture.
        /// </remarks>
        internal const string NativeShims = "Yubico.NativeShims.dll";

        /// <summary>
        /// Ensures the native library is properly loaded for .NET Framework 4.7.
        /// </summary>
        /// <exception cref="DllNotFoundException">
        /// Thrown when the native library cannot be loaded. This could be due to:
        /// - Missing DLL file in the architecture-specific directory (x86/x64)
        /// - Incorrect architecture (x86/x64 mismatch)
        /// - Missing dependencies
        /// - Insufficient permissions
        /// </exception>
        /// <remarks>
        /// This method must be called before any P/Invoke calls are made.
        /// The implementation details are handled in Libraries.Net47.cs.
        /// </remarks>
        public static void EnsureInitialized() => Net47Implementation.Initialize();
        
        /// <summary>
        /// Encapsulates the .NET Framework 4.7 specific implementation details for native library management.
        /// This nested class handles the dynamic loading of architecture-specific (x86/x64) native libraries.
        /// </summary>
        private static class Net47Implementation
        {
            // Handle to the loaded native library
            private static UnmanagedDynamicLibrary? _nativeShims;

            /// <summary>
            /// Gets the full path to the architecture-specific native library.
            /// </summary>
            /// <remarks>
            /// The path is constructed based on:
            /// - The application's base directory
            /// - The current process architecture (x86/x64)
            /// - The native library filename
            /// </remarks>
            private static string NativeShimsPath =>
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    RuntimeInformation.OSArchitecture switch
                    {
                        Architecture.X86 => "x86",
                        Architecture.X64 => "x64",
                        Architecture.Arm64 => "arm64",
                        var unsupportedArch  => throw new ArgumentOutOfRangeException($"Architecture {unsupportedArch } is not supported!")
                    },
                    NativeShims);
            
            /// <summary>
            /// Initializes the native library for the current architecture.
            /// </summary>
            /// <remarks>
            /// This method is called by the public EnsureInitialized method.
            /// It ensures the appropriate version (x86/x64) of the native library is loaded.
            /// </remarks>
            internal static void Initialize()
            {
                try
                {
                    EnsureNativeShimsLoaded();
                }
                catch (Exception ex)
                {
                    throw new DllNotFoundException(
                        $"Failed to load native library from {NativeShimsPath}. " +
                        $"Ensure the correct {(Environment.Is64BitProcess ? "x64" : "x86")} version is present.", 
                        ex);
                }
            }

            /// <summary>
            /// Loads the native library if it hasn't been loaded already.
            /// </summary>
            /// <exception cref="Exception">
            /// A variety of exceptions can be thrown during library loading, including but not limited to:
            /// - FileNotFoundException: If the DLL file is not found
            /// - BadImageFormatException: If the DLL is not compatible with the current architecture
            /// - Other exceptions based on the specific error condition encountered during loading
            /// </exception>
            private static void EnsureNativeShimsLoaded()
            {
                if (_nativeShims != null)
                {
                    return;
                }

                _nativeShims = UnmanagedDynamicLibrary.Open(NativeShimsPath);
            }
        }
    }
}
#endif

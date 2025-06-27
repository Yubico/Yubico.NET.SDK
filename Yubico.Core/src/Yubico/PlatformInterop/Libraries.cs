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

// As long as we have the Libraries.Net47.cs class which holds the opposite preprocessor directive check,
// this check is required - as having both at the same time is not possible.
#if !NET47 

namespace Yubico.PlatformInterop
{
    /// <summary>
    /// Handles the loading and management of native libraries required by the Yubico SDK.
    /// </summary>
    /// <remarks>
    /// This class provides cross-platform and cross-framework support for loading native libraries.
    /// The implementation differs based on the target framework:
    /// 
    /// For .NET Framework 4.7:
    /// - Native libraries must be placed in architecture-specific subdirectories (x86/x64)
    /// - Library loading is handled explicitly at runtime based on process architecture
    /// - Requires proper cleanup through the Cleanup method
    /// 
    /// For Modern .NET:
    /// - Native libraries are handled by the runtime's built-in library loading mechanism
    /// - Architecture-specific loading is managed automatically
    /// - No explicit cleanup is required
    /// </remarks>
    internal static partial class Libraries
    {
        /// <summary>
        /// The filename of the native shims library for modern .NET versions.
        /// </summary>
        /// <remarks>
        /// For modern .NET, the runtime automatically handles library loading and architecture selection.
        /// The DLL extension is omitted as it's platform-specific and managed by the runtime.
        /// The library should be properly packaged with the correct runtimes/* folder structure in the NuGet package.
        /// </remarks>
        internal const string NativeShims = "Yubico.NativeShims";

        /// <summary>
        /// No-op implementation for modern .NET versions.
        /// </summary>
        /// <remarks>
        /// Library loading is handled automatically by the runtime.
        /// This method exists only for API compatibility with .NET Framework 4.7 code.
        /// </remarks>
        public static void EnsureInitialized() { }
    }
}
#endif

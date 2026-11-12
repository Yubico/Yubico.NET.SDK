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

// Keep the existing target conditional; the active SDK targets modern .NET.

#if !NETFRAMEWORK

namespace Yubico.YubiKit.Core.Native;

/// <summary>
///     Names the native shim used by source-generated imports.
/// </summary>
/// <remarks>
///     Runtime/package resolution supplies the shared library for ordinary JIT deployment. Native AOT
///     package targets select a RID-specific static archive and system dependencies. This constant neither
///     loads a library nor proves the selected artifact's source, ABI, or device compatibility.
/// </remarks>
internal static class Libraries
{
    /// <summary>
    ///     The logical native shim name used by imports and package linking targets.
    /// </summary>
    /// <remarks>
    ///     The platform-specific filename suffix is omitted. Ordinary JIT deployment uses package runtime
    ///     assets; Native AOT targets bind this name to the selected static archive instead.
    /// </remarks>
    internal const string NativeShims = "Yubico.NativeShims";
}
#endif
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

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Yubico.YubiKit.Core.Native.Linux.Libc;

[SupportedOSPlatform("linux")]
public static class LibcHelpers
{
    /// <summary>
    /// Returns the native error message for the last failed P/Invoke. Call this immediately after
    /// the failing operation, before invoking other native or managed APIs.
    /// </summary>
    public static string GetErrnoString() =>
        Marshal.GetPInvokeErrorMessage(Marshal.GetLastPInvokeError());

    internal static string GetErrnoString(int errorCode) =>
        Marshal.GetPInvokeErrorMessage(errorCode);
}
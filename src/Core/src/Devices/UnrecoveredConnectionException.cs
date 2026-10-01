// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     The SDK could not prove that a previous native connection to this YubiKey was released.
/// </summary>
/// <remarks>
///     The physical-interface claim remains held to prevent a second native open from racing resources whose
///     ownership is unresolved. Restart the process after resolving the underlying reader or driver failure.
/// </remarks>
public sealed class UnrecoveredConnectionException : InvalidOperationException
{
    /// <summary>Creates an exception describing an unresolved native connection.</summary>
    public UnrecoveredConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
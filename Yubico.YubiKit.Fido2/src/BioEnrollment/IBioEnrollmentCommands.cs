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

namespace Yubico.YubiKit.Fido2.BioEnrollment;

/// <summary>
/// Interface for sending bio enrollment commands to the authenticator.
/// </summary>
/// <remarks>
/// This interface abstracts the CBOR communication for testability.
/// </remarks>
internal interface IBioEnrollmentCommands
{
    /// <summary>
    /// Sends a bio enrollment command to the authenticator.
    /// </summary>
    /// <param name="payload">The CBOR-encoded command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CBOR-encoded response data.</returns>
    Task<ReadOnlyMemory<byte>> SendBioEnrollmentCommandAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);
}

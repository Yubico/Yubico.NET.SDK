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

namespace Yubico.YubiKit.Fido2.Backend;

/// <summary>
/// Abstraction for FIDO transport backends (SmartCard CCID or FIDO HID).
/// </summary>
/// <remarks>
/// This allows FidoSession to be transport-agnostic while supporting both
/// SmartCard (CCID) and FIDO HID (CTAPHID) communication.
/// </remarks>
internal interface IFidoBackend : IDisposable
{
    /// <summary>
    /// Sends a CTAP CBOR command and receives the response.
    /// </summary>
    /// <param name="request">The serialized CTAP request (command byte + CBOR payload).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data (status byte has already been validated).</returns>
    Task<ReadOnlyMemory<byte>> SendCborAsync(
        ReadOnlyMemory<byte> request,
        CancellationToken cancellationToken = default);
}

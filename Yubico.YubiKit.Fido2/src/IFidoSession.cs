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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2;

/// <summary>
/// Interface for FIDO2/CTAP2 session operations on a YubiKey authenticator.
/// </summary>
/// <remarks>
/// <para>
/// Implements CTAP 2.1/2.3 specification.
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html
/// </para>
/// </remarks>
public interface IFidoSession : IApplicationSession
{
    /// <summary>
    /// Gets authenticator information. Always fetches fresh data from device.
    /// </summary>
    /// <remarks>
    /// Callers should cache the result if needed. The session does not cache InfoData
    /// to ensure fresh data is always returned.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticator information.</returns>
    Task<AuthenticatorInfo> GetInfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Requests the user to select this authenticator by touching the device.
    /// </summary>
    /// <remarks>
    /// This command is useful when multiple authenticators are present and the user
    /// needs to indicate which one they want to use.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SelectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the FIDO application to factory defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This operation requires user presence (touch) within 5 seconds of device insertion.
    /// All credentials and settings will be permanently deleted.
    /// </para>
    /// <para>
    /// WARNING: This is a destructive operation that cannot be undone.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="CtapException">
    /// Thrown with <see cref="CtapStatus.NotAllowed"/> if reset is attempted too long after
    /// device insertion.
    /// </exception>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

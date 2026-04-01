// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Session interface for the YubiOTP application on a YubiKey.
/// Provides operations for slot configuration, challenge-response, and NDEF setup.
/// </summary>
public interface IYubiOtpSession : IApplicationSession
{
    /// <summary>
    /// Reads the device serial number.
    /// </summary>
    Task<int> GetSerialAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current slot configuration state (which slots are programmed, touch-triggered).
    /// </summary>
    ConfigState GetConfigState();

    /// <summary>
    /// Writes a new slot configuration to the specified slot.
    /// </summary>
    /// <param name="slot">The slot to program (One or Two).</param>
    /// <param name="config">The slot configuration to write.</param>
    /// <param name="accessCode">Optional 6-byte access code to set on the slot.</param>
    /// <param name="currentAccessCode">Optional current 6-byte access code if the slot is protected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutConfigurationAsync(
        Slot slot,
        SlotConfiguration config,
        ReadOnlyMemory<byte> accessCode = default,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates behavioral flags on an existing slot without reprogramming key material.
    /// The slot must have been originally programmed with <see cref="ExtendedFlag.AllowUpdate"/> enabled.
    /// </summary>
    /// <param name="slot">The slot to update (One or Two).</param>
    /// <param name="config">The update configuration containing the flags to modify.</param>
    /// <param name="accessCode">Optional 6-byte access code to set on the slot.</param>
    /// <param name="currentAccessCode">Optional current 6-byte access code if the slot is protected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateConfigurationAsync(
        Slot slot,
        UpdateConfiguration config,
        ReadOnlyMemory<byte> accessCode = default,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Swaps the configurations of slot 1 and slot 2.
    /// </summary>
    Task SwapSlotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the configuration of the specified slot by writing zeros.
    /// </summary>
    /// <param name="slot">The slot to delete.</param>
    /// <param name="currentAccessCode">Optional current 6-byte access code if the slot is protected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSlotAsync(
        Slot slot,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a custom keyboard scan code map.
    /// </summary>
    /// <param name="scanMap">The 38-byte scan code map.</param>
    /// <param name="currentAccessCode">Optional current 6-byte access code if the slot is protected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetScanMapAsync(
        ReadOnlyMemory<byte> scanMap,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures NFC NDEF for a slot. When the YubiKey is tapped via NFC,
    /// it will present the configured URI or text record.
    /// </summary>
    /// <param name="slot">The slot to configure NDEF for.</param>
    /// <param name="uri">The URI or text content. If null, NDEF is disabled for the slot.</param>
    /// <param name="currentAccessCode">Optional current 6-byte access code if the slot is protected.</param>
    /// <param name="ndefType">The type of NDEF record (URI or Text).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetNdefConfigurationAsync(
        Slot slot,
        string? uri = null,
        ReadOnlyMemory<byte> currentAccessCode = default,
        NdefType ndefType = NdefType.Uri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an HMAC-SHA1 challenge-response operation on the specified slot.
    /// </summary>
    /// <param name="slot">The slot configured for HMAC-SHA1.</param>
    /// <param name="challenge">The challenge data (up to 64 bytes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The 20-byte HMAC-SHA1 response.</returns>
    Task<ReadOnlyMemory<byte>> CalculateHmacSha1Async(
        Slot slot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default);
}

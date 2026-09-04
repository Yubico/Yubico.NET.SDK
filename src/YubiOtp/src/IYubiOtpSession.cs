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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;

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
    /// <exception cref="NotSupportedException">
    /// Thrown when the device firmware does not support serial number reads (requires 2.2.0+).
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short serial number response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    Task<int> GetSerialNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current slot configuration state (which slots are programmed, touch-triggered).
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    ConfigState GetConfigState();

    /// <summary>
    /// Writes a new slot configuration to the specified slot.
    /// </summary>
    /// <param name="slot">The slot to program (One or Two).</param>
    /// <param name="config">The slot configuration to write.</param>
    /// <param name="accessCode">Optional 6-byte access code to set on the slot.</param>
    /// <param name="currentAccessCode">Optional current 6-byte access code if the slot is protected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="accessCode"/> or <paramref name="currentAccessCode"/> is
    /// non-empty and not exactly 6 bytes. No device I/O is performed when this is thrown.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slot"/> is not a defined value.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="config"/> requires a firmware version newer than the device has.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown (SmartCard transport only) when the device's programming-sequence counter does not
    /// advance as expected after the write, indicating the YubiKey rejected the configuration.
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short status response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="accessCode"/> or <paramref name="currentAccessCode"/> is
    /// non-empty and not exactly 6 bytes. No device I/O is performed when this is thrown.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slot"/> is not a defined value.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the device firmware does not support slot updates (requires 2.3.0+), or when
    /// <paramref name="config"/> requires a firmware version newer than the device has.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown (SmartCard transport only) when the device's programming-sequence counter does not
    /// advance as expected after the write, indicating the YubiKey rejected the configuration.
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short status response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    Task UpdateConfigurationAsync(
        Slot slot,
        UpdateConfiguration config,
        ReadOnlyMemory<byte> accessCode = default,
        ReadOnlyMemory<byte> currentAccessCode = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Swaps the configurations of slot 1 and slot 2.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when the device firmware does not support slot swapping (requires 2.3.0+).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown (SmartCard transport only) when the device's programming-sequence counter does not
    /// advance as expected after the write, indicating the YubiKey rejected the swap.
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short status response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    Task SwapSlotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the configuration of the specified slot by writing zeros.
    /// </summary>
    /// <param name="slot">The slot to delete.</param>
    /// <param name="currentAccessCode">Optional current 6-byte access code if the slot is protected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="currentAccessCode"/> is non-empty and not exactly 6 bytes.
    /// No device I/O is performed when this is thrown.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slot"/> is not a defined value.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown (SmartCard transport only) when the device's programming-sequence counter does not
    /// advance as expected after the write, indicating the YubiKey rejected the deletion.
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short status response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="scanMap"/> is not exactly 38 bytes, or
    /// <paramref name="currentAccessCode"/> is non-empty and not exactly 6 bytes. No device I/O
    /// is performed when this is thrown.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown (SmartCard transport only) when the device's programming-sequence counter does not
    /// advance as expected after the write, indicating the YubiKey rejected the scan map.
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short status response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="uri"/> (after URI prefix compression, or with the text
    /// language header) exceeds the maximum NDEF data size, or <paramref name="currentAccessCode"/>
    /// is non-empty and not exactly 6 bytes. No device I/O is performed when this is thrown.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slot"/> is not a defined value.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the device firmware does not support NDEF configuration (requires 3.0.0+).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown (SmartCard transport only) when the device's programming-sequence counter does not
    /// advance as expected after the write, indicating the YubiKey rejected the configuration.
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short status response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="challenge"/> is more than 64 bytes. No device I/O is
    /// performed when this is thrown.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slot"/> is not a defined value.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the device firmware does not support HMAC-SHA1 challenge-response (requires 2.2.0+).
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    Task<ReadOnlyMemory<byte>> CalculateHmacSha1Async(
        Slot slot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a Yubico OTP (AES-128) challenge-response operation on the specified slot.
    /// </summary>
    /// <param name="slot">The slot configured for Yubico OTP challenge-response.</param>
    /// <param name="challenge">The challenge data, which must be exactly 6 bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The 16-byte AES-128 response.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="challenge"/> is not exactly 6 bytes. No device I/O is
    /// performed when this is thrown.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slot"/> is not a defined value.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the device firmware does not support Yubico OTP challenge-response (requires 2.2.0+).
    /// </exception>
    /// <exception cref="BadResponseException">
    /// Thrown when the device returns a malformed or too-short response.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed.</exception>
    Task<ReadOnlyMemory<byte>> CalculateYubicoOtpAsync(
        Slot slot,
        ReadOnlyMemory<byte> challenge,
        CancellationToken cancellationToken = default);
}

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

using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <inheritdoc />
    public async Task VerifyPinAsync(
        ReadOnlyMemory<byte> pinUtf8,
        bool extended = false,
        CancellationToken cancellationToken = default)
    {
        // P2: 0x81 (Pw.User) = signature verification only (per spec)
        //     0x82 (Pw.Reset) = extended mode (decrypt/authenticate/attest, NOT sign)
        // Matches ykman canonical: pw + mode where mode = 1 if extended else 0
        var p2 = extended ? (byte)Pw.Reset : (byte)Pw.User;
        var pw = extended ? Pw.Reset : Pw.User;

        _logger.LogDebug("Verifying User PIN (P2=0x{P2:X2})", p2);
        await VerifyPwAsync(pw, p2, pinUtf8, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task VerifyAdminAsync(
        ReadOnlyMemory<byte> pinUtf8,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Verifying Admin PIN");
        await VerifyPwAsync(Pw.Admin, (byte)Pw.Admin, pinUtf8, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UnverifyPinAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureUnverify);

        // Unverify is VERIFY with empty data and P2=0x81 or 0x82
        // Sending empty VERIFY for both P2 values clears verification state
        _logger.LogDebug("Unverifying User PIN");

        var commandSig = new ApduCommand(0x00, (int)Ins.Verify, 0xFF, (int)Pw.User);
        await TransmitAsync(commandSig, cancellationToken).ConfigureAwait(false);

        var commandOther = new ApduCommand(0x00, (int)Ins.Verify, 0xFF, (int)Pw.Reset);
        await TransmitAsync(commandOther, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ChangePinAsync(
        ReadOnlyMemory<byte> currentPinUtf8,
        ReadOnlyMemory<byte> newPinUtf8,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Changing User PIN");
        await ChangePwAsync(Pw.User, currentPinUtf8, newPinUtf8, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ChangeAdminAsync(
        ReadOnlyMemory<byte> currentPinUtf8,
        ReadOnlyMemory<byte> newPinUtf8,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Changing Admin PIN");
        await ChangePwAsync(Pw.Admin, currentPinUtf8, newPinUtf8, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetResetCodeAsync(
        ReadOnlyMemory<byte> resetCodeUtf8,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting Reset Code");

        byte[]? derivedBytes = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            derivedBytes = kdf.Process(Pw.Reset, resetCodeUtf8.Span);

            await PutDataAsync(DataObject.ResettingCode, derivedBytes, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (derivedBytes is not null)
            {
                CryptographicOperations.ZeroMemory(derivedBytes);
            }
        }
    }

    /// <inheritdoc />
    public async Task ResetPinAsync(
        ReadOnlyMemory<byte> resetCodeUtf8,
        ReadOnlyMemory<byte> newPinUtf8,
        bool useAdmin = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resetting User PIN (useAdmin={UseAdmin})", useAdmin);

        byte[]? resetBytes = null;
        byte[]? newPinBytes = null;
        byte[]? data = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            newPinBytes = kdf.Process(Pw.User, newPinUtf8.Span);

            // P1=0x02 (admin): Admin PIN (PW3) must have been verified beforehand
            //   by the caller via VerifyAdminAsync. Data = new PIN only.
            // P1=0x00 (reset code): data = resetCode + newPin concatenated.
            var p1 = useAdmin ? (byte)0x02 : (byte)0x00;

            if (useAdmin)
            {
                data = newPinBytes;
            }
            else
            {
                resetBytes = kdf.Process(Pw.Reset, resetCodeUtf8.Span);
                data = new byte[resetBytes.Length + newPinBytes.Length];
                resetBytes.CopyTo(data.AsSpan());
                newPinBytes.CopyTo(data.AsSpan(resetBytes.Length));
            }

            var command = new ApduCommand(0x00, (int)Ins.ResetRetryCounter, p1, (int)Pw.User, data);

            await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (resetBytes is not null)
                CryptographicOperations.ZeroMemory(resetBytes);
            if (newPinBytes is not null)
                CryptographicOperations.ZeroMemory(newPinBytes);
            // Only zero 'data' separately if it's the combined buffer (not same ref as newPinBytes)
            if (data is not null && !ReferenceEquals(data, newPinBytes))
                CryptographicOperations.ZeroMemory(data);
        }
    }

    /// <inheritdoc />
    public async Task SetPinAttemptsAsync(
        int userAttempts,
        int resetAttempts,
        int adminAttempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userAttempts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resetAttempts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(adminAttempts);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(userAttempts, 255);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(resetAttempts, 255);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(adminAttempts, 255);

        _logger.LogDebug(
            "Setting PIN attempts (user={User}, reset={Reset}, admin={Admin})",
            userAttempts, resetAttempts, adminAttempts);

        var command = new ApduCommand(
            0x00,
            (int)Ins.SetPinRetries,
            0x00,
            0x00,
            new byte[] { (byte)userAttempts, (byte)resetAttempts, (byte)adminAttempts });

        await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetSignaturePinPolicyAsync(
        PinPolicy policy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting signature PIN policy to {Policy}", policy);

        // PW Status Bytes (DO C4): only byte 0 (policy) is writable via PUT DATA
        await PutDataAsync(DataObject.PwStatusBytes, new byte[] { (byte)policy }, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Private Helpers ───────────────────────────────────────────────

    private async Task VerifyPwAsync(
        Pw pw,
        byte p2,
        ReadOnlyMemory<byte> pinUtf8,
        CancellationToken cancellationToken)
    {
        byte[]? derivedBytes = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            derivedBytes = kdf.Process(pw, pinUtf8.Span);

            var command = new ApduCommand(0x00, (int)Ins.Verify, 0x00, p2, derivedBytes);
            var response = await TransmitNoThrowAsync(command, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsOK())
            {
                // Standard wrong-PIN SW: 0x63Cx where x = remaining retries
                var remaining = SWConstants.ExtractRetryCount(response.SW) ?? -1;

                // Some firmware (including 5.8.0-alpha) returns 0x6982 (Security Status
                // Not Satisfied) for wrong PIN instead of 0x63Cx. Per Python canonical:
                // query GET DATA for PW_STATUS_BYTES to get remaining attempts.
                if (remaining < 0 && response.SW == SWConstants.SecurityStatusNotSatisfied)
                {
                    try
                    {
                        var status = await GetPinStatusAsync(cancellationToken).ConfigureAwait(false);
                        remaining = pw == Pw.User
                            ? status.AttemptsUser
                            : pw == Pw.Admin
                                ? status.AttemptsAdmin
                                : status.AttemptsReset;
                    }
                    catch
                    {
                        // If status query fails, fall back to no-retry message
                    }
                }

                throw new ApduException(
                    remaining >= 0
                        ? $"PIN verification failed ({remaining} attempts remaining)"
                        : "PIN verification failed")
                {
                    SW = response.SW,
                };
            }
        }
        finally
        {
            if (derivedBytes is not null)
            {
                CryptographicOperations.ZeroMemory(derivedBytes);
            }
        }
    }

    private async Task ChangePwAsync(
        Pw pw,
        ReadOnlyMemory<byte> currentPinUtf8,
        ReadOnlyMemory<byte> newPinUtf8,
        CancellationToken cancellationToken)
    {
        byte[]? currentBytes = null;
        byte[]? newBytes = null;
        byte[]? combined = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            currentBytes = kdf.Process(pw, currentPinUtf8.Span);
            newBytes = kdf.Process(pw, newPinUtf8.Span);

            combined = new byte[currentBytes.Length + newBytes.Length];
            currentBytes.CopyTo(combined.AsSpan());
            newBytes.CopyTo(combined.AsSpan(currentBytes.Length));

            var p2 = pw switch
            {
                Pw.User => (byte)Pw.User,
                Pw.Admin => (byte)Pw.Admin,
                _ => throw new ArgumentOutOfRangeException(nameof(pw)),
            };

            var command = new ApduCommand(0x00, (int)Ins.ChangePin, 0x00, p2, combined);
            await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (currentBytes is not null)
                CryptographicOperations.ZeroMemory(currentBytes);
            if (newBytes is not null)
                CryptographicOperations.ZeroMemory(newBytes);
            if (combined is not null)
                CryptographicOperations.ZeroMemory(combined);
        }
    }
}
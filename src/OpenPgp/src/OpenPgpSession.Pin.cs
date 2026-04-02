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
using System.Text;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <inheritdoc />
    public async Task VerifyPinAsync(
        string pin,
        bool extended = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pin);

        // P2: 0x81 (Pw.User) = signature verification only (per spec)
        //     0x82 (Pw.Reset) = extended mode (decrypt/authenticate/attest, NOT sign)
        // Matches ykman canonical: pw + mode where mode = 1 if extended else 0
        var p2 = extended ? (byte)Pw.Reset : (byte)Pw.User;
        var pw = extended ? Pw.Reset : Pw.User;

        _logger.LogDebug("Verifying User PIN (P2=0x{P2:X2})", p2);
        await VerifyPwAsync(pw, p2, pin, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task VerifyAdminAsync(
        string pin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pin);

        _logger.LogDebug("Verifying Admin PIN");
        await VerifyPwAsync(Pw.Admin, (byte)Pw.Admin, pin, cancellationToken)
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
        string currentPin,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentPin);
        ArgumentNullException.ThrowIfNull(newPin);

        _logger.LogDebug("Changing User PIN");
        await ChangePwAsync(Pw.User, currentPin, newPin, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ChangeAdminAsync(
        string currentPin,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentPin);
        ArgumentNullException.ThrowIfNull(newPin);

        _logger.LogDebug("Changing Admin PIN");
        await ChangePwAsync(Pw.Admin, currentPin, newPin, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetResetCodeAsync(
        string resetCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resetCode);

        _logger.LogDebug("Setting Reset Code");

        byte[]? derivedBytes = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            derivedBytes = kdf.Process(Pw.Reset, resetCode);

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
        string resetCode,
        string newPin,
        bool useAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resetCode);
        ArgumentNullException.ThrowIfNull(newPin);

        _logger.LogDebug("Resetting User PIN (useAdmin={UseAdmin})", useAdmin);

        byte[]? resetBytes = null;
        byte[]? newPinBytes = null;
        byte[]? combined = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);

            resetBytes = useAdmin
                ? kdf.Process(Pw.Admin, resetCode)
                : kdf.Process(Pw.Reset, resetCode);
            newPinBytes = kdf.Process(Pw.User, newPin);

            combined = new byte[resetBytes.Length + newPinBytes.Length];
            resetBytes.CopyTo(combined.AsSpan());
            newPinBytes.CopyTo(combined.AsSpan(resetBytes.Length));

            // P1: 0x00 = reset code, 0x02 = admin PIN
            var p1 = useAdmin ? (byte)0x02 : (byte)0x00;
            var command = new ApduCommand(0x00, (int)Ins.ResetRetryCounter, p1, (int)Pw.User, combined);

            await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (resetBytes is not null)
                CryptographicOperations.ZeroMemory(resetBytes);
            if (newPinBytes is not null)
                CryptographicOperations.ZeroMemory(newPinBytes);
            if (combined is not null)
                CryptographicOperations.ZeroMemory(combined);
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
        string pin,
        CancellationToken cancellationToken)
    {
        byte[]? derivedBytes = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            derivedBytes = kdf.Process(pw, pin);

            var command = new ApduCommand(0x00, (int)Ins.Verify, 0x00, p2, derivedBytes);
            var response = await TransmitNoThrowAsync(command, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsOK())
            {
                // Standard wrong-PIN SW: 0x63Cx where x = remaining retries
                var remaining = (response.SW & 0xFF00) == 0x63C0
                    ? response.SW & 0x0F
                    : -1;

                // Some firmware (including 5.8.0-alpha) returns 0x6982 (Security Status
                // Not Satisfied) for wrong PIN instead of 0x63Cx. Per Python canonical:
                // query GET DATA for PW_STATUS_BYTES to get remaining attempts.
                if (remaining < 0 && response.SW == SWConstants.SecurityStatusNotSatisfied)
                {
                    try
                    {
                        var status = await GetPinStatusAsync(cancellationToken).ConfigureAwait(false);
                        remaining = (Pw)pw == Pw.User
                            ? status.AttemptsUser
                            : (Pw)pw == Pw.Admin
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
        string currentPin,
        string newPin,
        CancellationToken cancellationToken)
    {
        byte[]? currentBytes = null;
        byte[]? newBytes = null;
        byte[]? combined = null;
        try
        {
            var kdf = await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
            currentBytes = kdf.Process(pw, currentPin);
            newBytes = kdf.Process(pw, newPin);

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
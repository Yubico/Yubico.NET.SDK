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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <inheritdoc />
    public async Task VerifyPinAsync(
        ReadOnlyMemory<byte> pin,
        bool extended = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Card §7.2.2 uses P2=81/82 for two access states of the same User PIN.
        byte p2 = extended ? (byte)0x82 : (byte)Pw.User;

        _logger.LogDebug("Verifying User PIN (P2=0x{P2:X2})", p2);
        await VerifyPwAsync(Pw.User, p2, pin, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task VerifyAdminAsync(
        ReadOnlyMemory<byte> pin,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Verifying Admin PIN");
        await VerifyPwAsync(Pw.Admin, (byte)Pw.Admin, pin, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UnverifyPinAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Changing User PIN");
        await ChangePwAsync(Pw.User, currentPin, newPin, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ChangeAdminAsync(
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Changing Admin PIN");
        await ChangePwAsync(Pw.Admin, currentPin, newPin, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetResetCodeAsync(
        ReadOnlyMemory<byte> resetCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Setting Reset Code");

        byte[]? derivedBytes = null;
        try
        {
            using var kdf = await ReadKdfAsync(cancellationToken).ConfigureAwait(false);
            derivedBytes = EncodeCredential(kdf, Pw.Reset, resetCode, nameof(resetCode));

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
    public async Task ResetPinUsingResetCodeAsync(
        ReadOnlyMemory<byte> resetCode,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        await ResetPinCoreAsync(resetCode, newPin, useAdmin: false, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ResetPinUsingAdminAuthenticationAsync(
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken = default)
    {
        await ResetPinCoreAsync(ReadOnlyMemory<byte>.Empty, newPin, useAdmin: true, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ResetPinCoreAsync(
        ReadOnlyMemory<byte> resetCode,
        ReadOnlyMemory<byte> newPin,
        bool useAdmin,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Resetting User PIN (useAdmin={UseAdmin})", useAdmin);

        byte[]? resetBytes = null;
        byte[]? newPinBytes = null;
        byte[]? data = null;
        try
        {
            using var kdf = await ReadKdfAsync(cancellationToken).ConfigureAwait(false);
            newPinBytes = EncodeCredential(kdf, Pw.User, newPin, nameof(newPin));

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
                resetBytes = EncodeCredential(kdf, Pw.Reset, resetCode, nameof(resetCode));
                data = new byte[resetBytes.Length + newPinBytes.Length];
                resetBytes.CopyTo(data.AsSpan());
                newPinBytes.CopyTo(data.AsSpan(resetBytes.Length));
            }

            var command = new ApduCommand(0x00, (int)Ins.ResetRetryCounter, p1, (int)Pw.User, data);

            if (useAdmin)
                await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
            else
                await TransmitCredentialAsync(command, Pw.Reset, cancellationToken).ConfigureAwait(false);
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
        ThrowIfDisposed();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userAttempts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resetAttempts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(adminAttempts);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(userAttempts, 255);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(resetAttempts, 255);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(adminAttempts, 255);

        if (_pinAttemptsVersion is { IsAlphaOrBeta: false } version &&
            (version.Major == 1 ? version.IsLessThan(1, 0, 7) : version.IsLessThan(4, 3, 1)))
            throw new NotSupportedException("Changing OpenPGP PIN attempts is not supported on this firmware.");

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
        ThrowIfDisposed();
        if (policy is not (PinPolicy.Once or PinPolicy.Always))
            throw new ArgumentOutOfRangeException(nameof(policy));

        _logger.LogDebug("Setting signature PIN policy to {Policy}", policy);

        // PW Status Bytes (DO C4): only byte 0 (policy) is writable via PUT DATA
        await PutDataAsync(DataObject.PwStatusBytes, new byte[] { (byte)policy }, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Private Helpers ───────────────────────────────────────────────

    private async Task VerifyPwAsync(
        Pw pw,
        byte p2,
        ReadOnlyMemory<byte> pin,
        CancellationToken cancellationToken)
    {
        byte[]? derivedBytes = null;
        try
        {
            using var kdf = await ReadKdfAsync(cancellationToken).ConfigureAwait(false);
            derivedBytes = EncodeCredential(kdf, pw, pin, nameof(pin));

            var command = new ApduCommand(0x00, (int)Ins.Verify, 0x00, p2, derivedBytes);
            await TransmitCredentialAsync(command, pw, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (derivedBytes is not null)
            {
                CryptographicOperations.ZeroMemory(derivedBytes);
            }
        }
    }

    private async Task TransmitCredentialAsync(
        ApduCommand command,
        Pw credential,
        CancellationToken cancellationToken)
    {
        var response = await TransmitNoThrowAsync(command, cancellationToken).ConfigureAwait(false);
        if (response.IsOK())
            return;

        // Family-specific status interpretations require a known production version.
        bool legacy = _numericVersion is { IsAlphaOrBeta: false } version && version.IsLessThan(4, 0, 0);
        int? remaining = (response.SW, (Ins)command.Ins) switch
        {
            (SWConstants.AuthenticationMethodBlocked, _) => 0,
            (SWConstants.InvalidCommandDataParameter, Ins.ResetRetryCounter) => 0,
            (SWConstants.SecurityStatusNotSatisfied, _) =>
                await TryReadRetryCountAsync(credential, cancellationToken).ConfigureAwait(false),
            (SWConstants.ConditionsNotSatisfied, Ins.ChangePin) when legacy =>
                await TryReadRetryCountAsync(credential, cancellationToken).ConfigureAwait(false),
            (var sw, Ins.Verify) when legacy && SWConstants.ExtractRetryCount(sw) is { } count => count,
            _ => throw ApduException.FromResponse(response, command, "APDU command failed"),
        };

        throw new OpenPgpInvalidPinException(
            remaining,
            remaining is { } retries
                ? $"{credential} credential command failed ({retries} attempt(s) remaining)"
                : $"{credential} credential command failed (attempts remaining unknown)")
        {
            SW = response.SW,
            Cla = command.Cla,
            Ins = command.Ins,
            P1 = command.P1,
            P2 = command.P2,
        };
    }

    private async Task<int?> TryReadRetryCountAsync(Pw credential, CancellationToken cancellationToken)
    {
        // 6982 can also mean unverified PW or a secure-messaging problem (Card §7.9).
        // C4 only supplies the count; it cannot establish the cause of the original failure.
        try
        {
            var status = await GetPinStatusAsync(cancellationToken).ConfigureAwait(false);
            return status.GetAttempts(credential);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }

    private async Task ChangePwAsync(
        Pw pw,
        ReadOnlyMemory<byte> currentPin,
        ReadOnlyMemory<byte> newPin,
        CancellationToken cancellationToken)
    {
        byte[]? currentBytes = null;
        byte[]? newBytes = null;
        byte[]? combined = null;
        try
        {
            using var kdf = await ReadKdfAsync(cancellationToken).ConfigureAwait(false);
            currentBytes = EncodeCredential(kdf, pw, currentPin, nameof(currentPin));
            newBytes = EncodeCredential(kdf, pw, newPin, nameof(newPin));

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
            await TransmitCredentialAsync(command, pw, cancellationToken).ConfigureAwait(false);
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

    private byte[] EncodeCredential(Kdf kdf, Pw credential, ReadOnlyMemory<byte> input, string parameterName)
    {
        var status = _appData.Discretionary.PwStatus;
        bool usesPinBlock = credential == Pw.Admin ? status.AdminUsesPinBlock2 : status.UserUsesPinBlock2;
        if (usesPinBlock)
            throw new NotSupportedException("OpenPGP credential commands support UTF-8 and KDF, not PIN block format 2.");

        int minimum = credential == Pw.User ? 6 : 8;
        int maximum = status.GetMaxLen(credential);
        var encoded = kdf.Process(credential, input.Span);
        if (encoded.Length >= minimum && encoded.Length <= maximum)
            return encoded;

        CryptographicOperations.ZeroMemory(encoded);
        throw new ArgumentException($"The encoded {credential} credential must be between {minimum} and {maximum} bytes.", parameterName);
    }
}
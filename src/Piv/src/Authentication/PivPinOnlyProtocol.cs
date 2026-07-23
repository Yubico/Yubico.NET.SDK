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

using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backend;
using Yubico.YubiKit.Piv.DataObjects;
using Yubico.YubiKit.Piv.Metadata;

namespace Yubico.YubiKit.Piv.Authentication;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

/// <summary>
/// Implements PIN-only management-key mode: detecting, recovering, enabling, and disabling
/// PIN-protected and PIN-derived management keys.
/// </summary>
internal static class PivPinOnlyProtocol
{
    private const int PinProtectedTag = 0x88;
    private const int ManagementKeyTag = 0x89;
    private const int SaltLength = 16;
    private const int Pbkdf2Iterations = 10_000;
    private static readonly int[] ValidManagementKeyLengths = [16, 24, 32];

    internal static async Task<PivPinOnlyMode> GetPinOnlyModeAsync(
        IPivBackend backend,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Getting PIN-only mode from ADMIN DATA");

        var raw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.AdminData, cancellationToken).ConfigureAwait(false);

        if (!PivAdminData.TryDecode(raw, out var adminData))
        {
            logger.LogDebug("PIV: ADMIN DATA is not valid PIN-only data; treating both modes as unavailable");
            return PivPinOnlyMode.PinProtectedUnavailable | PivPinOnlyMode.PinDerivedUnavailable;
        }

        var mode = PivPinOnlyMode.None;
        if (adminData.PinProtected)
        {
            mode |= PivPinOnlyMode.PinProtected;
        }
        if (adminData.Salt is not null)
        {
            mode |= PivPinOnlyMode.PinDerived;
        }

        return mode;
    }

    internal static async Task<PivPinOnlyMode> RecoverPinOnlyModeAsync(
        IPivBackend backend,
        ILogger logger,
        PivManagementKeyType managementKeyType,
        ReadOnlyMemory<byte> pin,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> authenticateAsync,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyPinAsync,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Attempting to recover PIN-only management key authentication");

        var mode = PivPinOnlyMode.None;
        bool pinVerified = false;

        // Try PIN-protected: the management key is stored, in the clear, in the PRINTED object.
        ReadOnlyMemory<byte> printedRaw;
        try
        {
            printedRaw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.PrintedInformation, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ApduException ex) when (ex.SW == SWConstants.SecurityStatusNotSatisfied)
        {
            await verifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
            pinVerified = true;
            printedRaw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.PrintedInformation, cancellationToken)
                .ConfigureAwait(false);
        }

        Memory<byte> storedKey = Memory<byte>.Empty;
        bool pinProtectedAuthenticated = false;
        try
        {
            if (TryDecodePinProtectedManagementKey(MemoryMarshal.AsMemory(printedRaw).Span, out storedKey))
            {
                try
                {
                    await authenticateAsync(storedKey, cancellationToken).ConfigureAwait(false);
                    mode |= PivPinOnlyMode.PinProtected;
                    pinProtectedAuthenticated = true;
                    logger.LogDebug("PIV: PIN-protected management key authenticated");
                }
                catch (ApduException)
                {
                    logger.LogDebug("PIV: Stored PIN-protected management key did not authenticate");
                }
            }

            // Try PIN-derived: derive a candidate management key from the PIN and the ADMIN DATA salt.
            var adminRaw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.AdminData, cancellationToken).ConfigureAwait(false);

            if (PivAdminData.TryDecode(adminRaw, out var adminData) && adminData.Salt is { } salt)
            {
                int keyLength = GetManagementKeyLength(managementKeyType);
                byte[] derived = ArrayPool<byte>.Shared.Rent(keyLength);
                try
                {
                    // PIN must be verified before it is used as key-derivation input. Reuse verification
                    // performed to unlock a protected PRINTED object rather than consuming another retry.
                    if (!pinVerified)
                    {
                        await verifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
                        pinVerified = true;
                    }
                    DeriveManagementKey(pin.Span, salt.Span, derived.AsSpan(0, keyLength));

                    try
                    {
                        await authenticateAsync(derived.AsMemory(0, keyLength), cancellationToken).ConfigureAwait(false);
                        mode |= PivPinOnlyMode.PinDerived;
                        logger.LogDebug("PIV: PIN-derived management key authenticated");
                    }
                    catch (ApduException)
                    {
                        logger.LogDebug("PIV: Derived management key did not authenticate; ADMIN DATA salt is stale");

                        if (pinProtectedAuthenticated)
                        {
                            try
                            {
                                await authenticateAsync(storedKey, cancellationToken).ConfigureAwait(false);
                                logger.LogDebug("PIV: Restored PIN-protected management key authentication");
                            }
                            catch (ApduException)
                            {
                                mode &= ~PivPinOnlyMode.PinProtected;
                                logger.LogDebug("PIV: Failed to restore PIN-protected management key authentication");
                            }
                        }
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(derived.AsSpan(0, keyLength));
                    ArrayPool<byte>.Shared.Return(derived);
                }
            }

            return mode;
        }
        finally
        {
            if (!storedKey.IsEmpty)
            {
                CryptographicOperations.ZeroMemory(storedKey.Span);
            }
        }
    }

    internal static async Task SetPinOnlyModeAsync(
        IPivBackend backend,
        ILogger logger,
        bool isAuthenticated,
        PivManagementKeyType managementKeyType,
        PivPinOnlyMode pinOnlyMode,
        ReadOnlyMemory<byte> pin,
        ReadOnlyMemory<byte>? managementKey,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> authenticateAsync,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> verifyPinAsync,
        Func<PivManagementKeyType, ReadOnlyMemory<byte>, bool, CancellationToken, Task> setManagementKeyAsync,
        CancellationToken cancellationToken = default)
    {
        if (!isAuthenticated)
        {
            throw new InvalidOperationException("Management key authentication is required to change PIN-only mode.");
        }

        if ((pinOnlyMode & ~(PivPinOnlyMode.None | PivPinOnlyMode.PinProtected)) != 0)
        {
            throw new ArgumentException(
                "Only PivPinOnlyMode.None (disable) and PivPinOnlyMode.PinProtected (enable) are " +
                "supported. PIN-derived management keys are deprecated and cannot be enabled through " +
                "this API.",
                nameof(pinOnlyMode));
        }

        if (pinOnlyMode == PivPinOnlyMode.PinProtected)
        {
            if (managementKey is not { } keyToProtect)
            {
                throw new ArgumentNullException(nameof(managementKey), "A management key is required to enable PIN-protected mode.");
            }

            int expectedKeyLength = GetManagementKeyLength(managementKeyType);
            if (keyToProtect.Length != expectedKeyLength)
            {
                throw new ArgumentException(
                    $"Invalid management key length {keyToProtect.Length} for {managementKeyType}. Expected {expectedKeyLength} bytes.",
                    nameof(managementKey));
            }

            logger.LogDebug("PIV: Enabling PIN-protected management key mode");

            await authenticateAsync(keyToProtect, cancellationToken).ConfigureAwait(false);
            await verifyPinAsync(pin, cancellationToken).ConfigureAwait(false);
            await StorePinProtectedManagementKeyAsync(backend, logger, keyToProtect, cancellationToken).ConfigureAwait(false);
            await PivMetadataProtocol.BlockPukAsync(backend, logger, cancellationToken).ConfigureAwait(false);

            var currentAdminRaw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.AdminData, cancellationToken)
                .ConfigureAwait(false);
            _ = PivAdminData.TryDecode(currentAdminRaw, out var currentAdmin);
            var updatedAdmin = PivAdminData.Create(pukBlocked: true, pinProtected: true, currentAdmin.Salt, currentAdmin.PinLastUpdated);
            await PivTypedDataObjectProtocol.SetAdminDataAsync(backend, logger, isAuthenticated, updatedAdmin, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        // pinOnlyMode == None: disable, but only if a PIN-only mode is actually currently set.
        var currentMode = await GetPinOnlyModeAsync(backend, logger, cancellationToken).ConfigureAwait(false);
        if (!currentMode.HasFlag(PivPinOnlyMode.PinProtected) && !currentMode.HasFlag(PivPinOnlyMode.PinDerived))
        {
            logger.LogDebug("PIV: PIN-only mode is already disabled; no changes made");
            return;
        }

        logger.LogDebug("PIV: Disabling PIN-only management key mode");
        int defaultKeyLength = GetManagementKeyLength(managementKeyType);
        byte[] defaultKey = ArrayPool<byte>.Shared.Rent(defaultKeyLength);
        try
        {
            FillRepeatingDefaultKeyPattern(defaultKey.AsSpan(0, defaultKeyLength));
            await setManagementKeyAsync(managementKeyType, defaultKey.AsMemory(0, defaultKeyLength), false, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(defaultKey.AsSpan(0, defaultKeyLength));
            ArrayPool<byte>.Shared.Return(defaultKey);
        }

        await PivDataObjectProtocol.PutObjectAsync(backend, isAuthenticated, PivDataObject.PrintedInformation, null, cancellationToken)
            .ConfigureAwait(false);
        await PivDataObjectProtocol.PutObjectAsync(backend, isAuthenticated, PivDataObject.AdminData, null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Stores <paramref name="managementKey"/> in the PRINTED object as PIN-protected data:
    /// <c>88 len [ 89 len key ]</c>.
    /// </summary>
    private static async Task StorePinProtectedManagementKeyAsync(
        IPivBackend backend,
        ILogger logger,
        ReadOnlyMemory<byte> managementKey,
        CancellationToken cancellationToken)
    {
        if (Array.IndexOf(ValidManagementKeyLengths, managementKey.Length) < 0)
        {
            throw new ArgumentException("Management key must be 16, 24, or 32 bytes.", nameof(managementKey));
        }

        byte[] encoded;
        using (var keyTlv = new Tlv(ManagementKeyTag, managementKey.Span))
        using (var outer = new Tlv(PinProtectedTag, keyTlv.AsSpan()))
        {
            encoded = outer.AsMemory().ToArray();
        }

        try
        {
            await PivDataObjectProtocol.PutObjectAsync(backend, true, PivDataObject.PrintedInformation, encoded, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encoded);
        }

        logger.LogDebug("PIV: Stored PIN-protected management key in PRINTED");
    }

    /// <summary>
    /// Decodes PIN-protected data from the PRINTED object: <c>88 len [ 89 len key ]</c>.
    /// </summary>
    /// <remarks>
    /// This method takes ownership of <paramref name="data"/> for zeroing purposes: <paramref name="data"/>
    /// is the caller's raw PRINTED-object bytes, which contain the management key in the clear when
    /// present, and it is unconditionally zeroed before this method returns - on a successful decode,
    /// a "not found"/wrong-shape result, and a malformed-data decode failure alike. The returned
    /// <paramref name="managementKey"/> is a separate copy and remains the caller's responsibility to
    /// zero after use.
    /// </remarks>
    internal static bool TryDecodePinProtectedManagementKey(Span<byte> data, out Memory<byte> managementKey)
    {
        managementKey = Memory<byte>.Empty;

        try
        {
            if (data.IsEmpty)
            {
                return false;
            }

            using var outer = Tlv.Create(data);
            if (outer.Tag != PinProtectedTag)
            {
                return false;
            }

            // PRINTED's PIN-protected payload is a single, fixed-shape nested TLV (no other tags to
            // enumerate), so parse the inner management-key TLV directly instead of going through
            // TlvHelper.DecodeDictionary. Every intermediate copy then lives inside a disposable
            // Tlv that zeroes itself, instead of leaving a bare, unzeroed byte[] behind in a dictionary.
            using var keyTlv = Tlv.Create(outer.Value.Span);
            if (keyTlv.Tag != ManagementKeyTag || Array.IndexOf(ValidManagementKeyLengths, keyTlv.Length) < 0)
            {
                return false;
            }

            managementKey = keyTlv.Value.ToArray();
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Truncated/malformed TLV data can throw IndexOutOfRangeException from the
            // underlying Tlv parser rather than ArgumentException; treat both as decode failure.
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    private static void DeriveManagementKey(ReadOnlySpan<byte> pin, ReadOnlySpan<byte> salt, Span<byte> destination)
    {
        // SHA-1/10,000 iterations are retained for backward compatibility with management keys
        // derived by the v1 SDK; this is not a new cryptographic design.
#pragma warning disable CA5379, CA5387
        Rfc2898DeriveBytes.Pbkdf2(pin, salt, destination, Pbkdf2Iterations, HashAlgorithmName.SHA1);
#pragma warning restore CA5379, CA5387
    }

    private static int GetManagementKeyLength(PivManagementKeyType keyType) => keyType switch
    {
        PivManagementKeyType.Aes128 => 16,
        PivManagementKeyType.Aes256 => 32,
        _ => 24, // TripleDes and Aes192
    };

    private static void FillRepeatingDefaultKeyPattern(Span<byte> destination)
    {
        ReadOnlySpan<byte> pattern = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] = pattern[i % pattern.Length];
        }
    }
}
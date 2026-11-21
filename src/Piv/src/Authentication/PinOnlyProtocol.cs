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
using System.Security.Cryptography;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Piv.Backends;
using Yubico.YubiKit.Piv.DataObjects;

namespace Yubico.YubiKit.Piv.Authentication;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

/// <summary>
/// Encodes, decodes, and derives PIN-only management-key material. The session owns workflows.
/// </summary>
internal static class PinOnlyProtocol
{
    private static ILogger Logger => YubiKitLogging.CreateLogger(nameof(PinOnlyProtocol));

    private const int PinProtectedTag = 0x88;
    private const int ManagementKeyTag = 0x89;
    private const int Pbkdf2Iterations = 10_000;
    private static readonly int[] ValidManagementKeyLengths =
        [KeyType.Aes128.GetKeySizeBytes(), KeyType.Aes192.GetKeySizeBytes(), KeyType.Aes256.GetKeySizeBytes()];

    internal static async Task<PinOnlyMode> GetPinOnlyModeAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting PIN-only mode from ADMIN DATA");

        using var admin = await PinOnlyMetadata.ReadForRecoveryAsync(backend, DataObject.AdminData, cancellationToken).ConfigureAwait(false);

        if (admin is null)
        {
            Logger.LogDebug("PIV: ADMIN DATA is not valid PIN-only data; treating both modes as unavailable");
            return PinOnlyMode.PinProtectedUnavailable | PinOnlyMode.PinDerivedUnavailable;
        }

        var mode = PinOnlyMode.None;
        if ((admin.Flags & PinOnlyMetadata.KeyStored) != 0)
        {
            mode |= PinOnlyMode.PinProtected;
        }
        if (!admin.Salt.IsEmpty)
        {
            mode |= PinOnlyMode.PinDerived;
        }

        return mode;
    }

    /// <summary>
    /// Stores <paramref name="managementKey"/> in the PRINTED object as PIN-protected data:
    /// <c>88 len [ 89 len key ]</c>.
    /// </summary>
    internal static async Task StorePinProtectedManagementKeyAsync(
        IBackend backend,
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
            await DataObjectProtocol.PutObjectAsync(backend, DataObject.PrintedInformation, encoded, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encoded);
        }

        Logger.LogDebug("PIV: Stored PIN-protected management key in PRINTED");
    }

    internal static void DeriveManagementKey(ReadOnlySpan<byte> pin, ReadOnlySpan<byte> salt, Span<byte> destination)
    {
        // SHA-1/10,000 iterations are retained for backward compatibility with management keys
        // derived by the v1 SDK; this is not a new cryptographic design.
#pragma warning disable CA5379, CA5387
        Rfc2898DeriveBytes.Pbkdf2(pin, salt, destination, Pbkdf2Iterations, HashAlgorithmName.SHA1);
#pragma warning restore CA5379, CA5387
    }

}
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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backends;

namespace Yubico.YubiKit.Piv.DataObjects;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

/// <summary>
/// Reads and writes typed PIV data objects (CHUID, CCC, ADMIN DATA, Key History) on top of the
/// raw <see cref="DataObjectProtocol"/> GET/PUT DATA helpers.
/// </summary>
internal static class TypedDataObjectProtocol
{
    private static ILogger Logger => YubiKitLogging.CreateLogger("Yubico.YubiKit.Piv.DataObjects.TypedDataObjectProtocol");

    internal static async Task<CardholderUniqueId> GetCardholderUniqueIdAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting CHUID");
        var raw = await DataObjectProtocol.GetObjectAsync(backend, DataObject.Chuid, cancellationToken).ConfigureAwait(false);

        if (!CardholderUniqueId.TryDecode(raw, out var value))
        {
            throw new ApduException("CHUID object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetCardholderUniqueIdAsync(
        IBackend backend,
        CardholderUniqueId cardholderUniqueId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting CHUID");
        var encoded = cardholderUniqueId.Encode();
        return DataObjectProtocol.PutObjectAsync(
            backend, DataObject.Chuid, encoded.IsEmpty ? null : encoded, cancellationToken);
    }

    internal static async Task<CardCapabilityContainer> GetCardCapabilityContainerAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting CCC");
        var raw = await DataObjectProtocol.GetObjectAsync(backend, DataObject.Capability, cancellationToken).ConfigureAwait(false);

        if (!CardCapabilityContainer.TryDecode(raw, out var value))
        {
            throw new ApduException("CCC object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetCardCapabilityContainerAsync(
        IBackend backend,
        CardCapabilityContainer cardCapabilityContainer,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting CCC");
        var encoded = cardCapabilityContainer.Encode();
        return DataObjectProtocol.PutObjectAsync(
            backend, DataObject.Capability, encoded.IsEmpty ? null : encoded, cancellationToken);
    }

    internal static async Task<AdminData> GetAdminDataAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting ADMIN DATA");
        var raw = await DataObjectProtocol.GetObjectAsync(backend, DataObject.AdminData, cancellationToken).ConfigureAwait(false);

        if (!AdminData.TryDecode(raw, out var value))
        {
            throw new ApduException("ADMIN DATA object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetAdminDataAsync(
        IBackend backend,
        AdminData adminData,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting ADMIN DATA");
        var encoded = adminData.Encode();
        return DataObjectProtocol.PutObjectAsync(
            backend, DataObject.AdminData, encoded.IsEmpty ? null : encoded, cancellationToken);
    }

    internal static async Task<KeyHistory> GetKeyHistoryAsync(
        IBackend backend,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Getting Key History");
        var raw = await DataObjectProtocol.GetObjectAsync(backend, DataObject.KeyHistory, cancellationToken).ConfigureAwait(false);

        if (!KeyHistory.TryDecode(raw, out var value))
        {
            throw new ApduException("Key History object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetKeyHistoryAsync(
        IBackend backend,
        KeyHistory keyHistory,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("PIV: Setting Key History");
        var encoded = keyHistory.Encode();
        return DataObjectProtocol.PutObjectAsync(
            backend, DataObject.KeyHistory, encoded.IsEmpty ? null : encoded, cancellationToken);
    }
}
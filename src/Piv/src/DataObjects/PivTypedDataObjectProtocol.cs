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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Piv.Backend;

namespace Yubico.YubiKit.Piv.DataObjects;

#pragma warning disable CS1573 // Public XML docs live on PivSession/IPivSession; these are internal protocol helpers.

/// <summary>
/// Reads and writes typed PIV data objects (CHUID, CCC, ADMIN DATA, Key History) on top of the
/// raw <see cref="PivDataObjectProtocol"/> GET/PUT DATA helpers.
/// </summary>
internal static class PivTypedDataObjectProtocol
{
    internal static async Task<PivCardholderUniqueId> GetCardholderUniqueIdAsync(
        IPivBackend backend,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Getting CHUID");
        var raw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.Chuid, cancellationToken).ConfigureAwait(false);

        if (!PivCardholderUniqueId.TryDecode(raw, out var value))
        {
            throw new ApduException("CHUID object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetCardholderUniqueIdAsync(
        IPivBackend backend,
        ILogger logger,
        bool isAuthenticated,
        PivCardholderUniqueId cardholderUniqueId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Setting CHUID");
        var encoded = cardholderUniqueId.Encode();
        return PivDataObjectProtocol.PutObjectAsync(
            backend, isAuthenticated, PivDataObject.Chuid, encoded.IsEmpty ? null : encoded, cancellationToken);
    }

    internal static async Task<PivCardCapabilityContainer> GetCardCapabilityContainerAsync(
        IPivBackend backend,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Getting CCC");
        var raw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.Capability, cancellationToken).ConfigureAwait(false);

        if (!PivCardCapabilityContainer.TryDecode(raw, out var value))
        {
            throw new ApduException("CCC object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetCardCapabilityContainerAsync(
        IPivBackend backend,
        ILogger logger,
        bool isAuthenticated,
        PivCardCapabilityContainer cardCapabilityContainer,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Setting CCC");
        var encoded = cardCapabilityContainer.Encode();
        return PivDataObjectProtocol.PutObjectAsync(
            backend, isAuthenticated, PivDataObject.Capability, encoded.IsEmpty ? null : encoded, cancellationToken);
    }

    internal static async Task<PivAdminData> GetAdminDataAsync(
        IPivBackend backend,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Getting ADMIN DATA");
        var raw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.AdminData, cancellationToken).ConfigureAwait(false);

        if (!PivAdminData.TryDecode(raw, out var value))
        {
            throw new ApduException("ADMIN DATA object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetAdminDataAsync(
        IPivBackend backend,
        ILogger logger,
        bool isAuthenticated,
        PivAdminData adminData,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Setting ADMIN DATA");
        var encoded = adminData.Encode();
        return PivDataObjectProtocol.PutObjectAsync(
            backend, isAuthenticated, PivDataObject.AdminData, encoded.IsEmpty ? null : encoded, cancellationToken);
    }

    internal static async Task<PivKeyHistory> GetKeyHistoryAsync(
        IPivBackend backend,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Getting Key History");
        var raw = await PivDataObjectProtocol.GetObjectAsync(backend, PivDataObject.KeyHistory, cancellationToken).ConfigureAwait(false);

        if (!PivKeyHistory.TryDecode(raw, out var value))
        {
            throw new ApduException("Key History object is not encoded as expected.");
        }

        return value;
    }

    internal static Task SetKeyHistoryAsync(
        IPivBackend backend,
        ILogger logger,
        bool isAuthenticated,
        PivKeyHistory keyHistory,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PIV: Setting Key History");
        var encoded = keyHistory.Encode();
        return PivDataObjectProtocol.PutObjectAsync(
            backend, isAuthenticated, PivDataObject.KeyHistory, encoded.IsEmpty ? null : encoded, cancellationToken);
    }
}

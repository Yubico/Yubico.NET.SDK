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
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <summary>Firmware version that fixes Curve25519 algorithm information entries.</summary>
    private static readonly FirmwareVersion AlgorithmInfoFixVersion = new(5, 6, 1);

    /// <inheritdoc />
    public async Task<Uif> GetUifAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureUif);

        _logger.LogDebug("Getting UIF for {Slot}", keyRef);

        var data = await GetDataCoreAsync(keyRef.UifDo(), cancellationToken)
            .ConfigureAwait(false);
        return UifExtensions.ParseUif(data.Span);
    }

    /// <inheritdoc />
    public async Task SetUifAsync(
        KeyRef keyRef,
        Uif uif,
        CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureUif);

        _logger.LogInformation("Setting UIF for {Slot} to {Uif}", keyRef, uif);

        await PutDataAsync(keyRef.UifDo(), uif.ToBytes(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AlgorithmAttributes> GetAlgorithmAttributesAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting algorithm attributes for {Slot}", keyRef);

        var data = await GetDataCoreAsync(keyRef.AlgorithmAttributesDo(), cancellationToken)
            .ConfigureAwait(false);
        return AlgorithmAttributes.Parse(data.Span);
    }

    /// <inheritdoc />
    public async Task SetAlgorithmAttributesAsync(
        KeyRef keyRef,
        AlgorithmAttributes attributes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        _logger.LogInformation("Setting algorithm attributes for {Slot}", keyRef);

        await PutDataAsync(keyRef.AlgorithmAttributesDo(), attributes.ToBytes(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(KeyRef KeyRef, AlgorithmAttributes Attributes)>>
        GetAlgorithmInformationAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureAlgorithmInfo);

        _logger.LogDebug("Getting algorithm information");

        var data = await GetDataCoreAsync(DataObject.AlgorithmInformation, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<(KeyRef, AlgorithmAttributes)>();
        var span = data.Span;
        var offset = 0;

        while (offset < span.Length)
        {
            // Each entry: 1 byte key ref + algorithm attributes bytes
            using var tlv = Tlv.Create(span[offset..]);
            offset += tlv.TotalLength;

            var entryData = tlv.Value.Span;
            if (entryData.Length < 2)
            {
                continue;
            }

            var keyRefByte = (KeyRef)entryData[0];
            if (!Enum.IsDefined(keyRefByte))
            {
                continue;
            }

            try
            {
                var attrs = AlgorithmAttributes.Parse(entryData[1..]);
                result.Add((keyRefByte, attrs));
            }
            catch (ArgumentException)
            {
                // Skip unsupported algorithm entries
            }
        }

        // Fix Curve25519 entries for firmware < 5.6.1
        if (FirmwareVersion < AlgorithmInfoFixVersion)
        {
            result = FixCurve25519AlgorithmInfo(result);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Kdf> GetKdfAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting KDF configuration");

        return await GetOrLoadKdfAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetKdfAsync(
        Kdf kdf,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(kdf);

        _logger.LogInformation("Setting KDF configuration");

        await PutDataAsync(DataObject.Kdf, kdf.ToBytes(), cancellationToken)
            .ConfigureAwait(false);

        // Invalidate cached KDF
        _kdf = kdf;
    }

    // ── Private Helpers ───────────────────────────────────────────────

    /// <summary>
    ///     Fixes invalid Curve25519 entries in algorithm information for firmware &lt; 5.6.1.
    ///     Pre-5.6.1 firmware reports X25519 with EdDSA algorithm ID, which is invalid.
    ///     The fix removes invalid entries and ensures correct Curve25519 assignments:
    ///     Ed25519/EdDSA for SIG/AUT, X25519/ECDH for DEC.
    /// </summary>
    private static List<(KeyRef, AlgorithmAttributes)> FixCurve25519AlgorithmInfo(
        List<(KeyRef KeyRef, AlgorithmAttributes Attributes)> entries)
    {
        var result = new List<(KeyRef, AlgorithmAttributes)>();

        foreach (var (keyRef, attrs) in entries)
        {
            if (attrs is not EcAttributes ec)
            {
                result.Add((keyRef, attrs));
                continue;
            }

            // Remove X25519 entries with EdDSA algorithm ID (invalid)
            if (ec.Oid == CurveOid.X25519 && ec.AlgorithmId == EcAttributes.EddsaAlgorithmId)
            {
                // Replace: DEC gets X25519/ECDH, others skip
                if (keyRef == KeyRef.Dec)
                {
                    result.Add((keyRef, EcAttributes.Create(KeyRef.Dec, CurveOid.X25519)));
                }

                continue;
            }

            // Remove Ed25519 from DEC and ATT (invalid for those slots)
            if (ec.Oid == CurveOid.Ed25519 && keyRef is KeyRef.Dec or KeyRef.Att)
            {
                continue;
            }

            result.Add((keyRef, attrs));
        }

        return result;
    }
}

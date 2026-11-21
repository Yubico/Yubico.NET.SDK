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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <summary>Firmware version that fixes Curve25519 algorithm information entries.</summary>
    private static readonly FirmwareVersion AlgorithmInfoFixVersion = new(5, 6, 1);

    /// <summary>Reverse lookup from algorithm attribute Data Object tag to KeyRef, built once.</summary>
    private static readonly Dictionary<DataObject, KeyRef> AlgoAttributeToKeyRef =
        Enum.GetValues<KeyRef>().ToDictionary(kr => kr.AlgorithmAttributesDo(), kr => kr);

    /// <inheritdoc />
    public async Task<Uif> GetUifAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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
        ThrowIfDisposed();

        EnsureSupports(FeatureUif);
        if (keyRef is KeyRef.Att || uif is Uif.Cached or Uif.CachedFixed)
            EnsureSupports(FeatureCachedAndAttestationUif);

        _logger.LogInformation("Setting UIF for {Slot} to {Uif}", keyRef, uif);

        await PutDataAsync(keyRef.UifDo(), uif.ToBytes(), cancellationToken)
            .ConfigureAwait(false);
        _updatedUifs[keyRef] = uif;
    }

    /// <inheritdoc />
    public async Task<AlgorithmAttributes> GetAlgorithmAttributesAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Getting algorithm attributes for {Slot}", keyRef);

        // Per Python canonical: read from ApplicationRelatedData (DO 0x6E), not from
        // individual algorithm attribute DOs (0xC1/0xC2/0xC3) directly. Direct GET_DATA
        // for those DOs is not supported on all OpenPGP implementations.
        var appData = await GetApplicationRelatedDataAsync(cancellationToken).ConfigureAwait(false);
        return keyRef switch
        {
            KeyRef.Sig => appData.Discretionary.AlgorithmAttributesSig,
            KeyRef.Dec => appData.Discretionary.AlgorithmAttributesDec,
            KeyRef.Aut => appData.Discretionary.AlgorithmAttributesAut,
            KeyRef.Att => appData.Discretionary.AlgorithmAttributesAtt
                          ?? throw new NotSupportedException("ATT key slot not supported on this device"),
            _ => throw new ArgumentOutOfRangeException(nameof(keyRef))
        };
    }

    /// <inheritdoc />
    public async Task SetAlgorithmAttributesAsync(
        KeyRef keyRef,
        AlgorithmAttributes attributes,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attributes);
        bool supported = attributes switch
        {
            RsaAttributes rsa => rsa.AlgorithmId == RsaAttributes.RsaAlgorithmId && Enum.IsDefined(rsa.ImportFormat),
            EcAttributes ec => Enum.IsDefined(ec.ImportFormat) && ec.AlgorithmId is
                EcAttributes.EcdhAlgorithmId or EcAttributes.EcdsaAlgorithmId or EcAttributes.EddsaAlgorithmId,
            _ => false,
        };
        if (!supported)
            throw new NotSupportedException("Only modeled OpenPGP algorithm attributes can be written.");

        _logger.LogInformation("Setting algorithm attributes for {Slot}", keyRef);

        await PutDataAsync(keyRef.AlgorithmAttributesDo(), attributes.ToBytes(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SupportedAlgorithm>>
        GetSupportedAlgorithmsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsSupported(FeatureAlgorithmInfo))
            return GetLegacySupportedAlgorithms();

        _logger.LogDebug("Getting algorithm information");

        // GET_DATA returns TLV(0xFA, [TLV(DO_C1, attrs), TLV(DO_C2, attrs), ...]).
        // Must unwrap the outer 0xFA TLV first, then parse the inner list.
        // Each inner TLV tag is a DataObject (0xC1=Sig, 0xC2=Dec, 0xC3=Aut, 0xDA=Att)
        // and maps to a KeyRef via KeyRef.AlgorithmAttributesDo().
        // Python canonical: buf = Tlv.unpack(DO.ALGORITHM_INFORMATION, buf)
        var rawData = await GetDataCoreAsync(DataObject.AlgorithmInformation, cancellationToken)
            .ConfigureAwait(false);

        // Unwrap the outer 0xFA TLV to get the inner list.
        // Some firmware versions return malformed TLV — pad and retry (matches ykman pattern:
        // yubikit/openpgp.py Tlv.unpack(DO.ALGORITHM_INFORMATION, buf + b"\0\0")[:-2])
        var innerSpan = UnwrapAlgorithmInformation(rawData.Span);
        var result = new List<SupportedAlgorithm>();
        while (!innerSpan.IsEmpty)
        {
            var (tag, _, value) = Tlv.ParseData(ref innerSpan);

            // Tag is the DO (0xC1, 0xC2, 0xC3, 0xDA); value is algorithm attributes bytes
            if (!AlgoAttributeToKeyRef.TryGetValue((DataObject)tag, out var keyRef))
            {
                continue;
            }

            var attrs = AlgorithmAttributes.Parse(value);
            result.Add(new SupportedAlgorithm(keyRef, attrs));
        }

        // Fix Curve25519 entries for firmware < 5.6.1
        if (!FirmwareVersion.IsAlphaOrBeta && FirmwareVersion < AlgorithmInfoFixVersion)
        {
            result = FixCurve25519AlgorithmInfo(result);
        }

        return result.AsReadOnly();
    }

    private static ReadOnlySpan<byte> UnwrapAlgorithmInformation(ReadOnlySpan<byte> encoded)
    {
        if (encoded.IsEmpty || encoded[0] != (byte)DataObject.AlgorithmInformation)
            throw new ArgumentException("Expected OpenPGP algorithm information tag FA.", nameof(encoded));

        var remainder = encoded;
        byte[] value;
        try
        {
            (_, _, value) = Tlv.ParseData(ref remainder);
        }
        catch (ArgumentException)
        {
            // Legacy firmware declares exactly two bytes more than it sends (ykman compatibility).
            byte[] padded = [.. encoded, 0, 0];
            remainder = padded;
            (_, _, value) = Tlv.ParseData(ref remainder);
            if (!remainder.IsEmpty || value.Length < 2)
                throw;
            return value.AsSpan(0, value.Length - 2);
        }

        if (!remainder.IsEmpty)
            throw new ArgumentException("Trailing data after OpenPGP algorithm information.", nameof(encoded));
        return value;
    }

    /// <inheritdoc />
    public async Task<Kdf> GetKdfAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Getting KDF configuration");

        return await ReadKdfAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetKdfAsync(
        Kdf kdf,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(kdf);

        _logger.LogInformation("Setting KDF configuration");

        byte[] encoded = kdf.ToBytes();
        try
        {
            await PutDataAsync(DataObject.Kdf, encoded, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(encoded);
        }
    }

    // ── Private Helpers ───────────────────────────────────────────────

    private IReadOnlyList<SupportedAlgorithm> GetLegacySupportedAlgorithms()
    {
        bool neo = FirmwareVersion.Major is > 0 and < 4;
        var format = neo ? RsaImportFormat.CrtWithModulus : RsaImportFormat.Standard;
        RsaSize[] sizes = neo || FirmwareVersion is { Major: 4, Minor: 4 }
            ? [RsaSize.Rsa2048] : [RsaSize.Rsa2048, RsaSize.Rsa3072, RsaSize.Rsa4096];
        List<SupportedAlgorithm> algorithms = [];
        foreach (var slot in new[] { KeyRef.Sig, KeyRef.Dec, KeyRef.Aut })
            foreach (var size in sizes)
                algorithms.Add(new SupportedAlgorithm(slot, RsaAttributes.Create(size, format)));
        return algorithms;
    }

    /// <summary>
    ///     Fixes invalid Curve25519 entries in algorithm information for firmware &lt; 5.6.1.
    ///     Pre-5.6.1 firmware reports X25519 with EdDSA algorithm ID, which is invalid.
    ///     Replaces encountered X25519/EdDSA DEC entries with X25519/ECDH and removes invalid
    ///     Ed25519 DEC/ATT entries. Missing entries remain missing; list multiplicity is preserved.
    /// </summary>
    private static List<SupportedAlgorithm> FixCurve25519AlgorithmInfo(
        List<SupportedAlgorithm> entries)
    {
        var result = new List<SupportedAlgorithm>();

        foreach (var entry in entries)
        {
            var keyRef = entry.KeyRef;
            var attrs = entry.Attributes;
            if (attrs is not EcAttributes ec)
            {
                result.Add(entry);
                continue;
            }

            // Remove X25519 entries with EdDSA algorithm ID (invalid)
            if (ec.KeyType == KeyType.X25519 && ec.AlgorithmId == EcAttributes.EddsaAlgorithmId)
            {
                // Replace: DEC gets X25519/ECDH, others skip
                if (keyRef == KeyRef.Dec)
                {
                    result.Add(new SupportedAlgorithm(keyRef, EcAttributes.Create(KeyRef.Dec, KeyType.X25519)));
                }

                continue;
            }

            // Remove Ed25519 from DEC and ATT (invalid for those slots)
            if (ec.KeyType == KeyType.Ed25519 && keyRef is KeyRef.Dec or KeyRef.Att)
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }
}
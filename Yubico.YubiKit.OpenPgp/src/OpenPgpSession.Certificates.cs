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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <inheritdoc />
    public async Task<X509Certificate2?> GetCertificateAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting certificate for {Slot}", keyRef);

        if (keyRef == KeyRef.Att)
        {
            // Attestation certificate uses its own DO (0xFC)
            var attData = await GetDataCoreAsync(DataObject.AttCertificate, cancellationToken)
                .ConfigureAwait(false);
            return attData.Length > 0 ? X509CertificateLoader.LoadCertificate(attData.Span) : null;
        }

        EnsureSupports(FeatureCertificates);

        // SELECT DATA to choose the certificate slot
        await SelectCertificateSlotAsync(keyRef, cancellationToken).ConfigureAwait(false);

        // Read certificate via DO 0x7F21
        try
        {
            var certData = await GetDataCoreAsync(DataObject.CardholderCertificate, cancellationToken)
                .ConfigureAwait(false);
            return certData.Length > 0 ? X509CertificateLoader.LoadCertificate(certData.Span) : null;
        }
        catch (ApduException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task PutCertificateAsync(
        KeyRef keyRef,
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        _logger.LogInformation("Storing certificate for {Slot}", keyRef);

        if (keyRef == KeyRef.Att)
        {
            await PutDataAsync(DataObject.AttCertificate, certificate.RawData, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        EnsureSupports(FeatureCertificates);

        await SelectCertificateSlotAsync(keyRef, cancellationToken).ConfigureAwait(false);

        await PutDataAsync(DataObject.CardholderCertificate, certificate.RawData, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteCertificateAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting certificate for {Slot}", keyRef);

        if (keyRef == KeyRef.Att)
        {
            await PutDataAsync(DataObject.AttCertificate, ReadOnlyMemory<byte>.Empty, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        EnsureSupports(FeatureCertificates);

        await SelectCertificateSlotAsync(keyRef, cancellationToken).ConfigureAwait(false);

        await PutDataAsync(DataObject.CardholderCertificate, ReadOnlyMemory<byte>.Empty, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Private Helpers ───────────────────────────────────────────────

    private async Task SelectCertificateSlotAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken)
    {
        // Map key slot to certificate index (0-based: SIG=0, DEC=1, AUT=2)
        var index = keyRef switch
        {
            KeyRef.Sig => (byte)0x00,
            KeyRef.Dec => (byte)0x01,
            KeyRef.Aut => (byte)0x02,
            _ => throw new ArgumentOutOfRangeException(nameof(keyRef),
                "Only Sig, Dec, and Aut slots support per-slot certificates."),
        };

        // Build SELECT_DATA payload: TLV(0x60, TLV(0x5C, TLV(tag 0x7F21)))
        // The occurrence is encoded as the index in P2
        byte[] selectDataPayload;

        if (IsSupported(FeatureSelectDataFix))
        {
            // Firmware >= 5.4.4: standard SELECT_DATA
            using var innerTlv = new Tlv(0x5C, [(byte)0x7F, (byte)0x21]);
            using var outerTlv = new Tlv(0x60, innerTlv.AsSpan());
            selectDataPayload = outerTlv.AsMemory().ToArray();
        }
        else
        {
            // Firmware 5.2.0-5.4.3: non-standard SELECT_DATA with extra 0x06 byte
            using var innerTlv = new Tlv(0x5C, [(byte)0x06, (byte)0x7F, (byte)0x21]);
            using var outerTlv = new Tlv(0x60, innerTlv.AsSpan());
            selectDataPayload = outerTlv.AsMemory().ToArray();
        }

        var command = new ApduCommand(
            0x00,
            (int)Ins.SelectData,
            index,
            0x04,
            selectDataPayload);

        await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
    }
}
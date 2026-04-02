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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <inheritdoc />
    public async Task GenerateRsaKeyAsync(
        KeyRef keyRef,
        RsaSize size = RsaSize.Rsa2048,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating RSA {Size} key for {Slot}", (int)size, keyRef);

        var attributes = RsaAttributes.Create(size);
        await SetAlgorithmAttributesAsync(keyRef, attributes, cancellationToken)
            .ConfigureAwait(false);

        await GenerateKeyAsync(keyRef, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task GenerateEcKeyAsync(
        KeyRef keyRef,
        CurveOid curve,
        CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureEc);

        _logger.LogInformation("Generating EC {Curve} key for {Slot}", curve, keyRef);

        var attributes = EcAttributes.Create(keyRef, curve);
        await SetAlgorithmAttributesAsync(keyRef, attributes, cancellationToken)
            .ConfigureAwait(false);

        await GenerateKeyAsync(keyRef, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PutKeyAsync(
        KeyRef keyRef,
        PrivateKeyTemplate template,
        AlgorithmAttributes? attributes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        _logger.LogInformation("Importing key into {Slot}", keyRef);

        if (attributes is not null)
        {
            await SetAlgorithmAttributesAsync(keyRef, attributes, cancellationToken)
                .ConfigureAwait(false);
        }

        byte[]? templateBytes = null;
        try
        {
            templateBytes = template.ToBytes();

            var command = new ApduCommand
            {
                Cla = 0x00,
                Ins = (byte)Ins.PutDataOdd,
                P1 = 0x3F,
                P2 = 0xFF,
                Data = templateBytes,
            };

            await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (templateBytes is not null)
            {
                CryptographicOperations.ZeroMemory(templateBytes);
            }
        }

        // Refresh cached app data after key import
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting key from {Slot}", keyRef);

        // Python canonical: delete key by changing algorithm attributes twice.
        // First set to RSA 4096 (forces key deletion), then back to RSA 2048.
        // Use PutDataAsync directly (bypassing SetAlgorithmAttributesAsync checks)
        // to avoid RSA 4096 support verification.
        var rsa4096 = RsaAttributes.Create(RsaSize.Rsa4096);
        var rsa2048 = RsaAttributes.Create(RsaSize.Rsa2048);

        await PutDataAsync(keyRef.AlgorithmAttributesDo(), rsa4096.ToBytes(), cancellationToken)
            .ConfigureAwait(false);

        await SetAlgorithmAttributesAsync(keyRef, rsa2048, cancellationToken)
            .ConfigureAwait(false);

        // Refresh cached app data
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading public key from {Slot}", keyRef);

        var crtBytes = keyRef.GetCrt();

        // GENERATE ASYMMETRIC KEY PAIR with P1=0x81 (read existing)
        var command = new ApduCommand(0x00, (int)Ins.GenerateAsym, 0x81, 0x00, crtBytes);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return response.Data;
    }

    /// <inheritdoc />
    public async Task<X509Certificate2> AttestKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        EnsureSupports(FeatureAttestation);

        _logger.LogDebug("Attesting key in {Slot}", keyRef);

        var command = new ApduCommand(0x80, (int)Ins.GetAttestation, (int)keyRef, 0x00);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);

        return X509CertificateLoader.LoadCertificate(response.Data.Span);
    }

    /// <inheritdoc />
    public async Task<KeyInformation> GetKeyInformationAsync(
        CancellationToken cancellationToken = default)
    {
        // Key information is in the discretionary data objects
        var appData = await GetApplicationRelatedDataAsync(cancellationToken)
            .ConfigureAwait(false);
        return appData.Discretionary.KeyInfo;
    }

    /// <inheritdoc />
    public async Task<Fingerprints> GetFingerprintsAsync(
        CancellationToken cancellationToken = default)
    {
        var appData = await GetApplicationRelatedDataAsync(cancellationToken)
            .ConfigureAwait(false);
        return appData.Discretionary.Fingerprints;
    }

    /// <inheritdoc />
    public async Task<GenerationTimes> GetGenerationTimesAsync(
        CancellationToken cancellationToken = default)
    {
        var appData = await GetApplicationRelatedDataAsync(cancellationToken)
            .ConfigureAwait(false);
        return appData.Discretionary.GenerationTimes;
    }

    /// <inheritdoc />
    public async Task SetFingerprintAsync(
        KeyRef keyRef,
        ReadOnlyMemory<byte> fingerprint,
        CancellationToken cancellationToken = default)
    {
        if (fingerprint.Length != 20)
        {
            throw new ArgumentException("Fingerprint must be exactly 20 bytes.", nameof(fingerprint));
        }

        _logger.LogDebug("Setting fingerprint for {Slot}", keyRef);
        await PutDataAsync(keyRef.FingerprintDo(), fingerprint, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetGenerationTimeAsync(
        KeyRef keyRef,
        int timestamp,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting generation time for {Slot} to {Timestamp}", keyRef, timestamp);

        var data = new byte[4];
        data[0] = (byte)(timestamp >> 24);
        data[1] = (byte)(timestamp >> 16);
        data[2] = (byte)(timestamp >> 8);
        data[3] = (byte)timestamp;

        await PutDataAsync(keyRef.GenerationTimeDo(), data, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Private Helpers ───────────────────────────────────────────────

    private async Task GenerateKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken)
    {
        var crtBytes = keyRef.GetCrt();

        // GENERATE ASYMMETRIC KEY PAIR with P1=0x80 (generate new)
        var command = new ApduCommand(0x00, (int)Ins.GenerateAsym, 0x80, 0x00, crtBytes);
        await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);

        // Refresh cached app data after key generation
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
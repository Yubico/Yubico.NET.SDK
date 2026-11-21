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
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;

namespace Yubico.YubiKit.OpenPgp;

public sealed partial class OpenPgpSession
{
    /// <inheritdoc />
    public async Task GenerateKeyAsync(
        KeyRef keyRef,
        AlgorithmAttributes attributes,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attributes);

        if (attributes is EcAttributes)
            EnsureSupports(FeatureEc);

        // YSA-2017-01 lists 4.2.6–4.3.4; follow the conservative vendor SDK interval.
        if (attributes is RsaAttributes &&
            _numericVersion is { } numeric && numeric.IsAtLeast(4, 2, 0) && numeric.IsLessThan(4, 3, 5))
        {
            throw new NotSupportedException("On-card RSA generation is disabled on OpenPGP firmware 4.2.0 through 4.3.4 (ROCA policy). Import an independently generated key instead.");
        }

        _logger.LogInformation(
            "Generating {AlgorithmType} key for {Slot}",
            attributes.GetType().Name,
            keyRef);
        if (_appData.Discretionary.ExtendedCapabilities.Flags.HasFlag(ExtendedCapabilityFlags.AlgorithmAttributesChangeable))
        {
            await SetAlgorithmAttributesAsync(keyRef, attributes, cancellationToken).ConfigureAwait(false);
        }
        else if (attributes is not RsaAttributes { NLen: (int)RsaSize.Rsa2048 })
        {
            throw new NotSupportedException("This YubiKey only supports RSA 2048 keys.");
        }

        await GenerateKeyPairAsync(keyRef, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PutKeyAsync(
        KeyRef keyRef,
        IPrivateKey privateKey,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(privateKey);

        _logger.LogInformation("Importing {KeyType} key into {Slot}", privateKey.KeyType, keyRef);

        using var import = PrivateKeyImport.Create(privateKey, keyRef, FirmwareVersion);
        if (import.Attributes is EcAttributes)
            EnsureSupports(FeatureEc);

        if (_appData.Discretionary.ExtendedCapabilities.Flags.HasFlag(ExtendedCapabilityFlags.AlgorithmAttributesChangeable))
        {
            await SetAlgorithmAttributesAsync(keyRef, import.Attributes, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (import.Attributes is not RsaAttributes { NLen: (int)RsaSize.Rsa2048 })
        {
            throw new NotSupportedException("This YubiKey only supports RSA 2048 keys.");
        }

        byte[]? templateBytes = null;
        try
        {
            templateBytes = import.Template.ToBytes(keyRef);

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
        _updatedUifs.Clear();
    }

    /// <inheritdoc />
    public async Task DeleteKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (FirmwareVersion.Major is > 0 and < 4)
            throw new NotSupportedException("OpenPGP key deletion is not supported on YubiKey NEO.");

        _logger.LogInformation("Deleting key from {Slot}", keyRef);

        // Attribute switching is destructive and can leave partial state between writes.
        // Certificate and host metadata management remain separate.
        var rsa4096 = RsaAttributes.Create(RsaSize.Rsa4096);
        var rsa2048 = RsaAttributes.Create(RsaSize.Rsa2048);

        await PutDataAsync(keyRef.AlgorithmAttributesDo(), rsa4096.ToBytes(), cancellationToken)
            .ConfigureAwait(false);

        await SetAlgorithmAttributesAsync(keyRef, rsa2048, cancellationToken)
            .ConfigureAwait(false);

        // Refresh cached app data
        _appData = await GetApplicationRelatedDataCoreAsync(cancellationToken)
            .ConfigureAwait(false);
        _updatedUifs.Clear();
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetPublicKeyAsync(
        KeyRef keyRef,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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
        ThrowIfDisposed();

        EnsureSupports(FeatureAttestation);

        _logger.LogDebug("Attesting key in {Slot}", keyRef);

        // GET_ATTESTATION generates the attestation cert and writes it to the
        // certificate slot for the key. We then read it back with GetCertificateAsync.
        // The Python implementation uses the same send_apdu → get_certificate(key_ref) sequence.
        var command = new ApduCommand(0x80, (int)Ins.GetAttestation, (int)keyRef, 0x00);
        UserPresenceNotification userPresenceNotification = CreateUserPresenceNotification(
            await GetUserPresenceContextAsync(KeyRef.Att, UserPresenceOperations.OpenPgp.AttestKey, cancellationToken).ConfigureAwait(false));
        X509Certificate2? ownedCertificate = null;
        try
        {
            X509Certificate2 certificate = await RunWithUserPresenceNotificationAsync(
                userPresenceNotification,
                async token =>
                {
                    await TransmitAsync(command, token).ConfigureAwait(false);
                    return ownedCertificate = await GetCertificateAsync(keyRef, token).ConfigureAwait(false)
                        ?? throw new InvalidOperationException(
                            $"Attestation certificate was not found in slot {keyRef} after GET_ATTESTATION.");
                },
                cancellationToken)
                .ConfigureAwait(false);
            ownedCertificate = null;
            return certificate;
        }
        finally
        {
            ownedCertificate?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<KeyInformation> ListKeyInformationAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Key information is in the discretionary data objects
        var appData = await GetApplicationRelatedDataAsync(cancellationToken)
            .ConfigureAwait(false);
        return appData.Discretionary.KeyInformation;
    }

    /// <inheritdoc />
    public async Task<KeyFingerprints> GetKeyFingerprintsAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var appData = await GetApplicationRelatedDataAsync(cancellationToken)
            .ConfigureAwait(false);
        return appData.Discretionary.KeyFingerprints;
    }

    /// <inheritdoc />
    public async Task<GenerationTimes> GetGenerationTimesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var appData = await GetApplicationRelatedDataAsync(cancellationToken)
            .ConfigureAwait(false);
        return appData.Discretionary.GenerationTimes;
    }

    /// <inheritdoc />
    public async Task SetKeyFingerprintAsync(
        KeyRef keyRef,
        ReadOnlyMemory<byte> fingerprint,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (fingerprint.Length != 20)
        {
            throw new ArgumentException("Fingerprint must be exactly 20 bytes.", nameof(fingerprint));
        }

        _logger.LogDebug("Setting fingerprint for {Slot}", keyRef);
        await PutDataAsync(keyRef.KeyFingerprintDo(), fingerprint, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetGenerationTimeAsync(
        KeyRef keyRef,
        uint timestamp,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Setting generation time for {Slot} to {Timestamp}", keyRef, timestamp);

        var data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, timestamp);

        await PutDataAsync(keyRef.GenerationTimeDo(), data, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Private Helpers ───────────────────────────────────────────────

    private async Task GenerateKeyPairAsync(
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
        _updatedUifs.Clear();
    }
}
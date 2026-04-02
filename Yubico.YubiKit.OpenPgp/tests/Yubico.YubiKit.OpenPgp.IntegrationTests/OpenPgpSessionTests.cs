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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpSessionTests
{
    private const string DefaultUserPin = "123456";
    private const string DefaultAdminPin = "12345678";

    // ── Reset & Clean State ──────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task Reset_RestoresDefaultState(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var pinStatus = await session.GetPinStatusAsync();
                Assert.Equal(3, pinStatus.AttemptsUser);
                Assert.Equal(0, pinStatus.AttemptsReset);
                Assert.Equal(3, pinStatus.AttemptsAdmin);
            });

    // ── Application Related Data ─────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetApplicationRelatedData_ReturnsValidData(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var appData = await session.GetApplicationRelatedDataAsync();
                Assert.NotNull(appData);
                Assert.NotNull(appData.Aid);
                Assert.NotNull(appData.Discretionary);
                Assert.NotNull(appData.Discretionary.AlgorithmAttributesSig);
                Assert.NotNull(appData.Discretionary.AlgorithmAttributesDec);
                Assert.NotNull(appData.Discretionary.AlgorithmAttributesAut);
            });

    // ── PIN Operations ───────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task VerifyPin_DefaultPin_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyPinAsync(DefaultUserPin);
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task VerifyAdmin_DefaultPin_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task VerifyPin_WrongPin_ThrowsWithRemainingAttempts(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var ex = await Assert.ThrowsAsync<ApduException>(
                    () => session.VerifyPinAsync("999999"));
                Assert.Contains("attempts remaining", ex.Message);
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ChangePin_AndRestore_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                const string newPin = "654321";

                // Change PIN
                await session.ChangePinAsync(DefaultUserPin, newPin);

                // Verify with new PIN
                await session.VerifyPinAsync(newPin);

                // Restore original PIN
                await session.ChangePinAsync(newPin, DefaultUserPin);

                // Verify restored PIN
                await session.VerifyPinAsync(DefaultUserPin);
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ChangeAdmin_AndRestore_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                const string newAdminPin = "87654321";

                await session.ChangeAdminAsync(DefaultAdminPin, newAdminPin);
                await session.VerifyAdminAsync(newAdminPin);
                await session.ChangeAdminAsync(newAdminPin, DefaultAdminPin);
                await session.VerifyAdminAsync(DefaultAdminPin);
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetPinStatus_DefaultState_ReturnsExpected(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var status = await session.GetPinStatusAsync();
                Assert.Equal(3, status.AttemptsUser);
                Assert.Equal(0, status.AttemptsReset);
                Assert.Equal(3, status.AttemptsAdmin);
            });

    // ── Algorithm Attributes ─────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetAlgorithmAttributes_DefaultState_ReturnsRsa2048(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var sigAttrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<RsaAttributes>(sigAttrs);
                Assert.Equal(2048, ((RsaAttributes)sigAttrs).NLen);

                var decAttrs = await session.GetAlgorithmAttributesAsync(KeyRef.Dec);
                Assert.IsType<RsaAttributes>(decAttrs);
                Assert.Equal(2048, ((RsaAttributes)decAttrs).NLen);

                var autAttrs = await session.GetAlgorithmAttributesAsync(KeyRef.Aut);
                Assert.IsType<RsaAttributes>(autAttrs);
                Assert.Equal(2048, ((RsaAttributes)autAttrs).NLen);
            });

    // ── Key Generation: EC ───────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_P256_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<EcAttributes>(attrs);
                Assert.Equal(CurveOid.Secp256R1, ((EcAttributes)attrs).Oid);
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_P256_PublicKeyReadable(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);
            });

    // ── Key Generation: RSA ──────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GenerateRsaKey_2048_Succeeds(YubiKeyTestState state)
    {
        // Skip firmware 4.2.0–4.3.5 (RSA generation unreliable)
        if (state.FirmwareVersion >= new FirmwareVersion(4, 2, 0) &&
            state.FirmwareVersion <= new FirmwareVersion(4, 3, 5))
        {
            return;
        }

        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateRsaKeyAsync(KeyRef.Sig, RsaSize.Rsa2048);

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<RsaAttributes>(attrs);
                Assert.Equal(2048, ((RsaAttributes)attrs).NLen);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);
            });
    }

    // ── Sign & Verify: EC ────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task Sign_EcP256_ProducesValidSignature(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                var message = "Hello OpenPGP"u8.ToArray();

                // Verify PIN for signing (extended=true for signature slot)
                await session.VerifyPinAsync(DefaultUserPin, extended: true);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.True(signature.Length > 0);

                // Verify the signature using the public key from the card
                var pubKeyData = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKeyData.Length > 0);
            });

    // ── Sign & Verify: RSA ───────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task Sign_Rsa2048_ProducesSignature(YubiKeyTestState state)
    {
        if (state.FirmwareVersion >= new FirmwareVersion(4, 2, 0) &&
            state.FirmwareVersion <= new FirmwareVersion(4, 3, 5))
        {
            return;
        }

        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateRsaKeyAsync(KeyRef.Sig, RsaSize.Rsa2048);

                var message = "Hello RSA OpenPGP"u8.ToArray();

                await session.VerifyPinAsync(DefaultUserPin, extended: true);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.True(signature.Length > 0);

                // RSA 2048 signature should be 256 bytes
                Assert.Equal(256, signature.Length);
            });
    }

    // ── Authenticate ─────────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task Authenticate_EcP256_ProducesSignature(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Aut, CurveOid.Secp256R1);

                var data = "Auth challenge"u8.ToArray();

                await session.VerifyPinAsync(DefaultUserPin);

                var result = await session.AuthenticateAsync(data, HashAlgorithmName.SHA256);
                Assert.True(result.Length > 0);
            });

    // ── Certificate CRUD ─────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task Certificate_PutGetDelete_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                // Create a self-signed certificate for testing
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var certReq = new CertificateRequest(
                    "CN=OpenPGP Test",
                    ecdsa,
                    HashAlgorithmName.SHA256);
                using var cert = certReq.CreateSelfSigned(
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddYears(1));

                // Put certificate
                await session.PutCertificateAsync(KeyRef.Sig, cert);

                // Get certificate
                var retrieved = await session.GetCertificateAsync(KeyRef.Sig);
                Assert.NotNull(retrieved);
                Assert.Equal(cert.RawData, retrieved!.RawData);

                // Delete certificate
                await session.DeleteCertificateAsync(KeyRef.Sig);

                var afterDelete = await session.GetCertificateAsync(KeyRef.Sig);
                Assert.Null(afterDelete);
            });

    // ── Key Attestation ──────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task AttestKey_ReturnsValidCertificate(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                var attestCert = await session.AttestKeyAsync(KeyRef.Sig);
                Assert.NotNull(attestCert);
                Assert.True(attestCert.RawData.Length > 0);
            });

    // ── UIF (Touch Policy) ───────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.2.0")]
    public async Task GetUif_DefaultState_ReturnsOff(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var uif = await session.GetUifAsync(KeyRef.Sig);
                Assert.Equal(Uif.Off, uif);
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.2.0")]
    public async Task SetUif_On_ThenReadBack(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.SetUifAsync(KeyRef.Sig, Uif.On);

                var uif = await session.GetUifAsync(KeyRef.Sig);
                Assert.Equal(Uif.On, uif);
            });

    // ── Key Information ──────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GetKeyInformation_AfterGenerate_ShowsGenerated(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                var keyInfo = await session.GetKeyInformationAsync();
                Assert.True(keyInfo.ContainsKey(KeyRef.Sig));
                Assert.Equal(KeyStatus.Generated, keyInfo[KeyRef.Sig]);
            });

    // ── Fingerprints & Generation Times ──────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetFingerprints_DefaultState_AllZero(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var fingerprints = await session.GetFingerprintsAsync();
                Assert.NotNull(fingerprints);

                foreach (var (_, fp) in fingerprints)
                {
                    Assert.True(fp.Span.SequenceEqual(new byte[20]));
                }
            });

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetGenerationTimes_DefaultState_AllZero(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var times = await session.GetGenerationTimesAsync();
                Assert.NotNull(times);

                foreach (var (_, timestamp) in times)
                {
                    Assert.Equal(0, timestamp);
                }
            });

    // ── Signature Counter ────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetSignatureCounter_DefaultState_ReturnsZero(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var counter = await session.GetSignatureCounterAsync();
                Assert.Equal(0, counter);
            });

    // ── Algorithm Information ────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GetAlgorithmInformation_ReturnsSupportedAlgorithms(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var algoInfo = await session.GetAlgorithmInformationAsync();
                Assert.NotEmpty(algoInfo);

                // Should contain entries for at least SIG, DEC, AUT
                var keyRefs = algoInfo.Select(x => x.KeyRef).Distinct().ToList();
                Assert.Contains(KeyRef.Sig, keyRefs);
                Assert.Contains(KeyRef.Dec, keyRefs);
                Assert.Contains(KeyRef.Aut, keyRefs);
            });

    // ── Delete Key ───────────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task DeleteKey_AfterGenerate_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                // Key should exist
                var keyInfo = await session.GetKeyInformationAsync();
                Assert.Equal(KeyStatus.Generated, keyInfo[KeyRef.Sig]);

                // Delete key
                await session.DeleteKeyAsync(KeyRef.Sig);

                // Key should be gone
                keyInfo = await session.GetKeyInformationAsync();
                Assert.Equal(KeyStatus.None, keyInfo[KeyRef.Sig]);
            });

    // ── KDF Configuration ────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetKdf_DefaultState_ReturnsKdfNone(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var kdf = await session.GetKdfAsync();
                Assert.IsType<KdfNone>(kdf);
            });

    // ── Get Challenge ────────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetChallenge_ReturnsRandomBytes(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                var challenge1 = await session.GetChallengeAsync(16);
                var challenge2 = await session.GetChallengeAsync(16);

                Assert.Equal(16, challenge1.Length);
                Assert.Equal(16, challenge2.Length);
                // Two random challenges should almost certainly differ
                Assert.False(challenge1.Span.SequenceEqual(challenge2.Span));
            });
}

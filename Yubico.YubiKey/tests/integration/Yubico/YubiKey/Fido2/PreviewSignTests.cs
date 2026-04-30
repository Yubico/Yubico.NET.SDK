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

using System;
using System.Linq;
using Xunit;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Integration tests for the previewSign extension.
    /// </summary>
    /// <remarks>
    /// These tests require a physical YubiKey with previewSign support (5.8.0-beta or newer).
    /// They will be skipped if no suitable device is found.
    /// </remarks>
    public class PreviewSignTests : FidoSessionIntegrationTestBase
    {
        // Defeat the macOS YubiKeyDeviceListener startup race before the
        // base class's instance ctor runs. See DeviceListenerCacheWarmup
        // for the full rationale.
        static PreviewSignTests() => DeviceListenerCacheWarmup.WaitForFirstDevice();

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialWithPreviewSign_ReturnsGeneratedKey()
        {
            Skip.IfNot(
                Session.AuthenticatorInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            MakeCredentialParameters.AddPreviewSignGenerateKeyExtension(
                Session.AuthenticatorInfo,
                new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 });

            var credData = Session.MakeCredential(MakeCredentialParameters);
            var isValid = credData.VerifyAttestation(MakeCredentialParameters.ClientDataHash);
            Assert.True(isValid);

            var generatedKey = credData.GetPreviewSignGeneratedKey();
            Assert.NotNull(generatedKey);
            Assert.NotEmpty(generatedKey.KeyHandle.Span.ToArray());
            Assert.Equal(65, generatedKey.BlindingPublicKey.Length);
            Assert.Equal(0x04, generatedKey.BlindingPublicKey.Span[0]);
            Assert.Equal(65, generatedKey.KemPublicKey.Length);
            Assert.Equal(0x04, generatedKey.KemPublicKey.Span[0]);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void FullCeremony_RegisterDeriveSignVerify_RoundTrip()
        {
            Skip.IfNot(
                Session.AuthenticatorInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            // Step A: Register with previewSign (requires user presence - touch #1)
            MakeCredentialParameters.AddPreviewSignGenerateKeyExtension(
                Session.AuthenticatorInfo,
                new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 });

            var credData = Session.MakeCredential(MakeCredentialParameters);
            var generatedKey = credData.GetPreviewSignGeneratedKey();
            Assert.NotNull(generatedKey);

            // Step B: Offline derive public key
            byte[] ikm = new byte[32];
            new Random(42).NextBytes(ikm);
            byte[] ctx = System.Text.Encoding.ASCII.GetBytes("integration-test-ctx");

            var derived = generatedKey.DerivePublicKey(ikm, ctx);
            Assert.Equal(65, derived.PublicKey.Length);
            Assert.NotEmpty(derived.ArkgKeyHandle.Span.ToArray());

            // Step C: Sign with derived credential (requires user presence - touch #2)
            byte[] message = System.Text.Encoding.ASCII.GetBytes("hello-previewsign-integration-test");

            // sign-by-credential requires an allowList so the YubiKey knows
            // which credential to use; the firmware rejects the GetAssertion
            // at protocol level with "option or extension invalid" if it is
            // missing. Mirror what the upstream demo does: pass the FIDO2
            // credential ID returned from MakeCredential.
            byte[] credentialId = credData.AuthenticatorData.CredentialId!.Id.ToArray();
            GetAssertionParameters.AllowCredential(new CredentialId { Id = credentialId });

            GetAssertionParameters.AddPreviewSignByCredentialExtension(
                Session.AuthenticatorInfo,
                derived,
                message);

            var assertions = Session.GetAssertions(GetAssertionParameters);
            var signature = assertions[0].AuthenticatorData.GetPreviewSignSignature();
            Assert.NotNull(signature);

            // Step D: Offline verify signature
            bool verified = derived.VerifySignature(message, signature);
            Assert.True(verified);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialWithUnsupportedAlgorithm_Fails()
        {
            Skip.IfNot(
                Session.AuthenticatorInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            // Request previewSign with ES256 (not Esp256)
            // Hardware should reject this as unsupported for previewSign
            MakeCredentialParameters.AddPreviewSignGenerateKeyExtension(
                Session.AuthenticatorInfo,
                new[] { CoseAlgorithmIdentifier.ES256 });

            // YubiKey 5.8.0-beta rejects unsupported algorithms via
            // Fido2Exception (wrapping the underlying CTAP error code).
            Assert.Throws<Fido2Exception>(() => Session.MakeCredential(MakeCredentialParameters));
        }
    }
}

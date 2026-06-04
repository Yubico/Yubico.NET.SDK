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
using System.Formats.Cbor;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.TestUtilities;
using Yubico.YubiKey.TestUtilities.Fido2;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Integration tests for the previewSign extension.
    /// </summary>
        /// <remarks>
        /// These tests require a physical YubiKey with previewSign support (5.8.0-beta or newer).
        /// They will be skipped if no suitable device is found.
        /// WARNING: This code is for testing purposes only and is not intended to be a
        /// secure or complete implementation of ARKG.
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
                Session.AuthenticatorInfo.IsExtensionSupported(PreviewSignParametersExtensions.ExtensionName),
                "YubiKey does not advertise previewSign extension");

            MakeCredentialParameters.AddPreviewSignGenerateKeyExtension(
                Session.AuthenticatorInfo,
                new[] { PreviewSignParametersExtensions.ArkgP256ESP256 });

            var credData = Session.MakeCredential(MakeCredentialParameters);
            var isValid = credData.VerifyAttestation(MakeCredentialParameters.ClientDataHash);
            Assert.True(isValid);

            var generatedKey = credData.GetPreviewSignGeneratedKey();
            Assert.NotNull(generatedKey);
            Assert.NotEmpty(generatedKey.KeyHandle.Span.ToArray());
            Assert.NotEmpty(generatedKey.PublicKey.Span.ToArray());
            Assert.NotNull(generatedKey.AttestationObject.AuthenticatorData.EncodedCredentialPublicKey);
            Assert.Equal(
                generatedKey.AttestationObject.AuthenticatorData.EncodedCredentialPublicKey!.Value.ToArray(),
                generatedKey.PublicKey.ToArray());

            var innerAuthData = generatedKey.AttestationObject.AuthenticatorData;
            Assert.Equal(0, innerAuthData.SignatureCounter);
            Assert.NotNull(credData.AuthenticatorData.Aaguid);
            Assert.NotNull(innerAuthData.Aaguid);
            Assert.Equal(
                credData.AuthenticatorData.Aaguid!.Value.ToArray(),
                innerAuthData.Aaguid!.Value.ToArray());

            Assert.NotNull(credData.AuthenticatorData.CredentialId);
            Assert.NotNull(innerAuthData.CredentialId);
            Assert.NotEqual(
                credData.AuthenticatorData.CredentialId!.Id.ToArray(),
                innerAuthData.CredentialId!.Id.ToArray());

            Assert.NotNull(innerAuthData.Extensions);
            Assert.True(innerAuthData.Extensions!.TryGetValue(
                PreviewSignParametersExtensions.ExtensionName,
                out byte[]? innerPreviewSignOutput));
            Assert.True(TryReadPreviewSignFlags(innerPreviewSignOutput, out _));
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void FullCeremony_RegisterDeriveSignVerify_RoundTrip()
        {
            Skip.IfNot(
                Session.AuthenticatorInfo.IsExtensionSupported(PreviewSignParametersExtensions.ExtensionName),
                "YubiKey does not advertise previewSign extension");

            // Step A: Register with previewSign (requires user presence - touch #1)
            // WARNING: This code is for testing purposes only and is not intended to be a
            // secure or complete implementation of ARKG.
            MakeCredentialParameters.AddPreviewSignGenerateKeyExtension(
                Session.AuthenticatorInfo,
                new[] { PreviewSignParametersExtensions.ArkgP256ESP256 });

            var mcData = Session.MakeCredential(MakeCredentialParameters);
            var generatedKey = mcData.GetPreviewSignGeneratedKey();
            Assert.NotNull(generatedKey);

            // Step B: Offline derive public key
            // WARNING: This code is for testing purposes only and is not intended to be a
            // secure or complete implementation of ARKG.
            byte[] ikm = new byte[32];
            RandomNumberGenerator.Fill(ikm);
            byte[] ctx = System.Text.Encoding.ASCII.GetBytes("integration-test-ctx");

            var derived = generatedKey.DerivePublicKey(ikm, ctx);
            Assert.Equal(65, derived.PublicKey.Length);
            Assert.NotEmpty(derived.ArkgKeyHandle.Span.ToArray());

            // Step C: Sign with derived credential (requires user presence - touch #2)
            byte[] message = System.Text.Encoding.ASCII.GetBytes("hello-previewsign-integration-test");

            // Hash the message before signing
            byte[] tbs;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                tbs = sha.ComputeHash(message);
            }

            // previewSign requires an allowList so the YubiKey knows which credential to use;
            // the firmware rejects the GetAssertion at protocol level with "option or extension
            // invalid" if it is missing. Pass the FIDO2 credential ID returned from MakeCredential.
            byte[] credentialId = mcData.AuthenticatorData.CredentialId!.Id.ToArray();
            GetAssertionParameters.AllowCredential(new CredentialId { Id = credentialId });

            GetAssertionParameters.AddPreviewSignExtension(
                derived.DeviceKeyHandle,
                derived.ArkgKeyHandle,
                derived.Context,
                tbs);

            var assertions = Session.GetAssertions(GetAssertionParameters);
            var signature = assertions[0].AuthenticatorData.GetPreviewSignSignature();
            Assert.NotNull(signature);

            // Step D: Offline verify signature
            bool verified = derived.VerifySignature(message, signature);
            Assert.True(verified);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialWithUnsupportedAlgorithm_FailsWithUnsupportedAlgorithmStatus()
        {
            Skip.IfNot(
                Session.AuthenticatorInfo.IsExtensionSupported(PreviewSignParametersExtensions.ExtensionName),
                "YubiKey does not advertise previewSign extension");

            MakeCredentialParameters.AddPreviewSignGenerateKeyExtension(
                Session.AuthenticatorInfo,
                new[] { (CoseAlgorithmIdentifier)(-18) });

            var response = Connection.SendCommand(new MakeCredentialCommand(MakeCredentialParameters));

            Assert.Equal(CtapStatus.UnsupportedAlgorithm, response.CtapStatus);
        }

        private static bool TryReadPreviewSignFlags(byte[] encodedPreviewSignOutput, out int flags)
        {
            flags = 0;
            var reader = new CborReader(encodedPreviewSignOutput, CborConformanceMode.Ctap2Canonical);
            if (reader.PeekState() != CborReaderState.StartMap)
            {
                return false;
            }

            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = reader.ReadInt32();
                if (key == 4)
                {
                    flags = reader.ReadInt32();
                    found = true;
                    continue;
                }

                reader.SkipValue();
            }

            reader.ReadEndMap();
            return found;
        }
    }
}

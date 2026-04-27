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
using Xunit;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Integration tests for the previewSign extension.
    /// </summary>
    /// <remarks>
    /// These tests require a physical YubiKey with previewSign support (5.8.0-beta or newer).
    /// They will be skipped if no suitable device is found.
    /// </remarks>
    public class PreviewSignTests : SimpleIntegrationTestConnection
    {
        private readonly PinUvAuthProtocolTwo _protocol;
        private readonly AuthenticatorInfo _deviceInfo;

        public PreviewSignTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Fw5)
        {
            _protocol = new PinUvAuthProtocolTwo();
            var getKeyRsp = Connection.SendCommand(new GetKeyAgreementCommand(_protocol.Protocol));
            Assert.Equal(ResponseStatus.Success, getKeyRsp.Status);

            _protocol.Encapsulate(getKeyRsp.GetData());

            var infoRsp = Connection.SendCommand(new GetInfoCommand());
            _deviceInfo = infoRsp.GetData();
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialWithPreviewSign_ReturnsGeneratedKey()
        {
            // Phase 1: Skeleton implementation - will throw NotImplementedException when YubiKey is present
            // Skip if previewSign extension is not supported by the YubiKey
            Skip.IfNot(
                _deviceInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            // TODO Phase 5+: Implement full test
            // 1. Create MakeCredentialParameters with previewSign extension
            // 2. Call MakeCredential (requires user presence - touch YubiKey)
            // 3. Verify MakeCredentialData contains generated key via GetPreviewSignGeneratedKey()
            // 4. Assert non-null KeyHandle, BlindingPublicKey, KemPublicKey

            throw new NotImplementedException("Phase 1: Test scaffolding only");
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void FullCeremony_RegisterDeriveSignVerify_RoundTrip()
        {
            // Phase 1: Skeleton implementation - will throw NotImplementedException when YubiKey is present
            // Skip if previewSign extension is not supported by the YubiKey
            Skip.IfNot(
                _deviceInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            // TODO Phase 5+: Implement full ceremony
            // Step A: Register with previewSign (requires user presence)
            // Step B: Offline derive public key using returned generated key
            // Step C: Sign with derived credential (requires user presence)
            // Step D: Offline verify signature with derived public key
            // Assert signature verification succeeds

            throw new NotImplementedException("Phase 1: Test scaffolding only");
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void GetAssertionWithUnsupportedAlgorithm_Throws()
        {
            // Phase 1: Skeleton implementation - will throw NotImplementedException when YubiKey is present
            // Skip if previewSign extension is not supported by the YubiKey
            Skip.IfNot(
                _deviceInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            // TODO Phase 5+: Implement unsupported algorithm test
            // Create credential with ES256 (if possible, may need to skip this test if no unsupported alg available)
            // Attempt to sign with ES256 (not Esp256)
            // Assert appropriate exception or error response (requires user presence)

            throw new NotImplementedException("Phase 1: Test scaffolding only");
        }

        // Helper methods will be added in Phase 5+ following the pattern from HmacSecretTests
        // private bool GetMakeCredentialParams(out MakeCredentialParameters makeParams) { ... }
        // private bool GetGetAssertionParams(out GetAssertionParameters assertionParams) { ... }
        // private bool GetPinToken(PinUvAuthTokenPermissions permissions, out byte[] pinToken) { ... }
    }
}

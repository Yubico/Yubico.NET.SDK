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
            Skip.IfNot(
                _deviceInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            var isValid = GetMakeCredentialParams(out var makeParams);
            Assert.True(isValid);

            makeParams.AddPreviewSignGenerateKeyExtension(
                _deviceInfo,
                new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 });

            var cmd = new MakeCredentialCommand(makeParams);
            var rsp = Connection.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);

            var credData = rsp.GetData();
            isValid = credData.VerifyAttestation(makeParams.ClientDataHash);
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
                _deviceInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            // Step A: Register with previewSign (requires user presence - touch #1)
            var isValid = GetMakeCredentialParams(out var makeParams);
            Assert.True(isValid);

            makeParams.AddPreviewSignGenerateKeyExtension(
                _deviceInfo,
                new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 });

            var makeCmd = new MakeCredentialCommand(makeParams);
            var makeRsp = Connection.SendCommand(makeCmd);
            Assert.Equal(ResponseStatus.Success, makeRsp.Status);

            var credData = makeRsp.GetData();
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

            isValid = GetGetAssertionParams(out var assertionParams);
            Assert.True(isValid);

            assertionParams.AddPreviewSignByCredentialExtension(_deviceInfo, derived, message);

            var assertCmd = new GetAssertionCommand(assertionParams);
            var assertRsp = Connection.SendCommand(assertCmd);
            Assert.Equal(ResponseStatus.Success, assertRsp.Status);

            var assertData = assertRsp.GetData();
            var signature = assertData.AuthenticatorData.GetPreviewSignSignature();
            Assert.NotNull(signature);

            // Step D: Offline verify signature
            bool verified = derived.VerifySignature(message, signature);
            Assert.True(verified);
        }

        [SkippableFact(typeof(DeviceNotFoundException))]
        public void MakeCredentialWithUnsupportedAlgorithm_Fails()
        {
            Skip.IfNot(
                _deviceInfo.IsExtensionSupported(Extensions.PreviewSign),
                "YubiKey does not advertise previewSign extension");

            var isValid = GetMakeCredentialParams(out var makeParams);
            Assert.True(isValid);

            // Request previewSign with ES256 (not Esp256)
            // Hardware should reject this as unsupported for previewSign
            makeParams.AddPreviewSignGenerateKeyExtension(
                _deviceInfo,
                new[] { CoseAlgorithmIdentifier.ES256 });

            var cmd = new MakeCredentialCommand(makeParams);
            var rsp = Connection.SendCommand(cmd);

            // YubiKey 5.8.0-beta should reject unsupported algorithms
            Assert.NotEqual(ResponseStatus.Success, rsp.Status);
        }

        private bool GetMakeCredentialParams(out MakeCredentialParameters makeParams)
        {
            makeParams = new MakeCredentialParameters(FidoSessionIntegrationTestBase.Rp, FidoSessionIntegrationTestBase.UserEntity);

            if (!GetPinToken(PinUvAuthTokenPermissions.MakeCredential, out var pinToken))
            {
                return false;
            }

            var pinUvAuthParam = _protocol.AuthenticateUsingPinToken(pinToken, FidoSessionIntegrationTestBase.ClientDataHash);

            makeParams.ClientDataHash = FidoSessionIntegrationTestBase.ClientDataHash;
            makeParams.Protocol = _protocol.Protocol;
            makeParams.PinUvAuthParam = pinUvAuthParam;

            makeParams.AddOption(AuthenticatorOptions.rk, true);

            return true;
        }

        private bool GetGetAssertionParams(out GetAssertionParameters assertionParams)
        {
            assertionParams = new GetAssertionParameters(FidoSessionIntegrationTestBase.Rp, FidoSessionIntegrationTestBase.ClientDataHash);

            var permissions =
                PinUvAuthTokenPermissions.GetAssertion | PinUvAuthTokenPermissions.CredentialManagement;

            if (!GetPinToken(permissions, out var pinToken))
            {
                return false;
            }

            var pinUvAuthParam = _protocol.AuthenticateUsingPinToken(pinToken, FidoSessionIntegrationTestBase.ClientDataHash);

            assertionParams.Protocol = _protocol.Protocol;
            assertionParams.PinUvAuthParam = pinUvAuthParam;

            return true;
        }

        private bool GetPinToken(
            PinUvAuthTokenPermissions permissions,
            out byte[] pinToken)
        {
            pinToken = Array.Empty<byte>();

            string? rpId = null;
            if (permissions.HasFlag(PinUvAuthTokenPermissions.MakeCredential) ||
                permissions.HasFlag(PinUvAuthTokenPermissions.GetAssertion))
            {
                rpId = FidoSessionIntegrationTestBase.Rp.Id;
            }

            ResponseStatus status;
            do
            {
                var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(_protocol, FidoSessionIntegrationTestBase.TestPin1, permissions, rpId);
                var getTokenRsp = Connection.SendCommand(getTokenCmd);
                if (getTokenRsp.Status == ResponseStatus.Success)
                {
                    pinToken = getTokenRsp.GetData().ToArray();
                    return true;
                }

                if (getTokenRsp.StatusWord != 0x6F35)
                {
                    return false;
                }

                var setPinCmd = new SetPinCommand(_protocol, FidoSessionIntegrationTestBase.TestPin1);
                var setPinRsp = Connection.SendCommand(setPinCmd);
                status = setPinRsp.Status;

            } while (status == ResponseStatus.Success);

            return false;
        }
    }
}

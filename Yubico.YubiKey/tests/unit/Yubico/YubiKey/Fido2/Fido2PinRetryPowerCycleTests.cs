// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Security;
using NSubstitute;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    public class Fido2PinRetryPowerCycleTests
    {
        [Theory]
        [InlineData(CtapStatus.PowerCycleRequired)]
        [InlineData(CtapStatus.PinBlocked)]
        public void TryVerifyPin_NonPinInvalid_PreservesCtapStatus(CtapStatus status)
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>()).Returns(TokenError(status));

            using var session = CreateSession(connection);

            Fido2Exception exception = Assert.Throws<Fido2Exception>(() =>
                session.TryVerifyPin("0000"u8.ToArray(), PinUvAuthTokenPermissions.GetAssertion, "example.com", out _, out _));

            Assert.Equal(status, exception.Status);
        }

        [Fact]
        public void KeyCollector_PowerCycleRequired_DoesNotSubmitAnotherPin()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>()).Returns(TokenError(CtapStatus.PinInvalid));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>()).Returns(PinRetries(7, true));

            using var session = CreateSession(connection);
            var submissions = InstallPinCollector(session, maximumSubmissions: 1);

            Fido2Exception exception = Assert.Throws<Fido2Exception>(() =>
                session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "example.com"));

            Assert.Equal(CtapStatus.PowerCycleRequired, exception.Status);
            Assert.Single(submissions);
            _ = connection.Received(1).SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>());
        }

        [Fact]
        public void KeyCollector_OrdinaryPinInvalid_Retries()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>()).Returns(TokenError(CtapStatus.PinInvalid));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>()).Returns(PinRetries(7, false));

            using var session = CreateSession(connection);
            var submissions = InstallPinCollector(session, maximumSubmissions: 2);

            Assert.False(session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "example.com"));
            Assert.Equal(new[] { false, true }, submissions);
        }

        [Fact]
        public void KeyCollector_NoRetriesRemaining_ThrowsSecurityException()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>()).Returns(TokenError(CtapStatus.PinInvalid));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>()).Returns(PinRetries(0, null));

            using var session = CreateSession(connection);
            var submissions = InstallPinCollector(session);

            _ = Assert.Throws<SecurityException>(() =>
                session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "example.com"));

            Assert.Single(submissions);
        }

        private static List<bool> InstallPinCollector(Fido2Session session, int maximumSubmissions = int.MaxValue)
        {
            var submissions = new List<bool>();
            session.KeyCollector = keyEntryData =>
            {
                if (keyEntryData.Request == KeyEntryRequest.Release)
                {
                    return true;
                }

                if (submissions.Count == maximumSubmissions)
                {
                    return false;
                }

                submissions.Add(keyEntryData.IsRetry);
                keyEntryData.SubmitValue("0000"u8.ToArray());
                return true;
            };

            return submissions;
        }

        private static Fido2Session CreateSession(IYubiKeyConnection connection)
        {
            var device = Substitute.For<IYubiKeyDevice>();
            _ = device.Connect(YubiKeyApplication.Fido2).Returns(connection);
            return new Fido2Session(device);
        }

        private static void ConfigureBaseline(IYubiKeyConnection connection)
        {
            _ = connection.SendCommand(Arg.Any<GetInfoCommand>())
                .Returns(new GetInfoResponse(new ResponseApdu(PinEnabledInfo(), SWConstants.Success)));
            _ = connection.SendCommand(Arg.Any<GetKeyAgreementCommand>())
                .Returns(new GetKeyAgreementResponse(new ResponseApdu(KeyAgreementData(), SWConstants.Success)));
        }

        private static GetPinUvAuthTokenResponse TokenError(CtapStatus status) =>
            new GetPinUvAuthTokenResponse(
                new ResponseApdu(Array.Empty<byte>(), unchecked((short)(0x6F00 | (byte)status))));

        private static GetPinRetriesResponse PinRetries(int retriesRemaining, bool? powerCycleRequired)
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(powerCycleRequired.HasValue ? 2 : 1);
            writer.WriteInt32(3);
            writer.WriteInt32(retriesRemaining);
            if (powerCycleRequired.HasValue)
            {
                writer.WriteInt32(4);
                writer.WriteBoolean(powerCycleRequired.Value);
            }

            writer.WriteEndMap();
            return new GetPinRetriesResponse(new ResponseApdu(writer.Encode(), SWConstants.Success));
        }

        private static byte[] PinEnabledInfo()
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(4);
            writer.WriteInt32(1);
            writer.WriteStartArray(1);
            writer.WriteTextString("FIDO_2_1");
            writer.WriteEndArray();
            writer.WriteInt32(3);
            writer.WriteByteString(new byte[16]);
            writer.WriteInt32(4);
            writer.WriteStartMap(2);
            writer.WriteTextString("clientPin");
            writer.WriteBoolean(true);
            writer.WriteTextString("pinUvAuthToken");
            writer.WriteBoolean(true);
            writer.WriteEndMap();
            writer.WriteInt32(6);
            writer.WriteStartArray(1);
            writer.WriteInt32(1);
            writer.WriteEndArray();
            writer.WriteEndMap();
            return writer.Encode();
        }

        private static byte[] KeyAgreementData() =>
            new byte[]
            {
                0xA1, 0x01,
                0xA5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01,
                0x21, 0x58, 0x20,
                0x6B, 0x17, 0xD1, 0xF2, 0xE1, 0x2C, 0x42, 0x47,
                0xF8, 0xBC, 0xE6, 0xE5, 0x63, 0xA4, 0x40, 0xF2,
                0x77, 0x03, 0x7D, 0x81, 0x2D, 0xEB, 0x33, 0xA0,
                0xF4, 0xA1, 0x39, 0x45, 0xD8, 0x98, 0xC2, 0x96,
                0x22, 0x58, 0x20,
                0x4F, 0xE3, 0x42, 0xE2, 0xFE, 0x1A, 0x7F, 0x9B,
                0x8E, 0xE7, 0xEB, 0x4A, 0x7C, 0x0F, 0x9E, 0x16,
                0x2B, 0xCE, 0x33, 0x57, 0x6B, 0x31, 0x5E, 0xCE,
                0xCB, 0xB6, 0x40, 0x68, 0x37, 0xBF, 0x51, 0xF5
            };
    }
}

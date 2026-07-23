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
using System.Runtime.CompilerServices;
using System.Security;
using NSubstitute;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Regression tests for the FIDO2 PIN_AUTH_BLOCKED (CtapStatus.PowerCycleRequired, 0x34)
    /// handling in <see cref="Fido2Session"/>. These assert the FIXED behavior:
    /// the actual CTAP status is preserved on the thrown <see cref="Fido2Exception"/>, and the
    /// KeyCollector loop stops (rather than resubmitting) once the authenticator reports it
    /// requires a power cycle.
    /// </summary>
    public class Fido2PinRetryPowerCycleTests
    {
        // ---- Low-level overload -------------------------------------------------

        [Fact]
        public void LowLevel_PowerCycleRequired_ThrowsExceptionCarryingStatus()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(TokenError(CtapStatus.PowerCycleRequired));

            using var session = CreateSession(connection);

            Fido2Exception exception = Assert.Throws<Fido2Exception>(() =>
                session.TryVerifyPin(
                    "0000"u8.ToArray(),
                    PinUvAuthTokenPermissions.GetAssertion,
                    "sdk-repro.example",
                    out int? _,
                    out bool? _));

            // The core defect fix: the CTAP status is no longer discarded.
            Assert.Equal(CtapStatus.PowerCycleRequired, exception.Status);

            // A blocked device is refused without a retry-count query.
            _ = connection.DidNotReceive().SendCommand(Arg.Any<GetPinRetriesCommand>());
        }

        [Fact]
        public void LowLevel_PinBlocked_ThrowsExceptionCarryingStatus()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(TokenError(CtapStatus.PinBlocked));

            using var session = CreateSession(connection);

            Fido2Exception exception = Assert.Throws<Fido2Exception>(() =>
                session.TryVerifyPin(
                    "0000"u8.ToArray(),
                    PinUvAuthTokenPermissions.GetAssertion,
                    "sdk-repro.example",
                    out int? _,
                    out bool? _));

            // The general status-preservation fix applies to every non-PinInvalid status.
            Assert.Equal(CtapStatus.PinBlocked, exception.Status);
        }

        [Fact]
        public void LowLevel_PinInvalidWithPowerCycleState_ReturnsFalseAndReportsReboot()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(TokenError(CtapStatus.PinInvalid));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>())
                .Returns(PinRetries(7, powerCycleRequired: true));

            using var session = CreateSession(connection);

            bool verified = session.TryVerifyPin(
                "0000"u8.ToArray(),
                PinUvAuthTokenPermissions.GetAssertion,
                "sdk-repro.example",
                out int? retriesRemaining,
                out bool? rebootRequired);

            Assert.False(verified);
            Assert.Equal(7, retriesRemaining);
            Assert.True(rebootRequired);
        }

        // ---- KeyCollector overload ---------------------------------------------

        [Fact]
        public void KeyCollector_PinInvalidTwiceThenPowerCycleRequired_ThreeSubmissionsAndStatusPreserved()
        {
            // Models firmware (e.g. 5.8.0) that omits powerCycleState: the third wrong
            // attempt itself returns 0x34. The cascade still occurs (no earlier signal),
            // but the fix ensures the resulting exception carries the real status.
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(
                    TokenError(CtapStatus.PinInvalid),
                    TokenError(CtapStatus.PinInvalid),
                    TokenError(CtapStatus.PowerCycleRequired));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>())
                .Returns(PinRetries(7, null), PinRetries(6, null));

            using var session = CreateSession(connection);
            var (submissions, releases) = InstallWrongPinCollector(session);

            Fido2Exception exception = Assert.Throws<Fido2Exception>(() =>
                session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "sdk-repro.example"));

            Assert.Equal(CtapStatus.PowerCycleRequired, exception.Status);
            Assert.Equal(3, submissions.Count);
            Assert.Equal(1, releases.Value);
            _ = connection.Received(3).SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>());
        }

        [Fact]
        public void KeyCollector_PowerCycleRequiredOnFirstAttempt_StopsWithStatus()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(TokenError(CtapStatus.PowerCycleRequired));

            using var session = CreateSession(connection);
            var (submissions, releases) = InstallWrongPinCollector(session);

            Fido2Exception exception = Assert.Throws<Fido2Exception>(() =>
                session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "sdk-repro.example"));

            Assert.Equal(CtapStatus.PowerCycleRequired, exception.Status);
            Assert.Single(submissions);
            Assert.Equal(1, releases.Value);
            _ = connection.Received(1).SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>());
            _ = connection.DidNotReceive().SendCommand(Arg.Any<GetPinRetriesCommand>());
        }

        [Fact]
        public void KeyCollector_PinInvalidWithPowerCycleState_StopsBeforeSecondSubmission()
        {
            // Models firmware that DOES report powerCycleState=true after a wrong PIN.
            // The fix must stop before re-prompting the collector, preventing an
            // avoidable second real attempt.
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(TokenError(CtapStatus.PinInvalid));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>())
                .Returns(PinRetries(7, powerCycleRequired: true));

            using var session = CreateSession(connection);
            var (submissions, releases) = InstallWrongPinCollector(session);

            Fido2Exception exception = Assert.Throws<Fido2Exception>(() =>
                session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "sdk-repro.example"));

            Assert.Equal(CtapStatus.PowerCycleRequired, exception.Status);
            Assert.Single(submissions); // exactly one attempt; no re-prompt
            Assert.Equal(1, releases.Value);
            _ = connection.Received(1).SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>());
            _ = connection.Received(1).SendCommand(Arg.Any<GetPinRetriesCommand>());
        }

        [Fact]
        public void KeyCollector_PinInvalidWithoutPowerCycleState_RetryIsStillPermitted()
        {
            // Regression guard: when powerCycleState is false/absent, ordinary automatic
            // retry behavior must be preserved (the collector is called again).
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(TokenError(CtapStatus.PinInvalid), TokenError(CtapStatus.PinInvalid));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>())
                .Returns(PinRetries(7, powerCycleRequired: false), PinRetries(6, powerCycleRequired: false));

            using var session = CreateSession(connection);

            var submissions = new List<bool>();
            int releaseCount = 0;
            session.KeyCollector = keyEntryData =>
            {
                if (keyEntryData.Request == KeyEntryRequest.Release)
                {
                    releaseCount++;
                    return true;
                }

                // Submit two wrong PINs, then cancel on the third callback.
                if (submissions.Count >= 2)
                {
                    return false;
                }

                submissions.Add(keyEntryData.IsRetry);
                keyEntryData.SubmitValue("0000"u8.ToArray());
                return true;
            };

            bool verified = session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "sdk-repro.example");

            Assert.False(verified);                 // cancelled, not thrown
            Assert.Equal(2, submissions.Count);     // retry was permitted
            Assert.False(submissions[0]);           // first is not a retry
            Assert.True(submissions[1]);            // second is a retry
            Assert.Equal(1, releaseCount);
            _ = connection.Received(2).SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>());
        }

        [Fact]
        public void KeyCollector_PinInvalidWithZeroRetries_ThrowsSecurityException()
        {
            var connection = Substitute.For<IYubiKeyConnection>();
            ConfigureBaseline(connection);
            _ = connection.SendCommand(Arg.Any<GetPinUvAuthTokenUsingPinCommand>())
                .Returns(TokenError(CtapStatus.PinInvalid));
            _ = connection.SendCommand(Arg.Any<GetPinRetriesCommand>())
                .Returns(PinRetries(0, powerCycleRequired: null));

            using var session = CreateSession(connection);
            var (submissions, releases) = InstallWrongPinCollector(session);

            _ = Assert.Throws<SecurityException>(() =>
                session.TryVerifyPin(PinUvAuthTokenPermissions.GetAssertion, "sdk-repro.example"));

            Assert.Single(submissions);
            Assert.Equal(1, releases.Value);
        }

        // ---- Helpers ------------------------------------------------------------

        private static (List<bool> Submissions, StrongBox<int> ReleaseCount) InstallWrongPinCollector(
            Fido2Session session)
        {
            var submissions = new List<bool>();
            var releaseCount = new StrongBox<int>(0);
            session.KeyCollector = keyEntryData =>
            {
                if (keyEntryData.Request == KeyEntryRequest.Release)
                {
                    releaseCount.Value++;
                    return true;
                }

                submissions.Add(keyEntryData.IsRetry);
                keyEntryData.SubmitValue("0000"u8.ToArray());
                return true;
            };
            return (submissions, releaseCount);
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

            var keyAgreementResponse = new GetKeyAgreementResponse(
                new ResponseApdu(KeyAgreementData(), SWConstants.Success));
            _ = connection.SendCommand(Arg.Any<GetKeyAgreementCommand>())
                .Returns(keyAgreementResponse, keyAgreementResponse, keyAgreementResponse);
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

        // A valid NIST P-256 generator point encoded as a COSE_Key inside a
        // ClientPin key-agreement response map ({1: COSE key}).
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

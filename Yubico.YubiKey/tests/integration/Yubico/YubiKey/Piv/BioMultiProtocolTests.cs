// Copyright 2024 Yubico AB
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
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;
using Yubico.PlatformInterop;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class BioMultiProtocolTests
    {
        /// <summary>
        /// Verify authentication with YubiKey Bio Multi-protocol
        /// </summary>
        /// <remarks>
        /// To run the test, create a PIN and enroll at least one fingerprint. The test will ask twice
        /// for fingerprint authentication.
        /// <para>
        /// Tests with devices without Bio Metadata are skipped.
        /// </remarks>
        /// <param name="testDeviceType"></param>
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(StandardTestDevice.Fw5Bio)]
        public void BioMultiProtocol_Authenticate(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            using (var pivSession = new PivSession(testDevice))
            {
                var bioMetadata = pivSession.GetBioMetadata();
                var connection = pivSession.Connection;

                Assert.True(VerifyUv(connection, false, false).IsEmpty);
                Assert.False(pivSession.GetBioMetadata().HasTemporaryPin);

                // check verified state
                Assert.True(VerifyUv(connection, false, true).IsEmpty);
            }
        }

        /// <summary>
        /// Verify that AttemptsRemaining value in results is correctly reported
        /// </summary>
        /// <remarks>
        /// To run the test, create a PIN and enroll at least one fingerprint.
        /// The test will try to authenticate several times and requires following interaction:
        /// 1st biometric authentication: provide successful match
        /// 2nd: provide invalid match (attempts remaining is now 2)
        /// 3rd: provide invalid match (attempts remaining is now 1)
        /// 4th: provide invalid match (attempts remaining is now 0 and the biometric verification is blocked)
        /// The test calls VerifyPin to reset the biometric attempts to 3
        /// <para>
        /// Tests with devices without Bio Metadata are skipped.
        /// </remarks>
        /// <param name="testDeviceType"></param>
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(StandardTestDevice.Fw5Bio)]
        public void BioMultiProtocol_AttemptsRemaining(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            using (var pivSession = new PivSession(testDevice))
            {
                var connection = pivSession.Connection;

                var response = VerifyUv(connection, false, false);

                Assert.Equal(3, pivSession.GetBioMetadata().AttemptsRemaining);

                response = VerifyUv(connection, false, false, out int? attemptsRemaining);
                Assert.True(response.IsEmpty);
                Assert.Equal(2, attemptsRemaining);

                response = VerifyUv(connection, false, false, out attemptsRemaining);
                Assert.True(response.IsEmpty);
                Assert.Equal(1, attemptsRemaining);

                // this last attempt will invalidate the biometric verification
                response = VerifyUv(connection, false, false, out attemptsRemaining);
                Assert.True(response.IsEmpty);
                Assert.Equal(0, attemptsRemaining);
                Assert.Equal(0, pivSession.GetBioMetadata().AttemptsRemaining);

                // authenticate with PIN 
                Assert.Null(VerifyPin(connection));
                Assert.Equal(3, pivSession.GetBioMetadata().AttemptsRemaining);
            }
        }

        /// <summary>
        /// Verify that using temporary PIN reports expected results
        /// </summary>
        /// <remarks>
        /// To run the test, create a PIN and enroll at least one fingerprint.
        /// The test will need two successful matches.
        /// <para>
        /// Tests with devices without Bio Metadata are skipped.
        /// </remarks>
        /// <param name="testDeviceType"></param>
        [SkippableTheory(typeof(NotSupportedException))]
        [InlineData(StandardTestDevice.Fw5Bio)]
        public void BioMultiProtocol_TemporaryPin(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            using (var pivSession = new PivSession(testDevice))
            {
                var connection = pivSession.Connection;
                var temporaryPin = VerifyUv(connection, true, false);
                Assert.True(pivSession.GetBioMetadata().HasTemporaryPin);

                // use invalid temporary pin
                Assert.False(VerifyTemporaryPin(connection,
                    Encoding.ASCII.GetBytes("0102030405060708")));
                Assert.False(pivSession.GetBioMetadata().HasTemporaryPin);


                temporaryPin = VerifyUv(connection, true, false);
                Assert.False(temporaryPin.IsEmpty);
                Assert.True(pivSession.GetBioMetadata().HasTemporaryPin);

                Assert.True(VerifyTemporaryPin(connection, temporaryPin));
                Assert.True(pivSession.GetBioMetadata().HasTemporaryPin);
            }
        }

        // helper methods
        private ReadOnlyMemory<byte> VerifyUv(
            IYubiKeyConnection connection,
            bool requestTemporaryPin,
            bool checkOnly,
            out int? attemptsRemaining)
        {
            var command = new VerifyUvCommand(requestTemporaryPin, checkOnly);
            var response = connection.SendCommand(command);
            attemptsRemaining = response.AttemptsRemaining;
            return response.GetData();
        }

        private ReadOnlyMemory<byte> VerifyUv(
            IYubiKeyConnection connection,
            bool requestTemporaryPin,
            bool checkOnly)
        {
            var command = new VerifyUvCommand(requestTemporaryPin, checkOnly);
            var response = connection.SendCommand(command);
            return response.GetData();
        }

        private bool VerifyTemporaryPin(IYubiKeyConnection connection, ReadOnlyMemory<byte> temporaryPin)
        {
            var command = new VerifyTemporaryPinCommand(temporaryPin);
            return connection.SendCommand(command).Status == ResponseStatus.Success;
        }

        private int? VerifyPin(
            IYubiKeyConnection connection)
        {
            byte[] pin = Encoding.ASCII.GetBytes("12345678");
            var command = new VerifyPinCommand(pin);
            var response = connection.SendCommand(command);
            return response.GetData();
        }
    }
}

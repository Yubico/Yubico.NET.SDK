// Copyright 2021 Yubico AB
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
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    // All these tests will reset the PIV application, run, then reset the PIV
    // application again.
    public class PinOnlyStateTests : IDisposable
    {
        private const int AdminDataTag = 0x005FFF00;
        private const int PrintedTag = (int)PivDataTag.Printed;
        private readonly bool _alternateAlgorithm;
        private readonly IYubiKeyDevice _yubiKey;
        private readonly SpecifiedKeyCollector _specifiedCollector;
        private readonly Simple39KeyCollector _collectorObj;
        private readonly ReadOnlyMemory<byte> _defaultManagementKey;
        private readonly ReadOnlyMemory<byte> _defaultPin;

        private readonly byte[] _keyBytes = {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        };
        private readonly byte[] _pinBytes = {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36
        };

        public PinOnlyStateTests()
        {
            _specifiedCollector = new SpecifiedKeyCollector(
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 },
                new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38 },
                new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            );
            _collectorObj = new Simple39KeyCollector();
            _defaultManagementKey = new ReadOnlyMemory<byte>(_keyBytes);
            _defaultPin = new ReadOnlyMemory<byte>(_pinBytes);

            _yubiKey = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            if (_yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey))
            {
                _alternateAlgorithm = true;
            }

            ResetPiv(_yubiKey);
        }

        public void Dispose()
        {
            ResetPiv(_yubiKey);
        }

        // Start with a ResetPiv. Then set to mode/algorithm. Then set the
        // appropriate mode to unavailable.
        // Verify the result of GetPinOnlyMode.
        // Now set the YubiKey to None.
        // Verify the result of GetPinOnlyMode.
        // Verify the contents of ADMIN DATA and PRINTED are such that the state
        // is None.
        // Verify that the mgmt key is the default.
        [Theory]
        [InlineData(PivPinOnlyMode.PinProtected, PivAlgorithm.Aes128, PivPinOnlyMode.PinDerivedUnavailable)]
        [InlineData(PivPinOnlyMode.PinDerived, PivAlgorithm.TripleDes, PivPinOnlyMode.PinProtectedUnavailable)]
        [InlineData(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, PivAlgorithm.Aes192, PivPinOnlyMode.None)]
        [InlineData(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, PivAlgorithm.Aes128, PivPinOnlyMode.PinDerivedUnavailable)]
        public void ResetToNone_Success(
            PivPinOnlyMode mode, PivAlgorithm algorithm, PivPinOnlyMode unavailable)
        {
            if (!_alternateAlgorithm && !(algorithm == PivAlgorithm.TripleDes))
            {
                return;
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(mode, algorithm);

                SetUnavailable(pivSession, unavailable);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                PivPinOnlyMode expectedMode = GetExpectedMode(mode, unavailable);

                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(expectedMode, currentMode);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = _specifiedCollector.SpecifiedKeyCollectorDelegate;
                pivSession.SetPinOnlyMode(PivPinOnlyMode.None, algorithm);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                bool isValid = pivSession.TryAuthenticateManagementKey(_defaultManagementKey);
                Assert.True(isValid);

                PivPinOnlyMode expectedMode = GetExpectedMode(PivPinOnlyMode.None, unavailable);

                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();

                Assert.Equal(expectedMode, currentMode);

                isValid = AreContentsExpected(pivSession, PrintedTag, PivPinOnlyMode.None, unavailable);
                Assert.True(isValid);
                isValid = AreContentsExpected(pivSession, AdminDataTag, PivPinOnlyMode.None, unavailable);
                Assert.True(isValid);
            }
        }

        // Start with None, with the given mode unavailable, then try to set it
        // to None.
        [Theory]
        [InlineData(PivPinOnlyMode.PinDerivedUnavailable)]
        [InlineData(PivPinOnlyMode.PinProtectedUnavailable)]
        [InlineData(PivPinOnlyMode.PinDerivedUnavailable | PivPinOnlyMode.PinProtectedUnavailable)]
        public void StartWithNone_Unavailable_None(PivPinOnlyMode unavailable)
        {
            using (var pivSession = new PivSession(_yubiKey))
            {
                bool isValid = pivSession.TryAuthenticateManagementKey(_defaultManagementKey);
                Assert.True(isValid);

                isValid = pivSession.TryVerifyPin(_defaultPin, out int? _);
                Assert.True(isValid);

                SetUnavailable(pivSession, unavailable);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(PivPinOnlyMode.None, PivAlgorithm.TripleDes);

                PivPinOnlyMode expectedMode = GetExpectedMode(PivPinOnlyMode.None, unavailable);
                PivPinOnlyMode currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(expectedMode, currentMode);
            }
        }

        // Set a mode, then set something unavailable. Next, recover.
        // Specify a mode and unavailable such that the return from recover is
        // the original mode.
        [Theory]
        [InlineData(PivPinOnlyMode.PinProtected, PivPinOnlyMode.PinDerivedUnavailable)]
        [InlineData(PivPinOnlyMode.PinDerived, PivPinOnlyMode.PinProtectedUnavailable)]
        [InlineData(PivPinOnlyMode.PinDerived | PivPinOnlyMode.PinProtected, PivPinOnlyMode.PinProtectedUnavailable)]
        public void Unavailable_Recover(PivPinOnlyMode mode, PivPinOnlyMode unavailable)
        {
            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = _collectorObj.Simple39KeyCollectorDelegate;

                pivSession.SetPinOnlyMode(mode, PivAlgorithm.TripleDes);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = _specifiedCollector.SpecifiedKeyCollectorDelegate;
                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                SetUnavailable(pivSession, unavailable);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = _specifiedCollector.SpecifiedKeyCollectorDelegate;
                PivPinOnlyMode recoveredMode = pivSession.TryRecoverPinOnlyMode();
                Assert.Equal(mode, recoveredMode);

                PivPinOnlyMode getMode = pivSession.GetPinOnlyMode();
                Assert.Equal(mode, getMode);
            }
        }

        private bool AreContentsExpected(
            PivSession pivSession, int tag, PivPinOnlyMode currentMode, PivPinOnlyMode unavailable)
        {
            bool isValid = pivSession.TryVerifyPin(_defaultPin, out int? _);
            if (!isValid)
            {
                return false;
            }

            // What are we checking? AdminData or Printed?
            PivPinOnlyMode currentCheck = PivPinOnlyMode.PinDerived;
            PivPinOnlyMode unavailableCheck = PivPinOnlyMode.PinDerivedUnavailable;
            if (tag != AdminDataTag)
            {
                currentCheck = PivPinOnlyMode.PinProtected;
                unavailableCheck = PivPinOnlyMode.PinProtectedUnavailable;
            }

            // Determine what we expect, based on the currentMode and unavailable.
            // If expected is 0, then the contents are expected to be empty.
            // If expected is 1, then the contents are expected to be correct.
            // If expected is 2, then the contents are expected to be not empty,
            // but not correct.
            int expected = 0;
            if (unavailable.HasFlag(unavailableCheck))
            {
                expected = 2;
            }
            else if (currentMode.HasFlag(currentCheck))
            {
                expected = 1;
            }

            var getDataCommand = new GetDataCommand(tag);
            GetDataResponse getDataResponse = pivSession.Connection.SendCommand(getDataCommand);

            if (getDataResponse.Status == ResponseStatus.NoData)
            {
                return expected == 0;
            }
            if (getDataResponse.Status != ResponseStatus.Success)
            {
                return false;
            }

            ReadOnlyMemory<byte> encodedData = getDataResponse.GetData();

            bool isEmpty = false;
            if (tag == AdminDataTag)
            {
                var adminData = new AdminData();
                isValid = adminData.TryDecode(encodedData);
                if (isValid)
                {
                    isEmpty = adminData.IsEmpty;
                }
            }

            if (tag == PrintedTag)
            {
                var pinProtect = new PinProtectedData();
                isValid = pinProtect.TryDecode(encodedData);
                if (isValid)
                {
                    isEmpty = pinProtect.IsEmpty;
                }
            }

            if (!isValid)
            {
                return expected == 2;
            }

            if (isEmpty)
            {
                return expected == 0;
            }

            return expected == 1;
        }

        private void SetUnavailable(PivSession pivSession, PivPinOnlyMode unavailable)
        {
            byte[] unexpectedData = { 0x53, 0x04, 0x02, 0x02, 0x00, 0xff };

            if (unavailable.HasFlag(PivPinOnlyMode.PinProtectedUnavailable))
            {
                var putCmd = new PutDataCommand(PrintedTag, unexpectedData);
                PutDataResponse putRsp = pivSession.Connection.SendCommand(putCmd);
                if (putRsp.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(putRsp.StatusMessage);
                }
            }

            if (unavailable.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                var putCmd = new PutDataCommand(0x005FFF00, unexpectedData);
                PutDataResponse putRsp = pivSession.Connection.SendCommand(putCmd);
                if (putRsp.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(putRsp.StatusMessage);
                }
            }
        }

        // Get the expected mode returned by TetPinOnlyMode, if the mode was set
        // to currentMode, then some element is set to unavailable, based on the
        // unavailable arg (which can be None).
        private PivPinOnlyMode GetExpectedMode(PivPinOnlyMode currentMode, PivPinOnlyMode unavailable)
        {
            // If unavailable is Derived, then no matter what currentMode is, the
            // expected mode is both unavailable.
            if (unavailable.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                return PivPinOnlyMode.PinProtectedUnavailable | PivPinOnlyMode.PinDerivedUnavailable;
            }

            // If Derived is not unavailable, then the result will be what ADMIN
            // DATA says. That will be what currentMode says.
            // Even if Protected is actually set to unavailable, if ADMIN DATA
            // says Protected, that's what will be returned.
            PivPinOnlyMode returnValue = PivPinOnlyMode.None;
            if (currentMode.HasFlag(PivPinOnlyMode.PinProtected))
            {
                returnValue |= PivPinOnlyMode.PinProtected;
            }
            if (currentMode.HasFlag(PivPinOnlyMode.PinDerived))
            {
                returnValue |= PivPinOnlyMode.PinDerived;
            }

            return returnValue;
        }

        private static void ResetPiv(IYubiKeyDevice yubiKey)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                pivSession.ResetApplication();
            }
        }
    }
}

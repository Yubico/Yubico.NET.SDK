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
using System.Text;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    // All these tests will reset the PIV application, run, then reset the PIV
    // application again.
    public class PinOnlyStateTests : PivSessionIntegrationTestBase
    {
        private const int AdminDataTag = 0x005FFF00;
        private const int PrintedTag = (int)PivDataTag.Printed;
        private readonly SpecifiedKeyCollector _specifiedCollector;

        public PinOnlyStateTests()
        {
            _specifiedCollector = new SpecifiedKeyCollector(
                DefaultPin,
                DefaultPuk,
                new byte[8]
            );
        }

        // Start with a ResetPiv. Then set to mode/keyType. Then set the
        // appropriate mode to unavailable.
        // Verify the result of GetPinOnlyMode.
        // Now set the YubiKey to None.
        // Verify the result of GetPinOnlyMode.
        // Verify the contents of ADMIN DATA and PRINTED are such that the state
        // is None.
        // Verify that the mgmt key is the default.
        [Theory]
        [InlineData(PivPinOnlyMode.PinProtected, KeyType.AES128, PivPinOnlyMode.PinDerivedUnavailable)]
        [InlineData(PivPinOnlyMode.PinDerived, KeyType.TripleDES, PivPinOnlyMode.PinProtectedUnavailable)]
        [InlineData(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, KeyType.AES192, PivPinOnlyMode.None)]
        [InlineData(PivPinOnlyMode.PinProtected | PivPinOnlyMode.PinDerived, KeyType.AES128,
            PivPinOnlyMode.PinDerivedUnavailable)]
        public void ResetToNone_Success(
            PivPinOnlyMode mode,
            KeyType keyType,
            PivPinOnlyMode unavailable)
        {
            var supportsAes = Device.HasFeature(YubiKeyFeature.PivAesManagementKey);
            Skip.If(!supportsAes);

            var defaultKeyType = supportsAes && Device.FirmwareVersion > FirmwareVersion.V5_7_0
                ? KeyType.AES192
                : KeyType.TripleDES;

            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.SetPinOnlyMode(mode, keyType.GetPivAlgorithm());
                SetUnavailable(pivSession, unavailable);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                var expectedMode = GetExpectedMode(mode, unavailable);
                var currentMode = pivSession.GetPinOnlyMode();
                Assert.Equal(expectedMode, currentMode);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.KeyCollector = _specifiedCollector.SpecifiedKeyCollectorDelegate;
                pivSession.SetPinOnlyMode(PivPinOnlyMode.None, keyType.GetPivAlgorithm());
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                Assert.Equal(defaultKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

                var isValid = pivSession.TryAuthenticateManagementKey(DefaultManagementKey);
                Assert.True(isValid);

                var expectedMode = GetExpectedMode(PivPinOnlyMode.None, unavailable);
                var currentMode = pivSession.GetPinOnlyMode();
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
        public void StartWithNone_Unavailable_None(
            PivPinOnlyMode unavailable)
        {
            using (var pivSession = GetSession(authenticate: false))
            {
                var isValid = pivSession.TryAuthenticateManagementKey(DefaultManagementKey);
                Assert.True(isValid);

                isValid = pivSession.TryVerifyPin(DefaultPin, out _);
                Assert.True(isValid);

                SetUnavailable(pivSession, unavailable);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.SetPinOnlyMode(PivPinOnlyMode.None, KeyType.TripleDES.GetPivAlgorithm());

                var expectedMode = GetExpectedMode(PivPinOnlyMode.None, unavailable);
                var currentMode = pivSession.GetPinOnlyMode();
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
        public void Unavailable_Recover(
            PivPinOnlyMode mode,
            PivPinOnlyMode unavailable)
        {
            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.SetPinOnlyMode(mode, KeyType.TripleDES.GetPivAlgorithm());
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.KeyCollector = _specifiedCollector.SpecifiedKeyCollectorDelegate;
                var isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                SetUnavailable(pivSession, unavailable);
            }

            using (var pivSession = GetSession(authenticate: false))
            {
                pivSession.KeyCollector = _specifiedCollector.SpecifiedKeyCollectorDelegate;
                var recoveredMode = pivSession.TryRecoverPinOnlyMode();
                Assert.Equal(mode, recoveredMode);

                var getMode = pivSession.GetPinOnlyMode();
                Assert.Equal(mode, getMode);
            }
        }

        private bool AreContentsExpected(
            PivSession pivSession,
            int tag,
            PivPinOnlyMode currentMode,
            PivPinOnlyMode unavailable)
        {
            var isValid = pivSession.TryVerifyPin(DefaultPin, out _);
            if (!isValid)
            {
                return false;
            }

            // What are we checking? AdminData or Printed?
            var currentCheck = PivPinOnlyMode.PinDerived;
            var unavailableCheck = PivPinOnlyMode.PinDerivedUnavailable;
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
            var expected = 0;
            if (unavailable.HasFlag(unavailableCheck))
            {
                expected = 2;
            }
            else if (currentMode.HasFlag(currentCheck))
            {
                expected = 1;
            }

            var getDataCommand = new GetDataCommand(tag);
            var getDataResponse = pivSession.Connection.SendCommand(getDataCommand);

            if (getDataResponse.Status == ResponseStatus.NoData)
            {
                return expected == 0;
            }

            if (getDataResponse.Status != ResponseStatus.Success)
            {
                return false;
            }

            var encodedData = getDataResponse.GetData();

            var isEmpty = false;
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

        private static void SetUnavailable(
            PivSession pivSession,
            PivPinOnlyMode unavailable)
        {
            byte[] unexpectedData = { 0x53, 0x04, 0x02, 0x02, 0x00, 0xff };

            if (unavailable.HasFlag(PivPinOnlyMode.PinProtectedUnavailable))
            {
                var putCmd = new PutDataCommand(PrintedTag, unexpectedData);
                var putRsp = pivSession.Connection.SendCommand(putCmd);
                if (putRsp.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(putRsp.StatusMessage);
                }
            }

            if (unavailable.HasFlag(PivPinOnlyMode.PinDerivedUnavailable))
            {
                var putCmd = new PutDataCommand(0x005FFF00, unexpectedData);
                var putRsp = pivSession.Connection.SendCommand(putCmd);
                if (putRsp.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(putRsp.StatusMessage);
                }
            }
        }

        // Get the expected mode returned by TetPinOnlyMode, if the mode was set
        // to currentMode, then some element is set to unavailable, based on the
        // unavailable arg (which can be None).
        private PivPinOnlyMode GetExpectedMode(
            PivPinOnlyMode currentMode,
            PivPinOnlyMode unavailable)
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
            var returnValue = PivPinOnlyMode.None;
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
    }
}

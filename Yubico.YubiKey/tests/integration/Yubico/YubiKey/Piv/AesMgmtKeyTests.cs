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
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class AesMgmtKeyTests : IDisposable
    {
        private readonly Memory<byte> _currentKey;
        private readonly byte[] _currentKeyBytes;
        private readonly Memory<byte> _newKey;
        private readonly byte[] _newKeyBytes;
        private readonly bool _runTest;
        private readonly IYubiKeyDevice _yubiKey;
        private int _currentKeyLength;
        private int _newKeyLength;

        public AesMgmtKeyTests()
        {
            _currentKeyBytes = new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            };
            _newKeyBytes = new byte[32];

            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            {
                random.GetBytes(_newKeyBytes);
            }

            _currentKey = new Memory<byte>(_currentKeyBytes);
            _newKey = new Memory<byte>(_newKeyBytes);
            _currentKeyLength = 24;
            _newKeyLength = 32;

            _yubiKey = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            if (_yubiKey.HasFeature(YubiKeyFeature.PivAesManagementKey))
            {
                _runTest = true;

                ResetPiv(_yubiKey);
            }
        }

        public void Dispose()
        {
            if (_runTest)
            {
                ResetPiv(_yubiKey);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128, 16, true)]
        [InlineData(PivAlgorithm.Aes128, 16, false)]
        [InlineData(PivAlgorithm.Aes192, 24, true)]
        [InlineData(PivAlgorithm.Aes192, 24, false)]
        [InlineData(PivAlgorithm.Aes256, 32, true)]
        [InlineData(PivAlgorithm.Aes256, 32, false)]
        [InlineData(PivAlgorithm.TripleDes, 24, true)]
        [InlineData(PivAlgorithm.TripleDes, 24, false)]
        public void ChangeMgmtKey_Auth_Succeeds(PivAlgorithm algorithm, int keySize, bool mutualAuth)
        {
            if (!_runTest)
            {
                return;
            }

            AuthenticateManagementKeyResult expectedResult = mutualAuth
                ? AuthenticateManagementKeyResult.MutualFullyAuthenticated
                : AuthenticateManagementKeyResult.SingleAuthenticated;

            SetKeyLengths(24, keySize);
            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                pivSession.AuthenticateManagementKey(mutualAuth);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                pivSession.ChangeManagementKey(PivTouchPolicy.None, algorithm);

                // The Change call will always use mutual auth.
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);

                // start temp
                //                pivSession.AuthenticateManagementKey(mutualAuth);
                //
                //                var setCmd = new SetManagementKeyCommand(
                //                    _newKey.Slice(0, keySize), PivTouchPolicy.Never, algorithm);
                //
                //                SetManagementKeyResponse setRsp = pivSession.Connection.SendCommand(setCmd);
                //                Assert.Equal(ResponseStatus.Success, setRsp.Status);
                //                Assert.True(pivSession.ManagementKeyAuthenticated);
                //                Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
                // end temp

                SwapKeys();
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);

                pivSession.AuthenticateManagementKey(mutualAuth);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128, 16, true)]
        [InlineData(PivAlgorithm.Aes128, 16, false)]
        [InlineData(PivAlgorithm.Aes192, 24, true)]
        [InlineData(PivAlgorithm.Aes192, 24, false)]
        [InlineData(PivAlgorithm.Aes256, 32, true)]
        [InlineData(PivAlgorithm.Aes256, 32, false)]
        [InlineData(PivAlgorithm.TripleDes, 24, true)]
        [InlineData(PivAlgorithm.TripleDes, 24, false)]
        public void ChangeMgmtKey_TryAuth_Succeeds(PivAlgorithm algorithm, int keySize, bool mutualAuth)
        {
            if (!_runTest)
            {
                return;
            }

            AuthenticateManagementKeyResult expectedResult = mutualAuth
                ? AuthenticateManagementKeyResult.MutualFullyAuthenticated
                : AuthenticateManagementKeyResult.SingleAuthenticated;

            SetKeyLengths(24, keySize);
            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                bool isValid = pivSession.TryAuthenticateManagementKey(mutualAuth);
                Assert.True(isValid);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                bool isValid = pivSession.TryChangeManagementKey(PivTouchPolicy.None, algorithm);
                Assert.True(isValid);

                // The Change call will always use mutual auth.
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);

                SwapKeys();
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);

                bool isValid = pivSession.TryAuthenticateManagementKey(mutualAuth);
                Assert.True(isValid);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Aes128, 16, true)]
        [InlineData(PivAlgorithm.Aes128, 16, false)]
        [InlineData(PivAlgorithm.Aes192, 24, true)]
        [InlineData(PivAlgorithm.Aes192, 24, false)]
        [InlineData(PivAlgorithm.Aes256, 32, true)]
        [InlineData(PivAlgorithm.Aes256, 32, false)]
        [InlineData(PivAlgorithm.TripleDes, 24, true)]
        [InlineData(PivAlgorithm.TripleDes, 24, false)]
        public void ChangeMgmtKey_TryAuthNoColl_Succeeds(PivAlgorithm algorithm, int keySize, bool mutualAuth)
        {
            if (!_runTest)
            {
                return;
            }

            AuthenticateManagementKeyResult expectedResult = mutualAuth
                ? AuthenticateManagementKeyResult.MutualFullyAuthenticated
                : AuthenticateManagementKeyResult.SingleAuthenticated;

            SetKeyLengths(24, keySize);
            using (var pivSession = new PivSession(_yubiKey))
            {
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                bool isValid =
                    pivSession.TryAuthenticateManagementKey(_currentKey.Slice(0, _currentKeyLength), mutualAuth);
                Assert.True(isValid);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(PivAlgorithm.TripleDes, pivSession.ManagementKeyAlgorithm);

                bool isValid = pivSession.TryChangeManagementKey(
                    _currentKey.Slice(0, _currentKeyLength),
                    _newKey.Slice(0, _newKeyLength),
                    PivTouchPolicy.None,
                    algorithm);
                Assert.True(isValid);

                // The Change call will always use mutual auth.
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                    pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);

                SwapKeys();
            }

            using (var pivSession = new PivSession(_yubiKey))
            {
                pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
                Assert.False(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);

                bool isValid =
                    pivSession.TryAuthenticateManagementKey(_currentKey.Slice(0, _currentKeyLength), mutualAuth);
                Assert.True(isValid);
                Assert.True(pivSession.ManagementKeyAuthenticated);
                Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
                Assert.Equal(algorithm, pivSession.ManagementKeyAlgorithm);
            }
        }

        public bool AesMgmtKeyTestsKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry)
            {
                return false;
            }

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    break;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    keyEntryData.SubmitValue(_currentKey.Slice(0, _currentKeyLength).Span);
                    break;

                case KeyEntryRequest.ChangePivManagementKey:
                    keyEntryData.SubmitValues(
                        _currentKey.Slice(0, _currentKeyLength).Span,
                        _newKey.Slice(0, _newKeyLength).Span);
                    break;
            }

            return true;
        }

        // Put the newKey data into currentKey, and vice versa. This will also
        // swap the lengths.
        private void SwapKeys()
        {
            byte[] swapBuffer = new byte[32];
            Array.Copy(_currentKeyBytes, swapBuffer, 32);
            Array.Copy(_newKeyBytes, _currentKeyBytes, 32);
            Array.Copy(swapBuffer, _newKeyBytes, 32);

            int swapLength = _currentKeyLength;
            _currentKeyLength = _newKeyLength;
            _newKeyLength = swapLength;
        }

        private void SetKeyLengths(int currentKeyLength, int newKeyLength)
        {
            _currentKeyLength = currentKeyLength;
            _newKeyLength = newKeyLength;
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

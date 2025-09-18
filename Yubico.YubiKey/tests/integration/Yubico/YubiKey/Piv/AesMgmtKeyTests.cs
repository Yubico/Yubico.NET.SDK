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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class AesMgmtKeyTests : PivSessionIntegrationTestBase
{
    private readonly Memory<byte> _currentKey;
    private readonly byte[] _currentKeyBytes;
    private readonly Memory<byte> _newKey;
    private readonly byte[] _newKeyBytes;
    private int _currentKeyLength;
    private int _newKeyLength;

    public AesMgmtKeyTests()
    {
        Skip.If(!Device.HasFeature(YubiKeyFeature.PivAesManagementKey));

        _currentKeyBytes = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
        };
        _newKeyBytes = new byte[32];

        using var random = RandomObjectUtility.GetRandomObject(null);
        random.GetBytes(_newKeyBytes);

        _currentKey = new Memory<byte>(_currentKeyBytes);
        _newKey = new Memory<byte>(_newKeyBytes);
        _currentKeyLength = 24;
        _newKeyLength = 32;

        DefaultMgmtKeyType = Device.FirmwareVersion >= FirmwareVersion.V5_7_0 ? KeyType.AES192 : KeyType.TripleDES;
    }

    private KeyType DefaultMgmtKeyType { get; }

    [Theory]
    [InlineData(KeyType.AES128, 16, true)]
    [InlineData(KeyType.AES128, 16, false)]
    [InlineData(KeyType.AES192, 24, true)]
    [InlineData(KeyType.AES192, 24, false)]
    [InlineData(KeyType.AES256, 32, true)]
    [InlineData(KeyType.AES256, 32, false)]
    [InlineData(KeyType.TripleDES, 24, true)]
    [InlineData(KeyType.TripleDES, 24, false)]
    public void ChangeMgmtKey_Auth_Succeeds(
        KeyType keyType,
        int keySize,
        bool mutualAuth)
    {
        var expectedResult = mutualAuth
            ? AuthenticateManagementKeyResult.MutualFullyAuthenticated
            : AuthenticateManagementKeyResult.SingleAuthenticated;

        SetKeyLengths(24, keySize);
        using (var pivSession = GetSession())
        {
            pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(DefaultMgmtKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            pivSession.AuthenticateManagementKey(mutualAuth);
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(DefaultMgmtKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
        }

        using (var pivSession = GetSession())
        {
            pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(DefaultMgmtKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            pivSession.ChangeManagementKey(PivTouchPolicy.None, keyType.GetPivAlgorithm());

            // The Change call will always use mutual auth.
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            SwapKeys();
        }

        using (var pivSession = GetSession())
        {
            pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            pivSession.AuthenticateManagementKey(mutualAuth);
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
        }
    }

    [Theory]
    [InlineData(KeyType.AES128, 16, true)]
    [InlineData(KeyType.AES128, 16, false)]
    [InlineData(KeyType.AES192, 24, true)]
    [InlineData(KeyType.AES192, 24, false)]
    [InlineData(KeyType.AES256, 32, true)]
    [InlineData(KeyType.AES256, 32, false)]
    [InlineData(KeyType.TripleDES, 24, true)]
    [InlineData(KeyType.TripleDES, 24, false)]
    public void ChangeMgmtKey_TryAuth_Succeeds(
        KeyType keyType,
        int keySize,
        bool mutualAuth)
    {
        var expectedResult = mutualAuth
            ? AuthenticateManagementKeyResult.MutualFullyAuthenticated
            : AuthenticateManagementKeyResult.SingleAuthenticated;

        SetKeyLengths(24, keySize);
        using (var pivSession = GetSession())
        {
            pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(DefaultManagementKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            var isValid = pivSession.TryAuthenticateManagementKey(mutualAuth);
            Assert.True(isValid);
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(DefaultManagementKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
        }

        using (var pivSession = GetSession())
        {
            pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(DefaultManagementKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            var isValid = pivSession.TryChangeManagementKey(PivTouchPolicy.None, keyType.GetPivAlgorithm());
            Assert.True(isValid);

            // The Change call will always use mutual auth.
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            SwapKeys();
        }

        using (var pivSession = GetSession())
        {
            pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            var isValid = pivSession.TryAuthenticateManagementKey(mutualAuth);
            Assert.True(isValid);
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
        }
    }

    [Theory]
    [InlineData(KeyType.AES128, 16, true)]
    [InlineData(KeyType.AES128, 16, false)]
    [InlineData(KeyType.AES192, 24, true)]
    [InlineData(KeyType.AES192, 24, false)]
    [InlineData(KeyType.AES256, 32, true)]
    [InlineData(KeyType.AES256, 32, false)]
    [InlineData(KeyType.TripleDES, 24, true)]
    [InlineData(KeyType.TripleDES, 24, false)]
    public void ChangeMgmtKey_TryAuthNoColl_Succeeds(
        KeyType keyType,
        int keySize,
        bool mutualAuth)
    {
        var expectedResult = mutualAuth
            ? AuthenticateManagementKeyResult.MutualFullyAuthenticated
            : AuthenticateManagementKeyResult.SingleAuthenticated;

        SetKeyLengths(24, keySize);
        using (var pivSession = GetSession())
        {
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(DefaultManagementKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            var isValid =
                pivSession.TryAuthenticateManagementKey(_currentKey[.._currentKeyLength], mutualAuth);
            Assert.True(isValid);
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(DefaultManagementKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
        }

        using (var pivSession = GetSession())
        {
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(DefaultManagementKeyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            var isValid = pivSession.TryChangeManagementKey(
                _currentKey[.._currentKeyLength],
                _newKey[.._newKeyLength],
                PivTouchPolicy.None,
                keyType.GetPivAlgorithm());
            Assert.True(isValid);

            // The Change call will always use mutual auth.
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(AuthenticateManagementKeyResult.MutualFullyAuthenticated,
                pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            SwapKeys();
        }

        using (var pivSession = GetSession())
        {
            pivSession.KeyCollector = AesMgmtKeyTestsKeyCollectorDelegate;
            Assert.False(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);

            var isValid =
                pivSession.TryAuthenticateManagementKey(_currentKey[.._currentKeyLength], mutualAuth);
            Assert.True(isValid);
            Assert.True(pivSession.ManagementKeyAuthenticated);
            Assert.Equal(expectedResult, pivSession.ManagementKeyAuthenticationResult);
            Assert.Equal(keyType.GetPivAlgorithm(), pivSession.ManagementKeyAlgorithm);
        }
    }

    private bool AesMgmtKeyTestsKeyCollectorDelegate(
        KeyEntryData keyEntryData)
    {
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
                keyEntryData.SubmitValue(_currentKey[.._currentKeyLength].Span);
                break;

            case KeyEntryRequest.ChangePivManagementKey:
                keyEntryData.SubmitValues(
                    _currentKey[.._currentKeyLength].Span,
                    _newKey[.._newKeyLength].Span);
                break;
        }

        return true;
    }

    // Put the newKey data into currentKey, and vice versa. This will also
    // swap the lengths.
    private void SwapKeys()
    {
        var swapBuffer = new byte[32];
        Array.Copy(_currentKeyBytes, swapBuffer, 32);
        Array.Copy(_newKeyBytes, _currentKeyBytes, 32);
        Array.Copy(swapBuffer, _newKeyBytes, 32);

        (_currentKeyLength, _newKeyLength) = (_newKeyLength, _currentKeyLength);
    }

    private void SetKeyLengths(
        int currentKeyLength,
        int newKeyLength)
    {
        _currentKeyLength = currentKeyLength;
        _newKeyLength = newKeyLength;
    }
}

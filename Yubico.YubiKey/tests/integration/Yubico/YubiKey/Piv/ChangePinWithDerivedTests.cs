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
using Yubico.YubiKey.Piv.Objects;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    // All these tests will reset the PIV application, run, then reset the PIV
    // application again.
    // All these tests will also use a random number generator with a specified
    // set of bytes, followed by 2048 random bytes. If you want to get only
    // random bytes, skip the first SpecifiedStart bytes (get a random object and
    // generate that many bytes).
    [Trait("Category", "Simple")]
    public class ChangePinWithDerivedTests : IDisposable
    {
        private readonly IYubiKeyDevice yubiKey;

        public ChangePinWithDerivedTests()
        {
            yubiKey = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            ResetPiv(yubiKey);
        }

        public void Dispose()
        {
            ResetPiv(yubiKey);
        }

        [Fact]
        public void SetPinOnly_TryChangePin_DerivedKeyUpdated()
        {
            var firstSalt = new Memory<byte>(new byte[16]);
            var secondSalt = new Memory<byte>(new byte[16]);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var isValid = pivSession.TryReadObject(out AdminData adminData);
                using (adminData)
                {
                    Assert.True(isValid);
                    _ = Assert.NotNull(adminData.Salt);
                    Assert.False(adminData.PinProtected);

                    if (!(adminData.Salt is null))
                    {
                        var src = (ReadOnlyMemory<byte>)adminData.Salt;
                        src.CopyTo(firstSalt);
                    }
                }
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                byte[] currentPin =
                {
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36
                };
                byte[] newPin =
                {
                    0x39, 0x32, 0x33, 0x34, 0x35, 0x36
                };

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                var isChanged = pivSession.TryChangePin(currentPin, newPin, out var retriesRemaining);
                Assert.True(isChanged);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector
                {
                    KeyFlag = 1
                };
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                var isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var isValid = pivSession.TryReadObject(out AdminData adminData);
                using (adminData)
                {
                    Assert.True(isValid);
                    _ = Assert.NotNull(adminData.PinLastUpdated);
                    _ = Assert.NotNull(adminData.Salt);
                    Assert.False(adminData.PinProtected);

                    if (!(adminData.Salt is null))
                    {
                        var src = (ReadOnlyMemory<byte>)adminData.Salt;
                        src.CopyTo(secondSalt);

                        var isSame = firstSalt.Span.SequenceEqual(secondSalt.Span);
                        Assert.False(isSame);
                    }
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void SetPinOnly_ChangeRetryCount_DerivedKeyUpdated(int whichCall)
        {
            using (var pivSession = new PivSession(yubiKey))
            {
                byte[] currentPin =
                {
                    0x31, 0x32, 0x33, 0x34, 0x35, 0x36
                };
                byte[] newPin =
                {
                    0x39, 0x32, 0x33, 0x34, 0x35, 0x36
                };

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                var isChanged = pivSession.TryChangePin(currentPin, newPin, out var retriesRemaining);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector
                {
                    KeyFlag = 1
                };
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                byte[] currentPin =
                {
                    0x39, 0x32, 0x33, 0x34, 0x35, 0x36
                };

                if (whichCall == 1)
                {
                    var isChanged = pivSession.TryChangePinAndPukRetryCounts(
                        ReadOnlyMemory<byte>.Empty, currentPin, newRetryCountPin: 12, newRetryCountPuk: 13,
                        out var retriesRemaining);
                    Assert.True(isChanged);
                    Assert.Null(retriesRemaining);
                }
                else
                {
                    var collectorObj = new Simple39KeyCollector
                    {
                        KeyFlag = 1
                    };
                    pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
                    pivSession.ChangePinAndPukRetryCounts(newRetryCountPin: 13, newRetryCountPuk: 14);
                }
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                var isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);
                isValid = pivSession.TryVerifyPin();
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var isValid = pivSession.TryReadObject(out AdminData adminData);
                using (adminData)
                {
                    Assert.True(isValid);
                    _ = Assert.NotNull(adminData.PinLastUpdated);
                    _ = Assert.NotNull(adminData.Salt);
                    Assert.False(adminData.PinProtected);
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void SetBothPinOnly_ChangePin_KeysUpdated(int whichCall)
        {
            var firstKey = new Memory<byte>(new byte[24]);
            var secondKey = new Memory<byte>(new byte[24]);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                pivSession.SetPinOnlyMode(PivPinOnlyMode.PinDerived | PivPinOnlyMode.PinProtected);

                Assert.True(pivSession.PinVerified);
                Assert.True(pivSession.ManagementKeyAuthenticated);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                var isValid = pivSession.TryReadObject(out PinProtectedData pinProtect);
                using (pinProtect)
                {
                    Assert.True(isValid);
                    _ = Assert.NotNull(pinProtect.ManagementKey);
                    if (!(pinProtect.ManagementKey is null))
                    {
                        var src = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                        src.CopyTo(firstKey);
                        isValid = pivSession.TryAuthenticateManagementKey(src);
                        Assert.True(isValid);
                        Assert.True(pivSession.ManagementKeyAuthenticated);
                    }
                }
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                Assert.False(pivSession.PinVerified);
                Assert.False(pivSession.ManagementKeyAuthenticated);

                if (whichCall == 1)
                {
                    pivSession.ChangePin();
                }
                else
                {
                    var isValid = pivSession.TryChangePin();
                    Assert.True(isValid);
                }

                collectorObj.KeyFlag = 1;
                pivSession.VerifyPin();

                Assert.True(pivSession.PinVerified);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector
                {
                    KeyFlag = 1
                };
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                var isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector
                {
                    KeyFlag = 1
                };
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                var isValid = pivSession.TryReadObject(out PinProtectedData pinProtect);
                using (pinProtect)
                {
                    Assert.True(isValid);
                    _ = Assert.NotNull(pinProtect.ManagementKey);
                    if (!(pinProtect.ManagementKey is null))
                    {
                        var src = (ReadOnlyMemory<byte>)pinProtect.ManagementKey;
                        src.CopyTo(secondKey);
                        isValid = pivSession.TryAuthenticateManagementKey(src);
                        Assert.True(isValid);
                        Assert.True(pivSession.ManagementKeyAuthenticated);

                        var isSame = firstKey.Span.SequenceEqual(secondKey.Span);
                        Assert.False(isSame);
                    }
                }
            }

            using (var pivSession = new PivSession(yubiKey))
            {
                var isValid = pivSession.TryReadObject(out AdminData adminData);
                using (adminData)
                {
                    Assert.True(isValid);
                    _ = Assert.NotNull(adminData.PinLastUpdated);
                    _ = Assert.NotNull(adminData.Salt);
                    Assert.True(adminData.PinProtected);
                }
            }
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

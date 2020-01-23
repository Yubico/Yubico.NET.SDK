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
using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    public sealed class OathSessionCredentialTests : IClassFixture<CredentialFixture>, IDisposable
    {
        // Shared object instance across tests.
        private readonly CredentialFixture _fixture;

        private readonly bool _isValid;
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private IYubiKeyConnection? _connection;
        private readonly OathSession _oathSession;
        private readonly SimpleOathKeyCollector _collectorObj;

        public OathSessionCredentialTests(CredentialFixture fixture)
        {
            _fixture = fixture;

            _isValid = SelectSupport.TrySelectYubiKey(out _yubiKeyDevice);

            if (_isValid)
            {
                _connection = _yubiKeyDevice.Connect(YubiKeyApplication.Oath);
            }

            _oathSession = new OathSession(_yubiKeyDevice);
            _collectorObj = new SimpleOathKeyCollector();
            _oathSession.KeyCollector = _collectorObj.SimpleKeyCollectorDelegate;
        }

        [Fact, TestPriority(0)]
        public void AddCredentials()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _oathSession.AddCredential(_fixture.TotpCredential);
            _oathSession.AddCredential(_fixture.HotpCredential);
            _oathSession.AddCredential(_fixture.TotpCredentialWithDefaultPeriod);
            _oathSession.AddCredential(_fixture.CredentialToDelete);
        }

        [Fact, TestPriority(1)]
        public void GetCredentials()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            IList<Credential> data = _oathSession.GetCredentials();

            Assert.NotEmpty(data);
            Assert.Contains(_fixture.TotpCredential, data);
            Assert.Contains(_fixture.HotpCredential, data);
            Assert.Contains(_fixture.TotpCredentialWithDefaultPeriod, data);
            Assert.Contains(_fixture.CredentialToDelete, data);
        }

        [Fact, TestPriority(2)]
        public void CalculateAllCredentials()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            IDictionary<Credential, Code> data = _oathSession.CalculateAllCredentials();
            Assert.NotEmpty(data);

            foreach (KeyValuePair<Credential, Code> pair in data)
            {
                if (pair.Key.Type == CredentialType.Totp)
                {
                    Assert.NotEmpty(pair.Value.Value);
                }
                else
                {
                    Assert.Empty(pair.Value.Value);
                }
            }
        }

        [Fact, TestPriority(2)]
        public void CalculateTotpCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            Code data = _oathSession.CalculateCredential(_fixture.TotpCredentialWithDefaultPeriod);

            Assert.NotNull(data.Value);
            Assert.NotNull(data.ValidFrom);
            Assert.NotNull(data.ValidUntil);

            int difference = (int)(data.ValidUntil! - data.ValidFrom!).Value.TotalSeconds;

            Assert.Equal(30, difference);
        }

        [Fact, TestPriority(2)]
        public void CalculateHotpCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            Code data = _oathSession.CalculateCredential(_fixture.HotpCredential);

            Assert.NotNull(data.Value);
            Assert.NotNull(data.ValidFrom);
            Assert.NotNull(data.ValidUntil);
            Assert.Equal(DateTimeOffset.MaxValue, data.ValidUntil);
        }

        [Fact, TestPriority(2)]
        public void CalculateTotpCredentialUsingParameters()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            Code data = _oathSession.CalculateCredential(
                "Microsoft",
                "test@outlook.com",
                CredentialType.Totp,
                CredentialPeriod.Period15);

            Assert.NotNull(data.Value);
            Assert.NotNull(data.ValidFrom);
            Assert.NotNull(data.ValidUntil);

            int difference = (int)(data.ValidUntil! - data.ValidFrom!).Value.TotalSeconds;

            Assert.Equal(15, difference);
        }

        [Fact, TestPriority(2)]
        public void CalculateHotpCredentialUsingParameters()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            Code data = _oathSession.CalculateCredential(
                "Apple",
                "test@icloud.com",
                CredentialType.Hotp,
                0);

            Assert.NotNull(data.Value);
            Assert.NotNull(data.ValidFrom);
            Assert.NotNull(data.ValidUntil);
            Assert.Equal(DateTimeOffset.MaxValue, data.ValidUntil);
        }

        [Fact, TestPriority(2)]
        public void CalculateNotExitsingCredential_ThrowsException()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _ = Assert.Throws<InvalidOperationException>(() => _oathSession.CalculateCredential(
                "Google",
                "test@outlook.com",
                CredentialType.Hotp,
                0));
        }

        [Fact, TestPriority(3)]
        public void AddTotpWithTouchCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion < FirmwareVersion.V4_3_1)
            {
                _ = Assert.Throws<InvalidOperationException>(() => _oathSession.AddCredential(_fixture.TotpWithTouchCredential));
            }
            else
            {
                _oathSession.AddCredential(_fixture.TotpWithTouchCredential);
                IList<Credential> data = _oathSession.GetCredentials();

                Assert.Contains(_fixture.TotpWithTouchCredential, data);
            }
        }

        [Fact, TestPriority(3)]
        public void AddTotpWithSha512AlgorithmCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion < FirmwareVersion.V4_2_4)
            {
                _ = Assert.Throws<InvalidOperationException>(() => _oathSession.AddCredential(_fixture.TotpWithSha512Credential));
            }
            else
            {
                _oathSession.AddCredential(_fixture.TotpWithSha512Credential);
                IList<Credential> data = _oathSession.GetCredentials();

                Assert.Contains(_fixture.TotpWithSha512Credential, data);
            }
        }

        [Fact, TestPriority(3)]
        public void AddHotpCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            Credential credential = _oathSession.AddCredential(
                "GitHub",
                "test@gmail.com",
                CredentialType.Hotp,
                0);

            Assert.Equal("GitHub", credential.Issuer);
            Assert.Equal("test@gmail.com", credential.AccountName);
            Assert.Equal(CredentialType.Hotp, credential.Type);
            Assert.Equal(CredentialPeriod.Undefined, credential.Period);
        }

        [Fact, TestPriority(3)]
        public void AddDefaultCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            Credential credential = _oathSession.AddCredential("Google", "test@gmail.com");

            Assert.Equal("Google", credential.Issuer);
            Assert.Equal("test@gmail.com", credential.AccountName);
            Assert.Equal(CredentialType.Totp, credential.Type);
            Assert.Equal(CredentialPeriod.Period30, credential.Period);
        }

        [Fact, TestPriority(3)]
        public void AddCredentialFromUri()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            Credential credential = _oathSession.AddCredential(
                "otpauth://totp/ACME%20Co:test@example.com?secret=HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30");

            Assert.Equal("ACME Co", credential.Issuer);
            Assert.Equal("test@example.com", credential.AccountName);
            Assert.Equal("HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ", credential.Secret);
            Assert.Equal(CredentialType.Totp, credential.Type);
            Assert.Equal(HashAlgorithm.Sha1, credential.Algorithm);
            Assert.Equal(CredentialPeriod.Period30, credential.Period);
            Assert.Equal(6, credential.Digits);
            Assert.Null(credential.Counter);
        }

        [Fact, TestPriority(3)]
        public void AddInvalidCredential_ThrowsException()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _ = Assert.Throws<InvalidOperationException>(() => _oathSession.AddCredential(
                "GitHub",
                "test@gmail.com",
                CredentialType.Hotp,
                CredentialPeriod.Period30));
        }

        [Fact, TestPriority(4)]
        public void OverwriteCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            var credential = new Credential
            {
                Issuer = "Apple",
                AccountName = "test@icloud.com",
                Type = CredentialType.Totp,
                Period = CredentialPeriod.Period30,
                Algorithm = HashAlgorithm.Sha1
            };

            _oathSession.AddCredential(credential);

            IList<Credential> data = _oathSession.GetCredentials();

            Assert.Contains(credential, data);
            Assert.DoesNotContain(_fixture.HotpCredential, data);
        }

        [Fact, TestPriority(5)]
        public void RenameTotpCredentialWithDefaultPeriod()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion < FirmwareVersion.V5_3_0)
            {
                _ = Assert.Throws<InvalidOperationException>(()
                    => _oathSession.RenameCredential(
                        _fixture.TotpCredentialWithDefaultPeriod.Issuer,
                        _fixture.TotpCredentialWithDefaultPeriod.AccountName!,
                        "",
                        "test@example.com"));
            }
            else
            {
                _ = _oathSession.RenameCredential(
                    "Amazon",
                    "test@gmail.com",
                    "",
                    "test@example.com");

                var renamedCredential = new Credential
                {
                    Issuer = "",
                    AccountName = "test@example.com",
                    Type = _fixture.TotpCredentialWithDefaultPeriod.Type,
                    Period = _fixture.TotpCredentialWithDefaultPeriod.Period,
                    Algorithm = _fixture.TotpCredentialWithDefaultPeriod.Algorithm
                };

                IList<Credential> data = _oathSession.GetCredentials();
                Assert.DoesNotContain(_fixture.TotpCredentialWithDefaultPeriod, data);
                Assert.Contains(renamedCredential, data);
            }
        }

        [Fact, TestPriority(5)]
        public void RenameCredential()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion < FirmwareVersion.V5_3_0)
            {
                _ = Assert.Throws<InvalidOperationException>(()
                    => _oathSession.RenameCredential(
                        _fixture.TotpCredential,
                        "Test",
                        "test@example.com"));
            }
            else
            {
                _oathSession.RenameCredential(
                    _fixture.TotpCredential,
                    "Test",
                    "test@example.com");

                IList<Credential> data = _oathSession.GetCredentials();
                Assert.DoesNotContain(_fixture.TotpCredential, data);

                _fixture.TotpCredential.Issuer = "Test";
                _fixture.TotpCredential.AccountName = "test@example.com";
                Assert.Contains(_fixture.TotpCredential, data);
            }
        }

        [Fact, TestPriority(5)]
        public void RenameNotExistingCredential_ThrowsException()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion < FirmwareVersion.V5_3_0)
            {
                _ = Assert.Throws<InvalidOperationException>(()
                    => _oathSession.RenameCredential(
                        _fixture.TotpCredential,
                        "Test",
                        "test@example.com"));
            }
            else
            {
                _ = Assert.Throws<InvalidOperationException>(() =>
                    _oathSession.RenameCredential(
                        "Google",
                        "test@outlook.com",
                        "Test",
                        "test@example.com"));
            }
        }

        [Fact, TestPriority(6)]
        public void RemoveNotExistingCredential_ThrowsException()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _ = Assert.Throws<InvalidOperationException>(() => _oathSession.RemoveCredential("Google", "test@outlook.com"));
        }

        [Fact, TestPriority(6)]
        public void RemoveCredentials()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _oathSession.RemoveCredential(_fixture.TotpWithTouchCredential);
            _oathSession.RemoveCredential(_fixture.TotpWithSha512Credential);

            IList<Credential> data = _oathSession.GetCredentials();

            Assert.DoesNotContain(_fixture.TotpWithTouchCredential, data);
            Assert.DoesNotContain(_fixture.TotpWithSha512Credential, data);

            if (_yubiKeyDevice.FirmwareVersion < FirmwareVersion.V5_3_0)
            {
                _oathSession.RemoveCredential(_fixture.TotpCredentialWithDefaultPeriod);
                _oathSession.RemoveCredential(_fixture.TotpCredential);

                data = _oathSession.GetCredentials();
                Assert.DoesNotContain(_fixture.TotpCredentialWithDefaultPeriod, data);
                Assert.DoesNotContain(_fixture.TotpCredential, data);
            }
        }

        [Fact, TestPriority(6)]
        public void RemoveCredentialsWithIssuerAndAccount()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _ = _oathSession.RemoveCredential("Twitter", "test@gmail.com");
            Credential acmeCredential = _oathSession.RemoveCredential("ACME Co", "test@example.com");
            Credential googleCredential = _oathSession.RemoveCredential("Google", "test@gmail.com");
            Credential gitHubCredential = _oathSession.RemoveCredential("GitHub", "test@gmail.com");
            Credential appleCredential = _oathSession.RemoveCredential("Apple", "test@icloud.com");

            IList<Credential> data = _oathSession.GetCredentials();

            Assert.DoesNotContain(_fixture.CredentialToDelete, data);
            Assert.DoesNotContain(acmeCredential, data);
            Assert.DoesNotContain(googleCredential, data);
            Assert.DoesNotContain(gitHubCredential, data);
            Assert.DoesNotContain(appleCredential, data);

            if (_yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0)
            {
                Credential emptyIssuerCredential = _oathSession.RemoveCredential("", "test@example.com");
                Credential renamedCredential = _oathSession.RemoveCredential(
                    "Test",
                    "test@example.com",
                    CredentialType.Totp,
                    CredentialPeriod.Period15);

                data = _oathSession.GetCredentials();

                Assert.DoesNotContain(renamedCredential, data);
                Assert.DoesNotContain(emptyIssuerCredential, data);
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _oathSession.Dispose();
            _connection = null;
        }
    }
}

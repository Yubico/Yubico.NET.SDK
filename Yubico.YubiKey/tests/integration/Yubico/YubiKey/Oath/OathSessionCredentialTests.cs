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
    public sealed class OathSessionCredentialTests : IClassFixture<CredentialFixture>
    {
        // Shared object instance across tests.
        private readonly CredentialFixture _fixture;

        public OathSessionCredentialTests(CredentialFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory, TestPriority(0)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddCredentials(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                oathSession.AddCredential(_fixture.TotpCredential);
                oathSession.AddCredential(_fixture.HotpCredential);
                oathSession.AddCredential(_fixture.TotpCredentialWithDefaultPeriod);
                oathSession.AddCredential(_fixture.CredentialToDelete);
            }
        }

        [Theory, TestPriority(1)]
        [InlineData(StandardTestDevice.Fw5)]
        public void GetCredentials(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                IList<Credential> data = oathSession.GetCredentials();

                Assert.NotEmpty(data);
                Assert.Contains(_fixture.TotpCredential, data);
                Assert.Contains(_fixture.HotpCredential, data);
                Assert.Contains(_fixture.TotpCredentialWithDefaultPeriod, data);
                Assert.Contains(_fixture.CredentialToDelete, data);
            }
        }

        [Theory, TestPriority(2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateAllCredentials(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                IDictionary<Credential, Code> data = oathSession.CalculateAllCredentials();
                Assert.NotEmpty(data);
            }
        }

        [Theory, TestPriority(2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateTotpCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                Code data = oathSession.CalculateCredential(_fixture.TotpCredentialWithDefaultPeriod);

                Assert.NotNull(data.Value);
                Assert.NotNull(data.ValidFrom);
                Assert.NotNull(data.ValidUntil);

                int difference = (int)(data.ValidUntil! - data.ValidFrom!).Value.TotalSeconds;

                Assert.Equal(30, difference);
            }
        }

        [Theory, TestPriority(2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateHotpCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                Code data = oathSession.CalculateCredential(_fixture.HotpCredential);

                Assert.NotNull(data.Value);
                Assert.NotNull(data.ValidFrom);
                Assert.NotNull(data.ValidUntil);
                Assert.Equal(DateTimeOffset.MaxValue, data.ValidUntil);
            }
        }

        [Theory, TestPriority(2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateTotpCredentialUsingParameters(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                Code data = oathSession.CalculateCredential(
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
        }

        [Theory, TestPriority(2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateHotpCredentialUsingParameters(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                Code data = oathSession.CalculateCredential(
                    "Apple",
                    "test@icloud.com",
                    CredentialType.Hotp,
                    0);

                Assert.NotNull(data.Value);
                Assert.NotNull(data.ValidFrom);
                Assert.NotNull(data.ValidUntil);
                Assert.Equal(DateTimeOffset.MaxValue, data.ValidUntil);
            }
        }

        [Theory, TestPriority(2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateNotExistingCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = Assert.Throws<InvalidOperationException>(() => oathSession.CalculateCredential(
                    "Google",
                    "test@outlook.com",
                    CredentialType.Hotp,
                    0));
            }
        }

        [Theory, TestPriority(3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddTotpWithTouchCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                if (testDevice.HasFeature(YubiKeyFeature.OathTouchCredential))
                {
                    oathSession.AddCredential(_fixture.TotpWithTouchCredential);
                    IList<Credential> data = oathSession.GetCredentials();

                    Assert.Contains(_fixture.TotpWithTouchCredential, data);
                }
                else
                {
                    _ = Assert.Throws<InvalidOperationException>(() =>
                        oathSession.AddCredential(_fixture.TotpWithTouchCredential));
                }
            }
        }

        [Theory, TestPriority(3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddTotpWithSha512AlgorithmCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                if (testDevice.HasFeature(YubiKeyFeature.OathSha512))
                {
                    oathSession.AddCredential(_fixture.TotpWithSha512Credential);
                    IList<Credential> data = oathSession.GetCredentials();

                    Assert.Contains(_fixture.TotpWithSha512Credential, data);
                }
                else
                {
                    _ = Assert.Throws<InvalidOperationException>(() =>
                        oathSession.AddCredential(_fixture.TotpWithSha512Credential));
                }
            }
        }

        [Theory, TestPriority(3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddHotpCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                Credential credential = oathSession.AddCredential(
                    "GitHub",
                    "test@gmail.com",
                    CredentialType.Hotp,
                    0);

                Assert.Equal("GitHub", credential.Issuer);
                Assert.Equal("test@gmail.com", credential.AccountName);
                Assert.Equal(CredentialType.Hotp, credential.Type);
                Assert.Equal(CredentialPeriod.Undefined, credential.Period);
            }
        }

        [Theory, TestPriority(3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddDefaultCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                Credential credential = oathSession.AddCredential("Google", "test@gmail.com");

                Assert.Equal("Google", credential.Issuer);
                Assert.Equal("test@gmail.com", credential.AccountName);
                Assert.Equal(CredentialType.Totp, credential.Type);
                Assert.Equal(CredentialPeriod.Period30, credential.Period);
            }
        }

        [Theory, TestPriority(3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddCredentialFromUri(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                Credential credential = oathSession.AddCredential(
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
        }

        [Theory, TestPriority(3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddInvalidCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = Assert.Throws<InvalidOperationException>(() => oathSession.AddCredential(
                    "GitHub",
                    "test@gmail.com",
                    CredentialType.Hotp,
                    CredentialPeriod.Period30));
            }
        }

        [Theory, TestPriority(4)]
        [InlineData(StandardTestDevice.Fw5)]
        public void OverwriteCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var credential = new Credential
                {
                    Issuer = "Apple",
                    AccountName = "test@icloud.com",
                    Type = CredentialType.Totp,
                    Period = CredentialPeriod.Period30,
                    Algorithm = HashAlgorithm.Sha1
                };

                oathSession.AddCredential(credential);

                IList<Credential> data = oathSession.GetCredentials();

                Assert.Contains(credential, data);
                Assert.DoesNotContain(_fixture.HotpCredential, data);
            }
        }

        [Theory, TestPriority(5)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameTotpCredentialWithDefaultPeriod(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                if (testDevice.HasFeature(YubiKeyFeature.OathRenameCredential))
                {
                    _ = oathSession.RenameCredential(
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

                    IList<Credential> data = oathSession.GetCredentials();
                    Assert.DoesNotContain(_fixture.TotpCredentialWithDefaultPeriod, data);
                    Assert.Contains(renamedCredential, data);
                }
                else
                {
                    _ = Assert.Throws<InvalidOperationException>(()
                        => oathSession.RenameCredential(
                            _fixture.TotpCredentialWithDefaultPeriod.Issuer,
                            _fixture.TotpCredentialWithDefaultPeriod.AccountName!,
                            "",
                            "test@example.com"));
                }
            }
        }

        [Theory, TestPriority(5)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameCredential(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                if (testDevice.HasFeature(YubiKeyFeature.OathRenameCredential))
                {
                    oathSession.RenameCredential(
                        _fixture.TotpCredential,
                        "Test",
                        "test@example.com");

                    IList<Credential> data = oathSession.GetCredentials();
                    Assert.DoesNotContain(_fixture.TotpCredential, data);

                    _fixture.TotpCredential.Issuer = "Test";
                    _fixture.TotpCredential.AccountName = "test@example.com";
                    Assert.Contains(_fixture.TotpCredential, data);
                }
                else
                {
                    _ = Assert.Throws<InvalidOperationException>(()
                        => oathSession.RenameCredential(
                            _fixture.TotpCredential,
                            "Test",
                            "test@example.com"));
                }
            }
        }

        [Theory, TestPriority(5)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameNotExistingCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = Assert.Throws<InvalidOperationException>(() =>
                    oathSession.RenameCredential(
                        "Google",
                        "test@outlook.com",
                        "Test",
                        "test@example.com"));
            }
        }

        [Theory, TestPriority(6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RemoveNotExistingCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = Assert.Throws<InvalidOperationException>(() =>
                    oathSession.RemoveCredential("Google", "test@outlook.com"));
            }
        }

        [Theory, TestPriority(6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RemoveCredentials(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                oathSession.RemoveCredential(_fixture.TotpWithTouchCredential);
                oathSession.RemoveCredential(_fixture.TotpWithSha512Credential);

                IList<Credential> data = oathSession.GetCredentials();

                Assert.DoesNotContain(_fixture.TotpWithTouchCredential, data);
                Assert.DoesNotContain(_fixture.TotpWithSha512Credential, data);
            }
        }

        [Theory, TestPriority(6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RemoveCredentialsWithIssuerAndAccount(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = oathSession.RemoveCredential("Twitter", "test@gmail.com");
                Credential acmeCredential = oathSession.RemoveCredential("ACME Co", "test@example.com");
                Credential googleCredential = oathSession.RemoveCredential("Google", "test@gmail.com");
                Credential gitHubCredential = oathSession.RemoveCredential("GitHub", "test@gmail.com");
                Credential appleCredential = oathSession.RemoveCredential("Apple", "test@icloud.com");

                IList<Credential> data = oathSession.GetCredentials();

                Assert.DoesNotContain(_fixture.CredentialToDelete, data);
                Assert.DoesNotContain(acmeCredential, data);
                Assert.DoesNotContain(googleCredential, data);
                Assert.DoesNotContain(gitHubCredential, data);
                Assert.DoesNotContain(appleCredential, data);

                Credential emptyIssuerCredential = oathSession.RemoveCredential("", "test@example.com");
                Credential renamedCredential = oathSession.RemoveCredential(
                    "Test",
                    "test@example.com",
                    CredentialType.Totp,
                    CredentialPeriod.Period15);

                data = oathSession.GetCredentials();

                Assert.DoesNotContain(renamedCredential, data);
                Assert.DoesNotContain(emptyIssuerCredential, data);
            }
        }
    }
}

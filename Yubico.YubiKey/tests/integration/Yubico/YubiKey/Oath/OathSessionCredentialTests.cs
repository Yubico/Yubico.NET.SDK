﻿// Copyright 2021 Yubico AB
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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    [Trait("Category", "Simple")]
    public sealed class OathSessionCredentialTests : IClassFixture<CredentialFixture>
    {
        // Shared object instance across tests.
        private readonly CredentialFixture _fixture;

        public OathSessionCredentialTests(CredentialFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [TestPriority(priority: 0)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddCredentials(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

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

        [Theory]
        [TestPriority(priority: 1)]
        [InlineData(StandardTestDevice.Fw5)]
        public void GetCredentials(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var data = oathSession.GetCredentials();

                Assert.NotEmpty(data);
                Assert.Contains(_fixture.TotpCredential, data);
                Assert.Contains(_fixture.HotpCredential, data);
                Assert.Contains(_fixture.TotpCredentialWithDefaultPeriod, data);
                Assert.Contains(_fixture.CredentialToDelete, data);
            }
        }

        [Theory]
        [TestPriority(priority: 2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateAllCredentials(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var data = oathSession.CalculateAllCredentials();
                Assert.NotEmpty(data);
            }
        }

        [Theory]
        [TestPriority(priority: 2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateTotpCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var data = oathSession.CalculateCredential(_fixture.TotpCredentialWithDefaultPeriod);

                Assert.NotNull(data.Value);
                _ = Assert.NotNull(data.ValidFrom);
                _ = Assert.NotNull(data.ValidUntil);

                var difference = (int)(data.ValidUntil! - data.ValidFrom!).Value.TotalSeconds;

                Assert.Equal(expected: 30, difference);
            }
        }

        [Theory]
        [TestPriority(priority: 2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateHotpCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var data = oathSession.CalculateCredential(_fixture.HotpCredential);

                Assert.NotNull(data.Value);
                _ = Assert.NotNull(data.ValidFrom);
                _ = Assert.NotNull(data.ValidUntil);
                Assert.Equal(DateTimeOffset.MaxValue, data.ValidUntil);
            }
        }

        [Theory]
        [TestPriority(priority: 2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateTotpCredentialUsingParameters(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var data = oathSession.CalculateCredential(
                    "Microsoft",
                    "test@outlook.com",
                    CredentialType.Totp,
                    CredentialPeriod.Period15);

                Assert.NotNull(data.Value);
                _ = Assert.NotNull(data.ValidFrom);
                _ = Assert.NotNull(data.ValidUntil);

                var difference = (int)(data.ValidUntil! - data.ValidFrom!).Value.TotalSeconds;

                Assert.Equal(expected: 15, difference);
            }
        }

        [Theory]
        [TestPriority(priority: 2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateHotpCredentialUsingParameters(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var data = oathSession.CalculateCredential(
                    "Apple",
                    "test@icloud.com",
                    CredentialType.Hotp,
                    period: 0);

                Assert.NotNull(data.Value);
                _ = Assert.NotNull(data.ValidFrom);
                _ = Assert.NotNull(data.ValidUntil);
                Assert.Equal(DateTimeOffset.MaxValue, data.ValidUntil);
            }
        }

        [Theory]
        [TestPriority(priority: 2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateNotExistingCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = Assert.Throws<InvalidOperationException>(() => oathSession.CalculateCredential(
                    "Google",
                    "test@outlook.com",
                    CredentialType.Hotp,
                    period: 0));
            }
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddTotpWithTouchCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                if (testDevice.HasFeature(YubiKeyFeature.OathTouchCredential))
                {
                    oathSession.AddCredential(_fixture.TotpWithTouchCredential);
                    var data = oathSession.GetCredentials();

                    Assert.Contains(_fixture.TotpWithTouchCredential, data);
                }
                else
                {
                    _ = Assert.Throws<InvalidOperationException>(() =>
                        oathSession.AddCredential(_fixture.TotpWithTouchCredential));
                }
            }
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddTotpWithSha512AlgorithmCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                if (testDevice.HasFeature(YubiKeyFeature.OathSha512))
                {
                    oathSession.AddCredential(_fixture.TotpWithSha512Credential);
                    var data = oathSession.GetCredentials();

                    Assert.Contains(_fixture.TotpWithSha512Credential, data);
                }
                else
                {
                    _ = Assert.Throws<InvalidOperationException>(() =>
                        oathSession.AddCredential(_fixture.TotpWithSha512Credential));
                }
            }
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddHotpCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var credential = oathSession.AddCredential(
                    "GitHub",
                    "test@gmail.com",
                    CredentialType.Hotp,
                    period: 0);

                Assert.Equal("GitHub", credential.Issuer);
                Assert.Equal("test@gmail.com", credential.AccountName);
                Assert.Equal(CredentialType.Hotp, credential.Type);
                Assert.Equal(CredentialPeriod.Undefined, credential.Period);
            }
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddDefaultCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var credential = oathSession.AddCredential("Google", "test@gmail.com");

                Assert.Equal("Google", credential.Issuer);
                Assert.Equal("test@gmail.com", credential.AccountName);
                Assert.Equal(CredentialType.Totp, credential.Type);
                Assert.Equal(CredentialPeriod.Period30, credential.Period);
            }
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddCredentialFromUri(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                var credential = oathSession.AddCredential(
                    "otpauth://totp/ACME%20Co:test@example.com?secret=HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30");

                Assert.Equal("ACME Co", credential.Issuer);
                Assert.Equal("test@example.com", credential.AccountName);
                Assert.Equal("HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ", credential.Secret);
                Assert.Equal(CredentialType.Totp, credential.Type);
                Assert.Equal(HashAlgorithm.Sha1, credential.Algorithm);
                Assert.Equal(CredentialPeriod.Period30, credential.Period);
                Assert.Equal(expected: 6, credential.Digits);
                Assert.Null(credential.Counter);
            }
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddInvalidCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                //Why should this fail?
                _ = Assert.Throws<InvalidOperationException>(() => oathSession.AddCredential(
                    "GitHub",
                    "test@gmail.com",
                    CredentialType.Hotp));
            }
        }

        [Theory]
        [TestPriority(priority: 4)]
        [InlineData(StandardTestDevice.Fw5)]
        public void OverwriteCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

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

                var data = oathSession.GetCredentials();

                Assert.Contains(credential, data);
                Assert.DoesNotContain(_fixture.HotpCredential, data);
            }
        }

        [Theory]
        [TestPriority(priority: 5)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameTotpCredentialWithDefaultPeriod(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

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

                    var data = oathSession.GetCredentials();
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

        [Theory]
        [TestPriority(priority: 5)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameCredential(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

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

                    var data = oathSession.GetCredentials();
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

        [Theory]
        [TestPriority(priority: 5)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameNotExistingCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

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

        [Theory]
        [TestPriority(priority: 6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RemoveNotExistingCredential_ThrowsException(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = Assert.Throws<InvalidOperationException>(() =>
                    oathSession.RemoveCredential("Google", "test@outlook.com"));
            }
        }

        [Theory]
        [TestPriority(priority: 6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RemoveCredentials(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                oathSession.RemoveCredential(_fixture.TotpWithTouchCredential);
                oathSession.RemoveCredential(_fixture.TotpWithSha512Credential);

                var data = oathSession.GetCredentials();

                Assert.DoesNotContain(_fixture.TotpWithTouchCredential, data);
                Assert.DoesNotContain(_fixture.TotpWithSha512Credential, data);
            }
        }

        [Theory]
        [TestPriority(priority: 6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RemoveCredentialsWithIssuerAndAccount(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var oathSession = new OathSession(testDevice))
            {
                var collectorObj = new SimpleOathKeyCollector();
                oathSession.KeyCollector = collectorObj.SimpleKeyCollectorDelegate;

                _ = oathSession.RemoveCredential("Twitter", "test@gmail.com");
                var acmeCredential = oathSession.RemoveCredential("ACME Co", "test@example.com");
                var googleCredential = oathSession.RemoveCredential("Google", "test@gmail.com");
                var gitHubCredential = oathSession.RemoveCredential("GitHub", "test@gmail.com");
                var appleCredential = oathSession.RemoveCredential("Apple", "test@icloud.com");

                var data = oathSession.GetCredentials();

                Assert.DoesNotContain(_fixture.CredentialToDelete, data);
                Assert.DoesNotContain(acmeCredential, data);
                Assert.DoesNotContain(googleCredential, data);
                Assert.DoesNotContain(gitHubCredential, data);
                Assert.DoesNotContain(appleCredential, data);

                var emptyIssuerCredential = oathSession.RemoveCredential("", "test@example.com");
                var renamedCredential = oathSession.RemoveCredential(
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

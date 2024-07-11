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

using Xunit;
using Yubico.YubiKey.Oath.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    [Trait("Category", "Simple")]
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    public class CredentialTests : IClassFixture<CredentialFixture>
    {
        // Shared object instance across tests.
        private readonly CredentialFixture _fixture;

        // Shared setup for every test that is run.
        public CredentialTests(CredentialFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [TestPriority(priority: 1)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddCredential_Totp(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            var response = connection.SendCommand(new PutCommand(_fixture.TotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Theory]
        [TestPriority(priority: 1)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddCredential_Hotp(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            var response = connection.SendCommand(new PutCommand(_fixture.HotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Theory]
        [TestPriority(priority: 2)]
        [InlineData(StandardTestDevice.Fw5)]
        public void FindAddedCredentials(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            var response = connection.SendCommand(new ListCommand());
            var data = response.GetData();
            Assert.Contains(_fixture.TotpCredential, data);
            Assert.Contains(_fixture.HotpCredential, data);
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateCredential_Totp(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            var response = connection.SendCommand(
                new CalculateCredentialCommand(_fixture.TotpCredential, ResponseFormat.Truncated));
            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.NotNull(response.GetData().Value);
        }

        [Theory]
        [TestPriority(priority: 3)]
        [InlineData(StandardTestDevice.Fw5)]
        public void CalculateCredential_Hotp(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            var response = connection.SendCommand(
                new CalculateCredentialCommand(_fixture.HotpCredential, ResponseFormat.Truncated));
            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.NotNull(response.GetData().Value);
        }

        [Theory]
        [TestPriority(priority: 4)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameCredential_Totp(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            var renameCommand = new RenameCommand(_fixture.TotpCredential, "Test", "test@example.com");
            OathResponse response = connection.SendCommand(renameCommand);

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Theory]
        [TestPriority(priority: 5)]
        [InlineData(StandardTestDevice.Fw5)]
        public void RenameCredential_EmptyIssuer(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            _fixture.TotpCredential.Issuer = "Test";
            _fixture.TotpCredential.AccountName = "test@example.com";

            var editCommand = new RenameCommand(_fixture.TotpCredential, "", "test@example.com");
            OathResponse response = connection.SendCommand(editCommand);

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Theory]
        [TestPriority(priority: 6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void DeleteCredential_Totp(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            _fixture.TotpCredential.Issuer = "";
            _fixture.TotpCredential.AccountName = "test@example.com";

            var response = connection.SendCommand(new DeleteCommand(_fixture.TotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Theory]
        [TestPriority(priority: 6)]
        [InlineData(StandardTestDevice.Fw5)]
        public void DeleteCredential_Hotp(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using var connection = testDevice.Connect(YubiKeyApplication.Oath);

            var response = connection.SendCommand(new DeleteCommand(_fixture.HotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }
    }
}

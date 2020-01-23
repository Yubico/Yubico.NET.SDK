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
using Yubico.YubiKey.Oath.Commands;

namespace Yubico.YubiKey.Oath
{
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    public class CredentialTests : IClassFixture<CredentialFixture>, IDisposable
    {
        // Shared object instance across tests.
        readonly CredentialFixture _fixture;

        private readonly bool _isValid;
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private readonly IYubiKeyConnection _connection;
        private bool disposedValue;

        // Shared setup for every test that is run.
        public CredentialTests(CredentialFixture fixture)
        {
            _fixture = fixture;
            _isValid = SelectSupport.TrySelectYubiKey(out _yubiKeyDevice);
            _connection = _yubiKeyDevice.Connect(YubiKeyApplication.Oath);
        }

        // Shared cleanup for every test that is run.
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _connection.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [Fact, TestPriority(0)]
        public void Constructor()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);
        }

        [Fact, TestPriority(1)]
        public void AddCredential_Totp()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            OathResponse response = _connection.SendCommand(new PutCommand(_fixture.TotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact, TestPriority(1)]
        public void AddCredential_Hotp()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            OathResponse response = _connection.SendCommand(new PutCommand(_fixture.HotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact, TestPriority(2)]
        public void FindAddedCredentials()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            ListResponse response = _connection.SendCommand(new ListCommand());
            List<Credential> data = response.GetData();
            Assert.Contains(_fixture.TotpCredential, data);
            Assert.Contains(_fixture.HotpCredential, data);
        }

        [Fact, TestPriority(3)]
        public void CalculateCredential_Totp()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            CalculateCredentialResponse response = _connection.SendCommand(
                new CalculateCredentialCommand(_fixture.TotpCredential, ResponseFormat.Truncated));
            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.NotNull(response.GetData().Value);
        }

        [Fact, TestPriority(3)]
        public void CalculateCredential_Hotp()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            CalculateCredentialResponse response = _connection.SendCommand(
                new CalculateCredentialCommand(_fixture.HotpCredential, ResponseFormat.Truncated));
            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.NotNull(response.GetData().Value);
        }

        [Fact, TestPriority(4)]
        public void RenameCredential_Totp()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0)
            {
                var renameCommand = new RenameCommand(_fixture.TotpCredential, "Test", "test@example.com");
                OathResponse response = _connection.SendCommand(renameCommand);

                Assert.Equal(ResponseStatus.Success, response.Status);
            }
        }

        [Fact, TestPriority(5)]
        public void RenameCredential_EmptyIssuer()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0)
            {
                _fixture.TotpCredential.Issuer = "Test";
                _fixture.TotpCredential.AccountName = "test@example.com";

                var editCommand = new RenameCommand(_fixture.TotpCredential, "", "test@example.com");
                OathResponse response = _connection.SendCommand(editCommand);

                Assert.Equal(ResponseStatus.Success, response.Status);
            }
        }

        [Fact, TestPriority(6)]
        public void DeleteCredential_Totp()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            if (_yubiKeyDevice.FirmwareVersion >= FirmwareVersion.V5_3_0)
            {
                _fixture.TotpCredential.Issuer = "";
                _fixture.TotpCredential.AccountName = "test@example.com";
            }

            DeleteResponse response = _connection.SendCommand(new DeleteCommand(_fixture.TotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact, TestPriority(6)]
        public void DeleteCredential_Hotp()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            DeleteResponse response = _connection.SendCommand(new DeleteCommand(_fixture.HotpCredential));
            Assert.Equal(ResponseStatus.Success, response.Status);
        }
    }
}

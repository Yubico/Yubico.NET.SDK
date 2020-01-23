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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath
{
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    public sealed class OathSessionPasswordTests : IDisposable
    {
        private readonly bool _isValid;
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private IYubiKeyConnection? _connection;
        private readonly OathSession _oathSession;
        private readonly SimpleOathKeyCollector _collectorObj;

        public OathSessionPasswordTests()
        {
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
        public void SetPassword()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _oathSession.SetPassword();

            Assert.False(_oathSession._oathData.Challenge.IsEmpty);
        }

        [Fact, TestPriority(1)]
        public void VerifyCorrectPassword()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);
            
            bool isVerified = _oathSession.TryVerifyPassword();
            Assert.True(isVerified);
        }

        [Fact, TestPriority(2)]
        public void VerifyWrongPassword()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _collectorObj.KeyFlag = 1;

            bool isVerified = _oathSession.TryVerifyPassword();
            Assert.False(isVerified);
        }

        [Fact, TestPriority(3)]
        public void ChangePassword()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _collectorObj.KeyFlag = 1;
            _oathSession.SetPassword();

            Assert.False(_oathSession._oathData.Challenge.IsEmpty);
        }

        [Fact, TestPriority(4)]
        public void UnsetPassword()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _collectorObj.KeyFlag = 1;
            _oathSession.UnsetPassword();

            Assert.True(_oathSession._oathData.Challenge.IsEmpty);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _oathSession.Dispose();
            _connection = null;
        }
    }
}

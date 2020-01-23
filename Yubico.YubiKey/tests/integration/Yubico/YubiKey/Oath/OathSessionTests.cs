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
    public sealed class OathSessionTests : IDisposable
    {
        private readonly bool _isValid;
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private IYubiKeyConnection? _connection;
        private readonly OathSession _oathSession;

        public OathSessionTests()
        {
            _isValid = SelectSupport.TrySelectYubiKey(out _yubiKeyDevice);
            
            if (_isValid)
            {
                _connection = _yubiKeyDevice.Connect(YubiKeyApplication.Oath);
            }

            _oathSession = new OathSession(_yubiKeyDevice);
        }

        [Fact]
        public void ResetOathApplication()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_connection);

            _oathSession.ResetApplication();
            IList<Credential> data = _oathSession.GetCredentials();

            Assert.True(_oathSession._oathData.Challenge.IsEmpty);
            Assert.Empty(data);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _oathSession.Dispose();
            _connection = null;
        }
    }
}

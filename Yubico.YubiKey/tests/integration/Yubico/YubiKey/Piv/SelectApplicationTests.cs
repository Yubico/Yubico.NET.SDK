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
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public sealed class SelectApplicationTests : IDisposable
    {
        private readonly bool _isValid;
        private readonly IYubiKeyDevice _yubiKeyDevice;
        private IYubiKeyConnection? _pivConnection;

        public SelectApplicationTests()
        {
            _isValid = SelectSupport.TrySelectYubiKey(out _yubiKeyDevice);
            if (_isValid)
            {
                _pivConnection = _yubiKeyDevice.Connect(YubiKeyApplication.Piv);
            }
        }

        [Fact]
        public void ConnectOathHasData()
        {
            Assert.True(_isValid);
            Assert.True(_yubiKeyDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Oath));
            Assert.NotNull(_pivConnection);

            // Connect does not actually select the app.  We need a command for this.  It can be anything.
            _ = _pivConnection!.SendCommand(new Piv.Commands.GetSerialNumberCommand());

            Assert.NotNull(_pivConnection!.SelectApplicationData);
            var data = Assert.IsType<InterIndustry.Commands.GenericSelectApplicationData>(_pivConnection.SelectApplicationData);

            Assert.False(data!.RawData.IsEmpty);
        }

        public void Dispose()
        {
            _pivConnection?.Dispose();
            _pivConnection = null;
        }
    }
}

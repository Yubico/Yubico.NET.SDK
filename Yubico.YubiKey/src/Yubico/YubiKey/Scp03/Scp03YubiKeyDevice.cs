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

using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey
{
    internal class Scp03YubiKeyDevice : YubiKeyDevice
    {
        public StaticKeys StaticKeys { get; private set; }

        public Scp03YubiKeyDevice(YubiKeyDevice device, StaticKeys staticKeys)
            : base(device.GetSmartCardDevice(), hidKeyboardDevice: null, hidFidoDevice: null, device)
        {
            StaticKeys = staticKeys.GetCopy();
        }

        internal override IYubiKeyConnection? Connect(
            YubiKeyApplication? application,
            byte[]? applicationId,
            StaticKeys? scp03Keys)
        {
            if (!HasSmartCard)
            {
                return null;
            }

            if (!(scp03Keys is null) && !StaticKeys.AreKeysSame(scp03Keys))
            {
                return null;
            }

            if (!(application is null))
            {
                return new Scp03Connection(GetSmartCardDevice(), (YubiKeyApplication)application, StaticKeys);
            }

            if (!(applicationId is null))
            {
                return new Scp03Connection(GetSmartCardDevice(), applicationId, StaticKeys);
            }

            return null;
        }
    }
}

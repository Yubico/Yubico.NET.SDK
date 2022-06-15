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

using System.Diagnostics.CodeAnalysis;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey
{
    internal class Scp03YubiKeyDevice : YubiKeyDevice
    {
        private StaticKeys StaticKeys { get; set; }

        public Scp03YubiKeyDevice(YubiKeyDevice device, StaticKeys staticKeys)
            : base(device.GetSmartCardDevice(), null, null, device)
        {
            StaticKeys = staticKeys;
        }

        public override bool TryConnect(
            YubiKeyApplication application,
            [MaybeNullWhen(returnValue: false)]
            out IYubiKeyConnection connection)
        {
            if (!HasSmartCard)
            {
                connection = null;
                return false;
            }

            connection = new CcidConnection(GetSmartCardDevice(), application, StaticKeys);

            return true;
        }

        public override bool TryConnect(
            byte[] applicationId,
            [MaybeNullWhen(returnValue: false)]
            out IYubiKeyConnection connection)
        {
            if (!HasSmartCard)
            {
                connection = null;
                return false;
            }

            connection = new CcidConnection(GetSmartCardDevice(), applicationId, StaticKeys);

            return true;
        }
    }
}

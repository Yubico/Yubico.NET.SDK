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

namespace Yubico.YubiKey.Scp
{
    /// <summary>
    /// TODO DO I EVEN NEED THIS CLASS?
    /// </summary>
    internal class ScpYubiKeyDevice : YubiKeyDevice
    {
        public ScpKeyParameters KeyParameters { get; private set; }

        public ScpYubiKeyDevice(
            YubiKeyDevice device,
            ScpKeyParameters keyParameters)
            : base(device.GetSmartCardDevice(),
                null,
                null,
                device)
        {
            KeyParameters = keyParameters;
        }

        // internal override IYubiKeyConnection? Connect(
        //     YubiKeyApplication application,
        //     ScpKeyParameters scpKeys)
        // {
        //     if (!HasSmartCard)
        //     {
        //         return null;
        //     }
        //
        //     //Scp3 check
        //
        //     if (scpKeys is Scp03KeyParameters scp03)
        //     {
        //         if (KeyParameters is Scp03KeyParameters deviceIsScp03 && // TODO Determine or set type of ScpDevice earlier?
        //             !deviceIsScp03.StaticKeys.AreKeysSame(scp03.StaticKeys))
        //         {
        //             return null;
        //         }
        //     }
        //
        //     return new ScpConnection(GetSmartCardDevice(), application, KeyParameters);
        // }
    }
}

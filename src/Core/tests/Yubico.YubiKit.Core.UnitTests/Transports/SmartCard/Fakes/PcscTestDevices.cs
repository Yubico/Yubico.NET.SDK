// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

internal static class PcscTestDevices
{
    public static YubiKeyDevice Create(ISCardConnectionApi api) =>
        Create(new SmartCardConnectionFactory(api), out _);

    public static YubiKeyDevice Create(
        ISmartCardConnectionFactory factory,
        out PcscConnectionSlot slot)
    {
        var pcscDevice = new PcscDevice
        {
            ReaderName = $"async-boundary-{Guid.NewGuid():N}",
            Atr = null
        };
        slot = new PcscConnectionSlot(pcscDevice, factory);
        return new YubiKeyDevice(slot.InterfaceId, slot, hidFido: null, hidOtp: null, deviceInfo: null);
    }
}
// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Core.YubiKey;

internal static class ConnectionTypeExtensions
{
    private const ConnectionType HidConnectionTypes = ConnectionType.Hid | ConnectionType.HidFido | ConnectionType.HidOtp;

    public static bool IncludesHidScan(this ConnectionType filter) => (filter & HidConnectionTypes) != 0;

    public static bool MatchesDevice(this ConnectionType filter, ConnectionType deviceConnectionType)
    {
        if (filter == ConnectionType.Unknown || deviceConnectionType == ConnectionType.Unknown)
        {
            return false;
        }

        if (filter == ConnectionType.All)
        {
            return true;
        }

        if (filter.HasFlag(ConnectionType.Hid) && deviceConnectionType is ConnectionType.Hid or ConnectionType.HidFido or ConnectionType.HidOtp)
        {
            return true;
        }

        return filter.HasFlag(deviceConnectionType);
    }
}
// Copyright 2025 Yubico AB
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

using System.Collections.Generic;

namespace Yubico.YubiKey;

internal class ProductIdentifiers
{
    // YubiKey Version 1 and Version 2
    public const short YubiKeyV1V2 = 0x0010;

    // YubiKey Plus
    public const short YubiKeyPlus = 0x0410;

    // YubiKey NEO(-N)
    public const short YubiKeyNeoOtp = 0x0110;
    public const short YubiKeyNeoOtpCcid = 0x0111;
    public const short YubiKeyNeoCcid = 0x0112;
    public const short YubiKeyNeoU2f = 0x0113;
    public const short YubiKeyNeoOtpU2f = 0x0114;
    public const short YubiKeyNeoU2fCcid = 0x0115;
    public const short YubiKeyNeoOtpU2fCcid = 0x0116;

    // YubiKey 4 and 5 series
    public const short YubiKeyOtp = 0x0401;
    public const short YubiKeyFido = 0x0402;
    public const short YubiKeyOtpFido = 0x0403;
    public const short YubiKeyCcid = 0x0404;
    public const short YubiKeyOtpCcid = 0x0405;
    public const short YubiKeyFidoCcid = 0x0406;
    public const short YubiKeyOtpFidoCcid = 0x0407;

    // Security Key Series
    public const short SecurityKey = 0x0120;

    public static IList<short> AllYubiKeys =>
        new List<short>
        {
            YubiKeyV1V2,
            YubiKeyPlus,
            YubiKeyNeoOtp,
            YubiKeyNeoOtpCcid,
            YubiKeyNeoU2f,
            YubiKeyNeoOtpU2f,
            YubiKeyNeoU2fCcid,
            YubiKeyNeoOtpU2fCcid,
            YubiKeyOtp,
            YubiKeyFido,
            YubiKeyOtpFido,
            YubiKeyCcid,
            YubiKeyOtpCcid,
            YubiKeyFidoCcid,
            YubiKeyOtpFidoCcid,
            SecurityKey
        };
}

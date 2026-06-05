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

using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.SecurityDomain;

internal static class SecurityDomainTlvEncoding
{
    private const byte TagKid = 0xD0;
    private const byte TagKvn = 0xD2;

    internal static (byte Kid, byte Kvn) NormalizeDeletePolicy(KeyReference keyReference)
    {
        var kid = keyReference.Kid;
        var kvn = keyReference.Kvn;

        if (kid == 0 && kvn == 0)
            throw new ArgumentException("At least one of KID or KVN must be non-zero", nameof(keyReference));

        if (kid is 0x01 or 0x02 or 0x03)
            kid = 0;

        return (kid, kvn);
    }

    internal static ReadOnlyMemory<byte> EncodeDeleteFilter(byte kid, byte kvn)
    {
        if (kid == 0 && kvn == 0)
            return ReadOnlyMemory<byte>.Empty;

        var dict = new Dictionary<int, byte[]?>(2);
        if (kid != 0)
            dict[TagKid] = [kid];
        if (kvn != 0)
            dict[TagKvn] = [kvn];

        return TlvHelper.EncodeDictionary(dict);
    }
}

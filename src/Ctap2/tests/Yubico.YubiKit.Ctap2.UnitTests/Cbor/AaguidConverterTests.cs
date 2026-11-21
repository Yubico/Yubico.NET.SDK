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

using Yubico.YubiKit.Ctap2.Cbor;

namespace Yubico.YubiKit.Ctap2.UnitTests.Cbor;

public sealed class AaguidConverterTests
{
    [Theory]
    [InlineData("00112233-4455-6677-8899-aabbccddeeff", "00112233445566778899AABBCCDDEEFF")]
    [InlineData("00000000-0000-0000-0000-000000000000", "00000000000000000000000000000000")]
    public void NetworkOrder_MatchesKnownVectorInBothDirections(string guidText, string wireHex)
    {
        var guid = Guid.Parse(guidText);
        byte[] wire = Convert.FromHexString(wireHex);

        Assert.Equal(guid, AaguidConverter.FromBigEndianBytes(wire));
        Assert.Equal(wire, AaguidConverter.ToBigEndianBytes(guid));
        Assert.Equal(guid, AaguidConverter.FromBigEndianBytes(AaguidConverter.ToBigEndianBytes(guid)));
    }
}
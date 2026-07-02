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

using Xunit;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

public class CoseSignArgsTests
{
    [Fact]
    public void CoseSignArgs_StaticFactory_ArkgP256_ProducesEquivalentInstance()
    {
        byte[] kh = BuildArkgKeyHandleFixture(tagPattern: 0x11, pubKeyPattern: 0x22);
        byte[] ctx = "ARKG-P256.test vectors"u8.ToArray();

        CoseSignArgs viaFactory = CoseSignArgs.ArkgP256(kh, ctx);
        var viaCtor = new ArkgP256SignArgs(kh, ctx);

        var fromFactory = Assert.IsType<ArkgP256SignArgs>(viaFactory);
        Assert.Equal(viaCtor.Algorithm, fromFactory.Algorithm);
        Assert.Equal(viaCtor.KeyHandle.ToArray(), fromFactory.KeyHandle.ToArray());
        Assert.Equal(viaCtor.Context.ToArray(), fromFactory.Context.ToArray());
    }

    private static byte[] BuildArkgKeyHandleFixture(byte tagPattern, byte pubKeyPattern)
    {
        byte[] kh = new byte[81];
        for (int i = 0; i < 16; i++)
        {
            kh[i] = tagPattern;
        }
        kh[16] = 0x04;
        for (int i = 17; i < 81; i++)
        {
            kh[i] = pubKeyPattern;
        }
        return kh;
    }
}
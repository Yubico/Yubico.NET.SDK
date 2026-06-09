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

using System.Formats.Cbor;
using Xunit;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

public class LargeBlobAssertionInputTests
{
    [Fact]
    public void LargeBlobAssertionInput_EncodesReadCorrectly()
    {
        var input = new LargeBlobAssertionInput { Read = true };

        var encoded = input.Encode();

        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("read", reader.ReadTextString());
        Assert.True(reader.ReadBoolean());
    }

    [Fact]
    public void LargeBlobAssertionInput_EncodesWriteCorrectly()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var input = new LargeBlobAssertionInput { Write = data };

        var encoded = input.Encode();

        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        reader.ReadStartMap();
        Assert.Equal("write", reader.ReadTextString());
        Assert.Equal(data, reader.ReadByteString());
    }

    [Fact]
    public void LargeBlobAssertionInput_ThrowsWhenEmpty()
    {
        var input = new LargeBlobAssertionInput();

        Assert.Throws<InvalidOperationException>(() => input.Encode());
    }
}
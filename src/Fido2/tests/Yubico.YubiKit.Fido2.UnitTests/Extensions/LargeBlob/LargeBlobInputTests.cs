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

public class LargeBlobInputTests
{
    [Fact]
    public void LargeBlobInput_EncodesPreferredCorrectly()
    {
        var input = new LargeBlobInput { Support = LargeBlobSupport.Preferred };

        var encoded = input.Encode();

        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        var count = reader.ReadStartMap();
        Assert.Equal(1, count);
        Assert.Equal("support", reader.ReadTextString());
        Assert.Equal("preferred", reader.ReadTextString());
    }

    [Fact]
    public void LargeBlobInput_EncodesRequiredCorrectly()
    {
        var input = new LargeBlobInput { Support = LargeBlobSupport.Required };

        var encoded = input.Encode();

        var reader = new CborReader(encoded, CborConformanceMode.Lax);
        reader.ReadStartMap();
        reader.ReadTextString();
        Assert.Equal("required", reader.ReadTextString());
    }
}

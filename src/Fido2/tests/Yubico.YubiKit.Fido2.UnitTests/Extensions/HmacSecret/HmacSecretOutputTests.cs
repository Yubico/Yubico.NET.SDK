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

public class HmacSecretOutputTests
{
    [Fact]
    public void HmacSecretOutput_DecodesCorrectly()
    {
        var outputData = new byte[48];
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteByteString(outputData);
        var encoded = writer.Encode();

        var output = HmacSecretOutput.Decode(encoded);

        Assert.Equal(48, output.Output.Length);
    }

    [Fact]
    public void HmacSecretMcOutput_DecodesCorrectly()
    {
        var outputData = new byte[48];
        Random.Shared.NextBytes(outputData);
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteByteString(outputData);
        var encoded = writer.Encode();

        var output = HmacSecretOutput.Decode(encoded);

        Assert.Equal(48, output.Output.Length);
        Assert.Equal(outputData, output.Output.ToArray());
    }
}

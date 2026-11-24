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
using Yubico.YubiKit.Ctap2.Extensions;

namespace Yubico.YubiKit.Ctap2.UnitTests.Extensions;

[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public class PreviewSignCborTests
{
    [Fact]
    public void PreviewSignWireCodec_IsNotPublic() => Assert.False(typeof(PreviewSignCbor).IsVisible);

    [Theory]
    [InlineData("A20328058101", null)] // Unknown key is skipped.
    [InlineData("A203280402", 2)] // Tolerated outer flags.
    public void DecodeRegistrationOutput_ReadsKnownFields(string hex, int? flags)
    {
        var reader = new CborReader(Convert.FromHexString(hex), CborConformanceMode.Ctap2Canonical);

        Assert.Equal((-9, flags), PreviewSignCbor.DecodeRegistrationOutput(reader));
    }

    [Fact]
    public void DecodeRegistrationOutput_MissingAlgorithm_Throws()
    {
        // {4: 1}: presence policy without the required algorithm at key 3.
        var reader = new CborReader(Convert.FromHexString("A10401"), CborConformanceMode.Ctap2Canonical);

        Assert.Throws<InvalidOperationException>(() => PreviewSignCbor.DecodeRegistrationOutput(reader));
    }

    [Fact]
    public void DecodeUnsignedRegistrationOutput_SkipsUnknownKeysAndKeepsStatementBytes()
    {
        // {7: bstr({1: "none", 2: h'42', 3: {"x": 1}, 4: []}), 8: {}}
        var decoded = PreviewSignCbor.DecodeUnsignedRegistrationOutput(
            Convert.FromHexString("A20751A401646E6F6E6502414203A1617801048008A0"));

        Assert.Equal("none", decoded.Fmt);
        Assert.Equal(new byte[] { 0x42 }, decoded.AuthData.ToArray());
        Assert.Equal(Convert.FromHexString("A1617801"), decoded.AttStmtRawCbor.ToArray());
    }

    [Theory]
    [InlineData("A1074CA301646E6F6E6502414203A000")] // Trailing bytes after the outer map.
    [InlineData("A1074DA301646E6F6E6502414203A000")] // Trailing bytes inside the attestation byte string.
    [InlineData("A1074CA301646E6F6E650241420380")] // Attestation statement is an array, not a map.
    public void DecodeUnsignedRegistrationOutput_MalformedEncoding_Throws(string hex) =>
        Assert.Throws<CborContentException>(() => PreviewSignCbor.DecodeUnsignedRegistrationOutput(Convert.FromHexString(hex)));

    [Theory]
    [InlineData("A0")] // No attestation object.
    [InlineData("A1074AA201646E6F6E65024142")] // No attestation statement.
    public void DecodeUnsignedRegistrationOutput_MissingRequiredField_Throws(string hex) =>
        Assert.Throws<InvalidOperationException>(() => PreviewSignCbor.DecodeUnsignedRegistrationOutput(Convert.FromHexString(hex)));
}
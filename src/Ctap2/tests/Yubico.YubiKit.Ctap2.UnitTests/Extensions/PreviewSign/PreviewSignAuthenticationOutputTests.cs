// Copyright Yubico AB
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
public class PreviewSignAuthenticationOutputTests
{
    [Fact]
    public void TryGetPreviewSign_Present_ReturnsSignatureCopy()
    {
        // {"previewSign": {6: h'1234', 7: []}}
        byte[] source = Convert.FromHexString("A16B707265766965775369676EA2064212340780");
        var extensions = ExtensionOutput.Decode(source);

        Assert.True(extensions.TryGetPreviewSign(out var output));
        source.AsSpan().Clear();
        Assert.Equal(new byte[] { 0x12, 0x34 }, output.Signature.ToArray());
    }

    [Fact]
    public void TryGetPreviewSign_Absent_ReturnsFalse()
    {
        var extensions = ExtensionOutput.Decode(new byte[] { 0xA0 });

        Assert.False(extensions.TryGetPreviewSign(out var output));
        Assert.Null(output);
    }

    [Theory]
    [InlineData("A0")] // Missing signature.
    [InlineData("A10640")] // Empty signature cannot construct a typed Ctap2 result.
    [InlineData("A10601")] // Signature is not a byte string.
    public void Decode_WithoutUsableSignature_Throws(string hex) =>
        Assert.Throws<InvalidOperationException>(() => PreviewSignAuthenticationOutput.Decode(Convert.FromHexString(hex)));

    [Fact]
    public void Decode_WithTrailingData_Throws() =>
        Assert.Throws<CborContentException>(() => PreviewSignAuthenticationOutput.Decode(Convert.FromHexString("A106411200")));

    [Fact]
    public void TryGetPreviewSign_OnRegistrationOutput_ThrowsInsteadOfTreatingItAsAbsent()
    {
        var extensions = ExtensionOutput.Decode(Convert.FromHexString("A16B707265766965775369676EA10328"));

        Assert.Throws<InvalidOperationException>(() => extensions.TryGetPreviewSign(out _));
    }
}
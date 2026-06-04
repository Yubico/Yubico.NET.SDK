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

using Xunit;
using WebAuthnPreviewSign = Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

/// <summary>
/// Tests for the WebAuthn-layer <c>PreviewSignSigningParams</c> raw additionalArgs surface.
/// </summary>
/// <remarks>
/// Validates that WebAuthn keeps the generic previewSign API algorithm-agile: <c>additionalArgs</c>
/// is caller-provided algorithm-specific data and is exposed unchanged.
/// </remarks>
public class PreviewSignSigningParamsTests
{
    [Fact]
    public void Constructor_AcceptsAdditionalArgs_AndExposesIt()
    {
        var kh = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var tbs = new byte[32];
        var additionalArgs = new byte[] { 0xA3, 0x03, 0x01 };

        var sut = new WebAuthnPreviewSign.PreviewSignSigningParams(kh, tbs, additionalArgs);

        Assert.Equal(kh, sut.KeyHandle.ToArray());
        Assert.Equal(tbs, sut.Tbs.ToArray());
        Assert.Equal(additionalArgs, sut.AdditionalArgs!.Value.ToArray());
    }

    [Fact]
    public void Constructor_AcceptsNullAdditionalArgs()
    {
        var sut = new WebAuthnPreviewSign.PreviewSignSigningParams(
            keyHandle: new byte[] { 0xAA },
            tbs: new byte[32],
            additionalArgs: null);

        Assert.Null(sut.AdditionalArgs);
    }

    [Fact]
    public void Constructor_DefaultsAdditionalArgsToNull()
    {
        var sut = new WebAuthnPreviewSign.PreviewSignSigningParams(
            keyHandle: new byte[] { 0xAA },
            tbs: new byte[32]);

        Assert.Null(sut.AdditionalArgs);
    }

    [Fact]
    public void Constructor_EmptyKeyHandle_ThrowsInvalidRequest()
    {
        var ex = Assert.Throws<WebAuthnClientError>(
            () => new WebAuthnPreviewSign.PreviewSignSigningParams(
                keyHandle: ReadOnlyMemory<byte>.Empty,
                tbs: new byte[32]));
        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
    }

    [Fact]
    public void Constructor_EmptyTbs_ThrowsInvalidRequest()
    {
        var ex = Assert.Throws<WebAuthnClientError>(
            () => new WebAuthnPreviewSign.PreviewSignSigningParams(
                keyHandle: new byte[] { 0xAA },
                tbs: ReadOnlyMemory<byte>.Empty));
        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, ex.Code);
    }

}

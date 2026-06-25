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
using Fido2Extensions = Yubico.YubiKit.Fido2.Extensions;
using WebAuthnPreviewSign = Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

/// <summary>
/// Tests for the WebAuthn-layer <c>PreviewSignSigningParams</c> typed-CoseSignArgs surface.
/// </summary>
/// <remarks>
/// Validates that the WebAuthn layer re-exports the Fido2 <c>CoseSignArgs</c> type unchanged
/// (no clone, no parallel CBOR encoder), per the no-duplication invariant
/// (<c>src/WebAuthn/CLAUDE.md</c> + repo <c>MEMORY.md</c>: "WebAuthn must duplicate zero
/// Fido2 behavior").
/// </remarks>
public class PreviewSignSigningParamsTests
{
    [Fact]
    public void Constructor_AcceptsTypedCoseSignArgs_AndExposesIt()
    {
        var kh = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var tbs = new byte[32];
        var arkg = new Fido2Extensions.ArkgP256SignArgs(
            keyHandle: BuildArkgKeyHandle(),
            context: "ARKG-P256.test vectors"u8.ToArray());

        var sut = new WebAuthnPreviewSign.PreviewSignSigningParams(kh, tbs, arkg);

        // Reference equality (passthrough — no clone): exposes the same instance the caller passed.
        Assert.Same(arkg, sut.CoseSignArgs);
        Assert.Equal(kh, sut.KeyHandle.ToArray());
        Assert.Equal(tbs, sut.Tbs.ToArray());
    }

    [Fact]
    public void Constructor_AcceptsNullCoseSignArgs()
    {
        var sut = new WebAuthnPreviewSign.PreviewSignSigningParams(
            keyHandle: new byte[] { 0xAA },
            tbs: new byte[32],
            coseSignArgs: null);

        Assert.Null(sut.CoseSignArgs);
    }

    [Fact]
    public void Constructor_DefaultsCoseSignArgsToNull()
    {
        var sut = new WebAuthnPreviewSign.PreviewSignSigningParams(
            keyHandle: new byte[] { 0xAA },
            tbs: new byte[32]);

        Assert.Null(sut.CoseSignArgs);
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

    [Fact]
    public void Constructor_AcceptsCoseSignArgsViaStaticFactory()
    {
        var kh = BuildArkgKeyHandle();
        var ctx = "ARKG-P256.test vectors"u8.ToArray();

        // Caller-friendly factory entry point on the abstract base — this is the recommended
        // construction pattern for everyday WebAuthn callers.
        Fido2Extensions.CoseSignArgs typed = Fido2Extensions.CoseSignArgs.ArkgP256(kh, ctx);
        var sut = new WebAuthnPreviewSign.PreviewSignSigningParams(kh, new byte[32], typed);

        var arkg = Assert.IsType<Fido2Extensions.ArkgP256SignArgs>(sut.CoseSignArgs);
        Assert.Equal(-65539, arkg.Algorithm);
        Assert.Equal(kh, arkg.KeyHandle.ToArray());
        Assert.Equal(ctx, arkg.Context.ToArray());
    }

    /// <summary>
    /// 81-byte ARKG-P256 KH fixture: 16-byte tag (0xA5) || 65-byte SEC1 point (0x04 || 64×0x5A).
    /// </summary>
    private static byte[] BuildArkgKeyHandle()
    {
        byte[] kh = new byte[81];
        for (int i = 0; i < 16; i++)
        {
            kh[i] = 0xA5;
        }
        kh[16] = 0x04;
        for (int i = 17; i < 81; i++)
        {
            kh[i] = 0x5A;
        }
        return kh;
    }
}
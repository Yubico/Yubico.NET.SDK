// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client.UserVerification;

/// <summary>
/// Truth table for "does this ceremony need a PIN/UV auth token", checked against the canonical
/// Rust client's <c>should_use_uv</c> (<c>crates/yubikit/src/webauthn/client.rs</c>).
/// </summary>
/// <remarks>
/// The case worth protecting is <see cref="UserVerificationPreference.Discouraged"/> on a key that
/// has a PIN set. Verification must be skipped there. An earlier implementation asked for a token
/// whenever one was reachable, which meant that merely configuring an <c>ICredentialPrompt</c> was
/// enough to start demanding PINs for relying parties that had explicitly asked for none.
/// </remarks>
public class UvDecisionLogicTests
{
    private const PinUvAuthTokenPermissions MakeCredentialPermissions =
        PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion;

    private const PinUvAuthTokenPermissions GetAssertionPermissions =
        PinUvAuthTokenPermissions.GetAssertion;

    private const PinUvAuthTokenPermissions LargeBlobWritePermissions =
        PinUvAuthTokenPermissions.MakeCredential
        | PinUvAuthTokenPermissions.GetAssertion
        | PinUvAuthTokenPermissions.LargeBlobWrite;

    /// <summary>
    /// Builds authenticator info from the four options that drive the decision. A <c>null</c>
    /// argument means the authenticator does not advertise the option at all, which is a different
    /// state from advertising it as <c>false</c>.
    /// </summary>
    private static AuthenticatorInfo InfoWithOptions(
        bool? clientPin = null,
        bool? uv = null,
        bool? alwaysUv = null,
        bool? makeCredUvNotRqd = null,
        bool? bioEnroll = null)
    {
        var options = new Dictionary<string, bool>(StringComparer.Ordinal);

        if (clientPin is { } clientPinValue) options["clientPin"] = clientPinValue;
        if (uv is { } uvValue) options["uv"] = uvValue;
        if (alwaysUv is { } alwaysUvValue) options["alwaysUv"] = alwaysUvValue;
        if (makeCredUvNotRqd is { } makeCredValue) options["makeCredUvNotRqd"] = makeCredValue;
        if (bioEnroll is { } bioEnrollValue) options["bioEnroll"] = bioEnrollValue;

        return new AuthenticatorInfo { Options = options };
    }

    // ---------------------------------------------------------------------------------------
    // Discouraged
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// A relying party that discouraged verification gets none, even though the key has a PIN and
    /// the client could ask for it. This is the regression the prompt feature would otherwise have
    /// introduced.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedOnModernKeyWithPinSet_DoesNotRequestToken()
    {
        var info = InfoWithOptions(clientPin: true, uv: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.False(decision.UseToken);
        Assert.False(decision.UseUv);
        Assert.Null(decision.UvOption);
        Assert.Null(decision.Method);
    }

    /// <summary>
    /// A pre-CTAP-2.1 authenticator does not advertise <c>makeCredUvNotRqd</c>, and refuses an
    /// unverified makeCredential once a PIN is set. Verification is required despite the
    /// preference.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public void Decide_DiscouragedMakeCredentialWithoutMakeCredUvNotRqd_RequestsToken(
        bool? makeCredUvNotRqd)
    {
        var info = InfoWithOptions(clientPin: true, uv: false, makeCredUvNotRqd: makeCredUvNotRqd);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.True(decision.UseToken);
        Assert.Equal(PinUvAuthMethod.Pin, decision.Method);
    }

    /// <summary>
    /// The same pre-CTAP-2.1 authenticator still allows an unverified getAssertion: the
    /// makeCredential-specific rule must not leak into authentication.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedGetAssertionWithoutMakeCredUvNotRqd_DoesNotRequestToken()
    {
        var info = InfoWithOptions(clientPin: true, uv: false);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            GetAssertionPermissions);

        Assert.False(decision.UseToken);
    }

    /// <summary>
    /// An authenticator configured to always verify outranks the relying party's preference.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedButAlwaysUvEnabled_RequestsToken()
    {
        var info = InfoWithOptions(clientPin: true, uv: false, alwaysUv: true, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.True(decision.UseToken);
    }

    /// <summary>
    /// <c>alwaysUv</c> cannot force verification that the key has no way to perform.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedWithAlwaysUvButNothingConfigured_DoesNotRequestToken()
    {
        var info = InfoWithOptions(clientPin: false, alwaysUv: true, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.False(decision.UseToken);
    }

    /// <summary>
    /// Permissions beyond an ordinary ceremony (here a large-blob write) require verification
    /// whatever the relying party asked for.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedWithPrivilegedPermission_RequestsToken()
    {
        var info = InfoWithOptions(clientPin: true, uv: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            LargeBlobWritePermissions);

        Assert.True(decision.UseToken);
    }

    /// <summary>
    /// A privileged permission still cannot conjure verification on a key that has none configured.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedWithPrivilegedPermissionButNoUvConfigured_DoesNotRequestToken()
    {
        var info = InfoWithOptions(clientPin: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            LargeBlobWritePermissions);

        Assert.False(decision.UseToken);
    }

    /// <summary>
    /// A key with no PIN set still advertises <c>clientPin: false</c>. Reading that as "verification
    /// is available" is the classic misread; it must count as not configured.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedOnKeyWithNoPinSet_DoesNotRequestToken()
    {
        var info = InfoWithOptions(clientPin: false, uv: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.False(decision.UseToken);
    }

    // ---------------------------------------------------------------------------------------
    // Preferred
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Preferred is the WebAuthn default. On a key with nothing configured it must degrade to an
    /// unverified credential rather than fail.
    /// </summary>
    /// <remarks>
    /// Deliberate divergence from canonical Rust, which tests whether the option is advertised
    /// rather than enabled and would therefore fail here. See the comment in
    /// <c>UvDecisionLogic.ShouldUseUv</c>.
    /// </remarks>
    [Fact]
    public void Decide_PreferredOnKeyWithNoUvConfigured_DegradesToNoUv()
    {
        var info = InfoWithOptions(clientPin: false, uv: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Preferred,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.False(decision.UseToken);
    }

    /// <summary>
    /// Preferred uses verification whenever the key actually has some.
    /// </summary>
    [Fact]
    public void Decide_PreferredOnKeyWithPinSet_RequestsToken()
    {
        var info = InfoWithOptions(clientPin: true, uv: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Preferred,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.True(decision.UseToken);
        Assert.Equal(PinUvAuthMethod.Pin, decision.Method);

        // Only Required pins the CTAP 'uv' option to true; Preferred leaves it unset.
        Assert.Null(decision.UvOption);
    }

    /// <summary>
    /// With a PIN set but no way to obtain it, Preferred proceeds unverified instead of failing.
    /// </summary>
    [Fact]
    public void Decide_PreferredWithPinSetButUnobtainable_DegradesToNoUv()
    {
        var info = InfoWithOptions(clientPin: true, uv: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Preferred,
            pinAvailable: false,
            MakeCredentialPermissions);

        Assert.False(decision.UseToken);
    }

    /// <summary>
    /// Built-in verification (biometrics) is used when no PIN can be obtained.
    /// </summary>
    [Fact]
    public void Decide_PreferredWithBuiltInUvAndNoPin_UsesBuiltInUv()
    {
        var info = InfoWithOptions(clientPin: false, uv: true, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Preferred,
            pinAvailable: false,
            MakeCredentialPermissions);

        Assert.True(decision.UseToken);
        Assert.True(decision.UseUv);
        Assert.Equal(PinUvAuthMethod.Uv, decision.Method);
        Assert.True(decision.UvOption);
    }

    // ---------------------------------------------------------------------------------------
    // Required
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Required always verifies, and pins the CTAP <c>uv</c> option to true.
    /// </summary>
    [Fact]
    public void Decide_RequiredWithPinSet_RequestsTokenAndSetsUvOption()
    {
        var info = InfoWithOptions(clientPin: true, uv: false, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Required,
            pinAvailable: true,
            MakeCredentialPermissions);

        Assert.True(decision.UseToken);
        Assert.Equal(PinUvAuthMethod.Pin, decision.Method);
        Assert.True(decision.UvOption);
    }

    /// <summary>
    /// Required is the one preference that fails rather than degrading, because the relying party
    /// asked for a guarantee the authenticator cannot give.
    /// </summary>
    [Theory]
    [InlineData(false, false, true)]  // PIN set but unobtainable
    [InlineData(false, true, false)]  // no PIN configured at all
    public void Decide_RequiredWithNoUsableMethod_ThrowsNotAllowed(
        bool uv,
        bool clientPin,
        bool pinAvailable)
    {
        var info = InfoWithOptions(clientPin: clientPin, uv: uv, makeCredUvNotRqd: true);

        var error = Assert.Throws<WebAuthnClientError>(() => UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Required,
            pinAvailable,
            MakeCredentialPermissions));

        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
    }

    /// <summary>
    /// Required is satisfied by built-in verification without any PIN.
    /// </summary>
    [Fact]
    public void Decide_RequiredWithBuiltInUvOnly_UsesBuiltInUv()
    {
        var info = InfoWithOptions(clientPin: false, uv: true, makeCredUvNotRqd: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Required,
            pinAvailable: false,
            MakeCredentialPermissions);

        Assert.True(decision.UseToken);
        Assert.True(decision.UseUv);
        Assert.Equal(PinUvAuthMethod.Uv, decision.Method);
    }

    // ---------------------------------------------------------------------------------------
    // Escalation without a usable method
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// When an override wants verification but no method can supply it, a non-Required ceremony
    /// proceeds unverified rather than failing on the client's own reading of the options. If the
    /// authenticator truly insists it answers <c>CTAP2_ERR_PUAT_REQUIRED</c>, and the client retries
    /// with Required.
    /// </summary>
    [Fact]
    public void Decide_DiscouragedEscalatedButPinUnobtainable_ProceedsWithoutUv()
    {
        // bioEnroll makes verification "configured", and the missing makeCredUvNotRqd forces UV,
        // but there is no PIN to fetch and no built-in uv to fall back on.
        var info = InfoWithOptions(clientPin: true, uv: false, bioEnroll: true);

        var decision = UvDecisionLogic.Decide(
            info,
            UserVerificationPreference.Discouraged,
            pinAvailable: false,
            MakeCredentialPermissions);

        Assert.False(decision.UseToken);
        Assert.Null(decision.Method);
    }

    [Fact]
    public void Decide_NullInfo_Throws() =>
        Assert.Throws<ArgumentNullException>(() => UvDecisionLogic.Decide(
            info: null!,
            UserVerificationPreference.Preferred,
            pinAvailable: true,
            MakeCredentialPermissions));
}
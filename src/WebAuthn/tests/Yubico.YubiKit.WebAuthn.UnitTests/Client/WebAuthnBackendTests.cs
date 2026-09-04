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

using NSubstitute;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
/// Covers how <see cref="WebAuthnBackend"/> handles the caller-owned <c>pinUvAuthParam</c>.
/// </summary>
/// <remarks>
/// <para>
/// The backend copies the caller's parameter into the CTAP options object and zeroes that copy
/// once the command returns. Both halves of that contract matter, and neither is observable
/// through <see cref="IWebAuthnBackend"/>, so these tests drive the concrete backend over a
/// substituted <see cref="IFidoSession"/>.
/// </para>
/// <para>
/// The reuse scenario is not hypothetical: <c>ExcludeListPreflight</c> derives one
/// <c>pinUvAuthParam</c>, hoists it above its chunk loop, and rebuilds a fresh request around the
/// very same buffer for every chunk. Were the backend to zero the caller's buffer, every chunk
/// after the first would authenticate with an all-zero parameter.
/// </para>
/// </remarks>
public class WebAuthnBackendTests
{
    private readonly IFidoSession _session = Substitute.For<IFidoSession>();

    [Fact]
    public async Task GetAssertionAsync_DoesNotZeroTheCallersPinUvAuthParam()
    {
        var callerParam = TokenBufferAssert.CreateSentinelToken();
        _ = ArrangeGetAssertion();

        await using var backend = new WebAuthnBackend(_session);
        _ = await backend.GetAssertionAsync(
            CreateGetAssertionRequest(callerParam),
            TestContext.Current.CancellationToken);

        TokenBufferAssert.NotZeroed(
            callerParam,
            "the caller still owns this buffer and reuses it for the next exclude-list chunk");
    }

    [Fact]
    public async Task GetAssertionAsync_SecondCallReusingTheSameParam_StillSendsLiveBytes()
    {
        var callerParam = TokenBufferAssert.CreateSentinelToken();
        var sent = ArrangeGetAssertion();

        await using var backend = new WebAuthnBackend(_session);
        _ = await backend.GetAssertionAsync(
            CreateGetAssertionRequest(callerParam),
            TestContext.Current.CancellationToken);
        _ = await backend.GetAssertionAsync(
            CreateGetAssertionRequest(callerParam),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, sent.ParamPerCall.Count);
        TokenBufferAssert.NotZeroed(
            sent.ParamPerCall[1],
            "the second chunk must authenticate with the real parameter, not an all-zero one");
        Assert.Equal(sent.ParamPerCall[0], sent.ParamPerCall[1]);
    }

    [Fact]
    public async Task GetAssertionAsync_ZeroesItsOwnCopyOfThePinUvAuthParam()
    {
        var callerParam = TokenBufferAssert.CreateSentinelToken();
        var sent = ArrangeGetAssertion();

        await using var backend = new WebAuthnBackend(_session);
        _ = await backend.GetAssertionAsync(
            CreateGetAssertionRequest(callerParam),
            TestContext.Current.CancellationToken);

        var options = Assert.Single(sent.OptionsPerCall);
        Assert.NotNull(options.PinUvAuthParam);
        TokenBufferAssert.Zeroed(
            options.PinUvAuthParam.Value.ToArray(),
            "the backend owns the copy it put in the CTAP options and must clear it");
    }

    [Fact]
    public async Task MakeCredentialAsync_DoesNotZeroTheCallersPinUvAuthParam()
    {
        var callerParam = TokenBufferAssert.CreateSentinelToken();
        _ = ArrangeMakeCredential();

        await using var backend = new WebAuthnBackend(_session);
        _ = await backend.MakeCredentialAsync(
            CreateMakeCredentialRequest(callerParam),
            TestContext.Current.CancellationToken);

        TokenBufferAssert.NotZeroed(
            callerParam,
            "the caller still owns this buffer and may reuse it for a follow-up command");
    }

    [Fact]
    public async Task MakeCredentialAsync_SecondCallReusingTheSameParam_StillSendsLiveBytes()
    {
        var callerParam = TokenBufferAssert.CreateSentinelToken();
        var sent = ArrangeMakeCredential();

        await using var backend = new WebAuthnBackend(_session);
        _ = await backend.MakeCredentialAsync(
            CreateMakeCredentialRequest(callerParam),
            TestContext.Current.CancellationToken);
        _ = await backend.MakeCredentialAsync(
            CreateMakeCredentialRequest(callerParam),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, sent.ParamPerCall.Count);
        TokenBufferAssert.NotZeroed(
            sent.ParamPerCall[1],
            "the second command must authenticate with the real parameter, not an all-zero one");
        Assert.Equal(sent.ParamPerCall[0], sent.ParamPerCall[1]);
    }

    [Fact]
    public async Task MakeCredentialAsync_ZeroesItsOwnCopyOfThePinUvAuthParam()
    {
        var callerParam = TokenBufferAssert.CreateSentinelToken();
        var sent = ArrangeMakeCredential();

        await using var backend = new WebAuthnBackend(_session);
        _ = await backend.MakeCredentialAsync(
            CreateMakeCredentialRequest(callerParam),
            TestContext.Current.CancellationToken);

        var options = Assert.Single(sent.OptionsPerCall);
        Assert.NotNull(options.PinUvAuthParam);
        TokenBufferAssert.Zeroed(
            options.PinUvAuthParam.Value.ToArray(),
            "the backend owns the copy it put in the CTAP options and must clear it");
    }

    /// <summary>
    /// What the substituted session saw: the options object the backend built, plus a snapshot of
    /// the <c>pinUvAuthParam</c> bytes taken while the command was still in flight.
    /// </summary>
    /// <remarks>
    /// The snapshot has to be taken inside the call. The backend clears its copy in a
    /// <c>finally</c>, so reading the options object afterwards only ever shows zeroes - which is
    /// exactly what <c>OptionsPerCall</c> is for.
    /// </remarks>
    private sealed record SentCalls<TOptions>(List<byte[]> ParamPerCall, List<TOptions> OptionsPerCall);

    private SentCalls<GetAssertionOptions> ArrangeGetAssertion()
    {
        var sent = new SentCalls<GetAssertionOptions>([], []);

        _session.GetAssertionAsync(
            Arg.Any<string>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<GetAssertionOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var options = call.Arg<GetAssertionOptions>();
                sent.OptionsPerCall.Add(options);
                sent.ParamPerCall.Add(SnapshotParam(options.PinUvAuthParam));
                return MockFido2Responses.CreateMockGetAssertionResponse();
            });

        return sent;
    }

    private SentCalls<MakeCredentialOptions> ArrangeMakeCredential()
    {
        var sent = new SentCalls<MakeCredentialOptions>([], []);

        _session.MakeCredentialAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<PublicKeyCredentialRpEntity>(),
            Arg.Any<PublicKeyCredentialUserEntity>(),
            Arg.Any<IReadOnlyList<PublicKeyCredentialParameters>>(),
            Arg.Any<MakeCredentialOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var options = call.Arg<MakeCredentialOptions>();
                sent.OptionsPerCall.Add(options);
                sent.ParamPerCall.Add(SnapshotParam(options.PinUvAuthParam));
                return MockFido2Responses.CreateMockMakeCredentialResponse();
            });

        return sent;
    }

    private static byte[] SnapshotParam(ReadOnlyMemory<byte>? pinUvAuthParam)
    {
        Assert.NotNull(pinUvAuthParam);
        return pinUvAuthParam.Value.ToArray();
    }

    private static BackendGetAssertionRequest CreateGetAssertionRequest(byte[] pinUvAuthParam) => new()
    {
        ClientDataHash = new byte[32],
        RpId = "example.com",
        AllowList = [new PublicKeyCredentialDescriptor(RandomNumberGenerator.GetBytes(32))],
        Options = new Dictionary<string, bool> { ["up"] = false },
        PinUvAuthParam = pinUvAuthParam,
        PinUvAuthProtocol = 2
    };

    private static BackendMakeCredentialRequest CreateMakeCredentialRequest(byte[] pinUvAuthParam) => new()
    {
        ClientDataHash = new byte[32],
        Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
        User = new PublicKeyCredentialUserEntity(RandomNumberGenerator.GetBytes(16), "user@example.com", "User"),
        PubKeyCredParams = [PublicKeyCredentialParameters.CreateES256()],
        PinUvAuthParam = pinUvAuthParam,
        PinUvAuthProtocol = 2
    };
}
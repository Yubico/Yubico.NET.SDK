# WebAuthn expansion and separate eligibility-replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; read complete original units plus relevant enclosing context.

## `core-001:unit:6cf5cdf2f8dbbd2b`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs:28–37`; original rank 12.

````csharp


    private static PublicKeyCredentialCreationOptions Registration() => new()
    {
        Challenge = new byte[32],
        Rp = new PublicKeyCredentialRpEntity("example.com", "Example"),
        User = new PublicKeyCredentialUserEntity(new byte[] { 1 }, "user", "User"),
        PubKeyCredParams = [CoseAlgorithm.Es256],
        UserVerification = UserVerificationPreference.Discouraged
    };
````

## `core-001:unit:49031be8c8952c54`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/CredentialPromptRetryTests.cs:85–94`; original rank 13.

````csharp


        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(
            CredentialPromptContext context, CancellationToken cancellationToken)
        {
            Contexts.Add(context);
            var answer = _answers.Count > 0 ? _answers.Dequeue() : null;
            return ValueTask.FromResult<IMemoryOwner<byte>?>(
                answer is null ? null : DisposableArrayPoolBuffer.CreateFromSpan(answer));
        }
````

## `core-001:unit:1d9844353d945bf9`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientMakeCredentialTests.cs:610–627`; original rank 14.

````csharp


    [Fact]
    public async Task MakeCredential_BackendDisposed_OnClientDisposeAsync()
    {
        // Arrange
        var mockBackend = Substitute.For<IWebAuthnBackend>();
        var client = new WebAuthnClient(
            mockBackend,
            _origin,
            new WebAuthnClientOptions { RpIdValidator = (_, rpId) => rpId == "example.com" });

        // Act
        await client.DisposeAsync();

        // Assert
        await mockBackend.Received(1).DisposeAsync();
    }
````

## `core-001:unit:b8a66cb98708847b`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/CredentialPromptRetryTests.cs:189–210`; original rank 15.

````csharp


    [Fact(Timeout = 10000)]
    public async Task WrongPinThenCorrect_RePromptsWithRetryContext_ThenSucceeds()
    {
        var prompt = new ScriptedPrompt(WrongPin, CorrectPin);
        await using var client = new WebAuthnClient(
            CreateBackend(), Origin(), new WebAuthnClientOptions { RpIdValidator = (_, rpId) => rpId == "example.com", CredentialPrompt = prompt });

        var result = await client.MakeCredentialAsync(
            CreateOptions(), pinBytes: null, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(2, prompt.Contexts.Count);

        Assert.False(prompt.Contexts[0].IsRetry);
        Assert.Equal(CredentialKind.Pin, prompt.Contexts[0].Kind);
        Assert.Equal("example.com", prompt.Contexts[0].Scope);

        Assert.True(prompt.Contexts[1].IsRetry);
        Assert.Equal(7, prompt.Contexts[1].RetriesRemaining);
    }
````

## `core-001:unit:4c275c7d070e03a5`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs:541–568`; original rank 16.

````csharp


    /// <summary>The timer covers user interaction too: a PIN prompt still waiting at the deadline is cancelled.</summary>
    [Fact(Timeout = 5000)]
    [Trait("Audit", "YESDK-1617")]
    public async Task YESDK1617_TimeoutCancelsPendingPinPrompt()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockAuthenticatorInfo(clientPinSupported: true));
        backend.GetPinRetriesAsync(Arg.Any<CancellationToken>()).Returns(8);
        var prompt = new HangingPrompt();
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        await using var client = new WebAuthnClient(backend, Assert.IsType<WebAuthnOrigin>(origin),
            new WebAuthnClientOptions { RpIdValidator = (_, rpId) => rpId == "example.com", CredentialPrompt = prompt });
        using var guard = new CancellationTokenSource(TestGuard);

        var operation = client.MakeCredentialAsync(
            Registration() with { Timeout = DeadlineThatExpires, UserVerification = UserVerificationPreference.Required },
            cancellationToken: guard.Token);
        await prompt.Requested.Task;

        var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => operation);
        Assert.Equal(WebAuthnClientErrorCode.NotAllowed, error.Code);
        Assert.True(prompt.ObservedToken.IsCancellationRequested, "the prompt must see the deadline on its own token");
        Assert.False(guard.IsCancellationRequested, "the ceremony deadline, not the test guard, must cancel");
        await backend.DidNotReceive().MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>());
    }
````

## `core-001:unit:ba9bbbedd0ad0f34`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/RpAuthorizationTests.cs:91–250`; original rank 17.

````csharp


    [Theory]
    [InlineData("bad rp id")]
    [InlineData("example.com/invalid")]
    [InlineData("example..com")]
    [InlineData("example.com\0")]
    [InlineData("example-.com")]
    [InlineData("example_foo.com")]
    [InlineData("ab--cd.com")]
    [InlineData("example.123")]
    [InlineData("example.0x")]
    [InlineData("example.0xdead")]
    [InlineData("x:bad%GG")]
    [InlineData("x:bad%")]
    [InlineData("x:bad value")]
    [InlineData("x:\\credential")]
    [InlineData("x://[bad]/credential")]
    [InlineData("x:credential[bad]")]
    [InlineData("x://partner.test:invalid/credential")]
    public async Task Ceremonies_ApplicationRejectsRpValidity_BlocksBothBeforeBackend(string rpId)
    {
        var backend = CreateBackend();
        int calls = 0;
        await using var client = new WebAuthnClient(backend, Origin(), Options((_, _) =>
        {
            calls++;
            return false;
        }));
        backend.ClearReceivedCalls();

        var registrationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(
            Registration(rpId), cancellationToken: TestContext.Current.CancellationToken));
        var authenticationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(
            Authentication(rpId), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.Security, registrationError.Code);
        Assert.Equal(WebAuthnClientErrorCode.Security, authenticationError.Code);
        Assert.Equal(2, calls);
        Assert.Empty(backend.ReceivedCalls());
    }

    [Fact]
    public async Task Ceremonies_CallbackCancellation_PropagatesAndBlocksAllBackendCalls()
    {
        var backend = CreateBackend();
        await using var client = new WebAuthnClient(backend, Origin(), Options((_, _) => throw new OperationCanceledException()));
        backend.ClearReceivedCalls();

        await Assert.ThrowsAsync<OperationCanceledException>(() => client.MakeCredentialAsync(
            Registration("example.com"), cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.GetAssertionAsync(
            Authentication("example.com"), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Empty(backend.ReceivedCalls());
    }

    [Fact]
    public async Task Ceremonies_InvalidUnicodeCannotBeReplacedDuringEncoding()
    {
        // Build at runtime: runner serialization can replace unpaired UTF-16 surrogates in theory data.
        await AssertWireRejectedAsync(new string('\ud800', 1));
        await AssertWireRejectedAsync("https://partner.test/" + new string('\udc00', 1));
    }

    [Fact]
    public async Task Ceremonies_IdnText_ReachesApplicationUnchanged()
    {
        await Ceremonies_ApplicationApprovesWireText_PreserveRpIdAndOrigin(new string('例', 32) + ".com");
        await Ceremonies_ApplicationRejectsRpValidity_BlocksBothBeforeBackend("a\u05d0.com");
    }

    [Fact]
    public async Task Ceremonies_DomainLengthPolicy_BelongsToApplication()
    {
        await Ceremonies_ApplicationApprovesWireText_PreserveRpIdAndOrigin(new string('a', 64) + ".com");
        await Ceremonies_ApplicationRejectsRpValidity_BlocksBothBeforeBackend(
            string.Join('.', Enumerable.Repeat(new string('a', 63), 4)));
    }

    [Fact]
    public async Task Authentication_NullOrEmptyRpId_StopsAtWireBoundary()
    {
        var backend = CreateBackend();
        int calls = 0;
        await using var client = new WebAuthnClient(backend, Origin(), Options((_, _) =>
        {
            calls++;
            return true;
        }));
        backend.ClearReceivedCalls();
        // Intentionally bypass nullable annotations to exercise malformed caller input.
        PublicKeyCredentialRequestOptions[] requests = [Authentication(""), Authentication("example.com") with { RpId = null! }];

        foreach (var request in requests)
        {
            var error = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(
                request, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, error.Code);
        }

        Assert.Equal(0, calls);
        Assert.Empty(backend.ReceivedCalls());
    }

    private static async Task AssertWireRejectedAsync(string rpId)
    {
        var backend = CreateBackend();
        int calls = 0;
        await using var client = new WebAuthnClient(backend, Origin(), Options((_, _) =>
        {
            calls++;
            return true;
        }));
        backend.ClearReceivedCalls();

        var registrationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.MakeCredentialAsync(
            Registration(rpId), cancellationToken: TestContext.Current.CancellationToken));
        var authenticationError = await Assert.ThrowsAsync<WebAuthnClientError>(() => client.GetAssertionAsync(
            Authentication(rpId), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, registrationError.Code);
        Assert.Equal(WebAuthnClientErrorCode.InvalidRequest, authenticationError.Code);
        Assert.Equal(0, calls);
        Assert.Empty(backend.ReceivedCalls());
    }

    private static WebAuthnClientOptions Options(Func<WebAuthnOrigin, string, bool> validator)
        => new() { RpIdValidator = validator.Invoke };

    private static WebAuthnOrigin Origin()
    {
        Assert.True(WebAuthnOrigin.TryParse("https://example.com", out var origin));
        return origin;
    }

    private static PublicKeyCredentialCreationOptions Registration(string rpId) => new()
    {
        Rp = new PublicKeyCredentialRpEntity(rpId, "Application"),
        User = new PublicKeyCredentialUserEntity(new byte[16], "user", "User"),
        Challenge = new byte[32],
        PubKeyCredParams = [CoseAlgorithm.Es256]
    };

    private static PublicKeyCredentialRequestOptions Authentication(string rpId) => new()
    {
        RpId = rpId,
        Challenge = new byte[32]
    };

    private static IWebAuthnBackend CreateBackend()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        backend.GetCachedInfoAsync(Arg.Any<CancellationToken>()).Returns(MockCtap2Responses.CreateMockAuthenticatorInfo());
        backend.MakeCredentialAsync(Arg.Any<BackendMakeCredentialRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockMakeCredentialResponse());
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(MockCtap2Responses.CreateMockGetAssertionResponse());
        return backend;
    }

````

## `core-001:unit:abfcfd663b9df0f2`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs:1–37`; original rank 18.

````csharp
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
using NSubstitute.ExceptionExtensions;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

/// <summary>
///     Covers the ceremony frame shared by MakeCredential and GetAssertion: UV
///     escalation happens at most once, the exclude-list pre-flight probes with up:false and narrows the
///     exclude list, probe failures keep their wrapping, CTAP statuses keep their mapping, and extension
///     outputs come from the attempt that succeeded.
/// </summary>

````

## `core-001:unit:197270a7f7e9bffc`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/PreviewSign/PreviewSignCodecTests.cs:1–29`; original rank 19.

````csharp
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
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions.Codec;
using Yubico.YubiKit.WebAuthn.Preferences;
using WebAuthnPreviewSign = Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.PreviewSign;

/// <summary>
/// Tests previewSign draft v4 §10.2.1 input translation and output extraction through PreviewSignCodec.
/// </summary>

````

## `core-001:unit:944095256af7ecd9`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/IYubiKeyExtensionsTransportTests.cs:160–176`; original rank 20.

````csharp


    // Applet-invalid concrete override (HID OTP) -> ArgumentException even when the device exposes it.
    [Fact]
    public async Task CreateWebAuthnClientAsync_OverrideHidOtp_AppletInvalid_ThrowsArgumentException()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido | ConnectionType.HidOtp);

        await Assert.ThrowsAsync<ArgumentException>(
            () => device.CreateWebAuthnClientAsync(
                Origin,
                ClientOptions,
                sessionOptions: new SessionCreationOptions { PreferredConnectionType = ConnectionType.HidOtp },
                cancellationToken: Ct));

        Assert.Null(device.RequestedConnection);
    }
````

## `core-001:unit:c17392c263e81b14`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs:394–401`; original rank 21.

````csharp


        public ValueTask<IMemoryOwner<byte>?> RequestSecretAsync(CredentialPromptContext context, CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            Requested.TrySetResult();
            return new ValueTask<IMemoryOwner<byte>?>(new TaskCompletionSource<IMemoryOwner<byte>?>().Task);
        }
````

## `core-001:unit:eb5669b588b07d03`
SAMPLE-WA-001-FULL-ORIGINAL-TEST-HELPER-QUOTA-EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/Yubico.YubiKit.WebAuthn.IntegrationTests.csproj:1–38`; original rank 22.

````markdown
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="Xunit.SkippableFact" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit"/>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Yubico.YubiKit.WebAuthn.csproj"/>
    <ProjectReference Include="..\..\..\Ctap2\src\Yubico.YubiKit.Ctap2.csproj"/>
    <ProjectReference Include="..\..\..\Core\src\Yubico.YubiKit.Core.csproj"/>
    <ProjectReference Include="..\..\..\Tests.Shared\Yubico.YubiKit.Tests.Shared.csproj"/>
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>

````

## `core-001:unit:889cfdef6f963950`
SPECIFIC-ADMINISTRATIVE-ELIGIBILITY-REPLACEMENT; SEPARATE FROM MATERIAL-MISS EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/IYubiKeyExtensionsTransportTests.cs:54–67`; original rank 23.

````csharp


    [Fact]
    public async Task CreateWebAuthnClientAsync_UnconfiguredRpIdValidator_ThrowsBeforeConnection()
    {
        var device = new SelectionProbeYubiKey(ConnectionType.HidFido);
        WebAuthnClientOptions clientOptions = Activator.CreateInstance<WebAuthnClientOptions>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.CreateWebAuthnClientAsync(Origin, clientOptions, cancellationToken: Ct));

        Assert.Contains("RpIdValidator must be configured", exception.Message, StringComparison.Ordinal);
        Assert.Null(device.RequestedConnection);
    }
````

## `core-001:unit:f7fc274baa684518`
SPECIFIC-ADMINISTRATIVE-ELIGIBILITY-REPLACEMENT; SEPARATE FROM MATERIAL-MISS EXPANSION; `src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Extensions/ExtensionSessionTests.cs:1–28`; original rank 24.

````csharp
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

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Processing;
using Yubico.YubiKit.WebAuthn.Preferences;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions;


````

## `core-001:unit:aeadc0996d07ac60`
SPECIFIC-ADMINISTRATIVE-ELIGIBILITY-REPLACEMENT; SEPARATE FROM MATERIAL-MISS EXPANSION; `src/WebAuthn/README.md:374–394`; original rank 5.

````markdown
## RP authorization belongs to the application

`RpIdValidator(WebAuthnOrigin origin, string rpId)` is synchronous and required. It is called once
for each registration and authentication, before any ceremony device I/O (including capability reads,
credential probes, and PIN/UV acquisition). The callback owns **RP validity, including domain/URI
syntax, and caller/RP authorization**. Return `true` only when the application considers the RP ID
valid under its chosen binding model and authorizes that exact origin/RP pair. Returning `false`
or throwing fails with `WebAuthnClientErrorCode.Security`;
exceptions are retained as `InnerException`. Cancellation exceptions thrown by the callback propagate
as cancellation, but also block the operation. The callback runs before the ceremony timeout starts:
keep it prompt, local, and thread-safe if clients share it; do not block on network lookups.

The SDK's RP wire checks reject only null/empty RP IDs and invalid UTF-16 that cannot be encoded
faithfully as UTF-8, using `InvalidRequest` without invoking the callback. **Wire-valid is not
RP-valid**: all other RP text reaches the callback unchanged, including whitespace or malformed
domain/URI strings. The SDK does not parse domains/URIs, perform IDNA conversion, or impose any
public-suffix or caller/RP-binding rule before or after callback approval. It sends the approved RP
ID unchanged (no case folding or normalization). CTAP encodes it as a CBOR text string; authenticator
message/credential limits remain in the CTAP2 layer. Passing the callback is an application decision,
not a blanket claim of browser-WebAuthn conformance.


````


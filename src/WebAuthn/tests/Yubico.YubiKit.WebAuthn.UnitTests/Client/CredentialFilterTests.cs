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

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.UnitTests.TestSupport;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client;

public class CredentialFilterTests
{
    private static PinUvAuthTokenSession CreateToken() =>
        new(new TestPinUvAuthProtocol(), new byte[32]);

    [Fact]
    public async Task SelectAsync_EmptyExcludeList_ReturnsNullWithoutCallingBackend()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8 };

        var emptyList = new List<PublicKeyCredentialDescriptor>();

        // Act
        var result = await CredentialFilter.SelectAsync(
            backend,
            "example.com",
            emptyList,
            info,
            CreateToken(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        // Verify backend was never called
        await backend.DidNotReceive().GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectAsync_SingleCredentialMatches_ReturnsThatDescriptor()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8 };

        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var singleCredList = new List<PublicKeyCredentialDescriptor>
        {
            new(credentialId)
        };

        // Mock backend to return success (actual response content doesn't matter
        // because chunk.Count == 1 short-circuits to return chunk[0])
        var mockResponse = MockFido2Responses.CreateMockGetAssertionResponse(credentialId);
        backend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(mockResponse);

        // Act
        var result = await CredentialFilter.SelectAsync(
            backend,
            "example.com",
            singleCredList,
            info,
            CreateToken(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(credentialId, result.Id.ToArray());
    }

    [Fact]
    public async Task SelectAsync_NoCredentialMatches_ReturnsNull()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8 };

        var excludeList = new List<PublicKeyCredentialDescriptor>
        {
            new(new byte[] { 0x01, 0x02, 0x03 }),
            new(new byte[] { 0x04, 0x05, 0x06 }),
            new(new byte[] { 0x07, 0x08, 0x09 })
        };

        // Mock backend to throw NoCredentials (no match in this chunk)
        backend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.NoCredentials));

        // Act
        var result = await CredentialFilter.SelectAsync(
            backend,
            "example.com",
            excludeList,
            info,
            CreateToken(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SelectAsync_LargerThanMaxChunkSize_ChunksAndIteratesUntilMatch()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 5 };

        // Create 12 credentials (will chunk as 5+5+2)
        var excludeList = Enumerable.Range(0, 12)
            .Select(i => new PublicKeyCredentialDescriptor(new byte[] { (byte)i }))
            .ToList();

        // Descriptor #5 (index 5, first item in second chunk) will match
        var matchingCredentialId = new byte[] { 0x05 };
        var matchingResponse = MockFido2Responses.CreateMockGetAssertionResponse(matchingCredentialId);

        var callCount = 0;
        backend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // First chunk (credentials 0-4): no match
                    throw new CtapException(CtapStatus.NoCredentials);
                }
                // Second chunk (credentials 5-9): match on credential #5
                return matchingResponse;
            });

        // Act
        var result = await CredentialFilter.SelectAsync(
            backend,
            "example.com",
            excludeList,
            info,
            CreateToken(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(matchingCredentialId, result.Id.ToArray());
        Assert.Equal(2, callCount);
    }

    /// <summary>A ceremony without a token (UV discouraged, no PIN) still probes; python-fido2 sends no pinUvAuthParam then.</summary>
    [Fact]
    public async Task SelectAsync_WithoutToken_SendsNoPinUvAuthParam()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8 };
        BackendGetAssertionRequest? sent = null;
        backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockGetAssertionResponse());

        var result = await CredentialFilter.SelectAsync(
            backend, "example.com", [new PublicKeyCredentialDescriptor(new byte[] { 0x01 })], info, token: null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(sent);
        Assert.Null(sent.PinUvAuthParam);
        Assert.Null(sent.PinUvAuthProtocol);
        Assert.False(sent.Options!["up"]);
    }

    /// <summary>CTAP2_ERR_REQUEST_TOO_LARGE shrinks the chunk at the same offset (python-fido2 _filter_creds).</summary>
    [Fact]
    public async Task SelectAsync_RequestTooLarge_RetriesWithSmallerChunksAtSameOffset()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 3 };
        var candidates = Enumerable.Range(1, 3).Select(i => new PublicKeyCredentialDescriptor(new byte[] { (byte)i })).ToList();
        var chunkSizes = new List<int>();
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var allowList = call.Arg<BackendGetAssertionRequest>().AllowList!;
                chunkSizes.Add(allowList.Count);
                if (allowList.Count > 1)
                {
                    throw new CtapException(CtapStatus.RequestTooLarge);
                }

                return allowList[0].Id.Span[0] == 3
                    ? MockFido2Responses.CreateMockGetAssertionResponse()
                    : throw new CtapException(CtapStatus.NoCredentials);
            });

        var result = await CredentialFilter.SelectAsync(
            backend, "example.com", candidates, info, CreateToken(), TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 3 }, result!.Id.ToArray());
        Assert.Equal([3, 2, 1, 1, 1], chunkSizes); // shrink 3→2→1 at offset 0, then one per credential
    }

    /// <summary>A single descriptor that is still too large is a real error, not something to shrink past.</summary>
    [Fact]
    public async Task SelectAsync_RequestTooLargeWithSingleDescriptor_Propagates()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 1 };
        backend.GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.RequestTooLarge));

        var error = await Assert.ThrowsAsync<CtapException>(() => CredentialFilter.SelectAsync(
            backend, "example.com", [new PublicKeyCredentialDescriptor(new byte[] { 0x01 })], info, CreateToken(),
            TestContext.Current.CancellationToken));

        Assert.Equal(CtapStatus.RequestTooLarge, error.Status);
    }

    /// <summary>CTAP §6.4 maxCredentialIdLength: ids the authenticator cannot have issued are never sent.</summary>
    [Fact]
    public async Task SelectAsync_IdsLongerThanMaxCredentialIdLength_AreSkipped()
    {
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8, MaxCredentialIdLength = 4 };
        var tooLong = new PublicKeyCredentialDescriptor(new byte[8]);
        var fits = new PublicKeyCredentialDescriptor(new byte[] { 0xAA, 0xBB });
        BackendGetAssertionRequest? sent = null;
        backend.GetAssertionAsync(Arg.Do<BackendGetAssertionRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(MockFido2Responses.CreateMockGetAssertionResponse());

        var result = await CredentialFilter.SelectAsync(
            backend, "example.com", [tooLong, fits], info, CreateToken(), TestContext.Current.CancellationToken);

        Assert.Same(fits, result);
        Assert.Equal([fits], sent!.AllowList);

        backend.ClearReceivedCalls();
        var none = await CredentialFilter.SelectAsync(
            backend, "example.com", [tooLong], info, CreateToken(), TestContext.Current.CancellationToken);
        Assert.Null(none);
        await backend.DidNotReceive().GetAssertionAsync(Arg.Any<BackendGetAssertionRequest>(), Arg.Any<CancellationToken>());
    }
}
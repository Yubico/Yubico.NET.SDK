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

using System.Formats.Cbor;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Internal;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Internal;

public class ExcludeListPreflightTests
{
    // Test-only protocol implementation (NSubstitute cannot mock methods with Span parameters)
    private static IPinUvAuthProtocol CreateMockProtocol()
    {
        return new TestPinUvAuthProtocol();
    }

    private sealed class TestPinUvAuthProtocol : IPinUvAuthProtocol
    {
        public int Version => 2;
        public int AuthenticationTagLength => 16;

        public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
        {
            return new byte[16]; // Return predictable 16-byte auth param
        }

        public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
            IReadOnlyDictionary<int, object?> peerCoseKey)
        {
            throw new NotImplementedException();
        }

        public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
        {
            throw new NotImplementedException();
        }

        public byte[] Kdf(ReadOnlySpan<byte> z)
        {
            throw new NotImplementedException();
        }

        public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task FindFirstMatchAsync_EmptyExcludeList_ReturnsNullWithoutCallingBackend()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8 };
        var protocol = CreateMockProtocol();

        var emptyList = new List<PublicKeyCredentialDescriptor>();
        var pinUvAuthToken = new byte[32];

        // Act
        var result = await ExcludeListPreflight.FindFirstMatchAsync(
            backend,
            "example.com",
            emptyList,
            info,
            pinUvAuthToken,
            protocol,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        // Verify backend was never called
        await backend.DidNotReceive().GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindFirstMatchAsync_SingleCredentialMatches_ReturnsThatDescriptor()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8 };
        var protocol = CreateMockProtocol();

        var credentialId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var singleCredList = new List<PublicKeyCredentialDescriptor>
        {
            new(credentialId)
        };
        var pinUvAuthToken = new byte[32];

        // Mock backend to return success (actual response content doesn't matter
        // because chunk.Count == 1 short-circuits to return chunk[0])
        var mockResponse = BuildMockGetAssertionResponse(credentialId);
        backend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Returns(mockResponse);

        // Act
        var result = await ExcludeListPreflight.FindFirstMatchAsync(
            backend,
            "example.com",
            singleCredList,
            info,
            pinUvAuthToken,
            protocol,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(credentialId, result.Id.ToArray());
    }

    [Fact]
    public async Task FindFirstMatchAsync_NoCredentialMatches_ReturnsNull()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 8 };
        var protocol = CreateMockProtocol();

        var excludeList = new List<PublicKeyCredentialDescriptor>
        {
            new(new byte[] { 0x01, 0x02, 0x03 }),
            new(new byte[] { 0x04, 0x05, 0x06 }),
            new(new byte[] { 0x07, 0x08, 0x09 })
        };
        var pinUvAuthToken = new byte[32];

        // Mock backend to throw NoCredentials (no match in this chunk)
        backend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
            Arg.Any<CancellationToken>())
            .Throws(new CtapException(CtapStatus.NoCredentials));

        // Act
        var result = await ExcludeListPreflight.FindFirstMatchAsync(
            backend,
            "example.com",
            excludeList,
            info,
            pinUvAuthToken,
            protocol,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindFirstMatchAsync_LargerThanMaxChunkSize_ChunksAndIteratesUntilMatch()
    {
        // Arrange
        var backend = Substitute.For<IWebAuthnBackend>();
        var info = new AuthenticatorInfo { MaxCredentialCountInList = 5 };
        var protocol = CreateMockProtocol();

        // Create 12 credentials (will chunk as 5+5+2)
        var excludeList = Enumerable.Range(0, 12)
            .Select(i => new PublicKeyCredentialDescriptor(new byte[] { (byte)i }))
            .ToList();
        var pinUvAuthToken = new byte[32];

        // Descriptor #5 (index 5, first item in second chunk) will match
        var matchingCredentialId = new byte[] { 0x05 };
        var matchingResponse = BuildMockGetAssertionResponse(matchingCredentialId);

        var callCount = 0;
        backend.GetAssertionAsync(
            Arg.Any<BackendGetAssertionRequest>(),
            Arg.Any<IProgress<CtapStatus>?>(),
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
        var result = await ExcludeListPreflight.FindFirstMatchAsync(
            backend,
            "example.com",
            excludeList,
            info,
            pinUvAuthToken,
            protocol,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(matchingCredentialId, result.Id.ToArray());
        Assert.Equal(2, callCount);
    }

    // Helper to build a minimal GetAssertionResponse mock
    private static GetAssertionResponse BuildMockGetAssertionResponse(byte[] credentialId)
    {
        // Build minimal CBOR response for GetAssertion
        // Key 1 = credential { id, type }
        // Key 2 = authData (minimal 37 bytes)
        // Key 3 = signature
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);

        // Key 1: credential
        writer.WriteInt32(1);
        writer.WriteStartMap(2);
        writer.WriteTextString("id");
        writer.WriteByteString(credentialId);
        writer.WriteTextString("type");
        writer.WriteTextString("public-key");
        writer.WriteEndMap();

        // Key 2: authData (37 bytes minimum)
        writer.WriteInt32(2);
        var authData = new byte[37];
        writer.WriteByteString(authData);

        // Key 3: signature (dummy)
        writer.WriteInt32(3);
        writer.WriteByteString(new byte[64]);

        writer.WriteEndMap();

        var responseBytes = writer.Encode();
        return GetAssertionResponse.Decode(responseBytes);
    }
}

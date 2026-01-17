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
using NSubstitute;
using Xunit;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.UnitTests.Pin;

/// <summary>
/// Unit tests for the <see cref="ClientPin"/> class.
/// </summary>
/// <remarks>
/// These tests use NSubstitute to mock the IFidoSession.
/// Tests that require protocol mocking use a concrete FakePinUvAuthProtocol.
/// </remarks>
public sealed class ClientPinTests : IDisposable
{
    private readonly IFidoSession _mockSession;
    private readonly FakePinUvAuthProtocol _fakeProtocol;
    private bool _disposed;
    
    public ClientPinTests()
    {
        _mockSession = Substitute.For<IFidoSession>();
        _fakeProtocol = new FakePinUvAuthProtocol();
    }
    
    private ClientPin CreateClientPin() => new(_mockSession, _fakeProtocol);
    
    [Fact]
    public void Constructor_WithNullCommands_ThrowsArgumentNullException()
    {
        var protocol = Substitute.For<IPinUvAuthProtocol>();
        Assert.Throws<ArgumentNullException>(() => new ClientPin((IFidoSession)null!, protocol));
    }
    
    [Fact]
    public void Constructor_WithNullProtocol_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ClientPin(_mockSession, null!));
    }
    
    [Fact]
    public void Protocol_ReturnsProvidedProtocol()
    {
        using var clientPin = CreateClientPin();
        
        Assert.Same(_fakeProtocol, clientPin.Protocol);
    }
    
    [Fact]
    public async Task GetPinRetriesAsync_ReturnsCorrectRetries()
    {
        using var clientPin = CreateClientPin();
        
        // Setup mock response: pinRetries = 5, powerCycleState = false
        var responseData = CreatePinRetriesResponse(5, false);
        SetupMockResponse(responseData);
        
        var (retries, powerCycleRequired) = await clientPin.GetPinRetriesAsync();
        
        Assert.Equal(5, retries);
        Assert.False(powerCycleRequired);
    }
    
    [Fact]
    public async Task GetPinRetriesAsync_WithPowerCycleRequired_ReturnsTrue()
    {
        using var clientPin = CreateClientPin();
        
        var responseData = CreatePinRetriesResponse(0, true);
        SetupMockResponse(responseData);
        
        var (retries, powerCycleRequired) = await clientPin.GetPinRetriesAsync();
        
        Assert.Equal(0, retries);
        Assert.True(powerCycleRequired);
    }
    
    [Fact]
    public async Task GetUvRetriesAsync_ReturnsCorrectRetries()
    {
        using var clientPin = CreateClientPin();
        
        var responseData = CreateUvRetriesResponse(3, false);
        SetupMockResponse(responseData);
        
        var (retries, powerCycleRequired) = await clientPin.GetUvRetriesAsync();
        
        Assert.Equal(3, retries);
        Assert.False(powerCycleRequired);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    public async Task SetPinAsync_WithShortPin_ThrowsArgumentException(string shortPin)
    {
        using var clientPin = CreateClientPin();
        
        await Assert.ThrowsAsync<ArgumentException>(() => clientPin.SetPinAsync(shortPin));
    }
    
    [Fact]
    public async Task SetPinAsync_WithNullPin_ThrowsArgumentNullException()
    {
        using var clientPin = CreateClientPin();
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => clientPin.SetPinAsync(null!));
    }
    
    [Fact]
    public async Task SetPinAsync_WithTooLongPin_ThrowsArgumentException()
    {
        using var clientPin = CreateClientPin();
        
        var longPin = new string('a', 64);
        await Assert.ThrowsAsync<ArgumentException>(() => clientPin.SetPinAsync(longPin));
    }
    
    [Fact]
    public async Task SetPinAsync_WithValidPin_SendsCommand()
    {
        using var clientPin = CreateClientPin();
        
        // Setup mock responses
        var keyAgreementResponse = CreateKeyAgreementResponse(CreateMockCoseKey());
        var emptyResponse = CreateEmptyResponse();
        
        _mockSession.SendCborRequestAsync(default, default)
            .ReturnsForAnyArgs(keyAgreementResponse, emptyResponse);
        
        await clientPin.SetPinAsync("1234");
        
        // Verify two commands were sent (GetKeyAgreement and SetPin)
        await _mockSession.ReceivedWithAnyArgs(2).SendCborRequestAsync(default, default);
    }
    
    [Fact]
    public async Task ChangePinAsync_WithValidPins_SendsCommand()
    {
        using var clientPin = CreateClientPin();
        
        // Setup mock responses
        var keyAgreementResponse = CreateKeyAgreementResponse(CreateMockCoseKey());
        var emptyResponse = CreateEmptyResponse();
        
        _mockSession.SendCborRequestAsync(default, default)
            .ReturnsForAnyArgs(keyAgreementResponse, emptyResponse);
        
        await clientPin.ChangePinAsync("oldpin1234", "newpin5678");
        
        // Verify two commands were sent (GetKeyAgreement and ChangePin)
        await _mockSession.ReceivedWithAnyArgs(2).SendCborRequestAsync(default, default);
    }
    
    [Theory]
    [InlineData("ab", "1234")]
    [InlineData("1234", "ab")]
    [InlineData("ab", "cd")]
    public async Task ChangePinAsync_WithInvalidPins_ThrowsArgumentException(string currentPin, string newPin)
    {
        using var clientPin = CreateClientPin();
        
        await Assert.ThrowsAsync<ArgumentException>(() => clientPin.ChangePinAsync(currentPin, newPin));
    }
    
    [Fact]
    public async Task GetPinTokenAsync_ReturnsDecryptedToken()
    {
        using var clientPin = CreateClientPin();
        
        // Setup mock responses
        var keyAgreementResponse = CreateKeyAgreementResponse(CreateMockCoseKey());
        var encryptedToken = new byte[32];
        var tokenResponse = CreatePinTokenResponse(encryptedToken);
        
        _mockSession.SendCborRequestAsync(default, default)
            .ReturnsForAnyArgs(keyAgreementResponse, tokenResponse);
        
        var token = await clientPin.GetPinTokenAsync("1234");
        
        // Token is decrypted by the fake protocol
        Assert.NotNull(token);
        Assert.Equal(encryptedToken.Length, token.Length);
    }
    
    [Fact]
    public async Task GetPinUvAuthTokenUsingPinAsync_WithNoPermissions_ThrowsArgumentException()
    {
        using var clientPin = CreateClientPin();
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => clientPin.GetPinUvAuthTokenUsingPinAsync("1234", PinUvAuthTokenPermissions.None));
    }
    
    [Fact]
    public async Task GetPinUvAuthTokenUsingPinAsync_WithPermissions_ReturnsToken()
    {
        using var clientPin = CreateClientPin();
        
        // Setup mock responses
        var keyAgreementResponse = CreateKeyAgreementResponse(CreateMockCoseKey());
        var tokenResponse = CreatePinTokenResponse(new byte[32]);
        
        _mockSession.SendCborRequestAsync(default, default)
            .ReturnsForAnyArgs(keyAgreementResponse, tokenResponse);
        
        var token = await clientPin.GetPinUvAuthTokenUsingPinAsync(
            "1234",
            PinUvAuthTokenPermissions.MakeCredential | PinUvAuthTokenPermissions.GetAssertion,
            "example.com");
        
        Assert.NotNull(token);
    }
    
    [Fact]
    public async Task GetPinUvAuthTokenUsingUvAsync_WithNoPermissions_ThrowsArgumentException()
    {
        using var clientPin = CreateClientPin();
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => clientPin.GetPinUvAuthTokenUsingUvAsync(PinUvAuthTokenPermissions.None));
    }
    
    [Fact]
    public async Task GetPinUvAuthTokenUsingUvAsync_WithPermissions_ReturnsToken()
    {
        using var clientPin = CreateClientPin();
        
        // Setup mock responses
        var keyAgreementResponse = CreateKeyAgreementResponse(CreateMockCoseKey());
        var tokenResponse = CreatePinTokenResponse(new byte[32]);
        
        _mockSession.SendCborRequestAsync(default, default)
            .ReturnsForAnyArgs(keyAgreementResponse, tokenResponse);
        
        var token = await clientPin.GetPinUvAuthTokenUsingUvAsync(
            PinUvAuthTokenPermissions.BioEnrollment,
            null);
        
        Assert.NotNull(token);
    }
    
    [Fact]
    public void Dispose_DisposesProtocol()
    {
        var clientPin = CreateClientPin();
        
        clientPin.Dispose();
        
        Assert.True(_fakeProtocol.IsDisposed);
    }
    
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var clientPin = CreateClientPin();
        
        clientPin.Dispose();
        var disposeCount = _fakeProtocol.DisposeCount;
        
        clientPin.Dispose();
        
        Assert.Equal(disposeCount, _fakeProtocol.DisposeCount);
    }
    
    [Fact]
    public async Task GetPinRetriesAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var clientPin = CreateClientPin();
        
        clientPin.Dispose();
        
        await Assert.ThrowsAsync<ObjectDisposedException>(() => clientPin.GetPinRetriesAsync());
    }
    
    [Fact]
    public async Task GetPinRetriesAsync_SendsCorrectProtocolVersion()
    {
        using var clientPin = CreateClientPin();
        
        var responseData = CreatePinRetriesResponse(8, false);
        SetupMockResponse(responseData);
        
        await clientPin.GetPinRetriesAsync();
        
        // Verify the request was sent
        await _mockSession.ReceivedWithAnyArgs(1).SendCborRequestAsync(default, default);
    }
    
    // Helper methods
    
    private void SetupMockResponse(ReadOnlyMemory<byte> response)
    {
        _mockSession.SendCborRequestAsync(default, default)
            .ReturnsForAnyArgs(response);
    }
    
    private static ReadOnlyMemory<byte> CreatePinRetriesResponse(int retries, bool powerCycleRequired)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(powerCycleRequired ? 2 : 1);
        
        writer.WriteInt32(0x03); // pinRetries
        writer.WriteInt32(retries);
        
        if (powerCycleRequired)
        {
            writer.WriteInt32(0x04); // powerCycleState
            writer.WriteBoolean(true);
        }
        
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static ReadOnlyMemory<byte> CreateUvRetriesResponse(int retries, bool powerCycleRequired)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(powerCycleRequired ? 2 : 1);
        
        writer.WriteInt32(0x05); // uvRetries
        writer.WriteInt32(retries);
        
        if (powerCycleRequired)
        {
            writer.WriteInt32(0x04); // powerCycleState
            writer.WriteBoolean(true);
        }
        
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static ReadOnlyMemory<byte> CreateKeyAgreementResponse(Dictionary<int, object?> coseKey)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        
        writer.WriteInt32(0x01); // keyAgreement
        WriteCoseKeyToWriter(writer, coseKey);
        
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static ReadOnlyMemory<byte> CreatePinTokenResponse(byte[] encryptedToken)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        
        writer.WriteInt32(0x02); // pinUvAuthToken
        writer.WriteByteString(encryptedToken);
        
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static ReadOnlyMemory<byte> CreateEmptyResponse()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static Dictionary<int, object?> CreateMockCoseKey()
    {
        return new Dictionary<int, object?>
        {
            [1] = 2,  // kty: EC2
            [3] = -25, // alg: ECDH-ES+HKDF-256
            [-1] = 1,  // crv: P-256
            [-2] = new byte[32], // x
            [-3] = new byte[32], // y
        };
    }
    
    private static void WriteCoseKeyToWriter(CborWriter writer, Dictionary<int, object?> key)
    {
        var sortedKeys = key.Keys.OrderBy(k => k).ToList();
        
        writer.WriteStartMap(key.Count);
        
        foreach (var k in sortedKeys)
        {
            writer.WriteInt32(k);
            
            switch (key[k])
            {
                case int intVal:
                    writer.WriteInt32(intVal);
                    break;
                case byte[] bytes:
                    writer.WriteByteString(bytes);
                    break;
                case null:
                    writer.WriteNull();
                    break;
            }
        }
        
        writer.WriteEndMap();
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _fakeProtocol.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Fake implementation of IPinUvAuthProtocol for unit testing.
/// </summary>
internal sealed class FakePinUvAuthProtocol : IPinUvAuthProtocol
{
    public int Version => 2;
    public int AuthenticationTagLength => 32;
    public bool IsDisposed { get; private set; }
    public int DisposeCount { get; private set; }
    
    public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IReadOnlyDictionary<int, object?> peerCoseKey)
    {
        var keyAgreement = new Dictionary<int, object?>
        {
            [1] = 2,
            [3] = -25,
            [-1] = 1,
            [-2] = new byte[32],
            [-3] = new byte[32],
        };
        
        return (keyAgreement, new byte[64]);
    }
    
    public byte[] Kdf(ReadOnlySpan<byte> z) => new byte[64];
    
    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
        => plaintext.ToArray();
    
    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
        => ciphertext.ToArray();
    
    public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
        => new byte[32];
    
    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        => true;
    
    public void Dispose()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;
            DisposeCount++;
        }
    }
}

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
using Yubico.YubiKit.Fido2.Config;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.UnitTests.Config;

/// <summary>
/// Unit tests for <see cref="AuthenticatorConfig"/>.
/// </summary>
public class AuthenticatorConfigTests
{
    private readonly IFidoSession _mockSession;
    private readonly TestPinUvAuthProtocol _testProtocol;
    private readonly byte[] _pinUvAuthToken;
    private readonly AuthenticatorConfig _config;
    
    public AuthenticatorConfigTests()
    {
        _mockSession = Substitute.For<IFidoSession>();
        _testProtocol = new TestPinUvAuthProtocol();
        _pinUvAuthToken = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        
        _config = new AuthenticatorConfig(_mockSession, _testProtocol, _pinUvAuthToken);
    }
    
    /// <summary>
    /// Test implementation of PIN/UV auth protocol that captures authenticate calls.
    /// </summary>
    private sealed class TestPinUvAuthProtocol : IPinUvAuthProtocol
    {
        public int Version => 2;
        public int AuthenticationTagLength => 16;
        public byte[]? LastAuthenticateMessage { get; private set; }
        
        public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
        {
            LastAuthenticateMessage = message.ToArray();
            return new byte[16]; // Return 16-byte auth param
        }
        
        public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext)
            => new byte[plaintext.Length];
        
        public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext)
            => new byte[ciphertext.Length];
        
        public (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
            IReadOnlyDictionary<int, object?> peerCoseKey)
            => (new Dictionary<int, object?>(), new byte[32]);
        
        public byte[] Kdf(ReadOnlySpan<byte> z) => new byte[32];
        
        public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
            => true;
        
        public void Dispose() { }
    }
    
    [Fact]
    public void Constructor_ThrowsOnNullSession()
    {
        Assert.Throws<ArgumentNullException>(
            () => new AuthenticatorConfig(null!, _testProtocol, _pinUvAuthToken));
    }
    
    [Fact]
    public void Constructor_ThrowsOnNullProtocol()
    {
        Assert.Throws<ArgumentNullException>(
            () => new AuthenticatorConfig(_mockSession, null!, _pinUvAuthToken));
    }
    
    [Fact]
    public async Task EnableEnterpriseAttestationAsync_SendsCorrectCommand()
    {
        // Arrange
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.EnableEnterpriseAttestationAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        var mapCount = reader.ReadStartMap();
        Assert.Equal(3, mapCount);
        
        // Verify subCommand is 0x01 (EnableEnterpriseAttestation)
        Assert.Equal(1, reader.ReadInt32()); // key
        Assert.Equal(0x01, reader.ReadInt32()); // value (subCommand)
    }
    
    [Fact]
    public async Task ToggleAlwaysUvAsync_SendsCorrectCommand()
    {
        // Arrange
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.ToggleAlwaysUvAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        reader.ReadStartMap();
        
        // Verify subCommand is 0x02 (ToggleAlwaysUv)
        Assert.Equal(1, reader.ReadInt32()); // key
        Assert.Equal(0x02, reader.ReadInt32()); // value (subCommand)
    }
    
    [Fact]
    public async Task SetMinPinLengthAsync_SendsCorrectCommand()
    {
        // Arrange
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.SetMinPinLengthAsync(8, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        var mapCount = reader.ReadStartMap();
        Assert.Equal(4, mapCount); // subCommand, params, protocol, authParam
        
        // Verify subCommand is 0x03 (SetMinPinLength)
        Assert.Equal(1, reader.ReadInt32()); // key 0x01
        Assert.Equal(0x03, reader.ReadInt32()); // value (subCommand)
        
        // Verify subCommandParams
        Assert.Equal(2, reader.ReadInt32()); // key 0x02
        var paramsCount = reader.ReadStartMap();
        Assert.True(paramsCount >= 1);
        
        // newMinPINLength
        Assert.Equal(1, reader.ReadInt32()); // key
        Assert.Equal(8, reader.ReadInt32()); // value
    }
    
    [Fact]
    public async Task SetMinPinLengthAsync_WithRpIds_IncludesRpIdsInParams()
    {
        // Arrange
        var rpIds = new[] { "example.com", "test.com" };
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.SetMinPinLengthAsync(6, rpIds, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        reader.ReadStartMap();
        
        // Skip to params
        reader.ReadInt32(); // key 0x01
        reader.ReadInt32(); // subCommand
        reader.ReadInt32(); // key 0x02
        
        var paramsCount = reader.ReadStartMap();
        Assert.True(paramsCount >= 2); // newMinPINLength + minPinLengthRPIDs
    }
    
    [Fact]
    public async Task SetMinPinLengthAsync_WithForceChangePin_IncludesForceChangePinInParams()
    {
        // Arrange
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.SetMinPinLengthAsync(6, forceChangePin: true, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        reader.ReadStartMap();
        
        // Skip to params
        reader.ReadInt32(); // key 0x01
        reader.ReadInt32(); // subCommand
        reader.ReadInt32(); // key 0x02
        
        var paramsCount = reader.ReadStartMap();
        Assert.Equal(2, paramsCount); // newMinPINLength + forceChangePin
    }
    
    [Fact]
    public async Task SetMinPinLengthAsync_ThrowsOnTooShort()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _config.SetMinPinLengthAsync(3, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task SetMinPinLengthAsync_ThrowsOnTooLong()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _config.SetMinPinLengthAsync(64, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task SetMinPinLengthAsync_AcceptsMinimumValue()
    {
        // Arrange
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act - should not throw
        await _config.SetMinPinLengthAsync(4, cancellationToken: TestContext.Current.CancellationToken);
    }
    
    [Fact]
    public async Task SetMinPinLengthAsync_AcceptsMaximumValue()
    {
        // Arrange
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act - should not throw
        await _config.SetMinPinLengthAsync(63, cancellationToken: TestContext.Current.CancellationToken);
    }
    
    [Fact]
    public async Task EnableEnterpriseAttestationAsync_UsesCorrectCommandByte()
    {
        // Arrange
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.EnableEnterpriseAttestationAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(0x0D, capturedRequest![0]); // Config command
    }
    
    [Fact]
    public async Task EnableEnterpriseAttestationAsync_UsesCorrectProtocolVersion()
    {
        // Arrange
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.EnableEnterpriseAttestationAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        reader.ReadStartMap();
        
        reader.ReadInt32(); // key 0x01
        reader.ReadInt32(); // subCommand
        
        // pinUvAuthProtocol
        Assert.Equal(3, reader.ReadInt32()); // key 0x03
        Assert.Equal(2, reader.ReadInt32()); // protocol version
    }
    
    [Fact]
    public async Task EnableEnterpriseAttestationAsync_IncludesPinUvAuthParam()
    {
        // Arrange
        byte[]? capturedRequest = null;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.EnableEnterpriseAttestationAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        reader.ReadStartMap();
        
        reader.ReadInt32(); // key 0x01
        reader.ReadInt32(); // subCommand
        reader.ReadInt32(); // key 0x03
        reader.ReadInt32(); // protocol version
        
        // pinUvAuthParam
        Assert.Equal(4, reader.ReadInt32()); // key 0x04
        var authParam = reader.ReadByteString();
        Assert.Equal(16, authParam.Length); // Our test protocol returns 16-byte auth param
    }
    
    [Fact]
    public async Task AuthenticatesOverCorrectMessage_EnableEnterpriseAttestation()
    {
        // Arrange
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.EnableEnterpriseAttestationAsync(TestContext.Current.CancellationToken);
        
        // Assert - check what message was authenticated via our test protocol
        var capturedMessage = _testProtocol.LastAuthenticateMessage;
        Assert.NotNull(capturedMessage);
        Assert.Equal(2, capturedMessage.Length);
        Assert.Equal(0xff, capturedMessage[0]); // Magic prefix
        Assert.Equal(0x01, capturedMessage[1]); // EnableEnterpriseAttestation
    }
    
    [Fact]
    public async Task AuthenticatesOverCorrectMessage_ToggleAlwaysUv()
    {
        // Arrange
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        // Act
        await _config.ToggleAlwaysUvAsync(TestContext.Current.CancellationToken);
        
        // Assert - check what message was authenticated via our test protocol
        var capturedMessage = _testProtocol.LastAuthenticateMessage;
        Assert.NotNull(capturedMessage);
        Assert.Equal(2, capturedMessage.Length);
        Assert.Equal(0xff, capturedMessage[0]); // Magic prefix
        Assert.Equal(0x02, capturedMessage[1]); // ToggleAlwaysUv
    }
}

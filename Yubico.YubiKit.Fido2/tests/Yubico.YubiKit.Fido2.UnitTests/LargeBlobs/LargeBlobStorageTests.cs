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
using NSubstitute;
using Xunit;
using Yubico.YubiKit.Fido2.LargeBlobs;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.UnitTests.LargeBlobs;

/// <summary>
/// Unit tests for <see cref="LargeBlobStorage"/>.
/// </summary>
public class LargeBlobStorageTests
{
    private readonly IFidoSession _mockSession;
    private readonly TestPinUvAuthProtocol _testProtocol;
    private readonly byte[] _pinUvAuthToken;
    
    public LargeBlobStorageTests()
    {
        _mockSession = Substitute.For<IFidoSession>();
        _testProtocol = new TestPinUvAuthProtocol();
        _pinUvAuthToken = new byte[32];
        RandomNumberGenerator.Fill(_pinUvAuthToken);
    }
    
    /// <summary>
    /// Test implementation of PIN/UV auth protocol.
    /// </summary>
    private sealed class TestPinUvAuthProtocol : IPinUvAuthProtocol
    {
        public int Version => 2;
        public int AuthenticationTagLength => 16;
        public byte[]? LastAuthenticateMessage { get; private set; }
        
        public byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
        {
            LastAuthenticateMessage = message.ToArray();
            return new byte[16];
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
    public void Constructor_ReadOnly_ThrowsOnNullSession()
    {
        Assert.Throws<ArgumentNullException>(() => new LargeBlobStorage(null!));
    }
    
    [Fact]
    public void Constructor_ReadOnly_ThrowsOnInvalidFragmentLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LargeBlobStorage(_mockSession, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LargeBlobStorage(_mockSession, -1));
    }
    
    [Fact]
    public void Constructor_ReadWrite_ThrowsOnNullProtocol()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LargeBlobStorage(_mockSession, null!, _pinUvAuthToken));
    }
    
    [Fact]
    public async Task ReadLargeBlobArrayAsync_SendsCorrectCommand()
    {
        // Arrange
        byte[]? capturedRequest = null;
        
        // Create empty array response
        var emptyArray = LargeBlobArray.CreateEmpty().Serialize();
        var response = CreateReadResponse(emptyArray);
        
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));
        
        var storage = new LargeBlobStorage(_mockSession, 1024);
        
        // Act
        await storage.ReadLargeBlobArrayAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(0x0C, capturedRequest![0]); // LargeBlobs command
        
        // Verify CBOR structure - skip command byte
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        var mapCount = reader.ReadStartMap();
        Assert.Equal(2, mapCount);
        
        // Key 0x01 (get)
        Assert.Equal(1, reader.ReadInt32());
        var getLength = reader.ReadInt32();
        Assert.True(getLength > 0);
        
        // Key 0x03 (offset)
        Assert.Equal(3, reader.ReadInt32());
        Assert.Equal(0, reader.ReadInt32());
    }
    
    [Fact]
    public async Task ReadLargeBlobArrayAsync_ReturnsEmptyArrayWhenNoData()
    {
        // Arrange
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        var storage = new LargeBlobStorage(_mockSession);
        
        // Act
        var result = await storage.ReadLargeBlobArrayAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Entries);
    }
    
    [Fact]
    public async Task WriteLargeBlobArrayAsync_RequiresPinAuth()
    {
        // Arrange - create read-only storage (no PIN/UV auth)
        var storage = new LargeBlobStorage(_mockSession);
        var array = LargeBlobArray.CreateEmpty();
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.WriteLargeBlobArrayAsync(array, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task WriteLargeBlobArrayAsync_ThrowsOnNullArray()
    {
        // Arrange
        var storage = new LargeBlobStorage(_mockSession, _testProtocol, _pinUvAuthToken);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => storage.WriteLargeBlobArrayAsync(null!, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task WriteLargeBlobArrayAsync_SendsCorrectCommand()
    {
        // Arrange
        byte[]? capturedRequest = null;
        
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        var storage = new LargeBlobStorage(_mockSession, _testProtocol, _pinUvAuthToken);
        var array = LargeBlobArray.CreateEmpty();
        
        // Act
        await storage.WriteLargeBlobArrayAsync(array, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(0x0C, capturedRequest![0]); // LargeBlobs command
        
        // Verify CBOR structure has set, offset, length, pinUvAuthParam, pinUvAuthProtocol
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        var mapCount = reader.ReadStartMap();
        Assert.Equal(5, mapCount); // First fragment includes length
    }
    
    [Fact]
    public async Task GetBlobAsync_ThrowsOnInvalidKeyLength()
    {
        // Arrange
        var storage = new LargeBlobStorage(_mockSession);
        var invalidKey = new byte[16]; // Should be 32
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => storage.GetBlobAsync(invalidKey, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task GetBlobAsync_ReturnsNullWhenNoMatchingBlob()
    {
        // Arrange
        var emptyArray = LargeBlobArray.CreateEmpty().Serialize();
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateReadResponse(emptyArray)));
        
        var storage = new LargeBlobStorage(_mockSession);
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        
        // Act
        var result = await storage.GetBlobAsync(key, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task SetBlobAsync_ThrowsOnInvalidKeyLength()
    {
        // Arrange
        var storage = new LargeBlobStorage(_mockSession, _testProtocol, _pinUvAuthToken);
        var invalidKey = new byte[16]; // Should be 32
        var data = new byte[] { 0x01 };
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => storage.SetBlobAsync(invalidKey, data, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task DeleteBlobAsync_ThrowsOnInvalidKeyLength()
    {
        // Arrange
        var storage = new LargeBlobStorage(_mockSession, _testProtocol, _pinUvAuthToken);
        var invalidKey = new byte[16]; // Should be 32
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => storage.DeleteBlobAsync(invalidKey, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task DeleteBlobAsync_ReturnsFalseWhenNoMatchingBlob()
    {
        // Arrange
        var emptyArray = LargeBlobArray.CreateEmpty().Serialize();
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateReadResponse(emptyArray)));
        
        var storage = new LargeBlobStorage(_mockSession, _testProtocol, _pinUvAuthToken);
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        
        // Act
        var result = await storage.DeleteBlobAsync(key, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task WriteLargeBlobArrayAsync_IncludesCorrectProtocolVersion()
    {
        // Arrange
        byte[]? capturedRequest = null;
        
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        var storage = new LargeBlobStorage(_mockSession, _testProtocol, _pinUvAuthToken);
        var array = LargeBlobArray.CreateEmpty();
        
        // Act
        await storage.WriteLargeBlobArrayAsync(array, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        // Find pinUvAuthProtocol in the CBOR - skip command byte
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        reader.ReadStartMap();
        
        var foundProtocol = false;
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadInt32();
            if (key == 6) // pinUvAuthProtocol
            {
                var version = reader.ReadInt32();
                Assert.Equal(2, version);
                foundProtocol = true;
                break;
            }
            reader.SkipValue();
        }
        
        Assert.True(foundProtocol);
    }
    
    [Fact]
    public async Task WriteLargeBlobArrayAsync_IncludesPinUvAuthParam()
    {
        // Arrange
        byte[]? capturedRequest = null;
        
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(x => capturedRequest = x.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadOnlyMemory<byte>.Empty));
        
        var storage = new LargeBlobStorage(_mockSession, _testProtocol, _pinUvAuthToken);
        var array = LargeBlobArray.CreateEmpty();
        
        // Act
        await storage.WriteLargeBlobArrayAsync(array, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.NotNull(capturedRequest);
        
        // Find pinUvAuthParam in the CBOR - skip command byte
        var reader = new CborReader(capturedRequest.AsSpan(1).ToArray());
        reader.ReadStartMap();
        
        var foundAuthParam = false;
        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadInt32();
            if (key == 5) // pinUvAuthParam
            {
                var param = reader.ReadByteString();
                Assert.Equal(16, param.Length); // Our test protocol returns 16-byte auth param
                foundAuthParam = true;
                break;
            }
            reader.SkipValue();
        }
        
        Assert.True(foundAuthParam);
    }
    
    /// <summary>
    /// Creates a CBOR response with config (0x01) containing the given data.
    /// </summary>
    private static ReadOnlyMemory<byte> CreateReadResponse(byte[] data)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteInt32(1); // config key
        writer.WriteByteString(data);
        writer.WriteEndMap();
        return writer.Encode();
    }
}

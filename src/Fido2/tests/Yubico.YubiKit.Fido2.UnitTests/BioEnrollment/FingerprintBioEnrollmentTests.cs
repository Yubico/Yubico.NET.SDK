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
using Yubico.YubiKit.Fido2.BioEnrollment;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;

namespace Yubico.YubiKit.Fido2.UnitTests.BioEnrollment;

/// <summary>
/// Tests for FingerprintBioEnrollment operations.
/// </summary>
public class FingerprintBioEnrollmentTests
{
    private readonly IFidoSession _mockSession;
    private readonly FakeBioPinUvAuthProtocol _fakeProtocol;
    private readonly byte[] _pinUvAuthToken = new byte[32];
    
    public FingerprintBioEnrollmentTests()
    {
        _mockSession = Substitute.For<IFidoSession>();
        _fakeProtocol = new FakeBioPinUvAuthProtocol();
    }
    
    private FingerprintBioEnrollment CreateBioEnrollment() => 
        new(_mockSession, _fakeProtocol, _pinUvAuthToken);
    
    [Fact]
    public void Constructor_WithNullCommands_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FingerprintBioEnrollment(
                (IFidoSession)null!,
                _fakeProtocol,
                _pinUvAuthToken));
    }
    
    [Fact]
    public void Constructor_WithNullProtocol_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FingerprintBioEnrollment(
                _mockSession,
                null!,
                _pinUvAuthToken));
    }
    
    [Fact]
    public async Task GetFingerprintSensorInfoAsync_ParsesResponse()
    {
        // Arrange
        var sensorInfoResponse = CreateSensorInfoResponse(FingerprintKind.Touch, 5);
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(sensorInfoResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        var info = await bioEnroll.GetFingerprintSensorInfoAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(FingerprintKind.Touch, info.FingerprintKind);
        Assert.Equal(5, info.MaxCaptureSamplesRequiredForEnroll);
    }
    
    [Fact]
    public async Task GetFingerprintSensorInfoAsync_SendsCorrectPayload()
    {
        // Arrange
        var sensorInfoResponse = CreateSensorInfoResponse(FingerprintKind.Touch, 5);
        ReadOnlyMemory<byte> capturedRequest = default;
        
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(p => capturedRequest = p),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(sensorInfoResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        await bioEnroll.GetFingerprintSensorInfoAsync(TestContext.Current.CancellationToken);
        
        // Assert - request format is [command][cbor_payload]
        // First byte is command, rest is CBOR
        Assert.True(capturedRequest.Length > 1, "Request should have command + payload");
        var payload = capturedRequest.Slice(1); // Skip command byte
        var reader = new CborReader(payload, CborConformanceMode.Lax);
        var mapCount = reader.ReadStartMap() ?? 0;
        Assert.Equal(1, mapCount);
        Assert.Equal(1, reader.ReadInt32()); // modality key
        Assert.Equal(1, reader.ReadInt32()); // fingerprint value
    }
    
    [Fact]
    public async Task EnrollBeginAsync_ReturnsEnrollmentResult()
    {
        // Arrange
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var enrollResponse = CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 4);
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(enrollResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        var result = await bioEnroll.EnrollBeginAsync(cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(templateId, result.TemplateId.ToArray());
        Assert.Equal(FingerprintSampleStatus.Good, result.LastSampleStatus);
        Assert.Equal(4, result.RemainingSamples);
        Assert.False(result.IsComplete);
    }
    
    [Fact]
    public async Task EnrollBeginAsync_WithTimeout_IncludesTimeoutInPayload()
    {
        // Arrange
        var templateId = new byte[] { 0x01 };
        var enrollResponse = CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 4);
        
        ReadOnlyMemory<byte> capturedRequest = default;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(p => capturedRequest = p),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(enrollResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        await bioEnroll.EnrollBeginAsync(timeout: 15000, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert - request format is [command][cbor_payload], skip command byte
        Assert.True(capturedRequest.Length > 1, "Request should have command + payload");
        var payload = capturedRequest.Slice(1);
        var reader = new CborReader(payload, CborConformanceMode.Lax);
        var mapCount = reader.ReadStartMap() ?? 0;
        
        var hasTimeout = false;
        for (var i = 0; i < mapCount; i++)
        {
            var key = reader.ReadInt32();
            if (key == 6) // timeout key
            {
                hasTimeout = true;
                Assert.Equal(15000, reader.ReadInt32());
            }
            else
            {
                reader.SkipValue();
            }
        }
        
        Assert.True(hasTimeout);
    }
    
    [Fact]
    public async Task EnrollCaptureNextSampleAsync_ReturnsUpdatedResult()
    {
        // Arrange
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var enrollResponse = CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 3);
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(enrollResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        var result = await bioEnroll.EnrollCaptureNextSampleAsync(templateId, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(3, result.RemainingSamples);
        Assert.Equal(FingerprintSampleStatus.Good, result.LastSampleStatus);
    }
    
    [Fact]
    public async Task EnrollCaptureNextSampleAsync_WithPoorQuality_ReturnsStatus()
    {
        // Arrange
        var templateId = new byte[] { 0x01 };
        var enrollResponse = CreateEnrollmentResponse(
            templateId, 
            FingerprintSampleStatus.TooFast, 
            3); // Remaining doesn't decrease
        
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(enrollResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        var result = await bioEnroll.EnrollCaptureNextSampleAsync(templateId, cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Equal(FingerprintSampleStatus.TooFast, result.LastSampleStatus);
    }
    
    [Fact]
    public async Task EnrollCancelAsync_SendsCommand()
    {
        // Arrange
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(ReadOnlyMemory<byte>.Empty));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        await bioEnroll.EnrollCancelAsync(TestContext.Current.CancellationToken);
        
        // Assert
        await _mockSession.Received(1).SendCborRequestAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task EnumerateEnrollmentsAsync_ReturnsTemplateList()
    {
        // Arrange
        var templateId = new byte[] { 0xAB, 0xCD };
        var templateResponse = CreateTemplateInfoResponse(templateId, "My Finger");
        
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(templateResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        var templates = await bioEnroll.EnumerateEnrollmentsAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Single(templates);
        Assert.Equal(templateId, templates[0].TemplateId.ToArray());
        Assert.Equal("My Finger", templates[0].FriendlyName);
    }
    
    [Fact]
    public async Task EnumerateEnrollmentsAsync_WhenNoTemplates_ReturnsEmptyList()
    {
        // Arrange
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns<ReadOnlyMemory<byte>>(x => throw new CtapException(CtapStatus.InvalidCommand, "No templates"));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        var templates = await bioEnroll.EnumerateEnrollmentsAsync(TestContext.Current.CancellationToken);
        
        // Assert
        Assert.Empty(templates);
    }
    
    [Fact]
    public async Task SetFriendlyNameAsync_SendsCorrectPayload()
    {
        // Arrange
        var templateId = new byte[] { 0x01, 0x02 };
        var friendlyName = "Right Index Finger";
        
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(ReadOnlyMemory<byte>.Empty));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        await bioEnroll.SetFriendlyNameAsync(templateId, friendlyName, TestContext.Current.CancellationToken);
        
        // Assert
        await _mockSession.Received(1).SendCborRequestAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task SetFriendlyNameAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var bioEnroll = CreateBioEnrollment();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            bioEnroll.SetFriendlyNameAsync(new byte[] { 1, 2, 3 }, null!, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task SetFriendlyNameAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var bioEnroll = CreateBioEnrollment();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            bioEnroll.SetFriendlyNameAsync(new byte[] { 1, 2, 3 }, "", TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task SetFriendlyNameAsync_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var bioEnroll = CreateBioEnrollment();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            bioEnroll.SetFriendlyNameAsync(new byte[] { 1, 2, 3 }, "   ", TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task RemoveEnrollmentAsync_SendsCorrectPayload()
    {
        // Arrange
        var templateId = new byte[] { 0x01, 0x02, 0x03 };
        
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(ReadOnlyMemory<byte>.Empty));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        await bioEnroll.RemoveEnrollmentAsync(templateId, TestContext.Current.CancellationToken);
        
        // Assert
        await _mockSession.Received(1).SendCborRequestAsync(
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task AllOperations_UseCorrectPinUvAuthProtocol()
    {
        // Arrange
        var templateId = new byte[] { 0x01 };
        var enrollResponse = CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 4);
        
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(enrollResponse));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act
        await bioEnroll.EnrollBeginAsync(cancellationToken: TestContext.Current.CancellationToken);
        
        // Assert - FakePinUvAuthProtocol records calls
        Assert.True(_fakeProtocol.AuthenticateWasCalled);
    }
    
    [Fact]
    public async Task EnrollmentWorkflow_CompletesSuccessfully()
    {
        // Arrange - simulate a complete enrollment workflow
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        
        var responses = new Queue<byte[]>(
        [
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 4),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 3),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.TooFast, 3), // Rejected sample
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 2),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 1),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 0) // Complete!
        ]);
        
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(x => Task.FromResult<ReadOnlyMemory<byte>>(responses.Dequeue()));
        
        var bioEnroll = CreateBioEnrollment();
        
        // Act - simulate enrollment loop
        var result = await bioEnroll.EnrollBeginAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.IsComplete);
        Assert.Equal(4, result.RemainingSamples);
        
        while (!result.IsComplete)
        {
            result = await bioEnroll.EnrollCaptureNextSampleAsync(result.TemplateId, cancellationToken: TestContext.Current.CancellationToken);
        }
        
        // Assert
        Assert.True(result.IsComplete);
        Assert.Equal(0, result.RemainingSamples);
    }
    
    // Helper methods to create CBOR responses
    
    private static byte[] CreateSensorInfoResponse(FingerprintKind kind, int maxSamples)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(2); // fingerprintKind
        writer.WriteInt32((int)kind);
        writer.WriteInt32(3); // maxCaptureSamplesRequiredForEnroll
        writer.WriteInt32(maxSamples);
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static byte[] CreateEnrollmentResponse(
        byte[] templateId,
        FingerprintSampleStatus status,
        int remainingSamples)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);
        writer.WriteInt32(4); // templateId
        writer.WriteByteString(templateId);
        writer.WriteInt32(5); // lastEnrollSampleStatus
        writer.WriteInt32((int)status);
        writer.WriteInt32(6); // remainingSamples
        writer.WriteInt32(remainingSamples);
        writer.WriteEndMap();
        return writer.Encode();
    }
    
    private static byte[] CreateTemplateInfoResponse(byte[] templateId, string? friendlyName)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(friendlyName != null ? 2 : 1);
        writer.WriteInt32(4); // templateId
        writer.WriteByteString(templateId);
        if (friendlyName != null)
        {
            writer.WriteInt32(7); // templateFriendlyName
            writer.WriteTextString(friendlyName);
        }
        writer.WriteEndMap();
        return writer.Encode();
    }
}

/// <summary>
/// Fake implementation of IPinUvAuthProtocol for testing.
/// </summary>
internal sealed class FakeBioPinUvAuthProtocol : IPinUvAuthProtocol
{
    public int Version => 2;
    public int AuthenticationTagLength => 32;
    public bool AuthenticateWasCalled { get; private set; }
    public bool IsDisposed { get; private set; }
    
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
    {
        AuthenticateWasCalled = true;
        return new byte[32];
    }
    
    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        => true;
    
    public void Dispose()
    {
        IsDisposed = true;
    }
}

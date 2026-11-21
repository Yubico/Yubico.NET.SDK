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

using NSubstitute;
using System.Formats.Cbor;
using Yubico.YubiKit.Ctap2.BioEnrollment;
using Yubico.YubiKit.Ctap2.Pin;

namespace Yubico.YubiKit.Ctap2.UnitTests.BioEnrollment;

/// <summary>
/// Tests for FingerprintBioEnrollment operations.
/// </summary>
/// <remarks>
/// Mock replies and all-zero authentication tags exercise current encoding/decoding and
/// call counts, not a fingerprint sensor or standard transcript interoperability. Sensor
/// and timeout assertions preserve the helper's known nonstandard fields. Flat inventory
/// replies and InvalidCommand-as-empty likewise model current limitations, not the standard
/// array/InvalidOption behavior. Cancel/rename/remove tests count one send without inspecting
/// its payload. Required-field failures, real timeout and cancellation recovery are not covered.
/// </remarks>
public class FingerprintBioEnrollmentTests
{
    private readonly ICtap2Session _mockSession;
    private readonly FakeBioPinUvAuthProtocol _fakeProtocol;
    private readonly byte[] _pinUvAuthToken = new byte[32];

    public FingerprintBioEnrollmentTests()
    {
        _mockSession = Substitute.For<ICtap2Session>();
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
                (ICtap2Session)null!,
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
                Arg.Do<ReadOnlyMemory<byte>>(p => capturedRequest = p.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(sensorInfoResponse));

        var bioEnroll = CreateBioEnrollment();

        // Act
        await bioEnroll.GetFingerprintSensorInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal("09A201010207", Convert.ToHexString(capturedRequest.Span));
        Assert.False(_fakeProtocol.AuthenticateWasCalled);
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

    [Theory]
    [InlineData(false, 15000, "09A50101020103A103193A980402055820", "0101A103193A98")]
    [InlineData(false, null, "09A4010102010402055820", "0101")]
    [InlineData(true, 15000, "40A5010102020402055820", "02")]
    public async Task EnrollBeginAsync_EncodesAndAuthenticatesSelectedProfile(
        bool preview, int? timeout, string requestPrefix, string transcript)
    {
        // Arrange
        var templateId = new byte[] { 0x01 };
        var enrollResponse = CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 4);

        ReadOnlyMemory<byte> capturedRequest = default;
        _mockSession.SendCborRequestAsync(
                Arg.Do<ReadOnlyMemory<byte>>(p => capturedRequest = p.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(enrollResponse));

        var bioEnroll = new FingerprintBioEnrollment(_mockSession, _fakeProtocol, _pinUvAuthToken, preview);

        // Act
        await bioEnroll.EnrollBeginAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(requestPrefix + new string('0', 64) + (preview ? "06193A98" : ""),
            Convert.ToHexString(capturedRequest.Span));
        Assert.Equal(transcript, Convert.ToHexString(_fakeProtocol.LastAuthenticationMessage));
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
            3); // Supplied count; this test asserts only the decoded TooFast status.

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
        // {7: [{1: h'abcd', 2: 'My Finger'}, {1: h'ef'}]}
        byte[] templateResponse = Convert.FromHexString("A10782A20142ABCD02694D792046696E676572A10141EF");

        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(templateResponse));

        var bioEnroll = CreateBioEnrollment();

        // Act
        var templates = await bioEnroll.EnumerateEnrollmentsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, templates.Count);
        Assert.Equal(templateId, templates[0].TemplateId.ToArray());
        Assert.Equal("My Finger", templates[0].FriendlyName);
        Assert.Equal(new byte[] { 0xEF }, templates[1].TemplateId.ToArray());
        Assert.Null(templates[1].FriendlyName);
    }

    [Fact]
    public async Task EnumerateEnrollmentsAsync_WhenNoTemplates_ReturnsEmptyList()
    {
        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns<ReadOnlyMemory<byte>>(_ => throw new CtapException(CtapStatus.InvalidOption, "No templates"));

        var bioEnroll = CreateBioEnrollment();

        // Act
        var templates = await bioEnroll.EnumerateEnrollmentsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(templates);
    }

    [Fact]
    public async Task EnumerateEnrollmentsAsync_UnsupportedCommandDoesNotLookEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        var error = new CtapException(CtapStatus.InvalidCommand);
        _mockSession.SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), ct)
            .Returns<ReadOnlyMemory<byte>>(_ => throw error);

        var actual = await Assert.ThrowsAsync<CtapException>(() => CreateBioEnrollment().EnumerateEnrollmentsAsync(ct));

        Assert.Same(error, actual);
    }

    [Theory]
    [InlineData("A10781A1026161")] // Entry is missing its required ID.
    [InlineData("A0")] // Success is missing the inventory array.
    public async Task EnumerateEnrollmentsAsync_RejectsMalformedInventory(string hex)
    {
        var ct = TestContext.Current.CancellationToken;
        _mockSession.SendCborRequestAsync(Arg.Any<ReadOnlyMemory<byte>>(), ct)
            .Returns((ReadOnlyMemory<byte>)Convert.FromHexString(hex));

        await Assert.ThrowsAsync<CborContentException>(() => CreateBioEnrollment().EnumerateEnrollmentsAsync(ct));
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

    [Theory]
    [InlineData(2, "09A50101020203A201410103193A980402055820", "0102A201410103193A98")]
    [InlineData(3, "09A201010203", "")]
    [InlineData(4, "09A4010102040402055820", "0104")]
    [InlineData(5, "09A50101020503A201410102616E0402055820", "0105A201410102616E")]
    [InlineData(6, "09A50101020603A10141010402055820", "0106A1014101")]
    public async Task StandardFingerprints_EncodeExactRequestAndAuthentication(int operation, string wire, string transcript)
    {
        byte[] templateId = [1];
        byte[] reply = operation == 4
            ? [0xA1, 0x07, 0x80]
            : CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 1);
        ReadOnlyMemory<byte> request = default;
        _mockSession.SendCborRequestAsync(Arg.Do<ReadOnlyMemory<byte>>(p => request = p.ToArray()),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReadOnlyMemory<byte>>(reply));
        var bio = CreateBioEnrollment();
        var ct = TestContext.Current.CancellationToken;
        await (operation switch
        {
            2 => bio.EnrollCaptureNextSampleAsync(templateId, 15000, ct),
            3 => bio.EnrollCancelAsync(ct),
            4 => bio.EnumerateEnrollmentsAsync(ct),
            5 => bio.SetFriendlyNameAsync(templateId, "n", ct),
            6 => bio.RemoveEnrollmentAsync(templateId, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        });

        Assert.Equal(wire + (operation == 3 ? "" : new string('0', 64)), Convert.ToHexString(request.Span));
        Assert.Equal(transcript, Convert.ToHexString(_fakeProtocol.LastAuthenticationMessage));
        Assert.Equal(operation != 3, _fakeProtocol.AuthenticateWasCalled);
    }

    [Fact]
    public async Task EnrollmentWorkflow_CompletesSuccessfully()
    {
        // Arrange a queue-driven simulation; every next reply repeats the ID, so standard-permitted omission is untested.
        var templateId = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var responses = new Queue<byte[]>(
        [
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 4),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 3),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.TooFast, 3), // Rejected sample
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 2),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 1),
            CreateEnrollmentResponse(templateId, FingerprintSampleStatus.Good, 0) // Decoded zero, not sensor persistence proof.
        ]);

        _mockSession.SendCborRequestAsync(
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>())
            .Returns(x => Task.FromResult<ReadOnlyMemory<byte>>(responses.Dequeue()));

        var bioEnroll = CreateBioEnrollment();

        // Simulated loop reuses each reply's ID. Real callers must retain begin's ID separately:
        // standard next replies need not repeat it, a case this fixture does not cover.
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

    // Synthetic selected-field replies; the template helper emits the current flat, nonstandard inventory shape.

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
        writer.WriteStartMap(friendlyName is not null ? 2 : 1);
        writer.WriteInt32(4); // templateId
        writer.WriteByteString(templateId);
        if (friendlyName is not null)
        {
            writer.WriteInt32(7); // templateFriendlyName
            writer.WriteTextString(friendlyName);
        }
        writer.WriteEndMap();
        return writer.Encode();
    }
}

/// <summary>
/// Call-recording fake with zero tags and identity encryption/decryption; not a protocol-conformance implementation.
/// </summary>
internal sealed class FakeBioPinUvAuthProtocol : IPinUvAuthProtocol
{
    public int Version => 2;
    public int AuthenticationTagLength => 32;
    public bool AuthenticateWasCalled { get; private set; }
    public byte[] LastAuthenticationMessage { get; private set; } = [];
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
        LastAuthenticationMessage = message.ToArray();
        return new byte[32];
    }

    public bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
        => true;

    public void Dispose()
    {
        IsDisposed = true;
    }
}
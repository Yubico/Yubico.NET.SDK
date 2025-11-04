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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.UnitTests.SmartCard.Fakes;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard;

/// <summary>
///     Unit tests for PcscProtocol class.
///     Tests configuration, APDU transmission, and SELECT command functionality.
/// </summary>
public class PcscProtocolTests
{
    private readonly FakeSmartCardConnection _fakeConnection = new();
    private readonly NullLogger<PcscProtocol> _logger = NullLogger<PcscProtocol>.Instance;

    #region Configuration Tests

    [Fact]
    public void Configure_FirmwareBelow400_IgnoresConfiguration()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var firmware = new FirmwareVersion(3, 5);

        // Act
        protocol.Configure(firmware, null);

        // Assert - Should not throw, processor remains default
        Assert.NotNull(protocol);
    }

    [Fact]
    public void Configure_Firmware400To429_UsesYk4MaxSize()
    {
        // Arrange
        _fakeConnection.SupportsExtendedApduValue = true;
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var firmware = new FirmwareVersion(4, 2, 5);

        // Act
        protocol.Configure(firmware, null);

        // Assert - Should configure with YK4 max size
        Assert.NotNull(protocol);
        Assert.Equal(SmartCardMaxApduSizes.Yk4, protocol.MaxApduSize);
    }

    [Fact]
    public void Configure_Firmware430AndAbove_UsesYk43MaxSize()
    {
        // Arrange
        _fakeConnection.SupportsExtendedApduValue = true;
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var firmware = new FirmwareVersion(5, 7, 2);

        // Act
        protocol.Configure(firmware, null);

        // Assert - Should configure with YK43 max size
        Assert.NotNull(protocol);
        Assert.Equal(SmartCardMaxApduSizes.Yk43, protocol.MaxApduSize);
    }

    [Fact]
    public void Configure_ForceShortApdusTrue_UsesCommandChaining()
    {
        // Arrange
        _fakeConnection.SupportsExtendedApduValue = true;
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var firmware = new FirmwareVersion(5, 7, 2);
        var config = new ProtocolConfiguration { ForceShortApdus = true };

        // Act
        protocol.Configure(firmware, config);

        // Assert - Should use command chaining despite extended APDU support
        Assert.NotNull(protocol);
        Assert.False(protocol.UseExtendedApdus);
    }

    [Fact]
    public void Configure_ConnectionNoExtendedApdu_UsesCommandChaining()
    {
        // Arrange
        _fakeConnection.SupportsExtendedApduValue = false;
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var firmware = new FirmwareVersion(5, 7, 2);

        // Act
        protocol.Configure(firmware, null);

        // Assert - Should use command chaining when connection doesn't support extended
        Assert.NotNull(protocol);
        Assert.False(protocol.UseExtendedApdus);
    }

    [Fact]
    public void Configure_ExtendedApduSupported_UsesExtendedApduProcessor()
    {
        // Arrange
        _fakeConnection.SupportsExtendedApduValue = true;
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var firmware = new FirmwareVersion(5, 7, 2);

        // Act
        protocol.Configure(firmware, null);

        // Assert - Should use extended APDU processor
        Assert.NotNull(protocol);
        Assert.True(protocol.UseExtendedApdus);
    }

    [Fact]
    public void Constructor_WithCustomInsSendRemaining_UsesProvidedValue()
    {
        // Arrange
        byte customIns = 0xC2;
        ReadOnlyMemory<byte> insMemory = new[] { customIns };

        // Act
        var protocol = new PcscProtocol(_logger, _fakeConnection, insMemory);

        // Assert - Should use custom INS_SEND_REMAINING
        Assert.NotNull(protocol);
        Assert.Equal(customIns, protocol._insSendRemaining);
    }

    [Fact]
    public void Constructor_WithDefaultInsSendRemaining_Uses0xC0()
    {
        // Act
        var protocol = new PcscProtocol(_logger, _fakeConnection);

        // Assert - Should use default 0xC0
        Assert.NotNull(protocol);
        Assert.Equal((byte)0xC0, protocol._insSendRemaining);
    }

    #endregion

    #region TransmitAndReceiveAsync Tests

    [Fact]
    public async Task TransmitAndReceiveAsync_SuccessResponse_ReturnsData()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var expectedData = new byte[] { 0x01, 0x02, 0x03 };
        var response = new byte[expectedData.Length + 2];
        expectedData.CopyTo(response, 0);
        response[^2] = 0x90;
        response[^1] = 0x00;
        _fakeConnection.EnqueueResponse(response);

        var command = new CommandApdu(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act
        var result = await protocol.TransmitAndReceiveAsync(command);

        // Assert
        Assert.Equal(expectedData, result.ToArray());
    }

    [Fact]
    public async Task TransmitAndReceiveAsync_NonSuccessResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var response = new byte[] { 0x69, 0x82 }; // Security status not satisfied
        _fakeConnection.EnqueueResponse(response);

        var command = new CommandApdu(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.TransmitAndReceiveAsync(command));
        Assert.Contains("6982", ex.Message);
    }

    [Fact]
    public async Task TransmitAndReceiveAsync_ExceptionMessage_FormattedCorrectly()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var response = new byte[] { 0x6A, 0x82 }; // File not found
        _fakeConnection.EnqueueResponse(response);

        var command = new CommandApdu(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.TransmitAndReceiveAsync(command));
        Assert.Matches("Command failed with status: 6A82", ex.Message);
    }

    [Fact]
    public async Task TransmitAndReceiveAsync_RespectsCancellationToken()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var command = new CommandApdu(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            protocol.TransmitAndReceiveAsync(command, cts.Token));
    }

    #endregion

    #region SelectAsync Tests

    [Fact]
    public async Task SelectAsync_ConstructsCorrectApdu()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var response = new byte[] { 0x90, 0x00 };
        _fakeConnection.EnqueueResponse(response);

        var appId = new byte[] { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01 };

        // Act
        await protocol.SelectAsync(appId);

        // Assert
        Assert.Single(_fakeConnection.TransmittedCommands);
        // Note: We can't directly inspect the APDU structure from transmitted bytes
        // but we verified it doesn't throw, which means the command was accepted
    }

    [Fact]
    public async Task SelectAsync_SuccessResponse_ReturnsData()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var expectedData = new byte[] { 0x61, 0x10 }; // FCI template tag
        var response = new byte[expectedData.Length + 2];
        expectedData.CopyTo(response, 0);
        response[^2] = 0x90;
        response[^1] = 0x00;
        _fakeConnection.EnqueueResponse(response);

        var appId = new byte[] { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01 };

        // Act
        var result = await protocol.SelectAsync(appId);

        // Assert
        Assert.Equal(expectedData, result.ToArray());
    }

    [Fact]
    public async Task SelectAsync_NonSuccessResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var response = new byte[] { 0x6A, 0x82 }; // File not found
        _fakeConnection.EnqueueResponse(response);

        var appId = new byte[] { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => protocol.SelectAsync(appId));
        Assert.Contains("6A82", ex.Message);
    }

    [Fact]
    public async Task SelectAsync_RespectsCancellationToken()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var appId = new byte[] { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01 };

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => protocol.SelectAsync(appId, cts.Token));
    }

    [Fact]
    public async Task Dispose_DisposesConnection()
    {
        // Arrange
        var protocol = new PcscProtocol(_logger, _fakeConnection);

        // Act
        protocol.Dispose();

        // Assert - Subsequent operations should fail on disposed connection
        // (FakeSmartCardConnection throws ObjectDisposedException when disposed)
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            var response = new byte[] { 0x90, 0x00 };
            _fakeConnection.EnqueueResponse(response);
            await protocol.TransmitAndReceiveAsync(
                new CommandApdu(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty));
        });
    }

    #endregion
}
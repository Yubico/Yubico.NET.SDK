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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Scp;

/// <summary>
///     Unit tests for ScpProtocolAdapter class.
///     Tests the decorator pattern implementation that wraps a protocol with SCP.
/// </summary>
public class PcscProtocolScpTests
{
    private readonly FakeSmartCardConnection _fakeConnection = new();
    private readonly FakeApduProcessor _fakeScpProcessor = new();
    private readonly NullLogger<PcscProtocol> _logger = NullLogger<PcscProtocol>.Instance;

    [Fact]
    public async Task TransmitAndReceiveAsync_DelegatesToScpProcessor()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var expectedData = new byte[] { 0x01, 0x02, 0x03 };
        _fakeScpProcessor.EnqueueResponse(0x90, 0x00, expectedData);

        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        var command = new ApduCommand(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act
        var result = await adapter.TransmitAndReceiveAsync(command, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedData, result.Data.ToArray());
        Assert.Single(_fakeScpProcessor.TransmittedCommands);
    }

    [Fact]
    public async Task SelectAsync_DelegatesToScpProcessor()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var responseData = new byte[] { 0x61, 0x10 };
        _fakeScpProcessor.EnqueueResponse(0x90, 0x00, responseData);

        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        var appId = new byte[] { 0xA0, 0x00, 0x00, 0x05, 0x27, 0x20, 0x01 };

        // Act
        var result = await adapter.SelectAsync(appId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(responseData, result.ToArray());
        Assert.Single(_fakeScpProcessor.TransmittedCommands);
    }

    [Fact]
    public void GetDataEncryptor_ReturnsProvidedEncryptor()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        DataEncryptor expectedEncryptor = data => data.ToArray(); // Simple pass-through encryptor
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, expectedEncryptor);

        // Act
        var result = adapter.GetDataEncryptor();

        // Assert
        Assert.Same(expectedEncryptor, result);
    }

    [Fact]
    public void GetDataEncryptor_WhenNull_ReturnsNull()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);

        // Act
        var result = adapter.GetDataEncryptor();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Configure_DelegatesToBaseProtocol()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        var firmware = new FirmwareVersion(5, 7, 2);

        // Act - Should not throw
        adapter.Configure(firmware);

        // Assert - Configuration is applied to base protocol
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Dispose_DisposesBaseProtocol()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);

        // Act
        adapter.Dispose();

        // Assert - Base connection should be disposed
        Assert.Throws<ObjectDisposedException>(() =>
        {
            _fakeConnection.TransmitAndReceiveAsync(new byte[] { 0x00 }, TestContext.Current.CancellationToken)
                .GetAwaiter().GetResult();
        });
    }

    [Fact]
    public void Dispose_DisposesScpProcessor()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var scpProcessor = new DisposableApduProcessor();
        var adapter = new PcscProtocolScp(baseProtocol, scpProcessor, null!);

        // Act
        adapter.Dispose();

        // Assert
        Assert.Equal(1, scpProcessor.DisposeCount);
    }

    [Fact]
    public void Dispose_Twice_DisposesBaseProtocolAndScpProcessorOnce()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var scpProcessor = new DisposableApduProcessor();
        var adapter = new PcscProtocolScp(baseProtocol, scpProcessor, null!);

        // Act
        adapter.Dispose();
        adapter.Dispose();

        // Assert
        Assert.Equal(1, _fakeConnection.DisposeCount);
        Assert.Equal(1, scpProcessor.DisposeCount);
    }

    [Fact]
    public async Task TransmitAndReceiveAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        adapter.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            adapter.TransmitAndReceiveAsync(new ApduCommand(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SelectAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        adapter.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            adapter.SelectAsync(ApplicationIds.Piv, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Configure_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        adapter.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            adapter.Configure(new FirmwareVersion(5, 7, 2)));
    }

    [Fact]
    public async Task TransmitAndReceiveAsync_NonSuccessResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        _fakeScpProcessor.EnqueueResponse(0x69, 0x82); // Security status not satisfied

        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        var command = new ApduCommand(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ApduException>(() =>
            adapter.TransmitAndReceiveAsync(command, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("6982", ex.Message);
    }

    [Fact]
    public async Task CreateSecureProcessor_ChainedResponse_WrapsSendRemainingCommand()
    {
        // Arrange
        var rawProcessor = new FakeApduProcessor();
        rawProcessor.EnqueueResponse(0x61, 0x01);
        rawProcessor.EnqueueResponse(0x90, 0x00);

        using var state = new ScpState(
            new SessionKeys(new byte[16], new byte[16], new byte[16]),
            new byte[16]);

        var secureProcessor = ScpInitializer.CreateSecureProcessor(
            rawProcessor,
            state,
            new FirmwareVersion(5, 7, 2),
            0xA5);

        var command = new ApduCommand(0x00, 0x01, 0x00, 0x00);

        // Act
        var response = await secureProcessor.TransmitAsync(
            command,
            useScp: true,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(SWConstants.Success, response.SW);
        Assert.Equal(2, rawProcessor.TransmittedCommands.Count);
        Assert.Equal((byte)0x01, rawProcessor.TransmittedCommands[0].Ins);
        Assert.Equal((byte)0xA5, rawProcessor.TransmittedCommands[1].Ins);
        Assert.True((rawProcessor.TransmittedCommands[1].Cla & 0x04) != 0);
        Assert.True(rawProcessor.TransmittedCommands[1].Data.Length >= 8);
    }

    private sealed class DisposableApduProcessor : IApduProcessor, IDisposable
    {
        public IApduFormatter Formatter { get; } = new FakeApduFormatter();
        public int DisposeCount { get; private set; }

        public Task<ApduResponse> TransmitAsync(
            ApduCommand command,
            bool useScp,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ApduResponse(new byte[] { 0x90, 0x00 }));

        public void Dispose() => DisposeCount++;
    }
}
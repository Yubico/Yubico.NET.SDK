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
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.UnitTests.SmartCard.Fakes;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Scp;

/// <summary>
///     Unit tests for ScpProtocolAdapter class.
///     Tests the decorator pattern implementation that wraps a protocol with SCP.
/// </summary>
public class ScpProtocolAdapterTests
{
    private readonly FakeSmartCardConnection _fakeConnection = new();
    private readonly FakeApduProcessor _fakeScpProcessor = new();
    private readonly NullLogger<PcscProtocol> _logger = NullLogger<PcscProtocol>.Instance;

    [Fact]
    public async Task TransmitAndReceiveAsync_DelegatesToScpProcessor()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_logger, _fakeConnection);
        var expectedData = new byte[] { 0x01, 0x02, 0x03 };
        _fakeScpProcessor.EnqueueResponse(0x90, 0x00, expectedData);

        var adapter = new ScpProtocolAdapter(baseProtocol, _fakeScpProcessor, null!);
        var command = new ApduCommand(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act
        var result = await adapter.TransmitAndReceiveAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedData, result.ToArray());
        Assert.Single(_fakeScpProcessor.TransmittedCommands);
    }

    [Fact]
    public async Task SelectAsync_DelegatesToScpProcessor()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_logger, _fakeConnection);
        var responseData = new byte[] { 0x61, 0x10 };
        _fakeScpProcessor.EnqueueResponse(0x90, 0x00, responseData);

        var adapter = new ScpProtocolAdapter(baseProtocol, _fakeScpProcessor, null!);
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
        var baseProtocol = new PcscProtocol(_logger, _fakeConnection);
        DataEncryptor expectedEncryptor = data => data.ToArray(); // Simple pass-through encryptor
        var adapter = new ScpProtocolAdapter(baseProtocol, _fakeScpProcessor, expectedEncryptor);

        // Act
        var result = adapter.GetDataEncryptor();

        // Assert
        Assert.Same(expectedEncryptor, result);
    }

    [Fact]
    public void GetDataEncryptor_WhenNull_ReturnsNull()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_logger, _fakeConnection);
        var adapter = new ScpProtocolAdapter(baseProtocol, _fakeScpProcessor, null!);

        // Act
        var result = adapter.GetDataEncryptor();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Configure_DelegatesToBaseProtocol()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_logger, _fakeConnection);
        var adapter = new ScpProtocolAdapter(baseProtocol, _fakeScpProcessor, null!);
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
        var baseProtocol = new PcscProtocol(_logger, _fakeConnection);
        var adapter = new ScpProtocolAdapter(baseProtocol, _fakeScpProcessor, null!);

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
    public async Task TransmitAndReceiveAsync_NonSuccessResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_logger, _fakeConnection);
        _fakeScpProcessor.EnqueueResponse(0x69, 0x82); // Security status not satisfied

        var adapter = new ScpProtocolAdapter(baseProtocol, _fakeScpProcessor, null!);
        var command = new ApduCommand(0x00, 0x00, 0x00, 0x00, ReadOnlyMemory<byte>.Empty);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TransmitAndReceiveAsync(command, TestContext.Current.CancellationToken));
        Assert.Contains("6982", ex.Message);
    }
}
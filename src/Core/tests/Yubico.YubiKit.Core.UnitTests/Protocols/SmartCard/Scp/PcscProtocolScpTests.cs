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
using System.Reflection;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Cryptography;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Scp;

/// <summary>
///     Unit tests for ScpProtocolAdapter class.
///     Tests the decorator pattern implementation that wraps a protocol with SCP.
/// </summary>
/// <remarks>
///     Shares <see cref="CryptographyProvidersCollection"/> because SCP paths read the mutable
///     <c>CryptographyProviders.EcdhPrimitivesCreator</c>/<c>CmacPrimitivesCreator</c> statics that
///     <see cref="Yubico.YubiKit.Core.UnitTests.Cryptography.CryptographyProviderExtensionTests"/> temporarily
///     swaps; without this, the two could race under parallel test-collection execution.
/// </remarks>
[Collection(CryptographyProvidersCollection.Name)]
public class PcscProtocolScpTests
{
    private readonly FakeSmartCardConnection _fakeConnection = new();
    private readonly FakeApduProcessor _fakeScpProcessor = new();
    private readonly NullLogger<PcscProtocol> _logger = NullLogger<PcscProtocol>.Instance;

    [Fact]
    public void Constructors_AreNonPublicAndRequireConcretePcscProtocol()
    {
        Assert.Empty(typeof(PcscProtocolScp).GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        var constructor = Assert.Single(
            typeof(PcscProtocolScp).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.Equal(typeof(PcscProtocol), constructor.GetParameters()[0].ParameterType);
    }

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
    public void Configure_AfterScpEstablishment_ThrowsInvalidOperationException()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);
        var firmware = new FirmwareVersion(5, 7, 2);

        // Act + Assert
        var exception = Assert.Throws<InvalidOperationException>(() => adapter.Configure(firmware));
        Assert.Contains("before", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Disposing the SCP wrapper disposes the base protocol it decorates — and stops there. The connection
    ///     belongs to whoever created it. This test previously proved base disposal by observing the CONNECTION
    ///     becoming unusable, which only worked because the protocol wrongly owned it.
    /// </summary>
    [Fact]
    public void Dispose_DisposesBaseProtocol_ButNotTheConnection()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var adapter = new PcscProtocolScp(baseProtocol, _fakeScpProcessor, null!);

        // Act
        adapter.Dispose();

        // Assert - the base protocol is disposed...
        Assert.Throws<ObjectDisposedException>(() => baseProtocol.Configure(new FirmwareVersion(5, 7, 2)));

        // ...and the connection is untouched and still usable.
        Assert.Equal(0, _fakeConnection.DisposeCount);
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
    public async Task DisposeAsync_DuringExchange_DrainsBeforeDisposingScpProcessor()
    {
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var scpProcessor = new BlockingDisposableApduProcessor();
        var adapter = new PcscProtocolScp(baseProtocol, scpProcessor, null!);
        Task<ApduResponse> exchange = adapter.TransmitAndReceiveAsync(
            new ApduCommand(0x00, 0x01, 0x00, 0x00),
            cancellationToken: TestContext.Current.CancellationToken);
        await scpProcessor.Started.Task.WaitAsync(TestContext.Current.CancellationToken);

        var asyncDisposable = Assert.IsAssignableFrom<IAsyncDisposable>(adapter);
        Task disposal = asyncDisposable.DisposeAsync().AsTask();

        Assert.False(disposal.IsCompleted);
        Assert.Equal(0, scpProcessor.DisposeCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => adapter.TransmitAndReceiveAsync(
            new ApduCommand(0x00, 0x02, 0x00, 0x00),
            cancellationToken: TestContext.Current.CancellationToken));

        scpProcessor.Release.SetResult();
        Assert.True((await exchange).IsOK());
        await disposal;
        Assert.Equal(1, scpProcessor.DisposeCount);
    }

    [Fact]
    public void Dispose_Twice_DisposesScpProcessorOnce_AndNeverTheConnection()
    {
        // Arrange
        var baseProtocol = new PcscProtocol(_fakeConnection, default, _logger);
        var scpProcessor = new DisposableApduProcessor();
        var adapter = new PcscProtocolScp(baseProtocol, scpProcessor, null!);

        // Act
        adapter.Dispose();
        adapter.Dispose();

        // Assert
        Assert.Equal(0, _fakeConnection.DisposeCount);
        Assert.Equal(1, scpProcessor.DisposeCount);
    }

    private sealed class BlockingDisposableApduProcessor : IApduProcessor, IDisposable
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int DisposeCount { get; private set; }
        public IApduFormatter Formatter { get; } = new FakeApduFormatter();

        public async Task<ApduResponse> TransmitAsync(
            ApduCommand command,
            bool useScp = true,
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new ApduResponse(new byte[] { 0x90, 0x00 });
        }

        public void Dispose() => DisposeCount++;
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

    [Fact]
    public async Task CreateSecureProcessor_ContinuationThrows_NotifiesOnceAndPropagatesOriginalFailure()
    {
        var connection = new FakeSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0x61, 0x01 });
        var rawProcessor = new ApduTransmitter(connection, new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43));
        using var state = CreateZeroedScpState();
        var failures = new List<Exception>();
        var secureProcessor = ScpInitializer.CreateSecureProcessor(
            rawProcessor, state, new FirmwareVersion(5, 7, 2), 0xA5,
            onRecoveryRequired: failures.Add);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            secureProcessor.TransmitAsync(
                new ApduCommand(0x00, 0x01, 0x00, 0x00),
                useScp: true,
                TestContext.Current.CancellationToken));

        Assert.Equal("No response enqueued for transmission", failure.Message);
        Assert.Same(failure, Assert.Single(failures));
        Assert.Equal(2, connection.TransmittedCommands.Count);
        Assert.Equal((byte)0x01, connection.TransmittedCommands[0].Span[1]);
        Assert.Equal((byte)0xA5, connection.TransmittedCommands[1].Span[1]);
        Assert.True((connection.TransmittedCommands[1].Span[0] & 0x04) != 0);
    }

    [Fact]
    public async Task PlainSelectContinuationFails_RefusesBaseAndSecureWrapperWithoutReplay()
    {
        var connection = new FakeSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0x61, 0x01 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.SelectAsync(new byte[] { 0xA0, 0x00, 0x00, 0x05, 0x27 },
                TestContext.Current.CancellationToken));

        Assert.Equal("No response enqueued for transmission", failure.Message);
        Assert.Equal(2, connection.TransmittedCommands.Count);
        Assert.Equal((byte)0xA4, connection.TransmittedCommands[0].Span[1]);
        Assert.Equal((byte)0xC0, connection.TransmittedCommands[1].Span[1]);
        Assert.Equal((byte)0x00, connection.TransmittedCommands[1].Span[0]);

        var baseRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            baseProtocol.SelectAsync(new byte[] { 0x01 }, TestContext.Current.CancellationToken));
        var wrappedRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(failure, baseRefusal.InnerException);
        Assert.Same(failure, wrappedRefusal.InnerException);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task ProtectedContinuationFails_ReportsOnceAndRefusesBaseAndWrapper()
    {
        var connection = new FakeSmartCardConnection { SupportsExtendedApduValue = false };
        connection.EnqueueResponse(new byte[] { 0x61, 0x01 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        var failures = new List<Exception>();
        void ReportFailure(Exception failure)
        {
            failures.Add(failure);
            baseProtocol.MarkRecoveryRequired(failure);
        }
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, ReportFailure),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(failure, Assert.Single(failures));
        Assert.Equal(2, connection.TransmittedCommands.Count);
        Assert.Equal((byte)0xC0, connection.TransmittedCommands[1].Span[1]);
        Assert.True((connection.TransmittedCommands[1].Span[0] & 0x04) != 0);
        var baseRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            baseProtocol.SelectAsync(new byte[] { 0x01 }, TestContext.Current.CancellationToken));
        var wrappedRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(failure, baseRefusal.InnerException);
        Assert.Same(failure, wrappedRefusal.InnerException);
        Assert.Single(failures);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task FirstProtectedTransmitFails_RefusesBaseAndWrapperWithoutReplay()
    {
        var connection = new FakeSmartCardConnection();
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());
        var command = new ApduCommand(0, 0xCA, 0, 0);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(command, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("No response enqueued for transmission", failure.Message);
        Assert.Single(connection.TransmittedCommands);

        var baseRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            baseProtocol.TransmitAndReceiveAsync(command, cancellationToken: TestContext.Current.CancellationToken));
        var wrappedRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(command, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(failure, baseRefusal.InnerException);
        Assert.Same(failure, wrappedRefusal.InnerException);
        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task InvalidProtectedResponseMac_RefusesNextExchangeWithoutReplay()
    {
        var connection = new FakeSmartCardConnection();
        connection.EnqueueResponse(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0x90, 0x00 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<BadResponseException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Wrong MAC", failure.Message);
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            baseProtocol.SelectAsync(new byte[] { 0x01 }, TestContext.Current.CancellationToken));
        Assert.Same(failure, refusal.InnerException);
        var wrappedRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(failure, wrappedRefusal.InnerException);
        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task FirstShortProtectedTransmitFails_RefusesNextExchangeWithoutReplay()
    {
        var connection = new FakeSmartCardConnection { SupportsExtendedApduValue = false };
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0, new byte[300]),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("No response enqueued for transmission", failure.Message);
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(failure, refusal.InnerException);
        Assert.Single(connection.TransmittedCommands);
    }

    [Fact]
    public async Task MiddleShortProtectedTransmitFails_RefusesNextExchangeWithoutReplay()
    {
        var connection = new FakeSmartCardConnection { SupportsExtendedApduValue = false };
        connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCA, 0, 0, new byte[300]),
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("No response enqueued for transmission", failure.Message);
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            baseProtocol.SelectAsync(new byte[] { 0x01 }, TestContext.Current.CancellationToken));
        Assert.Same(failure, refusal.InnerException);
        Assert.Equal(2, connection.TransmittedCommands.Count);
    }

    [Fact]
    public async Task InvalidLeAfterEncryption_RefusesNextExchangeWithoutWireTraffic()
    {
        var connection = new FakeSmartCardConnection();
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var failure = await Assert.ThrowsAsync<ArgumentException>(() => wrapped.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xCA, 0, 0, le: 65537),
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(connection.TransmittedCommands);
        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => wrapped.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xCB, 0, 0), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Same(failure, refusal.InnerException);
        Assert.Empty(connection.TransmittedCommands);
    }

    [Fact]
    public async Task ValidationBeforeCommandMac_DoesNotReportSecureFailure()
    {
        var rawProcessor = new RecordingApduProcessor(new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43));
        using var state = CreateZeroedScpState();
        var failures = new List<Exception>();
        using var processor = new ScpProcessor(rawProcessor, state, failures.Add);

        await Assert.ThrowsAsync<ArgumentException>(() => processor.TransmitAsync(
            new ApduCommand(0, 0xCA, 0, 0, le: 65537), true, false,
            TestContext.Current.CancellationToken));
        Assert.Empty(failures);
        Assert.Empty(rawProcessor.TransmittedCommandData);
        Assert.True((await processor.TransmitAsync(new ApduCommand(0, 0xCB, 0, 0), true, false,
            TestContext.Current.CancellationToken)).IsOK());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedIntermediateChunk_StopsWithoutReplay_OnlyProtectedFaults(bool protectedCommand)
    {
        var connection = new FakeSmartCardConnection { SupportsExtendedApduValue = false };
        connection.EnqueueResponse(new byte[] { 0x69, 0x85 });
        connection.EnqueueResponse(new byte[] { 0x90, 0x00 });
        var baseProtocol = new PcscProtocol(connection);
        using var state = CreateZeroedScpState();
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());
        var command = new ApduCommand(0, 0xCA, 0, 0, new byte[300]);

        var failure = await Assert.ThrowsAsync<ApduException>(() => protectedCommand
            ? wrapped.TransmitAndReceiveAsync(command, cancellationToken: TestContext.Current.CancellationToken)
            : baseProtocol.TransmitAndReceiveAsync(command, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal((short)0x6985, failure.SW);
        Assert.Single(connection.TransmittedCommands);

        if (protectedCommand)
        {
            var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                baseProtocol.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                    cancellationToken: TestContext.Current.CancellationToken));
            Assert.Same(failure, refusal.InnerException);
            var wrappedRefusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                    cancellationToken: TestContext.Current.CancellationToken));
            Assert.Same(failure, wrappedRefusal.InnerException);
            Assert.Single(connection.TransmittedCommands);
        }
        else
        {
            Assert.True((await baseProtocol.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
                cancellationToken: TestContext.Current.CancellationToken)).IsOK());
            Assert.Equal(2, connection.TransmittedCommands.Count);
        }
    }

    [Fact]
    public async Task AuthenticatedApplicationError_AllowsNextProtectedExchange()
    {
        byte[] commandKey = Convert.FromHexString("00112233445566778899AABBCCDDEEFF");
        byte[] responseKey = Convert.FromHexString("FFEEDDCCBBAA99887766554433221100");
        using var connection = new ProtectedChainingCard(commandKey, responseKey, applicationErrorFirst: true);
        var baseProtocol = new PcscProtocol(connection);
        using var state = new ScpState(
            new SessionKeys(new byte[16], commandKey, responseKey, new byte[16]), new byte[16]);
        using var wrapped = new PcscProtocolScp(baseProtocol,
            ScpInitializer.CreateSecureProcessor(baseProtocol.GetBaseCommandProcessor(), state,
                new FirmwareVersion(5, 7, 2), 0xC0, baseProtocol.MarkRecoveryRequired),
            data => data.ToArray());

        var error = await Assert.ThrowsAsync<ApduException>(() => wrapped.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xCA, 0, 0), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal((short)0x6A82, error.SW);
        Assert.True((await wrapped.TransmitAndReceiveAsync(new ApduCommand(0, 0xCB, 0, 0),
            cancellationToken: TestContext.Current.CancellationToken)).IsOK());
        Assert.Equal(new byte[] { 0xCA, 0xCB }, connection.Instructions);
    }

    [Fact]
    public async Task CancelDuringProtectedContinuation_CompletesAndPreservesCommandMacForNextExchange()
    {
        byte[] commandKey = Convert.FromHexString("00112233445566778899AABBCCDDEEFF");
        byte[] responseKey = Convert.FromHexString("FFEEDDCCBBAA99887766554433221100");
        using var connection = new ProtectedChainingCard(commandKey, responseKey);
        var baseProtocol = new PcscProtocol(connection);
        using var state = new ScpState(
            new SessionKeys(new byte[16], commandKey, responseKey, new byte[16]),
            new byte[16]);
        using var protocol = new PcscProtocolScp(
            baseProtocol,
            ScpInitializer.CreateSecureProcessor(
                baseProtocol.GetBaseCommandProcessor(), state, new FirmwareVersion(5, 7, 2), 0xC0),
            state.GetDataEncryptor());
        using var caller = new CancellationTokenSource();

        Task<ApduResponse> first = protocol.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xCA, 0, 0), cancellationToken: caller.Token);
        await connection.ContinuationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            caller.Cancel();
            Assert.False(first.IsCompleted);
        }
        finally
        {
            connection.ReleaseContinuation.SetResult();
        }

        Assert.True((await first.WaitAsync(TestContext.Current.CancellationToken)).IsOK());
        Assert.True((await protocol.TransmitAndReceiveAsync(
            new ApduCommand(0, 0xCB, 0, 0),
            cancellationToken: TestContext.Current.CancellationToken)).IsOK());
        Assert.Equal(new byte[] { 0xCA, 0xC0, 0xCB }, connection.Instructions);
    }

    // A card-side command MAC chain independent of ScpState. Empty protected responses still carry
    // an authenticated R-MAC; each command's full 16-byte C-MAC, not the transmitted 8 bytes,
    // becomes the chain for the next command (including GET RESPONSE).
    private sealed class ProtectedChainingCard(byte[] commandKey, byte[] responseKey, bool applicationErrorFirst = false) : ISmartCardConnection
    {
        private readonly byte[] _commandChain = new byte[16];
        private int _count;

        public TaskCompletionSource ContinuationStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseContinuation { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<byte> Instructions { get; } = [];
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public bool SupportsExtendedApdu() => true;

        public async Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var wire = command.Span;
            Assert.Equal((byte)0x04, wire[0]);
            Assert.Equal((byte)0x00, wire[4]);
            var dataLength = (wire[5] << 8) | wire[6];
            Assert.Equal(24, dataLength); // encrypted padding block + 8-byte command MAC
            Assert.Equal(9 + dataLength, wire.Length); // extended Lc, data, trailing Le
            var instruction = wire[1];
            Assert.Equal((applicationErrorFirst ? new byte[] { 0xCA, 0xCB } : new byte[] { 0xCA, 0xC0, 0xCB })[_count], instruction);
            using (var cmac = new AesCmac(commandKey))
            {
                cmac.AppendData(_commandChain);
                cmac.AppendData(wire[..(7 + dataLength - 8)]); // no command MAC or Le
                byte[] nextChain = cmac.GetHashAndReset();
                Assert.Equal(nextChain.AsSpan(0, 8).ToArray(), wire.Slice(7 + dataLength - 8, 8).ToArray());
                nextChain.CopyTo(_commandChain, 0);
                CryptographicOperations.ZeroMemory(nextChain);
            }

            if (_count == 1 && !applicationErrorFirst)
            {
                ContinuationStarted.SetResult();
                await ReleaseContinuation.Task.WaitAsync(cancellationToken);
            }

            _count++;
            Instructions.Add(instruction);
            byte sw1 = instruction == 0xCA ? (byte)(applicationErrorFirst ? 0x6A : 0x61) : (byte)0x90;
            byte sw2 = instruction == 0xCA ? (byte)(applicationErrorFirst ? 0x82 : 0x01) : (byte)0x00;
            using var responseMac = new AesCmac(responseKey);
            responseMac.AppendData(_commandChain);
            responseMac.AppendData([sw1, sw2]);
            byte[] fullMac = responseMac.GetHashAndReset();
            byte[] response = [.. fullMac.AsSpan(0, 8), sw1, sw2];
            CryptographicOperations.ZeroMemory(fullMac);
            return response;
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(_commandChain);
            CryptographicOperations.ZeroMemory(commandKey);
            CryptographicOperations.ZeroMemory(responseKey);
        }
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task TransmitAsync_ShortFormatter_MacInputExcludesAppendedLe()
    {
        var rawProcessor = new RecordingApduProcessor(new ApduFormatterShort());
        using var state = CreateZeroedScpState();
        using var processor = new ScpProcessor(rawProcessor, state);

        var commandData = new byte[] { 0x01, 0x02, 0x03 };
        var command = new ApduCommand(0x00, 0xCA, 0x00, 0x00, commandData, le: 0);

        await processor.TransmitAsync(command, useScp: true, encrypt: false, TestContext.Current.CancellationToken);

        var transmittedData = rawProcessor.TransmittedCommandData;
        var expectedMacInput = new byte[] { 0x04, 0xCA, 0x00, 0x00, 0x0B, 0x01, 0x02, 0x03 };
        var expectedMac = ComputeMac(expectedMacInput);
        Assert.Equal(expectedMac, transmittedData[^8..]);
    }

    [Fact]
    public async Task TransmitAsync_ExtendedFormatter_MacInputExcludesAppendedLe()
    {
        var rawProcessor = new RecordingApduProcessor(new ApduFormatterExtended(SmartCardMaxApduSizes.Yk43));
        using var state = CreateZeroedScpState();
        using var processor = new ScpProcessor(rawProcessor, state);

        var commandData = new byte[] { 0x01, 0x02, 0x03 };
        var command = new ApduCommand(0x00, 0xCA, 0x00, 0x00, commandData, le: 0);

        await processor.TransmitAsync(command, useScp: true, encrypt: false, TestContext.Current.CancellationToken);

        var transmittedData = rawProcessor.TransmittedCommandData;
        var expectedMacInput = new byte[] { 0x04, 0xCA, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x01, 0x02, 0x03 };
        var expectedMac = ComputeMac(expectedMacInput);
        Assert.Equal(expectedMac, transmittedData[^8..]);
    }

    private static ScpState CreateZeroedScpState() =>
        new(new SessionKeys(new byte[16], new byte[16], new byte[16]), new byte[16]);

    private static byte[] ComputeMac(byte[] macInput)
    {
        using var state = CreateZeroedScpState();
        return state.Mac(macInput);
    }

    private sealed class RecordingApduProcessor(IApduFormatter formatter) : IApduProcessor
    {
        public IApduFormatter Formatter { get; } = formatter;

        public byte[] TransmittedCommandData { get; private set; } = [];

        public Task<ApduResponse> TransmitAsync(
            ApduCommand command,
            bool useScp = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TransmittedCommandData = command.Data.ToArray();
            return Task.FromResult(new ApduResponse(new byte[] { 0x90, 0x00 }));
        }
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
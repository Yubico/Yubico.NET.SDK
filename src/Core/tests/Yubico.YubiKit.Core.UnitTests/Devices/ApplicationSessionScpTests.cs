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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Covers <c>ApplicationSession.InitializeProtocolAsync</c>'s SCP guard: SCP is only valid on a PC/SC SmartCard
///     protocol. This is the Core contract that backs the Phase 38 FIDO2 rule (ISC-9.1) — supplying
///     <c>scpKeyParams</c> while a non-SmartCard transport (e.g. the default HID FIDO) is selected throws,
///     rather than silently ignoring the requested secure channel.
/// </summary>
public class ApplicationSessionScpTests
{
    [Fact]
    public async Task InitializeProtocol_WithScpOnNonSmartCardProtocol_ThrowsNotSupported()
    {
        var session = new TestSession();
        using var protocol = new NonSmartCardProtocol();
        using var scp = Scp03KeyParameters.Default;

        var ex = await Assert.ThrowsAsync<NotSupportedException>(
            () => session.RunInitializeAsync(
                protocol,
                new FirmwareVersion(5, 7, 0),
                scp,
                TestContext.Current.CancellationToken));

        Assert.Contains("SmartCard", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task InitializeProtocol_WithoutScpOnNonSmartCardProtocol_Succeeds()
    {
        var session = new TestSession();
        using var protocol = new NonSmartCardProtocol();

        await session.RunInitializeAsync(
            protocol,
            new FirmwareVersion(5, 7, 0),
            scpKeyParams: null,
            TestContext.Current.CancellationToken);

        Assert.True(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task InitializeProtocol_WhenAlreadyInitialized_RejectsNullProtocol()
    {
        using var session = new TestSession();
        using var protocol = new TrackingProtocol();

        await session.RunInitializeAsync(
            protocol,
            new FirmwareVersion(5, 7, 0),
            scpKeyParams: null,
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            session.RunInitializeAsync(
                null!,
                new FirmwareVersion(5, 7, 0),
                scpKeyParams: null,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OwnedProtocol_ConfigurationFailure_IsDisposedExactlyOnce()
    {
        using var session = new TestSession();
        var expected = new InvalidOperationException("configuration failure");
        var protocol = new TrackingProtocol(configureException: expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.RunOwnedInitializeAsync(
                protocol,
                new FirmwareVersion(5, 7, 0),
                TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, protocol.DisposeCount);
    }

    [Fact]
    public async Task OwnedProtocol_CancellationDuringInitialization_IsDisposedExactlyOnce()
    {
        using var session = new TestSession();
        var protocol = new TrackingProtocol();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.RunCanceledInitializationAsync(protocol, cancellationSource.Token));

        Assert.Equal(1, protocol.DisposeCount);
    }

    [Fact]
    public async Task CleanupFailure_PreservesOriginalInitializationException()
    {
        using var session = new TestSession();
        var expected = new InvalidOperationException("initialization failure");
        var protocol = new TrackingProtocol(disposeException: new IOException("cleanup failure"));

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.RunFailingInitializationAsync(protocol, expected));

        Assert.Same(expected, actual);
        Assert.Equal(1, protocol.DisposeCount);
    }

    [Fact]
    public async Task ScpEstablishmentFailure_DisposesUnderlyingPcscProtocolExactlyOnce()
    {
        using var session = new TestSession();
        var connection = new FakeSmartCardConnection();
        var protocol = new PcscProtocol(connection);
        using var scp = Scp03KeyParameters.Default;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.RunOwnedInitializeAsync(
                protocol,
                new FirmwareVersion(5, 7, 2),
                TestContext.Current.CancellationToken,
                scp));

        Assert.Equal(1, connection.DisposeCount);
    }

    [Fact]
    public async Task SuccessfulInitialization_RetainsProtocolUntilExactlyOneSessionDisposal()
    {
        var session = new TestSession();
        var protocol = new TrackingProtocol();

        await session.RunOwnedInitializeAsync(
            protocol,
            new FirmwareVersion(5, 7, 0),
            TestContext.Current.CancellationToken);

        Assert.Equal(0, protocol.DisposeCount);

        session.Dispose();
        session.Dispose();

        Assert.Equal(1, protocol.DisposeCount);
    }

    private sealed class TestSession : ApplicationSession
    {
        public Task RunInitializeAsync(
            IProtocol protocol,
            FirmwareVersion firmwareVersion,
            ScpKeyParameters? scpKeyParams,
            CancellationToken cancellationToken) =>
            InitializeProtocolAsync(protocol, firmwareVersion, configuration: null, scpKeyParams, cancellationToken);

        public async Task RunOwnedInitializeAsync(
            IProtocol protocol,
            FirmwareVersion firmwareVersion,
            CancellationToken cancellationToken,
            ScpKeyParameters? scpKeyParams = null)
        {
            Protocol = protocol;
            try
            {
                await InitializeProtocolAsync(
                        protocol,
                        firmwareVersion,
                        configuration: null,
                        scpKeyParams,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                DisposeAfterInitializationFailure();
                throw;
            }
        }

        public async Task RunCanceledInitializationAsync(IProtocol protocol, CancellationToken cancellationToken)
        {
            Protocol = protocol;
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                DisposeAfterInitializationFailure();
                throw;
            }
        }

        public async Task RunFailingInitializationAsync(IProtocol protocol, Exception failure)
        {
            Protocol = protocol;
            try
            {
                await Task.Yield();
                throw failure;
            }
            catch
            {
                DisposeAfterInitializationFailure();
                throw;
            }
        }
    }

    private sealed class TrackingProtocol(
        Exception? configureException = null,
        Exception? disposeException = null) : IProtocol
    {
        public int DisposeCount { get; private set; }

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
            if (configureException is not null)
                throw configureException;
        }

        public void Dispose()
        {
            DisposeCount++;
            if (disposeException is not null)
                throw disposeException;
        }
    }

    // A protocol that is deliberately NOT an ISmartCardProtocol (mirrors a HID FIDO/OTP protocol for the
    // purposes of the SCP guard).
    private sealed class NonSmartCardProtocol : IProtocol
    {
        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
        }

        public void Dispose()
        {
        }
    }
}
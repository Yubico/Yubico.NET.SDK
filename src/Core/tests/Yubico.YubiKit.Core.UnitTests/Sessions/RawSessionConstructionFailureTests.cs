// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RawSessionConstructionFailureCollection
{
    public const string Name = nameof(RawSessionConstructionFailureCollection);
}

[Collection(RawSessionConstructionFailureCollection.Name)]
public class RawSessionConstructionFailureTests
{
    [Fact]
    public async Task CreateRawSmartCardSession_WhenProtocolConstructionThrows_ReleasesClaimWithoutDisposingConnection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException("Scripted SupportsExtendedApdu failure.");
        var connection = new ThrowOnceSmartCardConnection(failure);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RawSmartCardSession.CreateAsync(connection, cancellationToken));

        Assert.Same(failure, actual);
        await using (RawSmartCardSession second = await RawSmartCardSession.CreateAsync(connection, cancellationToken))
            Assert.NotNull(second);
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateRawFidoHidSession_WhenProtocolLoggerConstructionThrows_ReleasesClaimWithoutDisposingConnection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException("Scripted FIDO protocol logger failure.");
        var connection = new ReusableFidoConnection();

        using (YubiKitLogging.UseTemporary(new ThrowOnSecondLoggerFactory(failure)))
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => RawFidoHidSession.CreateAsync(connection, cancellationToken));
            Assert.Same(failure, actual);
        }

        await using (RawFidoHidSession second = await RawFidoHidSession.CreateAsync(connection, cancellationToken))
            Assert.NotNull(second);
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateRawOtpHidSession_WhenProtocolLoggerConstructionThrows_ReleasesClaimWithoutDisposingConnection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException("Scripted OTP protocol logger failure.");
        var connection = new ReusableOtpConnection();

        using (YubiKitLogging.UseTemporary(new ThrowOnSecondLoggerFactory(failure)))
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => RawOtpHidSession.CreateAsync(connection, cancellationToken));
            Assert.Same(failure, actual);
        }

        await using (RawOtpHidSession second = await RawOtpHidSession.CreateAsync(connection, cancellationToken))
            Assert.NotNull(second);
        Assert.Equal(0, connection.DisposeCount);
    }

    private sealed class ThrowOnSecondLoggerFactory(Exception failure) : ILoggerFactory
    {
        private int _createCount;

        public ILogger CreateLogger(string categoryName)
        {
            if (Interlocked.Increment(ref _createCount) == 2)
                throw failure;

            return NullLogger.Instance;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class ThrowOnceSmartCardConnection(Exception failure) : ISmartCardConnection
    {
        private int _supportsCallCount;

        public int DisposeCount { get; private set; }
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0x90, 0x00 });

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu()
        {
            if (Interlocked.Increment(ref _supportsCallCount) == 1)
                throw failure;

            return true;
        }

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ReusableFidoConnection : IFidoHidConnection
    {
        public int DisposeCount { get; private set; }
        public int PacketSize => 64;
        public ConnectionType Type => ConnectionType.HidFido;

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ReusableOtpConnection : IOtpHidConnection
    {
        public int DisposeCount { get; private set; }
        public int FeatureReportSize => 8;
        public ConnectionType Type => ConnectionType.HidOtp;

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
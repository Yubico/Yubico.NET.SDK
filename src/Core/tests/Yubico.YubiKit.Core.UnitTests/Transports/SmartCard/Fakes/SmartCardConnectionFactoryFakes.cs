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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard.Fakes;

internal sealed class SuccessfulSmartCardConnectionFactory : ISmartCardConnectionFactory
{
    private int _createCalls;
    public int CreateCalls => Volatile.Read(ref _createCalls);

    public Task<ISmartCardConnection> CreateAsync(
        IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default)
    {
        _ = Interlocked.Increment(ref _createCalls);
        return Task.FromResult<ISmartCardConnection>(new InertSmartCardConnection());
    }
}

internal sealed class FailOnceSmartCardConnectionFactory(Exception firstFailure) : ISmartCardConnectionFactory
{
    private int _createCalls;
    public int CreateCalls => Volatile.Read(ref _createCalls);

    public Task<ISmartCardConnection> CreateAsync(
        IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default) =>
        Interlocked.Increment(ref _createCalls) == 1
            ? Task.FromException<ISmartCardConnection>(firstFailure)
            : Task.FromResult<ISmartCardConnection>(new InertSmartCardConnection());
}

internal sealed class AlwaysFailingSmartCardConnectionFactory(Exception failure) : ISmartCardConnectionFactory
{
    public Task<ISmartCardConnection> CreateAsync(
        IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default) =>
        Task.FromException<ISmartCardConnection>(failure);
}

internal sealed class DelegatingSmartCardConnectionFactory(ISmartCardConnectionFactory inner)
    : ISmartCardConnectionFactory
{
    public Task<ISmartCardConnection> CreateAsync(
        IPcscDevice smartCardDevice,
        CancellationToken cancellationToken = default) =>
        inner.CreateAsync(smartCardDevice, cancellationToken);
}

internal sealed class InertSmartCardConnection : ISmartCardConnection
{
    public ConnectionType Type => ConnectionType.SmartCard;
    public Transport Transport => Transport.Usb;
    public bool SupportsExtendedApdu() => true;
    public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
        ReadOnlyMemory<byte> command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ReadOnlyMemory<byte>>(new byte[] { 0x90, 0x00 });
    public void Dispose()
    {
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class ThrowingLoggerFactory(Exception? failure = null) : ILoggerFactory
{
    private int _createCalls;

    public int CreateCalls => Volatile.Read(ref _createCalls);

    public void AddProvider(ILoggerProvider provider)
    {
    }
    public ILogger CreateLogger(string categoryName)
    {
        _ = Interlocked.Increment(ref _createCalls);
        throw failure ?? new InvalidOperationException("logger creation failed");
    }
    public void Dispose()
    {
    }
}

internal sealed class ThrowingLogLoggerFactory(int throwingLogCall) : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName) => new ThrowingLogLogger(throwingLogCall);

    public void Dispose()
    {
    }
}

internal sealed class ThrowingLogLogger(int throwingLogCall) : ILogger
{
    private int _logCalls;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Debug && Interlocked.Increment(ref _logCalls) == throwingLogCall)
            throw new InvalidOperationException("initialization log failed");
    }
}
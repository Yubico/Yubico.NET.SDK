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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public class ApplicationSessionDisposalTests
{
    [Fact]
    public void Dispose_Repeated_EntersManagedCleanupExactlyOnce()
    {
        var session = new ProbeSession(new TrackingConnection());

        session.Dispose();
        session.Dispose();

        Assert.Equal(1, session.ManagedCleanupCount);
        Assert.Equal(0, session.AsyncCleanupCount);
    }

    [Fact]
    public async Task DisposeAsync_Repeated_EntersCleanupExactlyOnce()
    {
        var session = new ProbeSession(new TrackingConnection());

        await session.DisposeAsync();
        await session.DisposeAsync();

        Assert.Equal(1, session.ManagedCleanupCount);
        Assert.Equal(1, session.AsyncCleanupCount);
    }

    [Fact]
    public async Task DisposeAsync_ThenDispose_EntersCleanupExactlyOnce()
    {
        var session = new ProbeSession(new TrackingConnection());

        await session.DisposeAsync();
        session.Dispose();

        Assert.Equal(1, session.ManagedCleanupCount);
        Assert.Equal(1, session.AsyncCleanupCount);
    }

    [Fact]
    public async Task Dispose_ConcurrentWithDisposeAsync_WaitsForWinnerAndEntersCleanupExactlyOnce()
    {
        var asyncCleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowAsyncCleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loserStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new ProbeSession(
            new TrackingConnection(),
            asyncCleanupEntered,
            allowAsyncCleanup);

        Task winner = session.DisposeAsync().AsTask();
        await asyncCleanupEntered.Task.WaitAsync(TestContext.Current.CancellationToken);

        Task loser = Task.Run(
            () =>
            {
                loserStarted.SetResult();
                session.Dispose();
            },
            TestContext.Current.CancellationToken);
        await loserStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.False(loser.IsCompleted);

        allowAsyncCleanup.SetResult();
        await Task.WhenAll(winner, loser);

        Assert.Equal(1, session.ManagedCleanupCount);
        Assert.Equal(1, session.AsyncCleanupCount);
    }

    [Fact]
    public async Task DisposeAsync_OwnedConnectionFailure_RunsManagedCleanupOnceAndSharesException()
    {
        var expected = new InvalidOperationException("async connection teardown failed");
        var connection = new TrackingConnection(expected);
        var session = new ProbeSession(connection, ownsConnection: true);

        Exception? first = await Record.ExceptionAsync(async () => await session.DisposeAsync());
        Exception? second = await Record.ExceptionAsync(async () => await session.DisposeAsync());
        Exception? third = Record.Exception(session.Dispose);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Same(expected, third);
        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, session.AsyncCleanupCount);
        Assert.Equal(1, session.ManagedCleanupCount);
    }

    [Fact]
    public async Task Dispose_OwnedConnectionFailure_IsSharedAndLeavesSessionTerminal()
    {
        var expected = new InvalidOperationException("sync connection teardown failed");
        var connection = new TrackingConnection(disposeException: expected);
        var session = new ProbeSession(connection, ownsConnection: true);

        Exception? first = Record.Exception(session.Dispose);
        Exception? second = Record.Exception(session.Dispose);
        Exception? third = await Record.ExceptionAsync(async () => await session.DisposeAsync());

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Same(expected, third);
        Assert.Equal(1, connection.DisposeCount);
        Assert.Equal(0, connection.DisposeAsyncCount);
        Assert.Equal(1, session.ManagedCleanupCount);
        Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);
    }

    [Fact]
    public async Task DisposeAsync_ThrowingProtocol_DisposesProtocolOnceAndLeavesSessionTerminal()
    {
        var expected = new InvalidOperationException("protocol teardown failed");
        var protocol = new ThrowingProtocol(expected);
        var session = new ProbeSession(new TrackingConnection(), protocol: protocol);

        Exception? first = await Record.ExceptionAsync(async () => await session.DisposeAsync());
        Exception? second = Record.Exception(session.Dispose);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Equal(1, protocol.DisposeCount);
        Assert.Equal(1, session.AsyncCleanupCount);
        Assert.Equal(1, session.ManagedCleanupCount);
        Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);
    }

    [Fact]
    public void Dispose_ThrowingProtocol_ReleasesOwnedConnectionAndDetachesSession()
    {
        var expected = new InvalidOperationException("protocol teardown failed");
        var protocol = new ThrowingProtocol(expected);
        var connection = new TrackingConnection();
        var session = new ProbeSession(connection, ownsConnection: true, protocol: protocol);

        Exception? exception = Record.Exception(session.Dispose);

        Assert.Same(expected, exception);
        Assert.Equal(1, protocol.DisposeCount);
        Assert.Equal(1, connection.DisposeCount);
        Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);

        // TrackingConnection deliberately remains usable after counting Dispose so this probe isolates
        // ConnectionSessionGuard detachment rather than modeling a real native handle after disposal.
        using var subsequent = new ProbeSession(connection);
        subsequent.AssertNotDisposed();
    }

    [Fact]
    public async Task DisposeAsync_ThrowingProtocol_ReleasesOwnedConnectionAsynchronouslyAndDetachesSession()
    {
        var expected = new InvalidOperationException("protocol teardown failed");
        var protocol = new ThrowingProtocol(expected);
        var connection = new TrackingConnection();
        var session = new ProbeSession(connection, ownsConnection: true, protocol: protocol);

        Exception? exception = await Record.ExceptionAsync(async () => await session.DisposeAsync());

        Assert.Same(expected, exception);
        Assert.Equal(1, protocol.DisposeCount);
        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);

        // TrackingConnection deliberately remains usable after counting disposal so successful construction
        // proves the first session detached from ConnectionSessionGuard.
        using var subsequent = new ProbeSession(connection);
        subsequent.AssertNotDisposed();
    }

    private sealed class ProbeSession : ApplicationSession
    {
        private readonly TaskCompletionSource? _asyncCleanupEntered;
        private readonly TaskCompletionSource? _allowAsyncCleanup;
        private int _asyncCleanupCount;
        private int _managedCleanupCount;

        public ProbeSession(
            IConnection connection,
            TaskCompletionSource? asyncCleanupEntered = null,
            TaskCompletionSource? allowAsyncCleanup = null,
            bool ownsConnection = false,
            IProtocol? protocol = null)
            : base(connection)
        {
            _asyncCleanupEntered = asyncCleanupEntered;
            _allowAsyncCleanup = allowAsyncCleanup;
            Protocol = protocol;

            if (ownsConnection)
                OwnConnection();
        }

        public int AsyncCleanupCount => Volatile.Read(ref _asyncCleanupCount);

        public int ManagedCleanupCount => Volatile.Read(ref _managedCleanupCount);

        public void AssertNotDisposed() => ThrowIfDisposed();

        protected override void Dispose(bool disposing)
        {
            _ = Interlocked.Increment(ref _managedCleanupCount);
            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            _ = Interlocked.Increment(ref _asyncCleanupCount);
            _asyncCleanupEntered?.SetResult();

            if (_allowAsyncCleanup is not null)
                await _allowAsyncCleanup.Task.ConfigureAwait(false);

            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
    }

    private sealed class TrackingConnection(
        Exception? asyncDisposeException = null,
        Exception? disposeException = null) : IConnection
    {
        private int _disposeAsyncCount;
        private int _disposeCount;

        public int DisposeAsyncCount => Volatile.Read(ref _disposeAsyncCount);

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ConnectionType Type => ConnectionType.SmartCard;

        public void Dispose()
        {
            _ = Interlocked.Increment(ref _disposeCount);

            if (disposeException is not null)
                throw disposeException;
        }

        public ValueTask DisposeAsync()
        {
            _ = Interlocked.Increment(ref _disposeAsyncCount);
            return asyncDisposeException is null
                ? ValueTask.CompletedTask
                : new ValueTask(Task.FromException(asyncDisposeException));
        }
    }

    private sealed class ThrowingProtocol(Exception disposeException) : IProtocol
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
        }

        public void Dispose()
        {
            _ = Interlocked.Increment(ref _disposeCount);
            throw disposeException;
        }
    }
}
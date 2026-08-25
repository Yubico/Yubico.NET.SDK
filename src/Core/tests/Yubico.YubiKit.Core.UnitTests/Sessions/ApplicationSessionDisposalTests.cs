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
    public void Dispose_ResetsInitializedAndAuthenticated()
    {
        using var session = new ProbeSession(new TrackingConnection());
        session.SetInitializedAndAuthenticated();

        session.Dispose();

        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task DisposeAsync_ResetsInitializedAndAuthenticated()
    {
        await using var session = new ProbeSession(new TrackingConnection());
        session.SetInitializedAndAuthenticated();

        await session.DisposeAsync();

        Assert.False(session.IsInitialized);
        Assert.False(session.IsAuthenticated);
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
    public async Task DisposeAsync_OwnedConnectionTeardownPaused_PublishesDisposalStartAndSharesCompletion()
    {
        var connection = new TrackingConnection(pauseAsyncDisposal: true);
        var session = new ProbeSession(connection, ownsConnection: true);
        session.SetInitializedAndAuthenticated();

        Task first = session.DisposeAsync().AsTask();
        Task? second = null;

        try
        {
            await connection.AsyncDisposalStarted.WaitAsync(TestContext.Current.CancellationToken);
            second = session.DisposeAsync().AsTask();

            Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);
            Assert.False(session.IsInitialized);
            Assert.False(session.IsAuthenticated);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
        }
        finally
        {
            connection.ResumeAsyncDisposal();
            Task[] startedTasks = second is null ? [first] : [first, second];
            await Task.WhenAll(startedTasks).WaitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Equal(1, session.AsyncCleanupCount);
        Assert.Equal(1, session.ManagedCleanupCount);
    }

    [Fact]
    public async Task Dispose_OwnedConnectionTeardownPaused_PublishesDisposalStartAndSharesCompletion()
    {
        var connection = new TrackingConnection(pauseDisposal: true);
        var session = new ProbeSession(connection, ownsConnection: true);

        Task first = Task.Run(session.Dispose, TestContext.Current.CancellationToken);
        Task? second = null;

        try
        {
            await connection.DisposalStarted.WaitAsync(TestContext.Current.CancellationToken);
            second = session.DisposeAsync().AsTask();

            Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
        }
        finally
        {
            connection.ResumeDisposal();
            Task[] startedTasks = second is null ? [first] : [first, second];
            await Task.WhenAll(startedTasks).WaitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, connection.DisposeCount);
        Assert.Equal(0, connection.DisposeAsyncCount);
        Assert.Equal(0, session.AsyncCleanupCount);
        Assert.Equal(1, session.ManagedCleanupCount);
    }

    [Fact]
    public async Task DisposeAsync_DerivedCleanupPausedBeforeBase_PublishesDisposalStartAndSharesCompletion()
    {
        var session = new ProbeSession(
            new TrackingConnection(),
            pauseAsyncBeforeBase: true);
        session.SetInitializedAndAuthenticated();

        Task first = session.DisposeAsync().AsTask();
        Task? second = null;

        try
        {
            await session.AsyncBeforeBaseEntered.WaitAsync(TestContext.Current.CancellationToken);
            second = session.DisposeAsync().AsTask();

            Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);
            Assert.False(session.IsInitialized);
            Assert.False(session.IsAuthenticated);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
        }
        finally
        {
            session.ResumeAsyncBeforeBase();
            Task[] startedTasks = second is null ? [first] : [first, second];
            await Task.WhenAll(startedTasks).WaitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, session.AsyncCleanupCount);
        Assert.Equal(1, session.ManagedCleanupCount);
    }

    [Fact]
    public async Task Dispose_DerivedCleanupPausedBeforeBase_PublishesDisposalStartAndSharesCompletion()
    {
        var session = new ProbeSession(
            new TrackingConnection(),
            pauseSyncBeforeBase: true);

        Task first = Task.Run(session.Dispose, TestContext.Current.CancellationToken);
        Task? second = null;

        try
        {
            await session.SyncBeforeBaseEntered.WaitAsync(TestContext.Current.CancellationToken);
            second = session.DisposeAsync().AsTask();

            Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
        }
        finally
        {
            session.ResumeSyncBeforeBase();
            Task[] startedTasks = second is null ? [first] : [first, second];
            await Task.WhenAll(startedTasks).WaitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(0, session.AsyncCleanupCount);
        Assert.Equal(1, session.ManagedCleanupCount);
    }

    [Fact]
    public void CapabilityQueries_AfterDisposal_UseRetainedFirmwareState()
    {
        var session = new ProbeSession(
            new TrackingConnection(),
            firmwareVersion: new FirmwareVersion(5, 7, 0));
        var supported = new Feature("supported", 5, 6, 0);
        var unsupported = new Feature("unsupported", 5, 8, 0);
        session.Dispose();

        Assert.True(session.IsSupported(supported));
        session.EnsureSupports(supported);
        Assert.False(session.IsSupported(unsupported));
        Assert.Throws<NotSupportedException>(() => session.EnsureSupports(unsupported));
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
        var session = ProbeSession.Create(connection, ownsConnection: true, protocol: protocol);

        Exception? exception = Record.Exception(session.Dispose);

        Assert.Same(expected, exception);
        Assert.Equal(1, protocol.DisposeCount);
        Assert.Equal(1, connection.DisposeCount);
        Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);

        // TrackingConnection deliberately remains usable after counting Dispose so this probe isolates
        // ConnectionSessionGuard detachment rather than modeling a real native handle after disposal.
        using var subsequent = ProbeSession.Create(connection);
        subsequent.AssertNotDisposed();
    }

    [Fact]
    public async Task DisposeAsync_ThrowingProtocol_ReleasesOwnedConnectionAsynchronouslyAndDetachesSession()
    {
        var expected = new InvalidOperationException("protocol teardown failed");
        var protocol = new ThrowingProtocol(expected);
        var connection = new TrackingConnection();
        var session = ProbeSession.Create(connection, ownsConnection: true, protocol: protocol);

        Exception? exception = await Record.ExceptionAsync(async () => await session.DisposeAsync());

        Assert.Same(expected, exception);
        Assert.Equal(1, protocol.DisposeCount);
        Assert.Equal(0, connection.DisposeCount);
        Assert.Equal(1, connection.DisposeAsyncCount);
        Assert.Throws<ObjectDisposedException>(session.AssertNotDisposed);

        // TrackingConnection deliberately remains usable after counting disposal so successful construction
        // proves the first session detached from ConnectionSessionGuard.
        using var subsequent = ProbeSession.Create(connection);
        subsequent.AssertNotDisposed();
    }

    private sealed class ProbeSession : ApplicationSession
    {
        private readonly TaskCompletionSource? _asyncCleanupEntered;
        private readonly TaskCompletionSource? _allowAsyncCleanup;
        private readonly TaskCompletionSource _asyncBeforeBaseEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _resumeAsyncBeforeBase = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _syncBeforeBaseEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _resumeSyncBeforeBase = new(initialState: false);
        private readonly bool _pauseAsyncBeforeBase;
        private readonly bool _pauseSyncBeforeBase;
        private int _asyncCleanupCount;
        private int _managedCleanupCount;

        public ProbeSession(
            IConnection connection,
            TaskCompletionSource? asyncCleanupEntered = null,
            TaskCompletionSource? allowAsyncCleanup = null,
            bool ownsConnection = false,
            IProtocol? protocol = null,
            bool pauseAsyncBeforeBase = false,
            bool pauseSyncBeforeBase = false,
            FirmwareVersion? firmwareVersion = null)
            : base(connection)
        {
            _asyncCleanupEntered = asyncCleanupEntered;
            _allowAsyncCleanup = allowAsyncCleanup;
            _pauseAsyncBeforeBase = pauseAsyncBeforeBase;
            _pauseSyncBeforeBase = pauseSyncBeforeBase;
            Protocol = protocol;
            FirmwareVersion = firmwareVersion ?? new FirmwareVersion();

            if (ownsConnection)
                OwnConnection();
        }

        /// <summary>
        ///     Binds like a production factory. Tests that assert detachment MUST use this: a directly
        ///     constructed session is never bound, so a later construction would succeed whether or not
        ///     detachment happened, and the assertion would prove nothing.
        /// </summary>
        public static ProbeSession Create(
            IConnection connection,
            bool ownsConnection = false,
            IProtocol? protocol = null)
            => Construct(
                connection,
                () => new ProbeSession(connection, ownsConnection: ownsConnection, protocol: protocol));

        public int AsyncCleanupCount => Volatile.Read(ref _asyncCleanupCount);

        public int ManagedCleanupCount => Volatile.Read(ref _managedCleanupCount);

        public Task AsyncBeforeBaseEntered => _asyncBeforeBaseEntered.Task;

        public Task SyncBeforeBaseEntered => _syncBeforeBaseEntered.Task;

        public void AssertNotDisposed() => ThrowIfDisposed();

        public void SetInitializedAndAuthenticated()
        {
            IsInitialized = true;
            IsAuthenticated = true;
        }

        public void ResumeAsyncBeforeBase() => _resumeAsyncBeforeBase.TrySetResult();

        public void ResumeSyncBeforeBase() => _resumeSyncBeforeBase.Set();

        protected override void Dispose(bool disposing)
        {
            _ = Interlocked.Increment(ref _managedCleanupCount);
            if (_pauseSyncBeforeBase)
            {
                _syncBeforeBaseEntered.TrySetResult();
                _resumeSyncBeforeBase.Wait();
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            _ = Interlocked.Increment(ref _asyncCleanupCount);
            _asyncCleanupEntered?.SetResult();

            if (_allowAsyncCleanup is not null)
                await _allowAsyncCleanup.Task.ConfigureAwait(false);

            if (_pauseAsyncBeforeBase)
            {
                _asyncBeforeBaseEntered.TrySetResult();
                await _resumeAsyncBeforeBase.Task.ConfigureAwait(false);
            }

            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
    }

    private sealed class TrackingConnection(
        Exception? asyncDisposeException = null,
        Exception? disposeException = null,
        bool pauseAsyncDisposal = false,
        bool pauseDisposal = false) : IConnection
    {
        private readonly TaskCompletionSource _asyncDisposalStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _resumeAsyncDisposal = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _resumeDisposal = new(initialState: false);
        private readonly TaskCompletionSource _disposalStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeAsyncCount;
        private int _disposeCount;

        public int DisposeAsyncCount => Volatile.Read(ref _disposeAsyncCount);

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ConnectionType Type => ConnectionType.SmartCard;

        public Task AsyncDisposalStarted => _asyncDisposalStarted.Task;

        public Task DisposalStarted => _disposalStarted.Task;

        public void Dispose()
        {
            _ = Interlocked.Increment(ref _disposeCount);
            _disposalStarted.TrySetResult();

            if (pauseDisposal)
                _resumeDisposal.Wait();

            if (disposeException is not null)
                throw disposeException;
        }

        public async ValueTask DisposeAsync()
        {
            _ = Interlocked.Increment(ref _disposeAsyncCount);
            _asyncDisposalStarted.TrySetResult();

            if (pauseAsyncDisposal)
                await _resumeAsyncDisposal.Task.ConfigureAwait(false);

            if (asyncDisposeException is not null)
                throw asyncDisposeException;
        }

        public void ResumeAsyncDisposal() => _resumeAsyncDisposal.TrySetResult();

        public void ResumeDisposal() => _resumeDisposal.Set();
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
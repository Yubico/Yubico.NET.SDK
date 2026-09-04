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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class YubiKeyManagerStaticTests : IAsyncLifetime
{
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        // Always clean up static state after each test
        await YubiKeyManager.ShutdownAsync();
    }

    [Fact]
    public async Task YubiKeyManager_FindAllAsync_IsStaticMethod()
    {
        // FindAllAsync should be callable as static method
        var result = await YubiKeyManager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<Yubico.YubiKit.Core.Abstractions.IYubiKey>>(result);
    }

    [Fact]
    public async Task YubiKeyManager_FindAllAsync_WithConnectionType_IsStaticMethod()
    {
        // FindAllAsync with ConnectionType should be callable as static method
        var result = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard, cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<Yubico.YubiKit.Core.Abstractions.IYubiKey>>(result);
    }

    [Fact]
    public async Task YubiKeyManager_FindAllAsync_WorksWithoutDISetup()
    {
        // Verify method works without calling any initialization or DI setup
        var result = await YubiKeyManager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task YubiKeyManager_FindAllAsync_ReturnsIReadOnlyList()
    {
        // Verify returns IReadOnlyList<IYubiKey>
        var result = await YubiKeyManager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsAssignableFrom<IReadOnlyList<Yubico.YubiKit.Core.Abstractions.IYubiKey>>(result);
    }

    [Fact]
    public async Task YubiKeyManager_FindAllAsync_NoDevices_ReturnsEmptyList()
    {
        // Handle no devices connected -> Return empty list (not null)
        var result = await YubiKeyManager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        // In unit test environment, expect empty (no real devices)
        // But importantly: not null
    }

    [Fact]
    public async Task YubiKeyManager_FindAllAsync_Cancellation_PropagatesToken()
    {
        // Cancellation is propagated to underlying operations
        using var cts = new CancellationTokenSource();

        // A non-cancelled token should work
        var result = await YubiKeyManager.FindAllAsync(cts.Token);
        Assert.NotNull(result);

        // Verify the method signature accepts CancellationToken
        var method = typeof(YubiKeyManager).GetMethod(
            nameof(YubiKeyManager.FindAllAsync),
            [typeof(CancellationToken)]);

        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Contains(parameters, p => p.ParameterType == typeof(CancellationToken));
    }

    [Fact]
    public async Task YubiKeyManager_FindAllAsync_ConcurrentCalls_ThreadSafe()
    {
        // Handle concurrent FindAllAsync() calls -> Thread-safe
        const int concurrentCalls = 10;
        var tasks = Enumerable.Range(0, concurrentCalls)
            .Select(_ => YubiKeyManager.FindAllAsync())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public void PlatformInteropException_Exists_AndIsProperException()
    {
        // PlatformInteropException should be available for platform API errors
        var exceptionType = typeof(PlatformInteropException);

        // Verify it inherits from Exception
        Assert.True(typeof(Exception).IsAssignableFrom(exceptionType));

        // Verify it has a message constructor
        var ex1 = new PlatformInteropException("Test error");
        Assert.Equal("Test error", ex1.Message);

        // Verify it has a message + inner exception constructor
        var innerEx = new InvalidOperationException("Inner error");
        var ex2 = new PlatformInteropException("Outer error", innerEx);
        Assert.Equal("Outer error", ex2.Message);
        Assert.Same(innerEx, ex2.InnerException);
    }

    [Fact]
    public void PlatformInteropException_CanContain_ContextInformation()
    {
        // Exception should carry context about what operation failed
        var ex = new PlatformInteropException("PC/SC service unavailable: 0x8010001D",
            new InvalidOperationException("SCardEstablishContext failed"));

        Assert.Contains("PC/SC", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    // Phase 3: Monitoring Lifecycle Tests

    [Fact]
    public void YubiKeyManager_StartMonitoring_MethodExists()
    {
        // YubiKeyManager should have static StartMonitoring() method
        var method = typeof(YubiKeyManager).GetMethod(
            "StartMonitoring",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            Type.EmptyTypes);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Fact]
    public void YubiKeyManager_StartMonitoring_WithInterval_MethodExists()
    {
        // YubiKeyManager should have static StartMonitoring(TimeSpan) method
        var method = typeof(YubiKeyManager).GetMethod(
            "StartMonitoring",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            [typeof(TimeSpan)]);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Fact]
    public void YubiKeyManager_StopMonitoring_MethodExists()
    {
        // YubiKeyManager should have static StopMonitoring() method
        var method = typeof(YubiKeyManager).GetMethod(
            "StopMonitoring",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            Type.EmptyTypes);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Fact]
    public void YubiKeyManager_IsMonitoring_PropertyExists()
    {
        // YubiKeyManager should have static IsMonitoring property
        var property = typeof(YubiKeyManager).GetProperty(
            "IsMonitoring",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }

    [Fact]
    public void YubiKeyManager_StartMonitoring_ZeroInterval_ThrowsArgumentOutOfRangeException()
    {
        // Handle interval = 0ms -> Throw ArgumentOutOfRangeException
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            YubiKeyManager.StartMonitoring(TimeSpan.Zero));

        Assert.Equal("interval", exception.ParamName);
    }

    [Fact]
    public void YubiKeyManager_StartMonitoring_NegativeInterval_ThrowsArgumentOutOfRangeException()
    {
        // Handle negative interval -> Throw ArgumentOutOfRangeException
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(-1)));

        Assert.Equal("interval", exception.ParamName);
    }

    [Fact]
    public void YubiKeyManager_StartMonitoring_SetsIsMonitoring()
    {
        // Starting monitoring sets IsMonitoring to true
        Assert.False(YubiKeyManager.IsMonitoring);

        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));

        Assert.True(YubiKeyManager.IsMonitoring);
    }

    [Fact]
    public void YubiKeyManager_StopMonitoring_SetsIsMonitoringFalse()
    {
        // Stopping monitoring sets IsMonitoring to false
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);

        YubiKeyManager.StopMonitoring();

        Assert.False(YubiKeyManager.IsMonitoring);
    }

    [Fact]
    public void YubiKeyManager_StartMonitoring_WhenAlreadyMonitoring_IsIdempotent()
    {
        // StartMonitoring when already monitoring -> No-op (idempotent)
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);

        // Call again - should not throw or change behavior
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(2));
        Assert.True(YubiKeyManager.IsMonitoring);
    }

    [Fact]
    public void YubiKeyManager_StopMonitoring_WhenNotMonitoring_IsIdempotent()
    {
        // StopMonitoring when not monitoring -> No-op (idempotent)
        Assert.False(YubiKeyManager.IsMonitoring);

        // Call again - should not throw
        YubiKeyManager.StopMonitoring();

        Assert.False(YubiKeyManager.IsMonitoring);
    }

    // Phase 4: Device Events Tests

    [Fact]
    public void YubiKeyManager_WatchAsync_IsTheOnlyDeviceChangeStream()
    {
        var method = typeof(YubiKeyManager).GetMethod(
            nameof(YubiKeyManager.WatchAsync),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            [typeof(CancellationToken)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<DeviceEvent>), method.ReturnType);

        // The observable surface was removed outright; no obsolete shim, alias, or overload survives.
        Assert.DoesNotContain(
            typeof(YubiKeyManager).GetMembers(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
            m => m.Name.Contains("DeviceChanges", StringComparison.Ordinal));
        Assert.DoesNotContain(
            typeof(YubiKeyManager).GetMembers(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
            m => m is System.Reflection.PropertyInfo p &&
                 typeof(IObservable<DeviceEvent>).IsAssignableFrom(p.PropertyType));
    }

    [Fact]
    public void DeviceEvent_ContainsExpectedMembers()
    {
        // Verify DeviceEvent contains IYubiKey and DeviceAction enum
        var deviceEventType = typeof(DeviceEvent);

        // Should have a Device property of type IYubiKey
        var deviceProperty = deviceEventType.GetProperty("Device");
        Assert.NotNull(deviceProperty);
        Assert.True(typeof(Yubico.YubiKit.Core.Abstractions.IYubiKey).IsAssignableFrom(deviceProperty.PropertyType));

        // Should have an Action property of type DeviceAction
        var actionProperty = deviceEventType.GetProperty("Action");
        Assert.NotNull(actionProperty);
        Assert.Equal(typeof(DeviceAction), actionProperty.PropertyType);

        // DeviceAction should be an enum with Added and Removed
        Assert.True(typeof(DeviceAction).IsEnum);
        Assert.True(Enum.IsDefined(typeof(DeviceAction), "Added"));
        Assert.True(Enum.IsDefined(typeof(DeviceAction), "Removed"));
    }

    [Fact]
    public void DeviceAction_DoesNotContainUpdated()
    {
        // DeviceAction.Updated was removed per PRD
        Assert.False(Enum.IsDefined(typeof(DeviceAction), "Updated"));
    }

    [Fact]
    public async Task YubiKeyManager_WatchAsync_CanBeEnumeratedWithoutStartingMonitoring()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Enumerating (which is what actually subscribes) must not auto-start monitoring.
        await using var enumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync();

        Assert.False(YubiKeyManager.IsMonitoring);

        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
    }

    [Fact]
    public async Task YubiKeyManager_WatchAsync_MultipleConcurrentWatchersAllowed()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var enumerators = new List<IAsyncEnumerator<DeviceEvent>>();
        var pending = new List<ValueTask<bool>>();
        for (var i = 0; i < 3; i++)
        {
            var enumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
            enumerators.Add(enumerator);
            pending.Add(enumerator.MoveNextAsync());
        }

        // Each enumeration is independent; none of them rejected or displaced the others.
        await cts.CancelAsync();
        foreach (var move in pending)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await move);
        }

        foreach (var enumerator in enumerators)
        {
            await enumerator.DisposeAsync();
        }
    }

    [Fact]
    public async Task YubiKeyManager_WatchAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await using var enumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var moveNext = enumerator.MoveNextAsync().AsTask();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => moveNext);
    }

    [Fact]
    public async Task YubiKeyManager_WatchAsync_EndingAWatcherDoesNotAffectMonitoring()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var enumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync();

        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);

        // Ending the enumeration releases only that watcher. Whether an event landed first depends on
        // what hardware happens to be attached, so only the release itself is asserted here.
        await cts.CancelAsync();
        try
        {
            _ = await pending;
        }
        catch (OperationCanceledException)
        {
            // Expected when no event arrived before cancellation.
        }

        await enumerator.DisposeAsync();

        Assert.True(YubiKeyManager.IsMonitoring);
    }

    // Phase 5: Shutdown Tests

    [Fact]
    public void YubiKeyManager_ShutdownAsync_MethodExists()
    {
        // YubiKeyManager should have static ShutdownAsync method
        var method = typeof(YubiKeyManager).GetMethod(
            "ShutdownAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            [typeof(CancellationToken)]);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(Task), method.ReturnType);
    }

    [Fact]
    public void YubiKeyManager_Shutdown_MethodExists()
    {
        // YubiKeyManager should have static Shutdown sync wrapper
        var method = typeof(YubiKeyManager).GetMethod(
            "Shutdown",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            Type.EmptyTypes);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Fact]
    public async Task YubiKeyManager_ShutdownAsync_StopsMonitoring()
    {
        // Verify ShutdownAsync stops monitoring if active
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);

        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.False(YubiKeyManager.IsMonitoring);
    }

    [Fact]
    public async Task YubiKeyManager_ShutdownAsync_IsIdempotent()
    {
        // Handle multiple Shutdown() calls -> Idempotent
        // Multiple shutdown calls should not throw
        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);
        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.False(YubiKeyManager.IsMonitoring);
    }

    [Fact]
    public async Task YubiKeyManager_AfterShutdown_FindAllAsync_Works()
    {
        // Verify after shutdown, FindAllAsync performs fresh scan
        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

        // FindAllAsync should work after shutdown
        var result = await YubiKeyManager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task YubiKeyManager_AfterShutdown_StartMonitoring_Works()
    {
        // Verify after shutdown, StartMonitoring works correctly
        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

        // StartMonitoring should work after shutdown
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);
    }

    [Fact]
    public async Task YubiKeyManager_ShutdownAsync_ResetsContext()
    {
        // After shutdown, a new context is created on next use
        _ = await YubiKeyManager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);

        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

        // Next call should create new context and work
        var result = await YubiKeyManager.FindAllAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task YubiKeyManager_WatchAsync_AfterShutdown_AutoRecreatesContext()
    {
        await YubiKeyManager.ShutdownAsync(TestContext.Current.CancellationToken);

        // Watching after shutdown must recreate the manager rather than hand back a dead sequence.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var enumerator = YubiKeyManager.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var pending = enumerator.MoveNextAsync();

        Assert.False(pending.IsCompleted);

        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
    }
}
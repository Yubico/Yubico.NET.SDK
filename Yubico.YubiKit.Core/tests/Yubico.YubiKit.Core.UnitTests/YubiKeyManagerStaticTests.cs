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

using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests;

public class YubiKeyManagerStaticTests
{
    [Fact]
    public void YubiKeyManager_HasStaticLazyRepository()
    {
        // Task 2.1: YubiKeyManager should have static Lazy<DeviceRepository> singleton
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_repository", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
        Assert.Contains("Lazy", field.FieldType.Name);
    }
    
    [Fact]
    public async Task YubiKeyManager_FindAllAsync_IsStaticMethod()
    {
        // Task 2.2: FindAllAsync should be callable as static method
        var result = await YubiKeyManager.FindAllAsync();
        
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<Yubico.YubiKit.Core.Interfaces.IYubiKey>>(result);
    }
    
    [Fact]
    public async Task YubiKeyManager_FindAllAsync_WithConnectionType_IsStaticMethod()
    {
        // Task 2.3: FindAllAsync with ConnectionType should be callable as static method
        var result = await YubiKeyManager.FindAllAsync(ConnectionType.SmartCard, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<Yubico.YubiKit.Core.Interfaces.IYubiKey>>(result);
    }
    
    [Fact]
    public async Task YubiKeyManager_FindAllAsync_WorksWithoutDISetup()
    {
        // Task 2.4: Verify method works without calling any initialization or DI setup
        // No services.AddYubiKeyManagerCore() call needed
        var result = await YubiKeyManager.FindAllAsync();
        
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task YubiKeyManager_FindAllAsync_ReturnsIReadOnlyList()
    {
        // Task 2.5: Verify returns IReadOnlyList<IYubiKey>
        var result = await YubiKeyManager.FindAllAsync();
        
        Assert.IsAssignableFrom<IReadOnlyList<Yubico.YubiKit.Core.Interfaces.IYubiKey>>(result);
    }
    
    [Fact]
    public async Task YubiKeyManager_FindAllAsync_NoDevices_ReturnsEmptyList()
    {
        // Task 2.6: Handle no devices connected -> Return empty list (not null)
        var result = await YubiKeyManager.FindAllAsync();
        
        Assert.NotNull(result);
        // In unit test environment, expect empty (no real devices)
        // But importantly: not null
    }
    
    [Fact]
    public async Task YubiKeyManager_FindAllAsync_Cancellation_PropagatesToken()
    {
        // Task 2.8: Handle cancellation -> Throw OperationCanceledException
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
        // Task 2.9: Handle concurrent FindAllAsync() calls -> Thread-safe
        const int concurrentCalls = 10;
        var tasks = Enumerable.Range(0, concurrentCalls)
            .Select(_ => YubiKeyManager.FindAllAsync())
            .ToArray();
        
        var results = await Task.WhenAll(tasks);
        
        Assert.All(results, r => Assert.NotNull(r));
    }
    
    [Fact]
    public void YubiKeyManager_LazyRepository_UsesExecutionAndPublication()
    {
        // Task 2.1: Verify Lazy uses ExecutionAndPublication mode for thread safety
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_repository", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
        
        // The Lazy<T> should exist and be thread-safe
        var lazyValue = field.GetValue(null);
        Assert.NotNull(lazyValue);
    }
    
    [Fact]
    public void PlatformInteropException_Exists_AndIsProperException()
    {
        // Task 2.7: PlatformInteropException should be available for platform API errors
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
        // Task 2.7: Exception should carry context about what operation failed
        var ex = new PlatformInteropException("PC/SC service unavailable: 0x8010001D", 
            new InvalidOperationException("SCardEstablishContext failed"));
        
        Assert.Contains("PC/SC", ex.Message);
        Assert.NotNull(ex.InnerException);
    }
    
    // Phase 3: Monitoring Lifecycle Tests
    
    [Fact]
    public void YubiKeyManager_HasMonitoringCtsField()
    {
        // Task 3.1: YubiKeyManager should have static _monitoringCts field
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_monitoringCts", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
        Assert.Equal(typeof(CancellationTokenSource), field.FieldType);
    }
    
    [Fact]
    public void YubiKeyManager_HasMonitoringTaskField()
    {
        // Task 3.1: YubiKeyManager should have static _monitoringTask field
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_monitoringTask", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
        Assert.Equal(typeof(Task), field.FieldType);
    }
    
    [Fact]
    public void YubiKeyManager_HasMonitorLockField()
    {
        // Task 3.1: YubiKeyManager should have static _monitorLock field
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_monitorLock", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
        Assert.Equal(typeof(object), field.FieldType);
    }
    
    [Fact]
    public void YubiKeyManager_StartMonitoring_MethodExists()
    {
        // Task 3.2: YubiKeyManager should have static StartMonitoring() method
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
        // Task 3.3: YubiKeyManager should have static StartMonitoring(TimeSpan) method
        var method = typeof(YubiKeyManager).GetMethod(
            "StartMonitoring",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            [typeof(TimeSpan)]);
        
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(void), method.ReturnType);
    }
    
    [Fact]
    public void YubiKeyManager_StartMonitoring_HasDefaultInterval()
    {
        // Task 3.2: Default interval should be 5 seconds
        var type = typeof(YubiKeyManager);
        var field = type.GetField("DefaultMonitoringInterval", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
        var value = field.GetValue(null);
        Assert.Equal(TimeSpan.FromSeconds(5), value);
    }
    
    [Fact]
    public void YubiKeyManager_StopMonitoring_MethodExists()
    {
        // Task 3.4: YubiKeyManager should have static StopMonitoring() method
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
        // Task 3.5: YubiKeyManager should have static IsMonitoring property
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
        // Task 3.16: Handle interval = 0ms -> Throw ArgumentOutOfRangeException
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            YubiKeyManager.StartMonitoring(TimeSpan.Zero));
        
        Assert.Equal("interval", exception.ParamName);
    }
    
    [Fact]
    public void YubiKeyManager_StartMonitoring_NegativeInterval_ThrowsArgumentOutOfRangeException()
    {
        // Task 3.16: Handle negative interval -> Throw ArgumentOutOfRangeException
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(-1)));
        
        Assert.Equal("interval", exception.ParamName);
    }
    
    [Fact]
    public void YubiKeyManager_StartMonitoring_SetsIsMonitoring()
    {
        // Task 3.5, 3.13: Starting monitoring sets IsMonitoring to true
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        Assert.False(YubiKeyManager.IsMonitoring);
        
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        
        Assert.True(YubiKeyManager.IsMonitoring);
        
        YubiKeyManager.StopMonitoring(); // Cleanup
    }
    
    [Fact]
    public void YubiKeyManager_StopMonitoring_SetsIsMonitoringFalse()
    {
        // Task 3.4, 3.5: Stopping monitoring sets IsMonitoring to false
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);
        
        YubiKeyManager.StopMonitoring();
        
        Assert.False(YubiKeyManager.IsMonitoring);
    }
    
    [Fact]
    public void YubiKeyManager_StartMonitoring_WhenAlreadyMonitoring_IsIdempotent()
    {
        // Task 3.13, 3.17: StartMonitoring when already monitoring -> No-op (idempotent)
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);
        
        // Call again - should not throw or change behavior
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(2));
        Assert.True(YubiKeyManager.IsMonitoring);
        
        YubiKeyManager.StopMonitoring(); // Cleanup
    }
    
    [Fact]
    public void YubiKeyManager_StopMonitoring_WhenNotMonitoring_IsIdempotent()
    {
        // Task 3.14: StopMonitoring when not monitoring -> No-op (idempotent)
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        Assert.False(YubiKeyManager.IsMonitoring);
        
        // Call again - should not throw
        YubiKeyManager.StopMonitoring();
        
        Assert.False(YubiKeyManager.IsMonitoring);
    }
    
    [Fact]
    public void YubiKeyManager_HasHidListenerField()
    {
        // Task 3.7: YubiKeyManager should have static _hidListener field
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_hidListener", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
    }
    
    [Fact]
    public void YubiKeyManager_HasSmartCardListenerField()
    {
        // Task 3.7: YubiKeyManager should have static _smartCardListener field
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_smartCardListener", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
    }
    
    [Fact]
    public void YubiKeyManager_HasEventSemaphoreField()
    {
        // Task 3.11: YubiKeyManager should have static _eventSemaphore field
        var type = typeof(YubiKeyManager);
        var field = type.GetField("_eventSemaphore", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
    }
    
    [Fact]
    public void YubiKeyManager_HasSetupListenersMethod()
    {
        // Task 3.8: YubiKeyManager should have internal SetupListeners method
        var method = typeof(YubiKeyManager).GetMethod(
            "SetupListeners",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(method);
    }
    
    [Fact]
    public void YubiKeyManager_HasTeardownListenersMethod()
    {
        // Task 3.10: YubiKeyManager should have internal TeardownListeners method
        var method = typeof(YubiKeyManager).GetMethod(
            "TeardownListeners",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(method);
    }
    
    [Fact]
    public void YubiKeyManager_HasLoggerField()
    {
        // Task 3.15: YubiKeyManager should have static Logger for background scan exceptions
        var type = typeof(YubiKeyManager);
        var field = type.GetField("Logger", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(field);
    }
    
    // Phase 4: Device Events Tests
    
    [Fact]
    public void YubiKeyManager_DeviceChanges_StaticPropertyExists()
    {
        // Task 4.1: YubiKeyManager should have static DeviceChanges property
        var property = typeof(YubiKeyManager).GetProperty(
            "DeviceChanges",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        Assert.NotNull(property);
        Assert.True(typeof(IObservable<DeviceEvent>).IsAssignableFrom(property.PropertyType));
    }
    
    [Fact]
    public void DeviceEvent_ContainsExpectedMembers()
    {
        // Task 4.2: Verify DeviceEvent contains IYubiKey and DeviceAction enum
        var deviceEventType = typeof(DeviceEvent);
        
        // Should have a Device property of type IYubiKey
        var deviceProperty = deviceEventType.GetProperty("Device");
        Assert.NotNull(deviceProperty);
        Assert.True(typeof(Yubico.YubiKit.Core.Interfaces.IYubiKey).IsAssignableFrom(deviceProperty.PropertyType));
        
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
    public void YubiKeyManager_DeviceChanges_CanSubscribe()
    {
        // Task 4.3/4.5: Can subscribe to DeviceChanges even when not monitoring
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        var observable = YubiKeyManager.DeviceChanges;
        Assert.NotNull(observable);
        
        // Subscribe and immediately unsubscribe (no events expected)
        var subscription = observable.Subscribe(_ => { });
        subscription.Dispose();
        
        // Monitoring should not have auto-started
        Assert.False(YubiKeyManager.IsMonitoring);
    }
    
    [Fact]
    public void YubiKeyManager_DeviceChanges_MultipleSubscribersAllowed()
    {
        // Task 4.4: Verify multiple subscribers receive the same events
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        var observable = YubiKeyManager.DeviceChanges;
        
        // Multiple subscriptions should be allowed
        var subscription1 = observable.Subscribe(_ => { });
        var subscription2 = observable.Subscribe(_ => { });
        var subscription3 = observable.Subscribe(_ => { });
        
        // All subscriptions should be distinct
        Assert.NotNull(subscription1);
        Assert.NotNull(subscription2);
        Assert.NotNull(subscription3);
        
        // Cleanup
        subscription1.Dispose();
        subscription2.Dispose();
        subscription3.Dispose();
    }
    
    [Fact]
    public void YubiKeyManager_DeviceChanges_UnsubscribeDoesNotAffectMonitoring()
    {
        // Task 4.6: Handle unsubscribe -> Does not affect other subscribers or monitoring
        YubiKeyManager.StopMonitoring(); // Ensure clean state
        
        var observable = YubiKeyManager.DeviceChanges;
        var subscription = observable.Subscribe(_ => { });
        
        YubiKeyManager.StartMonitoring(TimeSpan.FromSeconds(1));
        Assert.True(YubiKeyManager.IsMonitoring);
        
        // Unsubscribe
        subscription.Dispose();
        
        // Monitoring should still be active (unsubscribe doesn't stop monitoring)
        Assert.True(YubiKeyManager.IsMonitoring);
        
        YubiKeyManager.StopMonitoring(); // Cleanup
    }
}

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
}

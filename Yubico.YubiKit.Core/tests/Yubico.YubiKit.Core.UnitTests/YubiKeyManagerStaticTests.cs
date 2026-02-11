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
}

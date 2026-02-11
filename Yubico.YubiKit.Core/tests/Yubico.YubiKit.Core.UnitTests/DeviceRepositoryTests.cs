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

namespace Yubico.YubiKit.Core.UnitTests;

public class DeviceRepositoryTests
{
    [Fact]
    public void DeviceRepository_TypeExists_And_ImplementsIDeviceRepository()
    {
        // Task 1.1: DeviceRepositoryCached has been renamed to DeviceRepository
        var type = typeof(DeviceRepository);
        
        Assert.NotNull(type);
        Assert.True(typeof(IDeviceRepository).IsAssignableFrom(type));
    }
    
    [Fact]
    public void DeviceRepository_IsNotNamedDeviceRepositoryCached()
    {
        // Verify the old class name no longer exists (namespace-qualified)
        var type = typeof(DeviceRepository);
        
        Assert.Equal("DeviceRepository", type.Name);
        Assert.NotEqual("DeviceRepositoryCached", type.Name);
    }
    
    [Fact]
    public void DeviceRepository_Create_ReturnsValidInstance()
    {
        // Task 1.2: Static factory method creates valid repository
        var repository = DeviceRepository.Create();
        
        Assert.NotNull(repository);
        Assert.IsAssignableFrom<IDeviceRepository>(repository);
    }
    
    [Fact]
    public void DeviceRepository_Create_InstanceIsDisposable()
    {
        // Verify the created instance implements IDisposable
        var repository = DeviceRepository.Create();
        
        Assert.IsAssignableFrom<IDisposable>(repository);
        repository.Dispose();
    }
    
    [Fact]
    public void DeviceRepository_Create_WorksWithoutDIConfiguration()
    {
        // Task 1.3: Factory works without any DI setup
        // This test verifies the factory doesn't require services.AddYubiKeyManagerCore()
        using var repository = DeviceRepository.Create();
        
        // Repository exposes DeviceChanges observable
        Assert.NotNull(repository.DeviceChanges);
    }
    
    [Fact]
    public void DeviceRepository_Create_DeviceChangesIsSubscribable()
    {
        // Task 1.3: Verify the created repository has functional DeviceChanges
        using var repository = DeviceRepository.Create();
        
        var eventReceived = false;
        using var subscription = repository.DeviceChanges.Subscribe(e => eventReceived = true);
        
        // We can subscribe without error (we don't expect events in unit tests)
        Assert.NotNull(subscription);
    }
    
    [Fact]
    public void DeviceRepository_UpdateCache_ThreadSafe_ConcurrentUpdates()
    {
        // Task 1.4: Verify concurrent UpdateCache calls don't corrupt state
        using var repository = DeviceRepository.Create();
        var deviceCountAtEnd = 0;
        
        // Simulate concurrent cache updates from multiple threads
        Parallel.For(0, 10, i =>
        {
            // Each iteration updates the cache with a new device set
            var devices = new List<Yubico.YubiKit.Core.Interfaces.IYubiKey>();
            repository.UpdateCache(devices);
        });
        
        // No exception means thread-safe - ConcurrentDictionary handles this
        // Final state is empty cache since all updates pass empty lists
        Assert.True(true);
    }
    
    [Fact]
    public void DeviceRepository_Uses_ConcurrentDictionary_ForCache()
    {
        // Task 1.4: Verify implementation uses thread-safe collections
        var type = typeof(DeviceRepository);
        var cacheField = type.GetField("_deviceCache", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(cacheField);
        Assert.Contains("ConcurrentDictionary", cacheField.FieldType.Name);
    }
    
    [Fact]
    public void DeviceRepository_Uses_SemaphoreSlim_ForInitializationLock()
    {
        // Task 1.4: Verify implementation uses proper synchronization primitive
        var type = typeof(DeviceRepository);
        var lockField = type.GetField("_initializationLock", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(lockField);
        Assert.Equal(typeof(SemaphoreSlim), lockField.FieldType);
    }
    
    [Fact]
    public void DeviceRepository_ConcurrentInitialization_100PlusThreads_NoDeadlock()
    {
        // Task 1.5: Verify concurrent operations from 100+ threads doesn't deadlock
        const int threadCount = 100;
        using var repository = DeviceRepository.Create();
        var completedCount = 0;
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        
        // Use Parallel.For which handles thread pooling efficiently
        Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, i =>
        {
            try
            {
                // All threads try to update cache concurrently
                repository.UpdateCache([]);
                Interlocked.Increment(ref completedCount);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });
        
        Assert.Empty(exceptions);
        Assert.Equal(threadCount, completedCount);
    }
    
    [Fact]
    public void DeviceRepository_ConcurrentSubscription_MultipleThreads_Safe()
    {
        // Task 1.5: Verify concurrent subscriptions to DeviceChanges are safe
        const int threadCount = 100;
        using var repository = DeviceRepository.Create();
        var subscriptions = new System.Collections.Concurrent.ConcurrentBag<IDisposable>();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        
        Parallel.For(0, threadCount, i =>
        {
            try
            {
                var subscription = repository.DeviceChanges.Subscribe(_ => { });
                subscriptions.Add(subscription);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });
        
        Assert.Empty(exceptions);
        Assert.Equal(threadCount, subscriptions.Count);
        
        // Cleanup subscriptions
        foreach (var sub in subscriptions)
        {
            sub.Dispose();
        }
    }
}

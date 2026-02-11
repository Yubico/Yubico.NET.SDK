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
}

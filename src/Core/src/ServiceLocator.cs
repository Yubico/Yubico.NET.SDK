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

using Microsoft.Extensions.DependencyInjection;

namespace Yubico.YubiKit.Core;

/// <summary>
///     Holds a process-global service provider for tests, not the SDK's discovery entry point.
/// </summary>
/// <remarks>The caller owns the provider; replacing it does not dispose the previous provider.</remarks>
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>Sets the caller-owned provider used by subsequent lookups.</summary>
    public static void SetLocatorProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <summary>Resolves a required service from the configured provider.</summary>
    /// <exception cref="InvalidOperationException">No provider is set, or the provider cannot resolve T.</exception>
    public static T GetService<T>() where T : notnull => _serviceProvider is null
        ? throw new InvalidOperationException("Service provider not set.")
        : _serviceProvider.GetRequiredService<T>();
}
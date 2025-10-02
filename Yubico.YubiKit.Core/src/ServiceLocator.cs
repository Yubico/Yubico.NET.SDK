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
///     ONLY FOR TESTING, DONT USE IN PRODUCTION CODE.
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    public static void SetLocatorProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public static T GetService<T>() where T : notnull => _serviceProvider == null
        ? throw new InvalidOperationException("Service provider not set.")
        : _serviceProvider.GetRequiredService<T>();
}
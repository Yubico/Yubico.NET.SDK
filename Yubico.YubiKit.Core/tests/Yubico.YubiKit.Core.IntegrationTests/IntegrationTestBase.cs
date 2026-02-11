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

namespace Yubico.YubiKit.Core.IntegrationTests;

/// <summary>
/// Base class for integration tests using the static YubiKeyManager API.
/// </summary>
public abstract class IntegrationTestBase : IAsyncDisposable
{
    private bool _disposed;

    protected IntegrationTestBase()
    {
        // Start monitoring to enable device events
        YubiKeyManager.StartMonitoring();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await YubiKeyManager.ShutdownAsync();
        GC.SuppressFinalize(this);

        _disposed = true;
    }
}
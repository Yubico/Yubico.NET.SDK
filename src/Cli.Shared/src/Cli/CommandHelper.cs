// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Cli.Shared.Cli;

/// <summary>
/// Provides YubiKeyManager lifecycle helpers and CancellationTokenSource setup.
/// Shared boilerplate for CLI tool entry points.
/// </summary>
public static class CommandHelper
{
    /// <summary>
    /// Sets up a <see cref="CancellationTokenSource"/> that cancels on Ctrl+C.
    /// </summary>
    /// <returns>A <see cref="CancellationTokenSource"/> wired to Console.CancelKeyPress.</returns>
    public static CancellationTokenSource CreateConsoleCts()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        return cts;
    }

    /// <summary>
    /// Starts YubiKeyManager monitoring for device events.
    /// Call <see cref="ShutdownAsync"/> when done.
    /// </summary>
    public static void StartMonitoring()
    {
        YubiKeyManager.StartMonitoring();
    }

    /// <summary>
    /// Shuts down YubiKeyManager monitoring gracefully.
    /// </summary>
    public static async Task ShutdownAsync()
    {
        await YubiKeyManager.ShutdownAsync();
    }
}
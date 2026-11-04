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

using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;

/// <summary>
///     Requests Security Domain reset after each test that records a device with Track.
/// </summary>
/// <remarks>
///     <para>
///         Key operations can replace factory defaults and affect later tests using public default keys.
///         Cleanup uses an independent 60-second cancellation source so the test token cannot cancel it
///         immediately. Reset remains a destructive attempt, not verified factory-state restoration.
///     </para>
///     <para>
///         xUnit constructs the test class once per test, so <see cref="DisposeAsync" /> runs after each
///         one with that test's device. Derived tests must call <see cref="Track" /> with their state so
///         cleanup targets the correct device. Tests that do not call Track receive no cleanup here.
///     </para>
/// </remarks>
public abstract class SecurityDomainStateRestoringTests : IAsyncLifetime
{
    private static readonly TimeSpan RestoreTimeout = TimeSpan.FromSeconds(60);

    private YubiKeyTestState? _trackedState;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_trackedState is null)
            return;

        using var cts = new CancellationTokenSource(RestoreTimeout);
        await _trackedState.ResetSecurityDomainAsync(cts.Token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Records the device on which cleanup will request Security Domain reset after the test.
    /// </summary>
    protected YubiKeyTestState Track(YubiKeyTestState state)
    {
        _trackedState = state;
        return state;
    }
}
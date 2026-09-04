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
///     Base class for Security Domain test classes that leave the device's key state altered,
///     restoring the factory Security Domain after every test.
/// </summary>
/// <remarks>
///     <para>
///         Importing, rotating, or deleting key sets can leave the device with no key set matching
///         <c>Scp03KeyParameters.Default</c>. Every later consumer of the default keys then fails
///         secure-channel establishment with <c>SW=0x6A88</c> (referenced data not found). Because the
///         failure appears in whichever suite runs next rather than in the test that caused it, it reads
///         as an unrelated regression: the Management SCP03 tests were the visible symptom of Security
///         Domain SCP11 tests running first.
///     </para>
///     <para>
///         xUnit constructs the test class once per test, so <see cref="DisposeAsync" /> runs after each
///         one with that test's device. Derived tests must call <see cref="Track" /> with their state so
///         the correct device is restored.
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
    ///     Records the device under test so the Security Domain is restored after the test completes.
    /// </summary>
    protected YubiKeyTestState Track(YubiKeyTestState state)
    {
        _trackedState = state;
        return state;
    }
}

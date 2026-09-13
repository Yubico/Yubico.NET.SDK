// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Xunit.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests;

public class UserPresenceProbeTests(ITestOutputHelper output)
{
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.5.1", ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task SelectionAsync_TouchAfterDeviceWaitingPrompt_CompletesOnceAndSessionRemainsUsable(
        YubiKeyTestState state)
    {
        var prompt = new RecordingUserPresencePrompt(output, "Touch the YubiKey now.");
        await using var session = await state.Device.CreateFidoSessionAsync(
            new SessionCreationOptions
            {
                PreferredConnectionType = ConnectionType.HidFido,
                UserPresencePrompt = prompt
            });

        using var operationCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await session.SelectionAsync(operationCts.Token);

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
        (UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken) resolved =
            Assert.Single(prompt.Resolved);
        Assert.Same(requested, resolved.Context);
        Assert.Equal(UserPresenceOutcome.Completed, resolved.Outcome);
        Assert.Equal(CancellationToken.None, resolved.CancellationToken);

        using var getInfoCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var info = await session.GetInfoAsync(getInfoCts.Token);
        Assert.NotEmpty(info.Versions);
    }

    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.5.1", ConnectionType = ConnectionType.HidFido)]
    [Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task SelectionAsync_CancelAfterDeviceWaitingPrompt_ThrowsOperationCanceledExceptionAndSessionRemainsUsable(
        YubiKeyTestState state)
    {
        using var operationCts = new CancellationTokenSource();
        var prompt = new RecordingUserPresencePrompt(
            output,
            "Do not touch; cancelling now",
            () => Task.Run(operationCts.Cancel));
        await using var session = await state.Device.CreateFidoSessionAsync(
            new SessionCreationOptions
            {
                PreferredConnectionType = ConnectionType.HidFido,
                UserPresencePrompt = prompt
            });

        operationCts.CancelAfter(TimeSpan.FromSeconds(30));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => session.SelectionAsync(operationCts.Token));
        await prompt.CancellationTask;

        UserPresenceContext requested = Assert.Single(prompt.Requested);
        Assert.Equal(UserPresenceBasis.DeviceWaiting, requested.Basis);
        (UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken) resolved =
            Assert.Single(prompt.Resolved);
        Assert.Same(requested, resolved.Context);
        Assert.Equal(UserPresenceOutcome.Cancelled, resolved.Outcome);
        Assert.Equal(CancellationToken.None, resolved.CancellationToken);

        using var getInfoCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var info = await session.GetInfoAsync(getInfoCts.Token);
        Assert.NotEmpty(info.Versions);
    }

    private sealed class RecordingUserPresencePrompt(
        ITestOutputHelper output,
        string instruction,
        Func<Task>? scheduleCancellation = null) : IUserPresencePrompt
    {
        public List<UserPresenceContext> Requested { get; } = [];

        public List<(UserPresenceContext Context, UserPresenceOutcome Outcome, CancellationToken CancellationToken)>
            Resolved
        { get; } = [];

        public Task CancellationTask { get; private set; } = Task.CompletedTask;

        public ValueTask OnUserPresenceRequestedAsync(
            UserPresenceContext context,
            CancellationToken cancellationToken)
        {
            Requested.Add(context);
            output.WriteLine(
                $"[{DateTimeOffset.UtcNow:O}] SelectionAsync user-presence request: Basis={context.Basis} " +
                $"(expected {UserPresenceBasis.DeviceWaiting}). {instruction}");
            CancellationTask = scheduleCancellation?.Invoke() ?? Task.CompletedTask;
            return default;
        }

        public ValueTask OnUserPresenceResolvedAsync(
            UserPresenceContext context,
            UserPresenceOutcome outcome,
            CancellationToken cancellationToken)
        {
            Resolved.Add((context, outcome, cancellationToken));
            output.WriteLine(
                $"[{DateTimeOffset.UtcNow:O}] SelectionAsync user-presence resolution: Outcome={outcome}.");
            return default;
        }
    }
}
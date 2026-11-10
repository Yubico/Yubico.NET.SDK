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

using Microsoft.Extensions.Time.Testing;
using System.Threading.Channels;

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

/// <summary>Exposes timer registration so tests can await an armed wait before advancing fake time.</summary>
internal sealed class ObservedTimeProvider : FakeTimeProvider
{
    private readonly Channel<TimeSpan> _timers = Channel.CreateUnbounded<TimeSpan>();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        _timers.Writer.TryWrite(dueTime);
        return timer;
    }

    internal async Task WaitForTimerAsync(TimeSpan dueTime) =>
        Assert.Equal(dueTime, await _timers.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
}
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

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Cli.Shared.Output;

/// <summary>Writes user-presence instructions for terminal applications.</summary>
public sealed class ConsoleUserPresencePrompt : IUserPresencePrompt
{
    private const string TouchInstruction = "Touch your YubiKey.";
    private static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(300);

    private readonly Lock _gate = new();
    private readonly Dictionary<UserPresenceContext, PendingPrompt> _pending =
        new(ReferenceEqualityComparer.Instance);
    private readonly Action<string> _writeLine;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly TimeSpan _debounce;

    private ConsoleUserPresencePrompt()
        : this(Console.WriteLine, Task.Delay, DefaultDebounce)
    {
    }

    internal ConsoleUserPresencePrompt(
        Action<string> writeLine,
        Func<TimeSpan, CancellationToken, Task> delay,
        TimeSpan debounce)
    {
        ArgumentNullException.ThrowIfNull(writeLine);
        ArgumentNullException.ThrowIfNull(delay);
        ArgumentOutOfRangeException.ThrowIfLessThan(debounce, TimeSpan.Zero);

        _writeLine = writeLine;
        _delay = delay;
        _debounce = debounce;
    }

    /// <summary>Gets the shared terminal prompt instance.</summary>
    public static ConsoleUserPresencePrompt Instance { get; } = new();

    /// <inheritdoc />
    public ValueTask OnUserPresenceRequestedAsync(
        UserPresenceContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Basis is not UserPresenceBasis.PolicyMayRequire)
        {
            _writeLine(TouchInstruction);
            return default;
        }

        lock (_gate)
        {
            if (_pending.TryGetValue(context, out PendingPrompt? previous))
            {
                previous.Cancellation.Cancel();
                _pending.Remove(context);
            }

            var pending = new PendingPrompt(
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));
            _pending.Add(context, pending);
            pending.Completion = WriteAfterDelayAsync(context, pending);
        }

        return default;
    }

    /// <inheritdoc />
    public ValueTask OnUserPresenceResolvedAsync(
        UserPresenceContext context,
        UserPresenceOutcome outcome,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PendingPrompt? pending;
        lock (_gate)
        {
            if (_pending.TryGetValue(context, out pending))
            {
                pending.Cancellation.Cancel();
                _pending.Remove(context);
            }
        }

        return pending is null ? default : new ValueTask(pending.Completion);
    }

    private async Task WriteAfterDelayAsync(UserPresenceContext context, PendingPrompt pending)
    {
        try
        {
            await _delay(_debounce, pending.Cancellation.Token).ConfigureAwait(false);
            pending.Cancellation.Token.ThrowIfCancellationRequested();

            lock (_gate)
            {
                if (!_pending.TryGetValue(context, out PendingPrompt? current)
                    || !ReferenceEquals(current, pending)
                    || pending.Cancellation.IsCancellationRequested)
                {
                    return;
                }

                _pending.Remove(context);
                _writeLine(TouchInstruction);
            }
        }
        catch (OperationCanceledException) when (pending.Cancellation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            // A fire-and-forget terminal notification must not fault the device operation.
        }
        finally
        {
            lock (_gate)
            {
                if (_pending.TryGetValue(context, out PendingPrompt? current)
                    && ReferenceEquals(current, pending))
                {
                    _pending.Remove(context);
                }
            }

            pending.Cancellation.Dispose();
        }
    }

    private sealed class PendingPrompt(CancellationTokenSource cancellation)
    {
        public CancellationTokenSource Cancellation { get; } = cancellation;

        public Task Completion { get; set; } = Task.CompletedTask;
    }
}
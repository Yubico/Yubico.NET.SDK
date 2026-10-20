// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// The WebAuthn L3 §5.1.3 / §5.1.4 <c>lifetimeTimer</c>: a cancellation token that fires when the
/// ceremony's <c>timeout</c> elapses or when the caller cancels, and remembers which one happened.
/// </summary>
/// <remarks>
/// The deadline is a signal, not a hard stop: the transport decides when it can act on it (FIDO HID
/// sends <c>CTAPHID_CANCEL</c> at the next keep-alive; PC/SC exchanges run to their answer). A
/// <c>null</c> timeout starts no timer; the 300 s default WebAuthn L3 §15.1 recommends is not applied.
/// Values are not clamped to the L3 range.
/// </remarks>
internal sealed class CeremonyDeadline : IDisposable
{
    private readonly CancellationTokenSource? _timer;
    private readonly CancellationTokenSource? _linked;
    private readonly CancellationToken _caller;

    private CeremonyDeadline(TimeSpan? timeout, CancellationToken caller)
    {
        _caller = caller;
        if (timeout is { } value)
        {
            _timer = new CancellationTokenSource(value);
            _linked = CancellationTokenSource.CreateLinkedTokenSource(_timer.Token, caller);
            Token = _linked.Token;
        }
        else
        {
            Token = caller;
        }
    }

    /// <summary>Starts the timer (if <paramref name="timeout"/> is set) linked to the caller's token.</summary>
    public static CeremonyDeadline Start(TimeSpan? timeout, CancellationToken caller) => new(timeout, caller);

    /// <summary>The token to pass to every step of the ceremony.</summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// True when the timeout elapsed and the caller did not cancel. Caller cancellation always wins,
    /// so a caller who cancels keeps getting <see cref="OperationCanceledException"/>.
    /// </summary>
    public bool Expired => _timer?.IsCancellationRequested == true && !_caller.IsCancellationRequested;

    public void Dispose()
    {
        _linked?.Dispose();
        _timer?.Dispose();
    }
}
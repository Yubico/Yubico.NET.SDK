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

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

/// <summary>
/// An <see cref="IObserver{T}"/> that records everything it receives.
/// </summary>
/// <remarks>
/// <para>
/// This is the dependency-free equivalent of Rx's <c>Subscribe(Action&lt;T&gt;)</c> convenience
/// overload, and it is what a consumer who has not referenced <c>System.Reactive</c> would write.
/// Pass <paramref name="onNext"/> to additionally run a callback per event — that covers the
/// "assert inside the handler" and "throw from the handler" cases.
/// </para>
/// <para>Safe for concurrent <see cref="OnNext"/> calls.</para>
/// </remarks>
/// <param name="onNext">Optional callback invoked for each event, after it has been recorded.</param>
internal sealed class RecordingObserver<T>(Action<T>? onNext = null) : IObserver<T>
{
    private readonly Lock _gate = new();
    private readonly List<T> _items = [];

    private int _completedCount;

    /// <summary>Snapshot of everything received so far, in delivery order.</summary>
    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_gate)
            {
                return [.. _items];
            }
        }
    }

    /// <summary>Number of received events.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _items.Count;
            }
        }
    }

    /// <summary>How many times <see cref="OnCompleted"/> fired; used to assert idempotency.</summary>
    public int CompletedCount => Volatile.Read(ref _completedCount);

    /// <summary>Whether the sequence terminated normally.</summary>
    public bool IsCompleted => CompletedCount > 0;

    /// <summary>The error passed to <see cref="OnError"/>, if any.</summary>
    public Exception? Error { get; private set; }

    public void OnNext(T value)
    {
        lock (_gate)
        {
            _items.Add(value);
        }

        // Outside the lock so a callback that inspects this observer cannot deadlock.
        onNext?.Invoke(value);
    }

    public void OnCompleted() => Interlocked.Increment(ref _completedCount);

    public void OnError(Exception error) => Error = error;
}
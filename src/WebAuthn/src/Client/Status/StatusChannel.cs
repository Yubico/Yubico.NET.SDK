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

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Yubico.YubiKit.WebAuthn.Client.Status;

/// <summary>
/// Internal channel for coordinating status streaming between producer and consumer.
/// </summary>
/// <typeparam name="TResult">The terminal result type.</typeparam>
/// <remarks>
/// This channel manages:
/// - Unbounded status queue
/// - Deduplication of consecutive identical statuses
/// - Interactive responses (PIN submission, UV decision)
/// - Graceful completion on success, cancellation, or error
/// </remarks>
internal sealed class StatusChannel<TResult> : IAsyncDisposable
{
    private readonly Channel<WebAuthnStatus> _channel;
    private WebAuthnStatus? _lastWritten;
    private bool _readerStarted;
    private TaskCompletionSource<ReadOnlyMemory<byte>?>? _pinResponseTcs;
    private TaskCompletionSource<bool>? _uvResponseTcs;

    public StatusChannel()
    {
        _channel = Channel.CreateUnbounded<WebAuthnStatus>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
    }

    /// <summary>
    /// Gets the async enumerable reader for consuming status updates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop iteration.</param>
    /// <returns>Async enumerable of status updates.</returns>
    /// <exception cref="InvalidOperationException">Thrown if called more than once.</exception>
    public async IAsyncEnumerable<WebAuthnStatus> Reader([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_readerStarted)
        {
            throw new InvalidOperationException("StatusChannel.Reader can only be consumed once.");
        }

        _readerStarted = true;

        await foreach (var status in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return status;
        }
    }

    /// <summary>
    /// Writes a status update to the channel.
    /// </summary>
    /// <param name="status">The status to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// Consecutive duplicate statuses are automatically deduplicated using record value equality.
    /// </remarks>
    public async ValueTask WriteAsync(WebAuthnStatus status, CancellationToken cancellationToken = default)
    {
        // Deduplicate consecutive identical statuses
        if (_lastWritten is not null && _lastWritten.Equals(status))
        {
            return;
        }

        await _channel.Writer.WriteAsync(status, cancellationToken).ConfigureAwait(false);
        _lastWritten = status;
    }

    /// <summary>
    /// Completes the channel writer, signaling no more statuses will be written.
    /// </summary>
    /// <param name="error">Optional exception if the channel is being completed due to an error.</param>
    public void Complete(Exception? error = null)
    {
        _channel.Writer.Complete(error);
    }

    /// <summary>
    /// Creates a PIN request status that the producer can await.
    /// </summary>
    /// <returns>A tuple of (status, response task).</returns>
    public (WebAuthnStatusRequestingPin Status, Task<ReadOnlyMemory<byte>?> ResponseTask) CreatePinRequest()
    {
        _pinResponseTcs = new TaskCompletionSource<ReadOnlyMemory<byte>?>();

        var status = new WebAuthnStatusRequestingPin(
            SubmitPin: pinBytes =>
            {
                _pinResponseTcs?.TrySetResult(pinBytes);
                return ValueTask.CompletedTask;
            },
            Cancel: () =>
            {
                _pinResponseTcs?.TrySetResult(null);
                return ValueTask.CompletedTask;
            });

        return (status, _pinResponseTcs.Task);
    }

    /// <summary>
    /// Creates a UV request status that the producer can await.
    /// </summary>
    /// <returns>A tuple of (status, response task).</returns>
    public (WebAuthnStatusRequestingUv Status, Task<bool> ResponseTask) CreateUvRequest()
    {
        _uvResponseTcs = new TaskCompletionSource<bool>();

        var status = new WebAuthnStatusRequestingUv(
            SetUseUv: useUv =>
            {
                _uvResponseTcs?.TrySetResult(useUv);
                return ValueTask.CompletedTask;
            });

        return (status, _uvResponseTcs.Task);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Complete();
        return ValueTask.CompletedTask;
    }
}

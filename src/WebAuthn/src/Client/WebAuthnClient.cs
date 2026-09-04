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

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Yubico.YubiKit.Fido2;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// WebAuthn Client for high-level credential registration and authentication.
/// </summary>
/// <remarks>
/// <para>
/// This client wraps CTAP2 operations and handles WebAuthn protocol details like
/// clientDataJSON construction, RP ID validation, UV/PIN token acquisition, and retry logic.
/// </para>
/// <para>
/// The implementation is split across partial files by concern: this file holds construction,
/// disposal, and shared secret hygiene; <c>WebAuthnClient.Registration.cs</c> and
/// <c>WebAuthnClient.Authentication.cs</c> hold the two ceremonies;
/// <c>WebAuthnClient.PinUvAuth.cs</c> holds PIN/UV token acquisition and prompting; and
/// <c>WebAuthnClient.Validation.cs</c> holds request validation and CTAP error mapping.
/// </para>
/// </remarks>
public sealed partial class WebAuthnClient : IAsyncDisposable
{
    private readonly IWebAuthnBackend _backend;
    private readonly WebAuthnOrigin _origin;
    private readonly Func<string, bool> _isPublicSuffix;
    private readonly WebAuthnClientOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/>.
    /// </summary>
    /// <param name="fidoSession">The FIDO2 session that performs CTAP2 operations (ownership transferred).</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="isPublicSuffix">Checker used to reject public-suffix RP IDs.</param>
    /// <param name="options">
    /// Optional client configuration. When omitted, <see cref="WebAuthnClientOptions"/> defaults
    /// apply: no enterprise RP IDs, no credential prompt, and
    /// <see cref="WebAuthnClientOptions.DefaultMaxPromptAttempts"/> prompt attempts.
    /// </param>
    public WebAuthnClient(
        IFidoSession fidoSession,
        WebAuthnOrigin origin,
        PublicSuffixChecker isPublicSuffix,
        WebAuthnClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(fidoSession);
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        ArgumentNullException.ThrowIfNull(isPublicSuffix);
        _backend = new WebAuthnBackend(fidoSession);
        _isPublicSuffix = domain => isPublicSuffix(domain);
        _options = options ?? new WebAuthnClientOptions();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/> over an explicit backend.
    /// </summary>
    /// <param name="backend">The backend that performs CTAP2 operations (ownership transferred).</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="isPublicSuffix">Predicate to determine if a domain is a public suffix.</param>
    /// <param name="options">Optional client configuration.</param>
    internal WebAuthnClient(
        IWebAuthnBackend backend,
        WebAuthnOrigin origin,
        Func<string, bool> isPublicSuffix,
        WebAuthnClientOptions? options = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        _isPublicSuffix = isPublicSuffix ?? throw new ArgumentNullException(nameof(isPublicSuffix));
        _options = options ?? new WebAuthnClientOptions();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _backend.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }

    private static void ZeroAndDispose(IMemoryOwner<byte>? owner)
    {
        if (owner is null)
        {
            return;
        }

        try
        {
            CryptographicOperations.ZeroMemory(owner.Memory.Span);
        }
        finally
        {
            owner.Dispose();
        }
    }

    private static void ZeroMemory(ReadOnlyMemory<byte>? memory)
    {
        if (memory is null || memory.Value.IsEmpty)
        {
            return;
        }

        // A zeroing helper that quietly skips is a secret left in memory, so make the only
        // unreachable case loud rather than silent. Callers here always pass array-backed memory;
        // this cannot throw instead because every call site is a finally block, where throwing
        // would swallow the exception already in flight.
        var isArrayBacked = MemoryMarshal.TryGetArray(memory.Value, out var segment) && segment.Array is not null;
        Debug.Assert(isArrayBacked, "pinUvAuthParam must be array-backed so it can be zeroed");

        if (isArrayBacked)
        {
            CryptographicOperations.ZeroMemory(segment.AsSpan());
        }
    }
}
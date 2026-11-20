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
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2;

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// WebAuthn Client for high-level credential registration and authentication.
/// </summary>
/// <remarks>
/// <para>
/// This client wraps CTAP2 operations and handles WebAuthn protocol details like
/// clientDataJSON construction, application-supplied RP validation, UV/PIN token acquisition, and retry logic.
/// </para>
/// <para>
/// Successful construction transfers ownership of the supplied CTAP2 session to this client.
/// Dispose the client to release that session. If construction throws, the caller retains ownership.
/// The configured credential prompt is retained but is not disposed by the client.
/// Disposing the client does not clear returned response or extension-result buffers.
/// </para>
/// <para>
/// Serialize and fully await complete ceremonies before disposing this client or its dependencies.
/// Client disposal does not join an in-flight ceremony: Core drains individual exchanges, not the
/// complete capability/probe/token/assertion/extension-output workflow. Keep borrowed inputs and
/// caller-owned prompt resources usable through actual remaining work, including a credential prompt
/// that returns late after cancellation. A callback must not reenter or dispose the client awaiting it.
/// Adopting a session does not acquire ownership of a connection that session already borrowed.
/// </para>
/// <para>
/// The concrete backend uses PIN/UV auth protocol 2 without negotiating protocol 1. PIN/UV token
/// acquisition and extensions that use key agreement therefore need compatible authenticator support;
/// ceremonies that need neither may still work without it. Built-in UV token support also depends on
/// authenticator capabilities and is not fully checked before token acquisition.
/// </para>
/// <para>
/// The relying party server must verify returned responses, including the challenge, operation type,
/// origin and cross-origin context, RP ID hash, signature or attestation policy, UP/UV flags, and
/// credential/account binding. Constructing client data and parsing a response do not perform those checks.
/// </para>
/// </remarks>
public sealed partial class WebAuthnClient : IAsyncDisposable
{
    private readonly IWebAuthnBackend _backend;
    private readonly WebAuthnOrigin _origin;
    private readonly RpIdValidator _rpIdValidator;
    private readonly WebAuthnClientOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/>.
    /// </summary>
    /// <param name="ctap2Session">The CTAP2 session that performs authenticator operations. Ownership transfers only if construction succeeds.</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="options">
    /// Required client configuration, including application-owned RP validity and authorization. Defaults remain
    /// no credential prompt and
    /// <see cref="WebAuthnClientOptions.DefaultMaxPromptAttempts"/> prompt attempts.
    /// </param>
    public WebAuthnClient(
        ICtap2Session ctap2Session,
        WebAuthnOrigin origin,
        WebAuthnClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(ctap2Session);
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rpIdValidator = options.RpIdValidator;
        _backend = new WebAuthnBackend(ctap2Session);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="WebAuthnClient"/> over an explicit backend.
    /// </summary>
    /// <param name="backend">The backend that performs CTAP2 operations. Ownership transfers only if construction succeeds.</param>
    /// <param name="origin">The WebAuthn origin for this client.</param>
    /// <param name="options">Required client configuration, including application-owned RP validity and authorization.</param>
    internal WebAuthnClient(
        IWebAuthnBackend backend,
        WebAuthnOrigin origin,
        WebAuthnClientOptions options)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rpIdValidator = options.RpIdValidator;
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
}
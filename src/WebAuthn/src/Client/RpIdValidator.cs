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

namespace Yubico.YubiKit.WebAuthn.Client;

/// <summary>
/// Authorizes a relying party identifier for the application's configured origin.
/// </summary>
/// <param name="origin">The configured origin, also serialized into client data.</param>
/// <param name="rpId">The requested RP ID, unchanged and already checked for protocol syntax and encoding.</param>
/// <returns><see langword="true"/> only if the application authorizes this exact origin/RP pair.</returns>
/// <remarks>
/// <para>
/// The application owns the complete binding policy, including public-suffix and related-origin
/// rules for browser-compatible use, or trusted native bindings where appropriate. The SDK does
/// not impose additional domain-suffix authorization after approval. WebAuthn Level 3's RP ID
/// definition permits other native binding models, but still requires domain-string or URI syntax.
/// </para>
/// <para>
/// Invoked synchronously once per registration or authentication before ceremony device I/O,
/// including capability reads, credential probes, and PIN/UV acquisition. Return promptly using
/// trusted local policy; do not block on network lookups. The ceremony timeout starts afterwards.
/// If shared between clients, the callback must be safe for concurrent invocation.
/// </para>
/// <para>
/// False or a non-cancellation exception fails with <see cref="WebAuthnClientErrorCode.Security"/>;
/// a thrown exception is retained as the inner exception. Cancellation exceptions propagate.
/// Neither rejection nor any exception permits device I/O. Malformed protocol values fail with
/// <see cref="WebAuthnClientErrorCode.InvalidRequest"/> without invoking this callback.
/// </para>
/// <para>
/// Approval does not replace the RP server's independent challenge, origin, RP ID hash,
/// signature, UP/UV, and credential/account checks. <see cref="WebAuthnOrigin"/> still represents
/// only HTTPS or HTTP localhost origins, not arbitrary native or opaque origins.
/// </para>
/// </remarks>
public delegate bool RpIdValidator(WebAuthnOrigin origin, string rpId);
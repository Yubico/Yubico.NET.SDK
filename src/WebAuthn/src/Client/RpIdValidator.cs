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
/// Validates a relying party identifier and authorizes it for the application's configured origin.
/// </summary>
/// <param name="origin">The configured origin, also serialized into client data.</param>
/// <param name="rpId">The requested RP ID, unchanged, nonempty, and faithfully encodable as UTF-8 text.</param>
/// <returns><see langword="true"/> only if the application considers the RP ID valid and authorizes this exact origin/RP pair.</returns>
/// <remarks>
/// <para>
/// The application owns RP validity, including domain/URI syntax, and the complete caller/RP
/// binding policy. For browser-compatible use this includes the applicable WebAuthn syntax,
/// public-suffix, and related-origin rules; native clients must apply their established binding model.
/// See https://www.w3.org/TR/webauthn-3/#rp-id. Approval is an application decision, not a blanket
/// claim of browser conformance. The SDK checks only nonempty, faithfully UTF-8 encodable RP text;
/// it adds no domain/URI parser, IDNA conversion, public-suffix policy, or binding rule.
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
/// Neither rejection nor any exception permits device I/O. A null/empty RP ID or invalid UTF-16
/// that cannot be encoded faithfully as UTF-8 fails with <see cref="WebAuthnClientErrorCode.InvalidRequest"/>
/// without invoking this callback. Every RP ID passing those wire checks is presented unchanged
/// to this callback before a ceremony can proceed.
/// </para>
/// <para>
/// Approval does not replace the RP server's independent challenge, origin, RP ID hash,
/// signature, UP/UV, and credential/account checks. <see cref="WebAuthnOrigin"/> still represents
/// only HTTPS or HTTP localhost origins, not arbitrary native or opaque origins.
/// </para>
/// </remarks>
public delegate bool RpIdValidator(WebAuthnOrigin origin, string rpId);
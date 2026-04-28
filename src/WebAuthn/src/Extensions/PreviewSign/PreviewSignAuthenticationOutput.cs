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

namespace Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;

/// <summary>
/// Output from previewSign authentication ceremony.
/// </summary>
/// <remarks>
/// <para>
/// Contains the raw signature over the to-be-signed data. Unlike standard WebAuthn assertions,
/// this signature does NOT include clientDataJSON or authenticator data wrapping.
/// </para>
/// <para>
/// Per CTAP v4 draft specification §6.2:
/// - Signature is raw bytes in COSE signature format
/// - No clientDataJSON wrapping
/// - No authenticator data in what's signed
/// - Signature format depends on the algorithm used during registration
/// </para>
/// </remarks>
/// <param name="Signature">
/// Raw signature bytes over the to-be-signed data. Format depends on the COSE algorithm
/// (e.g., ECDSA produces r||s concatenation, EdDSA produces 64-byte signature).
/// </param>
public sealed record class PreviewSignAuthenticationOutput(ReadOnlyMemory<byte> Signature);

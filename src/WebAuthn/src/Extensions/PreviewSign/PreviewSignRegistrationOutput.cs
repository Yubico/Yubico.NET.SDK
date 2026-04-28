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
/// Output from previewSign registration ceremony.
/// </summary>
/// <remarks>
/// <para>
/// Contains the generated signing key details including the key handle, public key,
/// algorithm, attestation object, and user verification flags.
/// </para>
/// <para>
/// The attestation object is the authoritative source for the public key and should
/// be verified before trusting the other fields (per CTAP v4 draft §4).
/// </para>
/// </remarks>
/// <param name="GeneratedKey">
/// The generated signing key including key handle, public key, algorithm, attestation,
/// and flags policy.
/// </param>
public sealed record class PreviewSignRegistrationOutput(GeneratedSigningKey GeneratedKey);

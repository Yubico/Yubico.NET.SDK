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
/// Output from the previewSign extension registration.
/// </summary>
/// <remarks>
/// <para>
/// Represents the generated key material returned by the YubiKey during
/// previewSign extension registration.
/// </para>
/// </remarks>
/// <param name="GeneratedKey">
/// The generated signing key.
/// </param>
public sealed record class PreviewSignRegistrationOutput(GeneratedSigningKey GeneratedKey);

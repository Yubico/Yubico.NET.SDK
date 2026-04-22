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

using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.WebAuthn.Extensions.Inputs;

/// <summary>
/// Input for the credProtect extension during registration.
/// </summary>
/// <remarks>
/// Specifies the credential protection policy, controlling when user verification is required.
/// </remarks>
/// <param name="Policy">The credential protection policy level.</param>
/// <param name="EnforceCredentialProtectionPolicy">
/// If true, credential creation fails if the policy cannot be honored.
/// </param>
public sealed record class CredProtectInput(
    CredProtectPolicy Policy,
    bool EnforceCredentialProtectionPolicy = false);

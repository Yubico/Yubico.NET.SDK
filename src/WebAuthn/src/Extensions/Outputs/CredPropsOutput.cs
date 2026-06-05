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

namespace Yubico.YubiKit.WebAuthn.Extensions.Outputs;

/// <summary>
/// Output from the credProps extension during registration.
/// </summary>
/// <param name="ResidentKey">
/// Whether the credential is a resident/discoverable credential.
/// Null if the property could not be determined.
/// </param>
public sealed record class CredPropsOutput(bool? ResidentKey);

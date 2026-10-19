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
/// PRF evaluation results (1-2 outputs).
/// </summary>
/// <param name="First">The first PRF output (required).</param>
/// <param name="Second">The second PRF output (optional).</param>
public sealed record class PrfEvaluationResults(
    ReadOnlyMemory<byte> First,
    ReadOnlyMemory<byte>? Second = null);

/// <summary>
/// Output from the PRF extension during registration (WebAuthn L3 §10.1.4).
/// </summary>
/// <param name="Enabled">
/// "true if, and only if, the PRF is available for use with the created credential": the authenticator's
/// <c>hmac-secret</c> boolean, false when absent or not a boolean.
/// </param>
/// <param name="Results">
/// The evaluation results when <c>eval</c> was given and the authenticator evaluated it at creation time
/// through CTAP 2.2-2.3 §12.8 <c>hmac-secret-mc</c>; otherwise null ("Outputs may not be available during registration").
/// </param>
public sealed record class PrfRegistrationOutput(bool Enabled, PrfEvaluationResults? Results = null);

/// <summary>
/// Output from the PRF extension during authentication.
/// </summary>
/// <param name="Results">The PRF evaluation results.</param>
public sealed record class PrfAuthenticationOutput(PrfEvaluationResults Results);
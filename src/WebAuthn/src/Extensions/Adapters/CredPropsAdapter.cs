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

using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Adapters;

/// <summary>
/// Adapter for the credProps extension.
/// </summary>
/// <remarks>
/// CredProps is a client-derived extension - the authenticator doesn't send any output.
/// The client derives the "rk" property from the residentKey option that was set.
/// </remarks>
internal static class CredPropsAdapter
{
    /// <summary>
    /// Derives credProps output from the effective <c>requireResidentKey</c> the client used.
    /// </summary>
    /// <remarks>
    /// WebAuthn L3 §10.1.3: the <c>rk</c> output is "the value of requireResidentKey that was used" in
    /// the ceremony. The client resolves <c>preferred</c> against the authenticator's capability before
    /// the request, so the value is always known.
    /// </remarks>
    /// <param name="requireResidentKey">Whether the client requested a discoverable credential.</param>
    /// <returns>The derived credProps output.</returns>
    public static CredPropsOutput DeriveOutput(bool requireResidentKey) => new(requireResidentKey);
}
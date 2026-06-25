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
using Yubico.YubiKit.WebAuthn.Preferences;

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
    /// Derives credProps output from the residentKey option.
    /// </summary>
    /// <param name="residentKeyPreference">The resident key preference from registration options.</param>
    /// <returns>The derived credProps output.</returns>
    public static CredPropsOutput DeriveOutput(ResidentKeyPreference residentKeyPreference)
    {
        // Per WebAuthn spec: rk is true if residentKey was "required", false if "discouraged",
        // and null if the client cannot determine (e.g., "preferred")
        bool? rk = residentKeyPreference switch
        {
            ResidentKeyPreference.Required => true,
            ResidentKeyPreference.Discouraged => false,
            ResidentKeyPreference.Preferred => null, // Cannot determine without authenticator confirmation
            _ => null
        };

        return new CredPropsOutput(rk);
    }
}

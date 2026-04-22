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

using System.Formats.Cbor;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Adapters;

/// <summary>
/// Adapter for the credProtect extension.
/// </summary>
internal static class CredProtectAdapter
{
    /// <summary>
    /// Applies credProtect input to the CTAP extension builder.
    /// </summary>
    public static void ApplyToBuilder(ExtensionBuilder builder, CredProtectInput input)
    {
        builder.WithCredProtect(input.Policy, input.EnforceCredentialProtectionPolicy);
    }

    /// <summary>
    /// Parses credProtect output from authenticator data extensions.
    /// </summary>
    public static CredProtectOutput? ParseRegistrationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.CredProtect, out var rawValue))
        {
            return null;
        }

        var reader = new CborReader(rawValue, CborConformanceMode.Lax);
        var policyValue = reader.ReadInt32();

        if (policyValue is < 1 or > 3)
        {
            return null;
        }

        return new CredProtectOutput((CredProtectPolicy)policyValue);
    }
}

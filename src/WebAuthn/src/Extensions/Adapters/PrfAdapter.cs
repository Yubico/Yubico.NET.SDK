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
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Adapters;

/// <summary>
/// Adapter for the PRF (Pseudo-Random Function) extension.
/// </summary>
internal static class PrfAdapter
{
    /// <summary>
    /// Applies PRF input to the CTAP extension builder for registration.
    /// </summary>
    public static void ApplyToBuilderForRegistration(ExtensionBuilder builder, PrfInput input)
    {
        // For registration, just signal PRF support request
        builder.WithPrf();
    }

    /// <summary>
    /// Applies PRF input to the CTAP extension builder for authentication.
    /// </summary>
    public static void ApplyToBuilderForAuthentication(
        ExtensionBuilder builder,
        PrfInput input,
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowCredentials)
    {
        // If evalByCredential is set and there are allowed credentials, apply filtering
        if (input.EvalByCredential is not null && allowCredentials is not null && allowCredentials.Count > 0)
        {
            // CTAP only supports a single salt pair per request
            // Select the first matching credential from evalByCredential that's in the allow list
            if (input.EvalByCredential.Count > 1)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.NotSupported,
                    "PRF evalByCredential with multiple entries is not supported; CTAP supports only a single salt pair per request. Use 'First/Second' instead.");
            }

            // Use the provided prfInput directly (caller has already filtered/selected)
            builder.WithPrf(input);
        }
        else if (input.First is not null)
        {
            // Use direct First/Second evaluation
            builder.WithPrf(input);
        }
        else
        {
            // No eval specified, just signal support
            builder.WithPrf();
        }
    }

    /// <summary>
    /// Parses PRF output from registration.
    /// </summary>
    public static PrfRegistrationOutput? ParseRegistrationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.Prf, out var rawValue))
        {
            return null;
        }

        // For registration, presence of the extension indicates it's enabled
        return new PrfRegistrationOutput(Enabled: true);
    }

    /// <summary>
    /// Parses PRF output from authentication.
    /// </summary>
    public static PrfAuthenticationOutput? ParseAuthenticationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.Prf, out var rawValue))
        {
            return null;
        }

        // Parse the PRF results map
        var reader = new CborReader(rawValue, CborConformanceMode.Lax);
        var mapLength = reader.ReadStartMap();

        if (mapLength is null or 0)
        {
            return null;
        }

        // Look for "eval" key with results
        for (var i = 0; i < mapLength; i++)
        {
            var key = reader.ReadTextString();
            if (key == "eval")
            {
                // Delegate to Fido2's PrfOutput decoder
                var prfOutput = PrfOutput.Decode(reader);
                if (prfOutput is null || !prfOutput.First.HasValue)
                {
                    return null;
                }

                var results = new PrfEvaluationResults(prfOutput.First.Value, prfOutput.Second);
                return new PrfAuthenticationOutput(results);
            }
            else
            {
                reader.SkipValue();
            }
        }

        return null;
    }
}
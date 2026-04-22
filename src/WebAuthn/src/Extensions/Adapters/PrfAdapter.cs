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
/// Adapter for the PRF (Pseudo-Random Function) extension.
/// </summary>
internal static class PrfAdapter
{
    /// <summary>
    /// Applies PRF input to the CTAP extension builder for registration.
    /// </summary>
    public static void ApplyToBuilderForRegistration(ExtensionBuilder builder, Inputs.PrfInput input)
    {
        // For registration, just signal PRF support request
        builder.WithPrf();
    }

    /// <summary>
    /// Applies PRF input to the CTAP extension builder for authentication.
    /// </summary>
    public static void ApplyToBuilderForAuthentication(
        ExtensionBuilder builder,
        Inputs.PrfInput input,
        IReadOnlyList<WebAuthnCredentialDescriptor>? allowCredentials)
    {
        // If evalByCredential is set, filter to allowed credentials
        if (input.EvalByCredential is not null && allowCredentials is not null)
        {
            // Filter evalByCredential to only include credentials in the allow list
            var allowedIds = new HashSet<ReadOnlyMemory<byte>>(
                allowCredentials.Select(c => c.Id),
                new ByteArrayComparer());

            var filteredEvals = input.EvalByCredential
                .Where(kvp => allowedIds.Contains(kvp.Key))
                .ToList();

            if (filteredEvals.Any())
            {
                // CTAP only supports a single salt pair per request
                if (filteredEvals.Count > 1)
                {
                    throw new WebAuthnClientError(
                        WebAuthnClientErrorCode.NotSupported,
                        "PRF evalByCredential matched multiple credentials in allowList; CTAP supports only a single salt pair per request. Scope your allow list or use 'eval' instead.");
                }

                // Use the first matching credential's evaluation
                var firstEval = filteredEvals.First().Value;
                var prfInput = new Fido2.Extensions.PrfInput
                {
                    First = firstEval.First,
                    Second = firstEval.Second
                };
                builder.WithPrf(prfInput);
                return;
            }
        }

        // Use default eval if present
        if (input.Eval is not null)
        {
            var prfInput = new Fido2.Extensions.PrfInput
            {
                First = input.Eval.First,
                Second = input.Eval.Second
            };
            builder.WithPrf(prfInput);
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
                return ParsePrfEvalResults(reader);
            }
            else
            {
                reader.SkipValue();
            }
        }

        return null;
    }

    private static PrfAuthenticationOutput? ParsePrfEvalResults(CborReader reader)
    {
        var resultsMapLength = reader.ReadStartMap();
        if (resultsMapLength is null or 0)
        {
            return null;
        }

        ReadOnlyMemory<byte>? first = null;
        ReadOnlyMemory<byte>? second = null;

        for (var i = 0; i < resultsMapLength; i++)
        {
            var key = reader.ReadTextString();
            if (key == "first")
            {
                first = reader.ReadByteString();
            }
            else if (key == "second")
            {
                second = reader.ReadByteString();
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap(); // Close eval results map

        if (!first.HasValue)
        {
            return null;
        }

        var results = new PrfEvaluationResults(first.Value, second);
        return new PrfAuthenticationOutput(results);
    }

    /// <summary>
    /// Byte array comparer for ReadOnlyMemory{byte} dictionary keys.
    /// </summary>
    private class ByteArrayComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            var hash = new HashCode();
            hash.AddBytes(obj.Span);
            return hash.ToHashCode();
        }
    }
}

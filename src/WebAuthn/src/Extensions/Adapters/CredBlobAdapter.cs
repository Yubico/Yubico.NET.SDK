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
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Adapters;

/// <summary>
/// Adapter for the credBlob extension.
/// </summary>
internal static class CredBlobAdapter
{
    /// <summary>
    /// Applies credBlob input to the CTAP extension builder.
    /// </summary>
    public static void ApplyToBuilder(ExtensionBuilder builder, CredBlobInput input)
    {
        builder.WithCredBlob(input.Blob);
    }

    /// <summary>
    /// Parses credBlob output from registration (returns boolean "stored" indicator).
    /// </summary>
    public static CredBlobMakeCredentialOutput? ParseRegistrationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.CredBlob, out var rawValue))
        {
            return null;
        }

        var reader = new CborReader(rawValue, CborConformanceMode.Lax);
        return CredBlobMakeCredentialOutput.Decode(reader);
    }

    /// <summary>
    /// Parses credBlob output from authentication (returns actual blob data).
    /// </summary>
    public static CredBlobAssertionOutput? ParseAuthenticationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.CredBlob, out var rawValue))
        {
            return null;
        }

        var reader = new CborReader(rawValue, CborConformanceMode.Lax);
        return CredBlobAssertionOutput.Decode(reader);
    }
}
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
/// Adapter for the credBlob extension.
/// </summary>
internal static class CredBlobAdapter
{
    /// <summary>
    /// Applies credBlob input to the CTAP extension builder.
    /// </summary>
    public static void ApplyToBuilder(ExtensionBuilder builder, Inputs.CredBlobInput input)
    {
        input.Validate();
        builder.WithCredBlob(input.Blob);
    }

    /// <summary>
    /// Parses credBlob output from registration (returns boolean "stored" indicator).
    /// </summary>
    public static Outputs.CredBlobOutput? ParseRegistrationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.CredBlob, out var rawValue))
        {
            return null;
        }

        var reader = new CborReader(rawValue, CborConformanceMode.Lax);

        // Registration returns boolean
        if (reader.PeekState() == CborReaderState.Boolean)
        {
            return new Outputs.CredBlobOutput(reader.ReadBoolean());
        }

        return null;
    }

    /// <summary>
    /// Parses credBlob output from authentication (returns actual blob data).
    /// </summary>
    public static Outputs.CredBlobAssertionOutput? ParseAuthenticationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.CredBlob, out var rawValue))
        {
            return null;
        }

        var reader = new CborReader(rawValue, CborConformanceMode.Lax);

        // Authentication returns byte string
        if (reader.PeekState() == CborReaderState.ByteString)
        {
            var blob = reader.ReadByteString();
            // Per CTAP2.1, credBlob must be 1-32 bytes
            if (blob.Length is < 1 or > 32)
            {
                return null;
            }
            return new Outputs.CredBlobAssertionOutput(blob);
        }

        return null;
    }
}

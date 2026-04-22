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
/// Adapter for the largeBlob extension.
/// </summary>
internal static class LargeBlobAdapter
{
    /// <summary>
    /// Applies largeBlob input to the CTAP extension builder for registration.
    /// </summary>
    public static void ApplyToBuilder(ExtensionBuilder builder, Inputs.LargeBlobInput input)
    {
        // Check if Required enforcement was requested (not yet implemented)
        if (input.Support == Inputs.LargeBlobSupport.Required)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "LargeBlob support 'Required' enforcement is not yet implemented (Phase 6 scope deferred). Use 'Preferred' or upgrade SDK.");
        }

        // For registration, signal largeBlobKey request
        builder.WithLargeBlobKey();
    }

    /// <summary>
    /// Parses largeBlob output from registration.
    /// </summary>
    public static LargeBlobRegistrationOutput? ParseRegistrationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        // Check for largeBlobKey extension output
        if (extensions.TryGetValue(ExtensionIdentifiers.LargeBlobKey, out var keyValue))
        {
            // Key present means supported
            return new LargeBlobRegistrationOutput(Supported: true);
        }

        // No key means not supported
        return new LargeBlobRegistrationOutput(Supported: false);
    }

    /// <summary>
    /// Parses largeBlob output from authentication.
    /// </summary>
    public static LargeBlobAuthenticationOutput? ParseAuthenticationOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        // Phase 6 simplified scope - placeholder
        // Full read/write operations would be implemented here
        return null;
    }
}

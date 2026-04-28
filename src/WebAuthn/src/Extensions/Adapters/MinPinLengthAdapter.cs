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

namespace Yubico.YubiKit.WebAuthn.Extensions.Adapters;

/// <summary>
/// Adapter for the minPinLength extension.
/// </summary>
internal static class MinPinLengthAdapter
{
    /// <summary>
    /// Applies minPinLength input to the CTAP extension builder.
    /// </summary>
    public static void ApplyToBuilder(ExtensionBuilder builder)
    {
        builder.WithMinPinLength();
    }

    /// <summary>
    /// Parses minPinLength output from authenticator data extensions.
    /// </summary>
    public static MinPinLengthOutput? ParseOutput(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> extensions)
    {
        if (!extensions.TryGetValue(ExtensionIdentifiers.MinPinLength, out var rawValue))
        {
            return null;
        }

        var reader = new CborReader(rawValue, CborConformanceMode.Lax);
        return MinPinLengthOutput.Decode(reader);
    }
}
// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// Output from the previewSign extension authentication.
/// </summary>
/// <remarks>
/// Contains the signature over the to-be-signed data.
/// </remarks>
public sealed class PreviewSignAuthenticationOutput
{
    /// <summary>
    /// Gets the signature bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Signature { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="PreviewSignAuthenticationOutput"/>.
    /// </summary>
    /// <param name="signature">The signature bytes.</param>
    public PreviewSignAuthenticationOutput(ReadOnlyMemory<byte> signature)
    {
        if (signature.Length == 0)
        {
            throw new ArgumentException("Signature must not be empty.", nameof(signature));
        }

        Signature = signature;
    }
}

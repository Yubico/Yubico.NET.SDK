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

using System;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Extension methods for retrieving previewSign data from FIDO2 response objects.
    /// </summary>
    public static class PreviewSignExtensionMethods
    {
        /// <summary>
        /// Retrieves the previewSign generated key from the extension outputs.
        /// </summary>
        /// <param name="data">The MakeCredential response data.</param>
        /// <returns>
        /// A <see cref="PreviewSignGeneratedKey"/> if the extension was used and returned
        /// data; otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="Ctap2DataException">
        /// The extension outputs are present but malformed or incomplete.
        /// </exception>
        public static PreviewSignGeneratedKey? GetPreviewSignGeneratedKey(
            this MakeCredentialData data)
        {
            CoseAlgorithmIdentifier? signedAlgorithm = null;
            byte[]? signedValue = null;
            if (data.AuthenticatorData.Extensions is not null &&
                data.AuthenticatorData.Extensions.TryGetValue(Extensions.PreviewSign, out signedValue))
            {
                signedAlgorithm = PreviewSignExtension.DecodeGeneratedKeyAlgorithm(signedValue);
            }

            if (data.UnsignedExtensionOutputs is not null &&
                data.UnsignedExtensionOutputs.TryGetValue(Extensions.PreviewSign, out var unsignedValue))
            {
                if (signedAlgorithm is null)
                {
                    throw new Ctap2DataException(
                        "previewSign generated key is missing signed algorithm output.");
                }

                return PreviewSignExtension.DecodeGeneratedKey(unsignedValue, signedAlgorithm);
            }

            return signedValue is null
                ? null
                : throw new Ctap2DataException(
                    "previewSign generated key is missing unsigned extension output.");
        }
    }
}

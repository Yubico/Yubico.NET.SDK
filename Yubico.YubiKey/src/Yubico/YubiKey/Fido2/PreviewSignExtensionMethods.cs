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
            if (data.AuthenticatorData.Extensions is null ||
                !data.AuthenticatorData.Extensions.TryGetValue(Extensions.PreviewSign, out byte[]? signedValue))
            {
                return null;
            }

            if (data.UnsignedExtensionOutputs is null ||
                !data.UnsignedExtensionOutputs.TryGetValue(Extensions.PreviewSign, out var unsignedValue))
            {
                return null;
            }

            return PreviewSignExtension.DecodeGeneratedKey(signedValue, unsignedValue);
        }
    }
}

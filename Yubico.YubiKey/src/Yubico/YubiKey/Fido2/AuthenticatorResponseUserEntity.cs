// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Similar to <see cref="PublicKeyCredentialUserEntity" />, but all non-<c>Id</c> properties are optional
    /// </summary>
    /// <remarks>
    /// Authenticators do not necessarily return user data beyond the ID in GetAssertion responses.
    /// Specifically, if there is only one credential for the RP or the authenticator has its 
    /// own display, only the <c>Id</c> property will be present.
    /// See section 5.2 of the CTAP2 spec for more details.
    /// </remarks>
    /// [CborSerializable]
    internal class AuthenticatorResponseUserEntity
    {
        /// <summary>
        /// An optionally present human-palatable name for the user account. It is intended only for display.
        /// </summary>
        /// <example>"alex.p.mueller@example.com"</example>
        public string? Name { get; set; }

        /// <summary>
        /// The user handle of the user account entity. Maximum length is 64 bytes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] Id { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// An optionally present human-palatable name for the user account, chosen by the user, intended only for display.
        /// </summary>
        /// <example>"Alex P. Müller"</example>
        public string? DisplayName { get; set; }

        /// <summary>
        /// An optionally present serialized URL which resolves to an image associated with the user. Maximum length is 128 bytes.
        /// </summary>
        public Uri? Icon { get; set; }

        public const int MaximumUserIdLength = 64;
    }
}

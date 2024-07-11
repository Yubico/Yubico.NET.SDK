// Copyright 2022 Yubico AB
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
using System.Globalization;
using System.Text;

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    ///     The public properties of a long-lived secret stored in
    ///     the YubiHSM Auth application. The associated secret is
    ///     used to establish a secure session to a YubiHSM 2 device.
    /// </summary>
    public class Credential
    {
        /// <summary>
        ///     Minimum length of the <see cref="Label" />.
        /// </summary>
        public const int MinLabelByteCount = 1;

        /// <summary>
        ///     Maximum length of the <see cref="Label" />.
        /// </summary>
        public const int MaxLabelByteCount = 64;

        private static readonly Encoding _utf8ThrowOnInvalidBytes = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private CryptographicKeyType _keyType = CryptographicKeyType.None;
        private string _label = string.Empty;

        /// <summary>
        ///     Constructs an instance of the <see cref="Credential" /> class
        ///     with the provided arguments.
        /// </summary>
        /// <param name="keyType">
        ///     <para>
        ///         <inheritdoc cref="KeyType" path="/summary" />
        ///     </para>
        ///     <para>
        ///         <inheritdoc cref="KeyType" path="/remarks" />
        ///     </para>
        /// </param>
        /// <param name="label">
        ///     <para>
        ///         <inheritdoc cref="Label" path="/summary" />
        ///     </para>
        ///     <para>
        ///         <inheritdoc cref="Label" path="/remarks" />
        ///     </para>
        /// </param>
        /// <param name="touchRequired">
        ///     <inheritdoc cref="TouchRequired" path="/summary" />
        /// </param>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="label" /> has an invalid length.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="keyType" /> is an invalid value.
        /// </exception>
        public Credential(CryptographicKeyType keyType, string label, bool touchRequired)
        {
            KeyType = keyType;
            Label = label;
            TouchRequired = touchRequired;
        }

        /// <summary>
        ///     Constructs an instance of the <see cref="Credential" /> class.
        /// </summary>
        /// <remarks>
        ///     This constructor is provided to allow for object-initializer syntax.
        ///     The default values are not guaranteed to be valid, so all properties
        ///     should be set manually.
        /// </remarks>
        public Credential()
        {
        }

        /// <summary>
        ///     The cryptographic algorithm associated with the Credential.
        /// </summary>
        /// <remarks>
        ///     The value is not allowed to be <see cref="CryptographicKeyType.None" />.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value is not defined in <see cref="CryptographicKeyType" />,
        ///     or if there was an attempt to set it to <see cref="CryptographicKeyType.None" />.
        /// </exception>
        public CryptographicKeyType KeyType
        {
            get => _keyType;

            set
            {
                if (Enum.IsDefined(typeof(CryptographicKeyType), value)
                    && value != CryptographicKeyType.None)
                {
                    _keyType = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }

        /// <summary>
        ///     A short name or description of the Credential.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The string only contains characters that can be encoded with UTF-8,
        ///         and its UTF-8 byte count is between <see cref="MinLabelByteCount" />
        ///         and <see cref="MaxLabelByteCount" />. Non-printing characters are
        ///         allowed, as long as they can be encoded with UTF-8. For example,
        ///         null (UTF-8: 0x00) and Right-To-Left Mark U+200F (UTF-8: 0xE2 0x80
        ///         0x8F) would be acceptable.
        ///     </para>
        ///     <para>
        ///         The <see cref="UTF8Encoding" /> class contains methods such as
        ///         <see cref="UTF8Encoding.GetByteCount(string)" /> which can be used
        ///         to validate the string prior to attempting to set it here. It is
        ///         recommended to use the constructor
        ///         <see cref="UTF8Encoding(bool, bool)" /> so error detection is
        ///         enabled for invalid characters.
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the supplied string is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the string's UTF-8 byte count does not meet the length
        ///     parameters <see cref="MinLabelByteCount" /> and
        ///     <see cref="MaxLabelByteCount" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when there is a character that cannot be encoded with UTF-8.
        ///     The exact exception may be derived from ArgumentException.
        /// </exception>
        public string Label
        {
            get => _label;

            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                int byteCount = _utf8ThrowOnInvalidBytes.GetByteCount(value);

                if (byteCount < MinLabelByteCount || byteCount > MaxLabelByteCount)
                {
                    throw new ArgumentOutOfRangeException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.YubiHsmAuthInvalidCredentialLabelLength,
                            MinLabelByteCount,
                            MaxLabelByteCount,
                            byteCount));
                }

                _label = value;
            }
        }

        /// <summary>
        ///     Describes if the user is required to touch the YubiKey
        ///     when accessing the Credential.
        /// </summary>
        public bool TouchRequired { get; set; }
    }
}

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

namespace Yubico.YubiKey.YubiHsmAuth
{
    /// <summary>
    /// The public properties of a long-lived secret stored in
    /// the YubiHSM Auth application. The associated secret is
    /// used to establish a secure session to a YubiHSM 2 device.
    /// </summary>
    public class Credential
    {
        private CryptographicKeyType _keyType = CryptographicKeyType.None;
        private string _label = string.Empty;

        /// <summary>
        /// Minimum length of the <see cref="Label"/>.
        /// </summary>
        public const int MinLabelLength = 1;

        /// <summary>
        /// Maximum length of the <see cref="Label"/>.
        /// </summary>
        public const int MaxLabelLength = 64;

        /// <summary>
        /// The cryptographic algorithm associated with the Credential.
        /// </summary>
        /// <remarks>
        /// The value is not allowed to be <see cref="CryptographicKeyType.None"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is not defined in <see cref="CryptographicKeyType"/>,
        /// or if there was an attempt to set it to <see cref="CryptographicKeyType.None"/>.
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
        /// A short name or description of the Credential.
        /// </summary>
        /// <remarks>
        /// There is a minimum and maximum string length as defined
        /// by <see cref="MinLabelLength"/> and
        /// <see cref="MaxLabelLength"/>, respectively.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown if there is an attempt to set the value to a string
        /// which does not meet the length parameters as specified by
        /// <see cref="MinLabelLength"/> and <see cref="MaxLabelLength"/>.
        /// </exception>
        public string Label
        {
            get => _label;

            set
            {
                if (!(value is null)
                    && value.Length >= MinLabelLength
                    && value.Length <= MaxLabelLength)
                {
                    _label = value;
                }
                else
                {
                    int actualLength = string.IsNullOrEmpty(value) ? 0 : value!.Length;

                    throw new ArgumentException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.InvalidStringLength,
                                MinLabelLength,
                                MaxLabelLength,
                                actualLength));
                }
            }
        }

        /// <summary>
        /// Describes if the user is required to touch the YubiKey
        /// when accessing the Credential.
        /// </summary>
        public bool TouchRequired { get; set; }

        /// <summary>
        /// Constructs an instance of the <see cref="Credential"/> class
        /// with the provided arguments.
        /// </summary>
        /// <param name="keyType">
        /// <para>
        /// <inheritdoc cref="KeyType" path="/summary"/>
        /// </para>
        /// <para>
        /// <inheritdoc cref="KeyType" path="/remarks"/>
        /// </para>
        /// </param>
        /// <param name="label">
        /// <para>
        /// <inheritdoc cref="Label" path="/summary"/>
        /// </para>
        /// <para>
        /// <inheritdoc cref="Label" path="/remarks"/>
        /// </para>
        /// </param>
        /// <param name="touchRequired">
        /// <inheritdoc cref="TouchRequired" path="/summary"/>
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="label"/> has an invalid length.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="keyType"/> is an invalid value.
        /// </exception>
        public Credential(CryptographicKeyType keyType, string label, bool touchRequired)
        {
            KeyType = keyType;
            Label = label;
            TouchRequired = touchRequired;
        }

        /// <summary>
        /// Constructs an instance of the <see cref="Credential"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is provided to allow for object-initializer syntax.
        /// The default values are not guaranteed to be valid, so all properties
        /// should be set manually.
        /// </remarks>
        public Credential()
        {

        }
    }
}

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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// These are the possible values of FIDO2
    /// <see cref="AuthenticatorInfo.Options"/>.
    /// </summary>
    /// <remarks>
    /// If an option is specified, it will be <c>True</c> or <c>False</c>. If it
    /// is not specified, it is its default value. Each option has its default
    /// defined in section 6.4 of the FIDO2 standard. A default value can be
    /// either <c>True</c>, <c>False</c>, or <c>NotSupported</c>. This enum adds
    /// one more value, <c>Unknown</c>, if the given input option is not one
    /// defined in the FIDO2 standard.
    /// <para>
    /// See also <see cref="AuthenticatorOptions.GetDefaultOptionValue"/>.
    /// </para>
    /// </remarks>
    public enum OptionValue
    {
        /// <summary>
        /// The option is not listed and is not described in the standard, hence
        /// its value is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The value of the option, either specified or by default, is
        /// <c>true</c>.
        /// </summary>
        True = 1,

        /// <summary>
        /// The value of the option, either specified or by default, is
        /// <c>false</c>.
        /// </summary>
        False = 2,

        /// <summary>
        /// The option is not listed and the default is "Not Supported".
        /// </summary>
        NotSupported = 3,
    }
}

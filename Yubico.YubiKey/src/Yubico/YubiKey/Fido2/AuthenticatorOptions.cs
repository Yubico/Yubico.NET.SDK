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
    /// All the standard Option strings.
    /// </summary>
    /// <remarks>
    /// Some FIDO2 operations request the caller specify options. They are always
    /// provided as a key/value pair where the key is a string and the value is a
    /// boolean.
    /// <para>
    /// See section 6.4 of the FIDO2 standard for a list of all the defined
    /// options and their meanings. It is the chart with the column Option ID.
    /// Each option ID is a key of key/value. It is also the string that should
    /// be used. For example, the Option ID of <c>credMgmt</c> is defined as
    /// "Credential Management Support" and the meanings of <c>true</c>,
    /// <c>false</c>, and not present are described. To specify this Option, use
    /// the string "credMgmt".
    /// </para>
    /// <para>
    /// When specifying an option (e.g.,
    /// see <see cref="MakeCredentialParameters.Options"/> of the
    /// <c>MakeCredentialParameters</c> class) provide the string and boolean.
    /// You can use this class to specify the string (the key of key/value). This
    /// might be helpful if you rely on IDE features such as autocompletion or
    /// symbolic searching. You can also specify the option string directly, if
    /// you so choose. Either choice is valid; you should choose the one most
    /// appropriate to your project's development style.
    /// <code language="csharp">
    ///   var makeCredParams = new MakeCredentialParameters(relyingParty, userEntity)
    ///   {
    ///       ClientDataHash = clientDataHash,
    ///   };
    ///   makeCredParams.AddOption("rk", true);
    ///   // or
    ///   makeCredParams.AddOption(AuthenticatorOptions.rk, true);
    /// </code>
    /// </para>
    /// </remarks>
    public static class AuthenticatorOptions
    {
        /// <summary>
        /// The string to use when specifying the <c>plat</c> Option.
        /// </summary>
        public const string plat = "plat";

        /// <summary>
        /// The string to use when specifying the <c>rk</c> Option.
        /// </summary>
        public const string rk = "rk";

        /// <summary>
        /// The string to use when specifying the <c>clientPin</c> Option.
        /// </summary>
        public const string clientPin = "clientPin";

        /// <summary>
        /// The string to use when specifying the <c>up</c> Option.
        /// </summary>
        public const string up = "up";

        /// <summary>
        /// The string to use when specifying the <c>uv</c> Option.
        /// </summary>
        public const string uv = "uv";

        /// <summary>
        /// The string to use when specifying the <c>pinUvAuthToken</c> Option.
        /// </summary>
        public const string pinUvAuthToken = "pinUvAuthToken";

        /// <summary>
        /// The string to use when specifying the <c>noMcGaPermissionsWithClientPin</c>
        /// Option.
        /// </summary>
        public const string noMcGaPermissionsWithClientPin = "noMcGaPermissionsWithClientPin";

        /// <summary>
        /// The string to use when specifying the <c>largeBlobs</c> Option.
        /// </summary>
        public const string largeBlobs = "largeBlobs";

        /// <summary>
        /// The string to use when specifying the <c>ep</c> Option.
        /// </summary>
        public const string ep = "ep";

        /// <summary>
        /// The string to use when specifying the <c>bioEnroll</c> Option.
        /// </summary>
        public const string bioEnroll = "bioEnroll";

        /// <summary>
        /// The string to use when specifying the <c>userVerificationMgmtPreview</c>
        /// Option.
        /// </summary>
        public const string userVerificationMgmtPreview = "userVerificationMgmtPreview";

        /// <summary>
        /// The string to use when specifying the <c>uvBioEnroll</c> Option.
        /// </summary>
        public const string uvBioEnroll = "uvBioEnroll";

        /// <summary>
        /// The string to use when specifying the <c>authnrCfg</c> Option.
        /// </summary>
        public const string authnrCfg = "authnrCfg";

        /// <summary>
        /// The string to use when specifying the <c>uvAcfg</c> Option.
        /// </summary>
        public const string uvAcfg = "uvAcfg";

        /// <summary>
        /// The string to use when specifying the <c>credMgmt</c> Option.
        /// </summary>
        public const string credMgmt = "credMgmt";

        /// <summary>
        /// The string to use when specifying the <c>credentialMgmtPreview</c> Option.
        /// </summary>
        public const string credentialMgmtPreview = "credentialMgmtPreview";

        /// <summary>
        /// The string to use when specifying the <c>setMinPINLength</c> Option.
        /// </summary>
        public const string setMinPINLength = "setMinPINLength";

        /// <summary>
        /// The string to use when specifying the <c>makeCredUvNotRqd</c> Option.
        /// </summary>
        public const string makeCredUvNotRqd = "makeCredUvNotRqd";

        /// <summary>
        /// The string to use when specifying the <c>alwaysUv</c> Option.
        /// </summary>
        public const string alwaysUv = "alwaysUv";

        /// <summary>
        /// Return the default value for the given <c>option</c>.
        /// </summary>
        /// <remarks>
        /// If an option is listed in the <see cref="AuthenticatorInfo.Options"/>
        /// property, it will be <c>true</c> or <c>false</c>. If it is not
        /// listed, it will be a default value. The default value can be either
        /// True, False, or Not Supported. This method returns the default value
        /// for an option.
        /// <para>
        /// There is one more value it could be, Unknown. This happens when the
        /// option is a string not in the list here in this class.
        /// </para>
        /// <para>
        /// Note that this method is not returning the current value of the given
        /// <c>option</c>, only what its default value is.
        /// </para>
        /// </remarks>
        /// <returns>
        /// An <c>OptionValue</c> enum that specifies the default value for an
        /// option as either <c>True</c>, <c>False</c>, <c>NotSupported</c>, or
        /// <c>Unknown</c>.
        /// </returns>
        public static OptionValue GetDefaultOptionValue(string option)
        {
            switch (option)
            {
                case plat:
                case rk:
                case noMcGaPermissionsWithClientPin:
                case makeCredUvNotRqd:
                    return OptionValue.False;
                case up:
                    return OptionValue.True;
                case clientPin:
                case uv:
                case pinUvAuthToken:
                case largeBlobs:
                case ep:
                case bioEnroll:
                case userVerificationMgmtPreview:
                case uvBioEnroll:
                case authnrCfg:
                case uvAcfg:
                case credMgmt:
                case credentialMgmtPreview:
                case setMinPINLength:
                case alwaysUv:
                    return OptionValue.NotSupported;
                default:
                    return OptionValue.Unknown;
            }
        }
    }
}

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

using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.ParameterBuilder
{
    /// <summary>
    /// A fluent builder interface for specifying the optional parameter components for a <see cref="Fido2Session.MakeCredential"/>
    /// operation.
    /// </summary>
    public interface IMcOptionalBuilder
    {
        /// <summary>
        /// Adds extension specific parameters to the make credential operation.
        /// </summary>
        /// <param name="extensionKey">
        /// The key that identifies the extension.
        /// </param>
        /// <param name="encodedValue">
        /// The encoded parameters for this extension, usually a CBOR map containing multiple parameters.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder AddExtension(string extensionKey, byte[] encodedValue);

        /// <summary>
        /// Adds to the list of supported algorithms for a credential. The only
        /// algorithm that the YubiKey currently supports is ECDSA with SHA-256
        /// using the NIST P-256 curve.
        /// </summary>
        /// <param name="type">
        /// The type or usage of the algorithm. For example, "public-key".
        /// </param>
        /// <param name="alg">
        /// The algorithm to use. For example, <see cref="CoseAlgorithmIdentifier.ES256"/>.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder AddAlgorithm(string type, CoseAlgorithmIdentifier alg);

        /// <summary>
        /// Adds an entry to the exclude list. This allows RPs to limit the the number
        /// of credentials registered to the same account.
        /// </summary>
        /// <param name="credentialId">
        /// The credential identifier to exclude.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder ExcludeCredential(CredentialId credentialId);

        /// <summary>
        /// Indicates that the credential should be discoverable given only the RP ID.
        /// </summary>
        /// <param name="value">
        /// True by default. False if the credential should not be discoverable.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder IsDiscoverableCredential(bool value = true);

        /// <summary>
        /// Indicates that the YubiKey should require a user presence check (touch) to complete the operation.
        /// </summary>
        /// <param name="value">
        /// If this option is present, the value must be `true`. The value is `true` by default.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder RequireUserPresence(bool value = true);

        /// <summary>
        /// Indicates that the YubiKey should require user verification (finger print match) to complete the operation.
        /// </summary>
        /// <remarks>
        /// This option is deprecated by newer versions of the FIDO2 specification, but is still included in the SDK
        /// for completeness and backward compatability. To authenticate to the YubiKey using finger print match (UV),
        /// call the <see cref="Fido2Session.VerifyUv"/> function instead.
        /// </remarks>
        /// <param name="value">
        /// True by default. False if the credential does require user verification.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder RequireUserVerification(bool value = true);

        /// <summary>
        /// Parameters to influence the authenticator make credential operation.
        /// </summary>
        /// <remarks>
        /// This method can be used to add additional options that are not part of the FIDO2 standard or that may not
        /// have been explicitly added to the SDK yet.
        /// </remarks>
        /// <param name="optionName">
        /// The name of the option.
        /// </param>
        /// <param name="value">
        /// Whether the option is set to `true`, or `false`. The value is `true` by default.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder AddOption(string optionName, bool value = true);

        /// <summary>
        /// Indicates whether the credential should use enterprise attestation.
        /// </summary>
        /// <param name="attestationType">
        /// The type of enterprise attestation requested. See <see cref="EnterpriseAttestation"/> for more information.
        /// </param>
        /// <returns>
        /// The builder interface for optional parameters.
        /// </returns>
        IMcOptionalBuilder UseAttestation(EnterpriseAttestation attestationType);

        /// <summary>
        /// Builds a <see cref="MakeCredentialParameters"/> based on the information
        /// provided by the previous fluent builder method calls.
        /// </summary>
        /// <returns>
        /// A <see cref="MakeCredentialParameters"/> instance built from the data
        /// provided by the previous fluent builder calls.
        /// </returns>
        MakeCredentialParameters Build();
    }
}

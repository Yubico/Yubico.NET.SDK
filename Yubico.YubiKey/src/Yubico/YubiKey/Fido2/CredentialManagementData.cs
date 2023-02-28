// Copyright 2023 Yubico AB
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
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains the data returned by the YubiKey after calling one of the
    /// <c>authenticatorCredentialManagement</c> subcommands.
    /// </summary>
    /// <remarks>
    /// When a credential management subcommand is sent to the YubiKey, it
    /// returns data encoded following the definition of the
    /// <c>authenticatorCredentialManagement</c> response. The FIDO2 standard
    /// defines this encoded response as a map of a set of elements. The standard
    /// also specifies which subset of the total data is returned by each
    /// subcommand.
    /// <para>
    /// After calling one of the subcommands, get the data out of the response.
    /// It will be an instance of this class. Only those elements the particular
    /// subcommand returns will be represented in the object, the rest will be
    /// null.
    /// </para>
    /// <para>
    /// For example, if you call the get credential metadata subcommand, the
    /// YubiKey will return the number of discoverable credentials and the
    /// maximum number of credentials the YubiKey can yet hold (i.e. the number
    /// of remaining slots). Hence, the only two properties with values will be
    /// <c>NumberOfDiscoverableCredentials</c> and
    /// <c>RemainingCredentialCount</c>. All other properties will be null.
    /// </para>
    /// </remarks>
    public class CredentialManagementData
    {
        private const int KeyNumberCredentials = 1;
        private const int KeyRemainingCredentialCount = 2;
        private const int KeyRpEntity = 3;
        private const int KeyRpIdHash = 4;
        private const int KeyTotalRps = 5;
        private const int KeyUser = 6;
        private const int KeyCredentialId = 7;
        private const int KeyPublicKey = 8;
        private const int KeyTotalRpCredentials = 9;
        private const int KeyCredProtectPolicy = 10;
        private const int KeyLargeBlobKey = 11;

        /// <summary>
        /// The number of discoverable credentials on the YubiKey. This is not
        /// the total number of credentials, because there could be
        /// non-discoverable credentials as well.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public int? NumberOfDiscoverableCredentials { get; private set; }

        /// <summary>
        /// The number of credentials the YubiKey can still hold.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public int? RemainingCredentialCount { get; private set; }

        /// <summary>
        /// The relying party information when the request for data is one
        /// related to specific relying parties.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public RelyingParty? RelyingParty { get; private set; }

        /// <summary>
        /// The SHA-256 digest of the relying party ID when the request for data
        /// is one related to specific relying parties.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public ReadOnlyMemory<byte>? RelyingPartyIdHash { get; private set; }

        /// <summary>
        /// The total number of relying parties present on the YubiKey, when the
        /// request for data is one related to specific relying parties.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public int? TotalRelyingPartyCount { get; private set; }

        /// <summary>
        /// The user entity for a credential returned.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public UserEntity? User { get; private set; }

        /// <summary>
        /// The credential ID for a credential returned.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public CredentialId? CredentialId {get; private set; }

        /// <summary>
        /// The public key for a credential returned.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public CoseKey? CredentialPublicKey { get; private set; }

        /// <summary>
        /// The total number of credentials present on the YubiKey for a
        /// specidfied relying party.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public int? TotalCredentialsForRelyingParty { get; private set; }

        /// <summary>
        /// The credential protection policy. See section 12.1.1 of the FIDO2
        /// standard for a description of the meanings of the number returned.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public int? CredProtectPolicy { get; private set; }

        /// <summary>
        /// The large blob key for a credential.
        /// </summary>
        /// <remarks>
        /// Not all calls to get credential management data will return this
        /// element, hence, it can be null.
        /// </remarks>
        public ReadOnlyMemory<byte>? LargeBlobKey { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private CredentialManagementData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="CredentialManagementData"/> based on the
        /// given Cbor encoding.
        /// </summary>
        /// <remarks>
        /// The encoding must follow the definition of the
        /// <c>authenticatorCredentialManagement response structure</c> in section
        /// 6.8 of the CTAP 2.1 standard.
        /// </remarks>
        /// <param name="cborEncoding">
        /// The credential data, encoded following the CTAP 2.1 and CBOR (RFC
        /// 8949) standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        /// The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        /// correct encoding for FIDO2 credential management data.
        /// </exception>
        public CredentialManagementData(ReadOnlyMemory<byte> cborEncoding)
        {
            var cborReader = new CborReader(cborEncoding, CborConformanceMode.Ctap2Canonical);
            int? entries = cborReader.ReadStartMap();
            int count = entries ?? 0;

            while (count > 0)
            {
                int currentKey = cborReader.ReadInt32();
                switch (currentKey)
                {
                    default:
                        throw new Ctap2DataException(ExceptionMessages.Ctap2CborUnexpectedKey);

                    case KeyNumberCredentials:
                        NumberOfDiscoverableCredentials = cborReader.ReadInt32();
                        break;

                    case KeyRemainingCredentialCount:
                        RemainingCredentialCount = cborReader.ReadInt32();
                        break;

                    case KeyRpEntity:
                        RelyingParty = new RelyingParty(cborReader.ReadEncodedValue());
                        break;

                    case KeyRpIdHash:
                        RelyingPartyIdHash = cborReader.ReadByteString();
                        break;

                    case KeyTotalRps:
                        TotalRelyingPartyCount = cborReader.ReadInt32();
                        break;

                    case KeyUser:
                        User = new UserEntity(cborReader.ReadEncodedValue(), out int _);
                        break;

                    case KeyCredentialId:
                        CredentialId = new CredentialId(cborReader.ReadEncodedValue(), out int _);
                        break;

                    case KeyPublicKey:
                        CredentialPublicKey = CoseKey.Create(cborReader.ReadEncodedValue(), out int _);
                        break;

                    case KeyTotalRpCredentials:
                        TotalCredentialsForRelyingParty = cborReader.ReadInt32();
                        break;

                    case KeyCredProtectPolicy:
                        CredProtectPolicy = cborReader.ReadInt32();
                        break;

                    case KeyLargeBlobKey:
                        LargeBlobKey = cborReader.ReadByteString();
                        break;
                }
                count--;
            }

            cborReader.ReadEndMap();
        }
    }
}

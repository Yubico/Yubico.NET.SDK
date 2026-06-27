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
using System.Collections.Generic;
using System.Formats.Cbor;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.Cbor;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Contains the info about a user in one credential on the YubiKey. This is
    /// the class returned when calling
    /// <see cref="Fido2Session.EnumerateCredentialsForRelyingParty"/>.
    /// </summary>
    public class CredentialUserInfo
    {
        private const int KeyUser = 6;
        private const int KeyCredentialId = 7;
        private const int KeyPublicKey = 8;
        private const int KeyTotalRpCredentials = 9;
        private const int KeyCredProtectPolicy = 10;
        private const int KeyLargeBlobKey = 11;
        private const int KeyThirdPartyPayment = 12;

        private readonly IReadOnlyDictionary<int, ReadOnlyMemory<byte>>? _credentialManagementFields;

        /// <summary>
        /// The user entity for a credential returned.
        /// </summary>
        public UserEntity User { get; private set; }

        /// <summary>
        /// The credential ID for a credential returned.
        /// </summary>
        public CredentialId CredentialId { get; private set; }

        /// <summary>
        /// The public key for a credential returned.
        /// </summary>
        public CoseKey CredentialPublicKey { get; private set; }

        /// <summary>
        /// The credential protection policy. See section 12.1.1 of the FIDO2
        /// standard for a description of the meanings of the number returned.
        /// </summary>
        public CredProtectPolicy CredProtectPolicy { get; private set; }

        /// <summary>
        /// The large blob key for a credential. If this property is null, either
        /// the credential does not have a large blob key, or it does have a
        /// large blob key but it is not requested.
        /// </summary>
        public ReadOnlyMemory<byte>? LargeBlobKey { get; private set; }

        /// <summary>
        /// Whether this credential is third-party payment enabled.
        /// Null if the credential was not created with the thirdPartyPayment extension.
        /// </summary>
        public bool? ThirdPartyPayment { get; private set; }

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private CredentialUserInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new instance of <see cref="CredentialUserInfo"/> based on the
        /// given objects related to a user.
        /// </summary>
        /// <param name="user">
        /// The user associated with the credential.
        /// </param>
        /// <param name="credentialId">
        /// The credential ID, which includes the Id and Type.
        /// </param>
        /// <param name="credentialPublicKey">
        /// The public key to use when verifying a credential.
        /// </param>
        /// <param name="credProtectPolicy">
        /// An int specifying the credential protection policy.
        /// </param>
        /// <param name="largeBlobKey">
        /// If null, there is no large blob key. If not null, this is the key
        /// that can be used to encrypt or decrypt large blob data for the
        /// credential.
        /// </param>
        /// <param name="thirdPartyPayment">
        /// Whether the credential was created as payment-enabled. If null, the
        /// authenticator did not return the thirdPartyPayment element.
        /// </param>
        public CredentialUserInfo(
            UserEntity user,
            CredentialId credentialId,
            CoseKey credentialPublicKey,
            int credProtectPolicy,
            ReadOnlyMemory<byte>? largeBlobKey = null,
            bool? thirdPartyPayment = null)
        {
            User = user;
            CredentialId = credentialId;
            CredentialPublicKey = credentialPublicKey;
            CredProtectPolicy = (CredProtectPolicy)credProtectPolicy;
            LargeBlobKey = largeBlobKey;
            ThirdPartyPayment = thirdPartyPayment;
        }

        private CredentialUserInfo(
            UserEntity user,
            CredentialId credentialId,
            CoseKey credentialPublicKey,
            int credProtectPolicy,
            ReadOnlyMemory<byte>? largeBlobKey,
            bool? thirdPartyPayment,
            IReadOnlyDictionary<int, ReadOnlyMemory<byte>> credentialManagementFields)
            : this(
                user,
                credentialId,
                credentialPublicKey,
                credProtectPolicy,
                largeBlobKey,
                thirdPartyPayment)
        {
            _credentialManagementFields = credentialManagementFields;
        }

        /// <summary>
        /// Gets the raw CBOR-encoded value for a field in the credential
        /// management response that produced this instance.
        /// </summary>
        /// <remarks>
        /// This method provides access to credential-specific fields in the
        /// credential-management response that do not yet have a dedicated SDK
        /// property. The returned value is the raw CBOR encoding of the field
        /// value, not a decoded .NET object.
        /// It returns <see langword="false"/> for instances that were not built
        /// from a credential-management response returned by the SDK.
        /// </remarks>
        /// <param name="key">
        /// The integer CTAP map key to read.
        /// </param>
        /// <param name="encodedValue">
        /// The raw CBOR-encoded value for <paramref name="key"/>, if present.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the response contained <paramref name="key"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetCredentialManagementField(int key, out ReadOnlyMemory<byte> encodedValue)
        {
            encodedValue = default;
            if (_credentialManagementFields is null
                || !_credentialManagementFields.TryGetValue(key, out encodedValue))
            {
                return false;
            }

            return true;
        }

        internal static CredentialUserInfo FromCredentialManagementData(CborMap<int> credentialManagementData)
        {
            if (!credentialManagementData.Contains(KeyUser)
                || !credentialManagementData.Contains(KeyCredentialId)
                || !credentialManagementData.Contains(KeyPublicKey)
                || !credentialManagementData.Contains(KeyCredProtectPolicy))
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info);
            }

            try
            {
                var user = new UserEntity(credentialManagementData.ReadEncodedValue(KeyUser), out int _);
                var credentialId = new CredentialId(credentialManagementData.ReadEncodedValue(KeyCredentialId), out int _);
                var credentialPublicKey = CoseKey.Create(credentialManagementData.ReadEncodedValue(KeyPublicKey), out int _);
                int credProtectPolicy = ReadInt32(credentialManagementData, KeyCredProtectPolicy);
                ReadOnlyMemory<byte>? largeBlobKey = credentialManagementData.Contains(KeyLargeBlobKey)
                    ? credentialManagementData.ReadByteString(KeyLargeBlobKey)
                    : null;
                bool? thirdPartyPayment = credentialManagementData.Contains(KeyThirdPartyPayment)
                    ? credentialManagementData.ReadBoolean(KeyThirdPartyPayment)
                    : null;

                return new CredentialUserInfo(
                    user,
                    credentialId,
                    credentialPublicKey,
                    credProtectPolicy,
                    largeBlobKey,
                    thirdPartyPayment,
                    BuildCredentialManagementFields(credentialManagementData));
            }
            catch (Exception exception) when (
                exception is CborContentException ||
                exception is InvalidCastException ||
                exception is InvalidOperationException ||
                exception is KeyNotFoundException)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidFido2Info, exception);
            }
        }

        internal static int ReadInt32(CborMap<int> cborMap, int key)
        {
            var reader = new CborReader(cborMap.ReadEncodedValue(key), CborConformanceMode.Ctap2Canonical);
            int value = reader.ReadInt32();
            if (reader.BytesRemaining != 0)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidFido2Info);
            }

            return value;
        }

        private static IReadOnlyDictionary<int, ReadOnlyMemory<byte>> BuildCredentialManagementFields(
            CborMap<int> credentialManagementData)
        {
            var fields = new Dictionary<int, ReadOnlyMemory<byte>>();
            foreach (int key in credentialManagementData.Keys)
            {
                if (IsKnownField(key))
                {
                    continue;
                }

                fields[key] = credentialManagementData.ReadEncodedValue(key);
            }

            return fields;
        }

        private static bool IsKnownField(int key) => key switch
        {
            1 or 2 or 3 or 4 or 5 or KeyUser or KeyCredentialId or KeyPublicKey or KeyTotalRpCredentials or KeyCredProtectPolicy or KeyLargeBlobKey or KeyThirdPartyPayment => true,
            _ => false,
        };
    }
}

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
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Globalization;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    ///     Contains the data returned by the YubiKey after getting an assertion.
    /// </summary>
    /// <remarks>
    ///     When an assertion is obtained, the YubiKey returns data about that
    ///     assertion, including the credential. There are several elements
    ///     in this data and this structure contains those elements.
    /// </remarks>
    public class GetAssertionData : IDisposable
    {
        private const int KeyCredential = 1;
        private const int KeyAuthData = 2;
        private const int KeySignature = 3;
        private const int KeyUser = 4;
        private const int KeyNumberCredentials = 5;
        private const int KeyUserSelected = 6;
        private const int KeyLargeBlobKey = 7;

        private const string KeyCredentialType = "type";
        private const string KeyCredentialTransports = "transports";
        private const string KeyCredentialId = "id";
        private const string KeyUserId = "id";
        private const string KeyUserName = "name";
        private const string KeyUserDisplayName = "displayName";
        private readonly byte[]? _keyData;

        private bool _disposed;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private GetAssertionData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Build a new instance of <see cref="GetAssertionData" /> based on the
        ///     given CBOR encoding.
        /// </summary>
        /// <remarks>
        ///     The encoding must follow the definition of
        ///     <c>authenticatorGetAssertion response structure</c> in section
        ///     6.2.2 of the CTAP 2.1 standard.
        /// </remarks>
        /// <param name="cborEncoding">
        ///     The credential data, encoded following the CTAP 2.1 and CBOR (RFC
        ///     8949) standards.
        /// </param>
        /// <exception cref="Ctap2DataException">
        ///     The <c>cborEncoding</c> is not a valid CBOR encoding, or it is not a
        ///     correct encoding for FIDO2 assertion data.
        /// </exception>
        public GetAssertionData(ReadOnlyMemory<byte> cborEncoding)
        {
            try
            {
                var map = new CborMap<int>(cborEncoding);

                CborMap<string> stringMap = map.ReadMap<string>(KeyCredential);
                CredentialId = new CredentialId
                {
                    Type = stringMap.ReadTextString(KeyCredentialType),
                    Id = stringMap.ReadByteString(KeyCredentialId)
                };

                if (stringMap.Contains(KeyCredentialTransports))
                {
                    IReadOnlyList<string> transports = stringMap.ReadArray<string>(KeyCredentialTransports);
                    foreach (string current in transports)
                    {
                        CredentialId.AddTransport(current);
                    }
                }

                AuthenticatorData = new AuthenticatorData(map.ReadByteString(KeyAuthData));
                Signature = map.ReadByteString(KeySignature);
                if (map.Contains(KeyUser))
                {
                    stringMap = map.ReadMap<string>(KeyUser);
                    User = new UserEntity(stringMap.ReadByteString(KeyUserId))
                    {
                        Name = (string?)stringMap.ReadOptional<string>(KeyUserName),
                        DisplayName = (string?)stringMap.ReadOptional<string>(KeyUserDisplayName)
                    };
                }

                NumberOfCredentials = (int?)map.ReadOptional<int>(KeyNumberCredentials);
                UserSelected = (bool?)map.ReadOptional<bool>(KeyUserSelected);
                _keyData = (byte[]?)map.ReadOptional<byte[]>(KeyLargeBlobKey);
                if (!(_keyData is null))
                {
                    LargeBlobKey = new ReadOnlyMemory<byte>(_keyData);
                }
            }
            catch (CborContentException cborException)
            {
                throw new Ctap2DataException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidFido2Info),
                    cborException);
            }
        }

        /// <summary>
        ///     The credential ID for the assertion just obtained.
        /// </summary>
        public CredentialId CredentialId { get; }

        /// <summary>
        ///     The object that contains both the encoded authenticator data, which
        ///     is to be used in verifying the attestation statement, and the decoded
        ///     elements, including the credential itself, a public key.
        /// </summary>
        public AuthenticatorData AuthenticatorData { get; }

        /// <summary>
        ///     The assertion signature, which can be used to verify the assertion
        ///     the call to GetAssertion returned.
        /// </summary>
        /// <remarks>
        ///     Use the public key returned in the <c>AuthenticatorData</c> field of
        ///     the <see cref="MakeCredentialData" /> returned by the call to
        ///     <c>MakeCredential</c>
        ///     (<see cref="AuthenticatorData.CredentialPublicKey" />). The data to
        ///     verify is <see cref="AuthenticatorData.EncodedAuthenticatorData" />.
        /// </remarks>
        public ReadOnlyMemory<byte> Signature { get; }

        /// <summary>
        ///     The user's ID, along with optional descriptive strings. This is an
        ///     optional element and can be null.
        /// </summary>
        public UserEntity? User { get; private set; }

        /// <summary>
        ///     The total number of credentials found on the YubiKey for the relying
        ///     party. This is optional and can be null. If null, then there is only
        ///     one credential.
        /// </summary>
        public int? NumberOfCredentials { get; private set; }

        /// <summary>
        ///     If <c>true</c>, the credential was selected by the user via
        ///     interaction directly with the authenticator. This is optional and can
        ///     be null. If null, then this is considered <c>false</c>.
        /// </summary>
        public bool? UserSelected { get; private set; }

        /// <summary>
        ///     The large blob key, if there is one. This is optional and can be null.
        /// </summary>
        public ReadOnlyMemory<byte>? LargeBlobKey { get; private set; }

        /// <summary>
        ///     Releases any unmanaged resources and overwrites any sensitive data.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Use the given public key to verify the <see cref="Signature" />. This
        ///     method will use the <c>clientDataHash</c> and the
        ///     <see cref="AuthenticatorData" /> as the data to verify.
        /// </summary>
        /// <remarks>
        ///     If the signature verifies, this method will return <c>true</c>, and
        ///     if it does not verify, it will return <c>false</c>. If there are any
        ///     errors, this method will throw an exception.
        /// </remarks>
        /// <param name="publicKey">
        ///     The public key returned when the credential was first made, it will
        ///     be used to verify.
        /// </param>
        /// <param name="clientDataHash">
        ///     The client data hash used to get the assertion.
        /// </param>
        /// <returns>
        ///     A boolean, <c>true</c> if the signature verifies, <c>false</c>
        ///     otherwise.
        /// </returns>
        public bool VerifyAssertion(CoseKey publicKey, ReadOnlyMemory<byte> clientDataHash)
        {
            using SHA256 digester = CryptographyProviders.Sha256Creator();
            _ = digester.TransformBlock(
                AuthenticatorData.EncodedAuthenticatorData.ToArray(), inputOffset: 0,
                AuthenticatorData.EncodedAuthenticatorData.Length, outputBuffer: null, outputOffset: 0);

            _ = digester.TransformFinalBlock(clientDataHash.ToArray(), inputOffset: 0, clientDataHash.Length);

            using var ecdsaVfy = new EcdsaVerify(publicKey);
            return ecdsaVfy.VerifyDigestedData(digester.Hash, Signature.ToArray());
        }

        /// <summary>
        ///     Releases any unmanaged resources and overwrites any sensitive data.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                CryptographicOperations.ZeroMemory(_keyData);
            }

            _disposed = true;
        }
    }
}

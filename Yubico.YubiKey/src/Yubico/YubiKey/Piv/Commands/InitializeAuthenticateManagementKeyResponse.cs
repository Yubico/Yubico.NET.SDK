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
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// The response to the initialize authenticate management key command.
    /// </summary>
    /// <remarks>
    /// This is the partner Response class to <see
    /// cref="InitializeAuthenticateManagementKeyCommand"/>.
    /// <para>
    /// The data returned is a tuple consisting of a boolean and a
    /// <c>ReadOnlyMemory&lt;byte&gt;</c>. The boolean indicates if this is mutual
    /// authentication or not, <c>true</c> for mutual auth, <c>false</c> for
    /// single. The byte array is "Client Authentication Challenge".
    /// </para>
    /// <para>See the comments for the class
    /// <see cref="InitializeAuthenticateManagementKeyCommand"/>, there is a lengthy
    /// discussion of the process of authenticating the management key, including
    /// descriptions of the challenges and responses.
    /// </para>
    /// <para>
    /// It is likely that you will never need to call <c>GetData</c> in this
    /// class. You will pass an instance of this class to the constructor for
    /// <see cref="CompleteAuthenticateManagementKeyCommand"/>, which will process the
    /// challenge.
    /// </para>
    /// </remarks>
    public sealed class InitializeAuthenticateManagementKeyResponse : PivResponse, IYubiKeyResponseWithData<(bool isMutualAuth, ReadOnlyMemory<byte> clientAuthenticationChallenge)>
    {
        private const int NestedTag = 0x7C;
        private const int MutualAuthTag = 0x80;
        private const int SingleAuthTag = 0x81;
        private const int TDesDataLength = 8;
        private const int AesDataLength = 16;

        /// <summary>
        /// Which algorithm is the management key.
        /// </summary>
        public PivAlgorithm Algorithm { get; private set; }

        private readonly int _tag1;

        // This will contain the value of the challenge, not the entire
        // ResponseApdu.Data. It will be 8 bytes.
        private readonly byte[]? _clientAuthenticationChallenge;

        /// <summary>
        /// Constructs an InitializeAuthenticateManagementKeyResponse based on a ResponseApdu
        /// received from the YubiKey for the Triple-DES algorithm.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the Response APDU<br/>returned by the YubiKey.
        /// </param>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data provided does not meet the expectations, and cannot be parsed.
        /// </exception>
        public InitializeAuthenticateManagementKeyResponse(ResponseApdu responseApdu)
            : this(responseApdu, PivAlgorithm.TripleDes)
        {
        }

        /// <summary>
        /// Constructs an InitializeAuthenticateManagementKeyResponse based on a ResponseApdu
        /// received from the YubiKey for the specified algorithm.
        /// </summary>
        /// <param name="responseApdu">
        /// The object containing the Response APDU<br/>returned by the YubiKey.
        /// </param>
        /// <param name="algorithm">
        /// The algorithm of the management key. It must be <c>TripleDes</c>,
        /// <c>Aes128</c>, <c>Aes192</c>, or <c>Aes256</c>,
        /// </param>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// Thrown when the data provided does not meet the expectations, and cannot be parsed.
        /// </exception>
        public InitializeAuthenticateManagementKeyResponse(ResponseApdu responseApdu, PivAlgorithm algorithm) :
            base(responseApdu)
        {
            Algorithm = algorithm;

            // If Status is not Success, then there is no data to parse.
            if (Status != ResponseStatus.Success)
            {
                return;
            }

            // Verify the data is correct.
            // The data is one of the following four.
            //  7C 0A 80 08 <C1 (8 bytes)>,
            //  7C 0A 81 08 <C1 (8 bytes)>,
            //  7C 12 80 10 <C1 (16 bytes)>,
            //  7C 12 81 10 <C1 (16 bytes)>,
            // If the data tag is 80, this is mutual auth, if it is 81 this is single
            // auth.
            // We will not indicate Failed if there is "too much" data, we'll
            // just ignore any extra bytes.
            var tlvReader = new TlvReader(ResponseApdu.Data);
            int nestedTag = tlvReader.PeekTag();
            TlvReader authReader = tlvReader.ReadNestedTlv(nestedTag);
            int authTag = authReader.PeekTag();
            ReadOnlyMemory<byte> value = authReader.ReadValue(authTag);

            if ((nestedTag != NestedTag) || ((authTag != MutualAuthTag) && (authTag != SingleAuthTag)))
            {
                throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(InitializeAuthenticateManagementKeyResponse),
                };
            }
            if (value.Length < TDesDataLength)
            {
                throw new MalformedYubiKeyResponseException()
                {
                    ResponseClass = nameof(InitializeAuthenticateManagementKeyResponse),
                    ExpectedDataLength = TDesDataLength,
                    ActualDataLength = value.Length,
                };
            }

            int dataLength = value.Length < AesDataLength ? TDesDataLength : AesDataLength;

            _tag1 = authTag;

            // Get the 8 or 16 bytes of V in the TLV for tag1. This ignores any
            // extraneous trailing bytes.
            _clientAuthenticationChallenge = value.Slice(0, dataLength).ToArray();
        }

        /// <summary>
        /// Return the boolean indicating mutual auth or not, along with the
        /// value portion of the Response Data, namely, Client Authentication Challenge.
        /// </summary>
        /// <returns>
        /// A (bool, byte array) tuple<br/>if this is mutual auth and<br/>
        /// the 8 bytes that make up the Client Authentication Challenge.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="YubiKeyResponse.Status"/> is not <see cref="ResponseStatus.Success"/>.
        /// </exception>
        public (bool isMutualAuth, ReadOnlyMemory<byte> clientAuthenticationChallenge) GetData() => _clientAuthenticationChallenge switch
        {
            null => throw new InvalidOperationException(StatusMessage),
            _ => (_tag1 == MutualAuthTag, _clientAuthenticationChallenge),
        };
    }
}

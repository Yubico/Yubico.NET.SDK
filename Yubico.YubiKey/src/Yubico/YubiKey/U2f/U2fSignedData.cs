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
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.U2f
{
    /// <summary>
    /// This is a base class for those classes that need to verify.
    /// </summary>
    /// <remarks>
    /// Only the SDK will ever need to create subclasses, there is no reason for
    /// any other application to do so.
    /// </remarks>
    public abstract class U2fSignedData : U2fBuffer
    {
        private const int SignatureLength = 2 * CoordinateLength;

        // The longest possible BER signature is
        //  30 len
        //     02 coordLen+1 00 rVal
        //     02 coordLen+1 00 sVal
        internal const int MaxBerSignatureLength = SignatureLength + 8;

        private protected readonly int _signatureOffset;
        private protected int _berSignatureLength;

        /// <summary>
        /// The signature created by the YubiKey. This is the BER encoding
        /// version of the signature.
        /// </summary>
        public ReadOnlyMemory<byte> Signature
        {
            get => _bufferMemory.Slice(_signatureOffset, _berSignatureLength);
            set
            {
                SetBufferData(value, value.Length, _signatureOffset, nameof(Signature));
                _berSignatureLength = value.Length;
            }
        }

        /// <summary>
        /// Create an instance of <c>U2fSignedData</c>.
        /// </summary>
        protected U2fSignedData(int bufferLength, int appIdOffset, int clientDataOffset, int signatureOffset)
            : base(bufferLength, appIdOffset, clientDataOffset)
        {
            _signatureOffset = signatureOffset;
        }

        // The subclass will build the EcdsaVerify object based on the format of
        // the key it is using, and call this method to digest the data and
        // perform the verification.
        // For each subclass, the data to verify is in the _buffer (it is not yet
        // digested). In addition, the data to verify begins at index 0 and
        // continues until the signature. Therefore, the data to verify is the
        // byte array _buffer until _signatureOffset.
        private protected bool VerifySignature(
            EcdsaVerify verifier,
            ReadOnlyMemory<byte> applicationId,
            ReadOnlyMemory<byte> clientDataHash)
        {
            ApplicationId = applicationId;
            ClientDataHash = clientDataHash;

            return verifier.VerifyData(
                _bufferMemory.Slice(0, _signatureOffset).ToArray(),
                _bufferMemory.Slice(_signatureOffset, _berSignatureLength).ToArray());
        }
    }
}

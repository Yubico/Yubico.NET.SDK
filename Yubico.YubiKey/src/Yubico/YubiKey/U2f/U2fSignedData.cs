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
using System.Security.Cryptography;
using Yubico.Core.Tlv;

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
        private const int SignatureTag = 0x30;
        private const int IntegerTag = 0x02;
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

        // The subclass will build the ECDsa object and call this method to
        // perform the actual verification.
        private protected bool VerifySignature(
            ECDsa ecdsaObject, ReadOnlyMemory<byte> applicationId, ReadOnlyMemory<byte> clientDataHash)
        {
            ApplicationId = applicationId;
            ClientDataHash = clientDataHash;

            return TryConvertDerSignature(out byte[] convertedSignature) &&
                ecdsaObject.VerifyData(_buffer, 0, _signatureOffset, convertedSignature, HashAlgorithmName.SHA256);
        }

        // Convert the DER signature into (r || s), where r and s are exactly
        // CoordinateLength bytes long.
        // If successful, return true.
        // If the current _signature decodes to something that cannot be
        // converted, return false.
        private bool TryConvertDerSignature(out byte[] convertedSignature)
        {
            convertedSignature = new byte[SignatureLength];
            var signatureMemory = new Memory<byte>(convertedSignature);

            var tlvReader = new TlvReader(_bufferMemory.Slice(_signatureOffset, _berSignatureLength));
            if (tlvReader.TryReadNestedTlv(out tlvReader, SignatureTag))
            {
                if (TryCopyNextInteger(tlvReader, signatureMemory))
                {
                    return TryCopyNextInteger(tlvReader, signatureMemory.Slice(CoordinateLength));
                }
            }

            return false;
        }

        // Decode the next value in tlvReader, then copy the result into
        // signatureValue.
        // Copy exactly CoordinateLength bytes.
        // The decoded value might have a leading 00 byte. It is safe to ignore
        // it.
        // If the tag is wrong, return false.
        // If the number of non-zero bytes is < CoordinateLength, prepend 00
        // bytes in the output.
        // If the number of non-zero bytes is > CoordinateLength, return false.
        private static bool TryCopyNextInteger(TlvReader tlvReader, Memory<byte> signatureValue)
        {
            if (tlvReader.TryReadValue(out ReadOnlyMemory<byte> rsValue, IntegerTag))
            {
                // strip any leading 00 bytes.
                int length = rsValue.Length;
                int index = 0;
                while (length > 0)
                {
                    if (rsValue.Span[index] != 0)
                    {
                        break;
                    }

                    index++;
                    length--;
                }

                // If we still have data and it is not too long, copy
                if ((length > 0) && (length <= CoordinateLength))
                {
                    rsValue.Slice(index).CopyTo(signatureValue.Slice(CoordinateLength - length));
                    return true;
                }
            }

            return false;
        }
    }
}

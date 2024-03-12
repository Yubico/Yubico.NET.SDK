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
using System.Globalization;
using Yubico.YubiKey.Piv;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class converts between two DSA and ECDSA signature formats: standard
    // and non-standard.
    // DSA and ECDSA signatures are made up of two numbers, generally called "r"
    // and "s". The problem with that is how does one present a two-number
    // signature as a single buffer? Someone reading the signature would need to
    // know whether the two numbers are in order r then s, or s then r, and how
    // big each number is. This problem was solved in the 1990s when the first
    // DSA standards specified that a DSA signature was the BER encoding of
    //   SEQUENCE {
    //      r  INTEGER,
    //      s  INTEGER }
    // Today, almost everyone who creates a DSA or ECDSA signature will follow
    // the standard. Virtually every standard that uses DSA and/or ECDSA
    // specifies that the signature follow that format.
    // However, there are still some entities that do not follow the standard.
    // One such entity is the C# AsymmetricAlgorithm class (actually the
    // subclasses DSA and ECDsa). Those classes require the DSA and ECDSA
    // signatures to be in the format
    //   r || s
    //   with r and s each a fixed length based on the key size
    // If the Algorithm is ECDSA using the P-256 curve, then each number must be
    // 32 bytes long. The math of P-256 means that r and s will never be longer
    // than 32 bytes. However, if the actual r or s is less than 32 bytes long,
    // that element in the signature must have prepended 00 bytes.
    // This class contains two methods. One will convert the standard ECDSA
    // signature into a buffer the C# classes will accept (for use when verifying
    // a signature created by a YubiKey). The other will convert a non-standard
    // signature into the standard construction.
    public static class DsaSignatureConverter
    {
        private const string InvalidAlgorithmMessage = "The algorithm was not recognized.";
        private const string InvalidSignatureMessage = "The input signature was not recognized.";

        // Given a standard DSA or ECDSA signature:
        //   SEQUENCE {
        //      r  INTEGER,
        //      s  INTEGER }
        // build a new byte array that contains the non-standard signature:
        //   r || s
        //   where r and s are a fixed size.
        // The caller passes in the current signature along with the algorithm.
        // This method will create a new byte[] and fill it with the non-standard
        // signature. The algorithm is needed to determine the correct lengths of
        // r and s.
        // If the input argument signature is null or not a valid signature for
        // the given algorithm, this method will throw an exception.
        // If the algorithm is not DSA or ECDSA (e.g. it is Rsa1024 or
        // TripleDes), the method will throw an exception.
        public static byte[] GetNonStandardDsaFromStandard(byte[] signature, PivAlgorithm algorithm)
        {
            if (signature is null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            int elementLength = algorithm switch
            {
                PivAlgorithm.EccP256 => 32,
                PivAlgorithm.EccP384 => 48,
                _ => throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        InvalidAlgorithmMessage)),
            };

            var tlvReader = new TlvReader(signature);
            ReadOnlyMemory<byte> rValue = Memory<byte>.Empty;
            ReadOnlyMemory<byte> sValue = Memory<byte>.Empty;
            int offsetR = 0;
            int offsetS = 0;
            bool isValid = false;
            if (tlvReader.TryReadNestedTlv(out TlvReader seqReader, 0x30))
            {
                if (seqReader.TryReadValue(out rValue, 0x02))
                {
                    if (seqReader.TryReadValue(out sValue, 0x02))
                    {
                        // Skip any leading 00 bytes.
                        while (rValue.Span[offsetR] == 0)
                        {
                            offsetR++;
                            if (offsetR == rValue.Length - 1)
                            {
                                break;
                            }
                        }
                        while (sValue.Span[offsetS] == 0)
                        {
                            offsetS++;
                            if (offsetS == sValue.Length - 1)
                            {
                                break;
                            }
                        }

                        isValid = ((rValue.Length - offsetR) <= elementLength) && ((sValue.Length - offsetS) <= elementLength);
                    }
                }
            }

            if (isValid)
            {
                byte[] returnValue = new byte[2 * elementLength];
                var buffer = new Memory<byte>(returnValue);
                int offset = elementLength - (rValue.Length - offsetR);
                rValue[offsetR..].CopyTo(buffer[offset..]);
                offset = elementLength + (elementLength - (sValue.Length - offsetS));
                sValue[offsetS..].CopyTo(buffer[offset..]);

                return returnValue;
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    InvalidSignatureMessage));
        }

        // Given a signature in the format
        //   r || s
        //   where r and s are a fixed size,
        // build a new buffer containing the BER encoding of
        //   SEQUENCE {
        //      r  INTEGER,
        //      s  INTEGER }
        // If the input argument signature is null or not a supported length,
        // this method will throw an exception.
        // This method will accept any size signature, as long as the length is
        // greater than zero and even.
        public static byte[] GetStandardDsaFromNonStandard(byte[] signature)
        {
            if ((signature is null) || (signature.Length == 0))
            {
                throw new ArgumentNullException(nameof(signature));
            }

            if ((signature.Length & 1) != 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        InvalidSignatureMessage));
            }

            int elementLength = signature.Length / 2;

            int offsetR = 0;
            int offsetS = elementLength;
            // Skip any leading 00 bytes.
            while (signature[offsetR] == 0)
            {
                offsetR++;
                if (offsetR == elementLength - 1)
                {
                    break;
                }
            }
            while (signature[offsetS] == 0)
            {
                offsetS++;
                if (offsetS == signature.Length - 1)
                {
                    break;
                }
            }

            int rLength = elementLength - offsetR;
            int sLength = signature.Length - offsetS;

            // The first half of the buffer is r, the second is s.
            // For each element, build an INTEGER: 02 len value.
            // If the msBit of value is set, prepend a 00 byte.
            int startR = ((signature[offsetR] & 0x80) != 0) ? 1 : 0;
            int startS = ((signature[offsetS] & 0x80) != 0) ? 1 : 0;

            byte[] rBuffer = new byte[elementLength + 1];
            byte[] sBuffer = new byte[elementLength + 1];

            var valueR = new Span<byte>(rBuffer);
            var valueS = new Span<byte>(sBuffer);
            var sigBuffer = new ReadOnlySpan<byte>(signature);

            sigBuffer.Slice(offsetR, rLength).CopyTo(valueR[startR..]);
            sigBuffer.Slice(offsetS, sLength).CopyTo(valueS[startS..]);

            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(0x30))
            {
                tlvWriter.WriteValue(0x02, valueR.Slice(0, rLength + startR));
                tlvWriter.WriteValue(0x02, valueS.Slice(0, sLength + startS));
            }

            return tlvWriter.Encode();
        }
    }
}

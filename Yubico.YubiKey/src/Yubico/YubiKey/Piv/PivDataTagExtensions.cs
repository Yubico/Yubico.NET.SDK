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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// Extension methods to operate on the PivDataTag enum.
    /// </summary>
    public static class PivDataTagExtensions
    {
        private const byte PivPutDataTag = 0x53;

        /// <summary>
        /// Is the given tag allowed to be used in a standard version of PUT DATA.
        /// </summary>
        /// <param name="tag">
        /// The tag to check.
        /// </param>
        /// <returns>
        /// A boolean, true if the tag is allowed to be used in PUT DATA, and
        /// false otherwise.
        /// </returns>
        public static bool IsValidTagForPut(this PivDataTag tag) =>
            tag switch
            {
                PivDataTag.Printed => false,
                PivDataTag.Discovery => false,
                PivDataTag.BiometricGroupTemplate => false,
                _ => true,
            };

        /// <summary>
        /// Is the given encoding valid for PUT DATA using the specified tag.
        /// </summary>
        /// <remarks>
        /// Each tag has a defined encoding format for the data to PUT. This
        /// method will verify that the data follows the correct format. It does
        /// not verify the content, it simply verifies the format.
        /// </remarks>
        /// <param name="tag">
        /// The tag to check.
        /// </param>
        /// <param name="encoding">
        /// The encoding to check.
        /// </param>
        /// <returns>
        /// A boolean, true if the encoding follows the defined encoding format,
        /// and false otherwise.
        /// </returns>
        public static bool IsValidEncodingForPut(this PivDataTag tag, ReadOnlyMemory<byte> encoding)
        {
            var tlvReader = GetTlvReader(tag, encoding);
            if (tlvReader is null)
            {
                return false;
            }

            int[] expectedFormat;
            int optionalIndex = -1;

            switch (tag)
            {
                case PivDataTag.Chuid:
                    // Verify that the data is
                    //  30 19
                    //     --FASC-N, fixed at 25 bytes--
                    //  34 10
                    //     --GUID, fixed at 16 bytes--
                    //  35 08
                    //     --expiration data, ASCII YYYYMMDD, fixed at 8 bytes--
                    //  3E 00
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0x30, 0, 25,
                        0x34, 0, 16,
                        0x35, 0, 8,
                        0x3E, 0, 0,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.Capability:
                    // Verify that the data is
                    //  F0 15
                    //     --fixed at 21 bytes--
                    //  F1 01
                    //     --fixed at 1 byte--
                    //  F2 01
                    //     --fixed at 1 byte--
                    //  F3 00
                    //  F4 01
                    //     --fixed at 1 byte--
                    //  F5 01
                    //     --fixed at 1 byte--
                    //  F6 00
                    //  F7 00
                    //  FA 00
                    //  FB 00
                    //  FC 00
                    //  FD 00
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0xF0, 0, 21,
                        0xF1, 0, 1,
                        0xF2, 0, 1,
                        0xF3, 0, 0,
                        0xF4, 0, 1,
                        0xF5, 0, 1,
                        0xF6, 0, 0,
                        0xF7, 0, 0,
                        0xFA, 0, 0,
                        0xFB, 0, 0,
                        0xFC, 0, 0,
                        0xFD, 0, 0,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.Authentication:
                case PivDataTag.Signature:
                case PivDataTag.KeyManagement:
                case PivDataTag.CardAuthentication:
                case PivDataTag.Retired1:
                case PivDataTag.Retired2:
                case PivDataTag.Retired3:
                case PivDataTag.Retired4:
                case PivDataTag.Retired5:
                case PivDataTag.Retired6:
                case PivDataTag.Retired7:
                case PivDataTag.Retired8:
                case PivDataTag.Retired9:
                case PivDataTag.Retired10:
                case PivDataTag.Retired11:
                case PivDataTag.Retired12:
                case PivDataTag.Retired13:
                case PivDataTag.Retired14:
                case PivDataTag.Retired15:
                case PivDataTag.Retired16:
                case PivDataTag.Retired17:
                case PivDataTag.Retired18:
                case PivDataTag.Retired19:
                case PivDataTag.Retired20:
                    // Verify that the data is
                    //  70 L
                    //     --cert, up to 3052 bytes--
                    //  71 01
                    //     --fixed at 1 byte--
                    //  FE 00
                    // Note that the PIV standard says certs are limited to 1856
                    // bytes, but the YubiKey accepts certs up to 3052 bytes.
                    expectedFormat = new int[]
                    {
                        0x70, 3052, 0,
                        0x71, 0, 1,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.SecurityObject:
                    // Verify that the data is
                    //  BA L
                    //     --up to 30 bytes--
                    //  BB L
                    //     --up to 1298 bytes--
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0xBA, 30, 0,
                        0xBB, 1298, 0,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.KeyHistory:
                    // Verify that the data is
                    //  C1 01
                    //     --fixed at 1 byte--
                    //  C2 01
                    //     --fixed at 1 byte--
                    //  F3 L
                    //     --up to 118 bytes--
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0xC1, 0, 1,
                        0xC2, 0, 1,
                        0xF3, 118, 0,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.IrisImages:
                    // Verify that the data is
                    //  BC L
                    //     --image, up to 7100 bytes--
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0xBC, 7100, 0,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.FacialImage:
                    // Verify that the data is
                    //  BC L
                    //     --image, up to 12,704 bytes--
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0xBC, 12704, 0,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.Fingerprints:
                    // Verify that the data is
                    //  BC L
                    //     --up to 4000 bytes--
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0xBC, 4000, 0,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.SecureMessageSigner:
                    // Verify that the data is
                    //  70 L
                    //     --cert, up to 3048 bytes--
                    //  71 01
                    //     --fixed at 1 byte--
                    //  7F 21 L (optional)
                    //     --up to 3048 bytes--
                    //  FE 00
                    // Note that the PIV standard says certs are limited to 1856
                    // bytes, and the CVC (the 7F 21 element) is limited to 601
                    // bytes, but the YubiKey accepts a combined length of cert
                    // and CVC of 3048 bytes.
                    // That is, the total length of the cert and CVC must be
                    // <= 3048. That makes the total length <= 3064.
                    // If the
                    // cert is 3048 bytes and the CVC is 0 bytes, that will work.
                    // But a 2500-byte cert and a 600-byte CVC won't.
                    // This is because the total length of the encoding has to be
                    // 3064 bytes or fewer.
                    if (encoding.Length > 3064)
                    {
                        return false;
                    }

                    expectedFormat = new int[]
                    {
                        0x70, 3048, 0,
                        0x71, 0, 1,
                        0x7F21, 3048, 0,
                        0xFE, 0, 0
                    };

                    optionalIndex = 6;

                    break;

                case PivDataTag.PairingCodeReferenceData:
                    // Verify that the data is
                    //  99 08
                    //     --fixed at 8 bytes--
                    //  FE 00
                    expectedFormat = new int[]
                    {
                        0x99, 0, 8,
                        0xFE, 0, 0
                    };

                    break;

                case PivDataTag.BiometricGroupTemplate:
                // Verify that the data is
                //  02 01
                //     --fixed at 1 byte--
                //  7F 60 L
                //     --up to 28 bytes--
                //  7F 60 L (optional)
                //     --up to 28 bytes--
                // This is not yet supported. So for now, just return false.
                case PivDataTag.Printed:
                case PivDataTag.Discovery:
                default:
                    return false;
            }

            return VerifyTagLength(tlvReader, expectedFormat, optionalIndex);
        }

        // Get a TlvReader for the data. This will expect to find a nested
        // construction. The nested tag is either 53, 7E, or 7F 61 depending on
        // the tag. Get a TlvReader for the data in the nest.
        // If the data is not correct for the tag, return null.
        private static TlvReader? GetTlvReader(PivDataTag tag, ReadOnlyMemory<byte> encoding)
        {
            int expectedTag = PivPutDataTag;

            if (tag == PivDataTag.Discovery || tag == PivDataTag.BiometricGroupTemplate)
            {
                expectedTag = (int)tag;
            }

            try
            {
                var tlvReader = new TlvReader(encoding);
                var nestedReader = tlvReader.ReadNestedTlv(expectedTag);

                if (tlvReader.HasData == false)
                {
                    return nestedReader;
                }
            }
            catch (TlvException)
            {
            }

            return null;
        }

        // Verify that the elements in the reader have the tags specified and the
        // lengths are either <= to the max length, or exactly the fixed length.
        // If the max length is not 0, compare to that and ignore fixed length.
        // If max length is 0, ignore it and compare to fixed length.
        // Move the reader object to the next element.
        // The expectedFormat must be n sets of 3 ints: tag, max len, fixed len.
        //  e.g.
        //  tag 0, max 0, fixed 0, tag 1, max 1, fixed 1, etc.
        // If there is one element that is optional, provide its index (the index
        // of the tag). If there is no index for an optional element, pass in -1
        // to guarantee there will be no match for an optional tag. At the
        // moment, we have no formats that take more than one optional element,
        // and in all those cases, the tag is 2 bytes long. If that changes, we
        // will need to update this code.
        private static bool VerifyTagLength(TlvReader tlvReader, int[] expectedFormat, int optionalIndex)
        {
            bool verifySuccess = true;
            int index = 0;

            while (verifySuccess && index < expectedFormat.Length)
            {
                try
                {
                    if (index == optionalIndex)
                    {
                        int getTag = 0;

                        // Currently, all optional tags are 2 bytes long.
                        if (tlvReader.HasData)
                        {
                            getTag = tlvReader.PeekTag(2);
                        }

                        if (getTag != expectedFormat[index])
                        {
                            index += 3;

                            continue;
                        }
                    }

                    var value = tlvReader.ReadValue(expectedFormat[index]);

                    verifySuccess = expectedFormat[index + 1] != 0
                        ? value.Length <= expectedFormat[index + 1]
                        : value.Length == expectedFormat[index + 2];

                    index += 3;
                }
                catch (TlvException)
                {
                    verifySuccess = false;
                }
            }

            return verifySuccess;
        }
    }
}

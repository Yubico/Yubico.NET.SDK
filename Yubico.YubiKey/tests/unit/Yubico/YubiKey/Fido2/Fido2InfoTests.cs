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
using System.Linq;
using Xunit;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public class Fido2InfoTests
    {
        [Fact]
        public void Decode_RepeatKey_Throws()
        {
            byte[] encodedData = new byte[] {
                0xa4, 0x01, 0x81, 0x66, 0x55, 0x32, 0x46, 0x5f, 0x56, 0x32, 0x01, 0x81, 0x68, 0x46, 0x49, 0x44,
                0x4f, 0x5f, 0x32, 0x5f, 0x30, 0x11, 0x01, 0x14, 0x02
            };

            _ = Assert.Throws<Ctap2DataException>(() => new AuthenticatorInfo(encodedData));
        }

        [Fact]
        public void Decode_Versions_Correct()
        {
            string[] correctStrings = new string[] {
                "U2F_V2",
                "FIDO_2_0",
                "FIDO_2_1_PRE"
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            bool isValid = CompareStringLists(correctStrings, fido2Info.Versions);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_Extensions_Correct()
        {
            string[] correctStrings = new string[] {
                "credProtect",
                "hmac-secret"
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.Extensions);
            if (fido2Info.Extensions is null)
            {
                return;
            }
            bool isValid = CompareStringLists(correctStrings, fido2Info.Extensions);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_NoExtensions_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.Extensions);
        }

        [Fact]
        public void Decode_Aaguid_Correct()
        {
            byte[] correctValue = new byte[] {
                0x2f, 0xc0, 0x57, 0x9f, 0x81, 0x13, 0x47, 0xea, 0xb1, 0x16, 0xbb, 0x5a, 0x8d, 0xb9, 0x20, 0x2a
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            bool isValid = MemoryExtensions.SequenceEqual(correctValue, fido2Info.Aaguid.Span);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_Options_Correct()
        {
            string[] correctKeys = new string[] {
                "rk", "up", "plat", "clientPin", "credentialMgmtPreview"
            };
            bool[] correctValues = new bool[] {
                true, true, false, false, true
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.Options);

            if (fido2Info.Options is null)
            {
                return;
            }

            int index = 0;
            if (fido2Info.Options.Count == correctKeys.Length)
            {
                for (; index < correctKeys.Length; index++)
                {
                    if (fido2Info.Options.TryGetValue(correctKeys[index], out bool currentValue))
                    {
                        if (currentValue == correctValues[index])
                        {
                            continue;
                        }
                    }

                    break;
                }
            }

            // If we broke out early (index < Length), error.
            Assert.True(index >= correctKeys.Length);
        }

        [Fact]
        public void Decode_NoOptions_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.Options);
        }

        [Fact]
        public void Decode_MaxMsgSize_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(1200, fido2Info.MaximumMessageSize);
        }

        [Fact]
        public void Decode_NoMaxMsgSize_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.MaximumMessageSize);
        }

        [Fact]
        public void Decode_PinUvAuthProtocols_Correct()
        {
            // These must be in the correct order.
            var correctValues = new PinUvAuthProtocol[] {
                PinUvAuthProtocol.ProtocolTwo,
                PinUvAuthProtocol.ProtocolOne,
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.PinUvAuthProtocols);
            if (fido2Info.PinUvAuthProtocols is null)
            {
                return;
            }

            int index = 0;
            if (fido2Info.PinUvAuthProtocols.Count == correctValues.Length)
            {
                for (; index < correctValues.Length; index++)
                {
                    if (fido2Info.PinUvAuthProtocols[index] != correctValues[index])
                    {
                        break;
                    }
                }
            }

            Assert.True(index >= correctValues.Length);
        }

        [Fact]
        public void Decode_NoPinUvAuthProtocols_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.PinUvAuthProtocols);
        }

        [Fact]
        public void Decode_MaxCredCount_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(8, fido2Info.MaximumCredentialCountInList);
        }

        [Fact]
        public void Decode_NoMaxCredCount_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.MaximumCredentialCountInList);
        }

        [Fact]
        public void Decode_MaxCredIdLength_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(128, fido2Info.MaximumCredentialIdLength);
        }

        [Fact]
        public void Decode_NoMaxCredIdLength_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.MaximumCredentialIdLength);
        }

        [Fact]
        public void Decode_Transports_Correct()
        {
            string[] correctStrings = new string[] {
                "usb",
                "nfc"
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.Transports);
            if (fido2Info.Transports is null)
            {
                return;
            }
            bool isValid = CompareStringLists(correctStrings, fido2Info.Transports);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_NoTransports_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.Transports);
        }

        [Fact]
        public void Decode_Algorithms_Correct()
        {
            var correctAlgs = new CoseAlgorithmIdentifier[] {
                CoseAlgorithmIdentifier.ES256,
                CoseAlgorithmIdentifier.EdDSA
            };
            string[] correctTypes = new string[] {
                "public-key",
                "public-key"
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.Algorithms);
            if (fido2Info.Algorithms is null)
            {
                return;
            }

            int index = 0;
            if (fido2Info.Algorithms.Count == correctAlgs.Length)
            {
                for (; index < correctAlgs.Length; index++)
                {
                    string currentType = fido2Info.Algorithms[index].Item1;
                    CoseAlgorithmIdentifier currentAlg = fido2Info.Algorithms[index].Item2;
                    if (currentType.Equals(correctTypes[index], StringComparison.Ordinal))
                    {
                        if (currentAlg == correctAlgs[index])
                        {
                            continue;
                        }
                    }

                    break;
                }
            }

            // If we broke out early (index < Length), error.
            Assert.True(index >= correctAlgs.Length);
        }

        [Fact]
        public void Decode_NoAlgorithms_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.Algorithms);
        }

        [Fact]
        public void Decode_MaxBlobArraySize_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(2000, fido2Info.MaximumSerializedLargeBlobArray);
        }

        [Fact]
        public void Decode_NoMaxBlobArraySize_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.MaximumSerializedLargeBlobArray);
        }

        [Fact]
        public void Decode_ForcePinChange_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.True(fido2Info.ForcePinChange);
        }

        [Fact]
        public void Decode_NoForcePinChange_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.ForcePinChange);
        }

        [Fact]
        public void Decode_MinPinLength_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(4, fido2Info.MinimumPinLength);
        }

        [Fact]
        public void Decode_NoMinPinLength_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.MinimumPinLength);
        }

        [Fact]
        public void Decode_FirmwareVersion_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(0x00050403, fido2Info.FirmwareVersion);
        }

        [Fact]
        public void Decode_NoFirmwareVersion_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.FirmwareVersion);
        }

        [Fact]
        public void Decode_MaxCredBlobLength_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(36, fido2Info.MaximumCredentialBlobLength);
        }

        [Fact]
        public void Decode_NoMaxCredBlobLength_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.MaximumCredentialBlobLength);
        }

        [Fact]
        public void Decode_MaxRpidLength_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(8, fido2Info.MaximumRpidsForSetMinPinLength);
        }

        [Fact]
        public void Decode_NoMaxRpidLength_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.MaximumRpidsForSetMinPinLength);
        }

        [Fact]
        public void Decode_UvAttempts_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(1, fido2Info.PreferredPlatformUvAttempts);
        }

        [Fact]
        public void Decode_NoUvAttempts_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.PreferredPlatformUvAttempts);
        }

        [Fact]
        public void Decode_UvModality_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(2, fido2Info.UvModality);
        }

        [Fact]
        public void Decode_NoUvModality_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.UvModality);
        }

        [Fact]
        public void Decode_Certifications_Correct()
        {
            string[] correctKeys = new string[] {
                "FIDO"
            };
            int[] correctValues = new int[] {
                2
            };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.Certifications);
            if (fido2Info.Certifications is null)
            {
                return;
            }

            int index = 0;
            if (fido2Info.Certifications.Count == correctKeys.Length)
            {
                for (; index < correctKeys.Length; index++)
                {
                    if (fido2Info.Certifications.TryGetValue(correctKeys[index], out int currentValue))
                    {
                        if (currentValue == correctValues[index])
                        {
                            continue;
                        }
                    }

                    break;
                }
            }

            // If we broke out early (index < Length), error.
            Assert.True(index >= correctKeys.Length);
        }

        [Fact]
        public void Decode_NoCertifications_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.Certifications);
        }

        [Fact]
        public void Decode_RemainingDiscoverable_Correct()
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Equal(2, fido2Info.RemainingDiscoverableCredentials);
        }

        [Fact]
        public void Decode_NoRemainingDiscoverable_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.RemainingDiscoverableCredentials);
        }

        private static bool CompareStringLists(string[] correctStrings, IReadOnlyList<string> candidate)
        {
            if (correctStrings.Length != candidate.Count)
            {
                return false;
            }

            for (int index = 0; index < correctStrings.Length; index++)
            {
                if (!candidate.Contains(correctStrings[index]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static byte[] GetSampleEncoded()
        {
//                b4
//                  01
//                    83
//                      66  55 32 46 5f 56 32
//                      68  46 49 44 4f 5f 32 5f 30
//                      6c  46 49 44 4f 5f 32 5f 31 5f 50 52 45
//                  02
//                    82
//                      6b  63 72 65 64 50 72 6f 74 65 63 74
//                      6b  68 6d 61 63 2d 73 65 63 72 65 74
//                  03
//                    50
//                      2f c0 57 9f 81 13 47 ea b1 16 bb 5a 8d b9 20 2a
//                  04
//                    a5
//                      62  72 6b
//                       f5  // (true)
//                      62  75 70
//                       f5  // (true)
//                      64  70 6c 61 74
//                       f4  // (false)
//                      69  63 6c 69 65 6e 74 50 69 6e
//                       f4  // (false)
//                      75  63 72 65 64 65 6e 74 69 61 6c 4d 67 6d 74 50 72
//                          65 76 69 65 77
//                       f5  // (true)
//                  05
//                    19  04 b0
//                  06
//                    82
//                      02
//                      01
//                  07
//                    08
//                  08
//                    18  80
//                  09
//                    82
//                      63  6e 66 63
//                      63  75 73 62
//                  0a
//                    82
//                      a2
//                        63  61 6c 67
//                         26  // (-7) (P-256)
//                        64  74 79 70 65
//                         6a  70 75 62 6c 69 63 2d 6b 65 79
//                      a2
//                        63  61 6c 67
//                         27  // (-8) (Ed25519)
//                        64  74 79 70 65
//                         6a  70 75 62 6c 69 63 2d 6b 65 79
//                  0b
//                    19  07 D0
//                  0c  f5
//                  0d
//                    04
//                  0e
//                    1a 00 05 04 03
//                  0f  18 24
//                  10  08
//                  11  01
//                  12  02
//                  13
//                    a1
//                       64  46 49 44 4f
//                        02
//                  14 02

            byte[] encodedData = new byte[] {
                0xb4, 0x01, 0x83, 0x66, 0x55, 0x32, 0x46, 0x5f, 0x56, 0x32, 0x68, 0x46, 0x49, 0x44, 0x4f, 0x5f,
                0x32, 0x5f, 0x30, 0x6c, 0x46, 0x49, 0x44, 0x4f, 0x5f, 0x32, 0x5f, 0x31, 0x5f, 0x50, 0x52, 0x45,
                0x02, 0x82, 0x6b, 0x63, 0x72, 0x65, 0x64, 0x50, 0x72, 0x6f, 0x74, 0x65, 0x63, 0x74, 0x6b, 0x68,
                0x6d, 0x61, 0x63, 0x2d, 0x73, 0x65, 0x63, 0x72, 0x65, 0x74, 0x03, 0x50, 0x2f, 0xc0, 0x57, 0x9f,
                0x81, 0x13, 0x47, 0xea, 0xb1, 0x16, 0xbb, 0x5a, 0x8d, 0xb9, 0x20, 0x2a, 0x04, 0xa5, 0x62, 0x72,
                0x6b, 0xf5, 0x62, 0x75, 0x70, 0xf5, 0x64, 0x70, 0x6c, 0x61, 0x74, 0xf4, 0x69, 0x63, 0x6c, 0x69,
                0x65, 0x6e, 0x74, 0x50, 0x69, 0x6e, 0xf4, 0x75, 0x63, 0x72, 0x65, 0x64, 0x65, 0x6e, 0x74, 0x69,
                0x61, 0x6c, 0x4d, 0x67, 0x6d, 0x74, 0x50, 0x72, 0x65, 0x76, 0x69, 0x65, 0x77, 0xf5, 0x05, 0x19,
                0x04, 0xb0, 0x06, 0x82, 0x02, 0x01, 0x07, 0x08, 0x08, 0x18, 0x80, 0x09, 0x82, 0x63, 0x6e, 0x66,
                0x63, 0x63, 0x75, 0x73, 0x62, 0x0a, 0x82, 0xa2, 0x63, 0x61, 0x6c, 0x67, 0x26, 0x64, 0x74, 0x79,
                0x70, 0x65, 0x6a, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 0x2d, 0x6b, 0x65, 0x79, 0xa2, 0x63, 0x61,
                0x6c, 0x67, 0x27, 0x64, 0x74, 0x79, 0x70, 0x65, 0x6a, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 0x2d,
                0x6b, 0x65, 0x79, 0x0b, 0x19, 0x07, 0xD0, 0x0c, 0xf5, 0x0d, 0x04, 0x0e, 0x1a, 0x00, 0x05, 0x04,
                0x03, 0x0f, 0x18, 0x24, 0x10, 0x08, 0x11, 0x01, 0x12, 0x02, 0x13, 0xa1, 0x64, 0x46, 0x49, 0x44,
                0x4f, 0x02, 0x14, 0x02
            };

            // Return a new object so the caller can change data (to test errors
            // e.g.) if desired.
            byte[] returnValue = new byte[encodedData.Length];
            Array.Copy(encodedData, returnValue, encodedData.Length);

            return returnValue;
        }

        private static byte[] GetMinimumEncoded()
        {
//                a2
//                  01
//                    83
//                      66  55 32 46 5f 56 32
//                      68  46 49 44 4f 5f 32 5f 30
//                      6c  46 49 44 4f 5f 32 5f 31 5f 50 52 45
//                  03
//                    50
//                      2f c0 57 9f 81 13 47 ea b1 16 bb 5a 8d b9 20 2a

            byte[] encodedData = new byte[] {
                0xa2, 0x01, 0x83, 0x66, 0x55, 0x32, 0x46, 0x5f, 0x56, 0x32, 0x68, 0x46, 0x49, 0x44, 0x4f, 0x5f,
                0x32, 0x5f, 0x30, 0x6c, 0x46, 0x49, 0x44, 0x4f, 0x5f, 0x32, 0x5f, 0x31, 0x5f, 0x50, 0x52, 0x45,
                0x03, 0x50, 0x2f, 0xc0, 0x57, 0x9f, 0x81, 0x13, 0x47, 0xea, 0xb1, 0x16, 0xbb, 0x5a, 0x8d, 0xb9,
                0x20, 0x2a
            };

            // Return a new object so the caller can change data (to test errors
            // e.g.) if desired.
            byte[] returnValue = new byte[encodedData.Length];
            Array.Copy(encodedData, returnValue, encodedData.Length);

            return returnValue;
        }
    }
}

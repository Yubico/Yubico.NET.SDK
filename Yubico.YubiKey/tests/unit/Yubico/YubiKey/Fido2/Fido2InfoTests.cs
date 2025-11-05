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
using System.Linq;
using Xunit;
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2
{
    public class Fido2InfoTests
    {
        [Fact]
        public void Decode_AuthenticatorInfo()
        {
            var fido2Info = new AuthenticatorInfo(GetSampleEncoded());
            Assert.NotNull(fido2Info);
            Assert.NotNull(fido2Info.EncIdentifier);
            Assert.NotNull(fido2Info.EncCredStoreState);
            Assert.NotNull(fido2Info.AuthenticatorConfigCommands);
        }

        [Fact]
        public void Decode_RepeatKey_Throws()
        {
            byte[] encodedData = new byte[]
            {
                0xa4, 0x01, 0x81, 0x66, 0x55, 0x32, 0x46, 0x5f, 0x56, 0x32, 0x01, 0x81, 0x68, 0x46, 0x49, 0x44,
                0x4f, 0x5f, 0x32, 0x5f, 0x30, 0x11, 0x01, 0x14, 0x02
            };

            _ = Assert.Throws<Ctap2DataException>(() => new AuthenticatorInfo(encodedData));
        }

        [Fact]
        public void Decode_Versions_Correct()
        {
            string[] correctStrings = new string[]
            {
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
            string[] correctStrings = new string[]
            {
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
            byte[] correctValue = new byte[]
            {
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
            string[] correctKeys = new string[]
            {
                "rk", "up", "plat", "clientPin", "credentialMgmtPreview"
            };
            bool[] correctValues = new bool[]
            {
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
            var correctValues = new PinUvAuthProtocol[]
            {
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
            string[] correctStrings = new string[]
            {
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
            var correctAlgs = new CoseAlgorithmIdentifier[]
            {
                CoseAlgorithmIdentifier.ES256,
                CoseAlgorithmIdentifier.EdDSA
            };
            string[] correctTypes = new string[]
            {
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
            string[] correctKeys = new string[]
            {
                "FIDO"
            };
            int[] correctValues = new int[]
            {
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

        [Fact]
        public void Decode_VendorIds_Correct()
        {
            long[] correctIds = new long[] { 0x4d0619f94a0ee581, 0x0000000080000000 };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            bool isValid = CompareLongLists(correctIds, fido2Info.VendorPrototypeConfigCommands);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_NoVendorIds_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.VendorPrototypeConfigCommands);
        }

        [Theory]
        [InlineData("madeUpOption", OptionValue.Unknown)]
        [InlineData("up", OptionValue.True)]
        [InlineData("plat", OptionValue.False)]
        [InlineData("rk", OptionValue.False)]
        [InlineData("noMcGaPermissionsWithClientPin", OptionValue.False)]
        [InlineData("makeCredUvNotRqd", OptionValue.False)]
        [InlineData("clientPin", OptionValue.NotSupported)]
        [InlineData("uv", OptionValue.NotSupported)]
        [InlineData("pinUvAuthToken", OptionValue.NotSupported)]
        [InlineData("largeBlobs", OptionValue.NotSupported)]
        [InlineData("ep", OptionValue.NotSupported)]
        [InlineData("bioEnroll", OptionValue.NotSupported)]
        [InlineData("userVerificationMgmtPreview", OptionValue.NotSupported)]
        [InlineData("uvBioEnroll", OptionValue.NotSupported)]
        [InlineData("authnrCfg", OptionValue.NotSupported)]
        [InlineData("uvAcfg", OptionValue.NotSupported)]
        [InlineData("credMgmt", OptionValue.NotSupported)]
        [InlineData("credentialMgmtPreview", OptionValue.NotSupported)]
        [InlineData("setMinPINLength", OptionValue.NotSupported)]
        [InlineData("alwaysUv", OptionValue.NotSupported)]
        public void GetOptionValue_ReturnsCorrect(
            string option,
            OptionValue expectedValue)
        {
            OptionValue returnedValue = AuthenticatorOptions.GetDefaultOptionValue(option);
            Assert.Equal(expectedValue, returnedValue);
        }

        [Theory]
        [InlineData("madeUpExtension", false)]
        [InlineData("credProtect", true)]
        public void Extensions_IsSupported_Correct(
            string extension,
            bool expectedValue)
        {
            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            bool isSupported = fido2Info.IsExtensionSupported(extension);
            Assert.Equal(expectedValue, isSupported);
        }

        [Fact]
        public void Decode_AuthenticatorConfigCommands_Correct()
        {
            int[] correctInts = new int[] { 1, 2, 3 };

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.AuthenticatorConfigCommands);
            if (fido2Info.AuthenticatorConfigCommands is null)
            {
                return;
            }

            bool isValid = CompareIntLists(correctInts, fido2Info.AuthenticatorConfigCommands);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_NoAuthenticatorConfigCommands_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.AuthenticatorConfigCommands);
        }

        [Fact]
        public void Decode_EncCredStoreState_Correct()
        {
            byte[] correctValue = "encCredStoreStateByte"u8.ToArray();

            byte[] encodedData = GetSampleEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.NotNull(fido2Info.EncCredStoreState);
            if (fido2Info.EncCredStoreState is null)
            {
                return;
            }

            bool isValid = MemoryExtensions.SequenceEqual(correctValue, fido2Info.EncCredStoreState.Value.Span);

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_NoEncCredStoreState_Null()
        {
            byte[] encodedData = GetMinimumEncoded();

            var fido2Info = new AuthenticatorInfo(encodedData);
            Assert.Null(fido2Info.EncCredStoreState);
        }

        private static bool CompareIntLists(
            int[] correctInts,
            IReadOnlyList<int>? candidate)
        {
            if (candidate is null)
            {
                return false;
            }

            if (correctInts.Length != candidate.Count)
            {
                return false;
            }

            for (int index = 0; index < correctInts.Length; index++)
            {
                if (!candidate.Contains(correctInts[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareLongLists(
            long[] correctInts,
            IReadOnlyList<long>? candidate)
        {
            if (candidate is null)
            {
                return false;
            }

            if (correctInts.Length != candidate.Count)
            {
                return false;
            }

            for (int index = 0; index < correctInts.Length; index++)
            {
                if (!candidate.Contains(correctInts[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareStringLists(
            string[] correctStrings,
            IReadOnlyList<string> candidate)
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
            var cw = new CborWriter(CborConformanceMode.Ctap2Canonical, true);
            var cborMapWriter = new CborMapWriter<int>(cw)
                .Entry(1, new [] // Versions
                {
                    "U2F_V2",
                    "FIDO_2_0",
                    "FIDO_2_1_PRE"
                })
                .Entry(2, new [] // Extensions
                {
                    "credProtect",
                    "hmac-secret",
                })
                .Entry(3, Convert.FromHexString("2FC0579F811347EAB116BB5A8DB9202A")) // Aaguid
                .Entry(4, new Dictionary<string, bool> // Options
                {
                    { "rk", true },
                    { "up", true },
                    { "plat", false },
                    { "clientPin", false },
                    { "credentialMgmtPreview", true }
                })
                .Entry(5, 1200) // Maximum message size
                .Entry(6, new[] // PinUvAuthProtocols
                {
                    (int)PinUvAuthProtocol.ProtocolTwo,
                    (int)PinUvAuthProtocol.ProtocolOne
                })
                .Entry(7, 8) // Maximum credential count in list
                .Entry(8, 128) // Maximum credential ID length
                .Entry(9, new [] // Transports
                {
                    "nfc",
                    "usb"
                })
                .Entry(10, new Dictionary<string, object?>[] // Algorithms
                {
                    new()
                    {
                        { "type", "public-key" },
                        { "alg", (int)CoseAlgorithmIdentifier.ES256 }
                    },
                    new()
                    {
                        { "type", "public-key" },
                        { "alg", (int)CoseAlgorithmIdentifier.EdDSA }
                    }
                })
                .Entry(11, 2000) // Maximum serialized large blob array
                .Entry(12, true) // Force PIN change
                .Entry(13, 4) // Minimum PIN length
                .Entry(14, 0x00050403) // Firmware version
                .Entry(15, 36) // Maximum credential blob length
                .Entry(16, 8) // Maximum RPID length for SetMinPinLength
                .Entry(17, 1) // Preferred platform UV attempts
                .Entry(18, 2) // UV modality
                .Entry(19, new Dictionary<string, int> // Certifications
                {
                    { "FIDO", 2 }
                })
                .Entry(20, 2) // Remaining discoverable credentials
                .Entry(21, new [] // vendorPrototypeConfigCommands
                {
                    0x4d0619f94a0ee581, 0x0000000080000000
                })
                .Entry(22, new[] { AttestationFormats.Packed }) // Attestation formats
                .Entry(23, 0) // UvCountSinceLastPinEntry
                .Entry(24, true) // LongTouchForReset
                .Entry(25, "encIdentifierBytes"u8.ToArray()) // EncIdentifier
                .Entry(26, new[] { AuthenticatorTransports.Usb }) // TransportsForReset
                .Entry(27, true) // PinComplexityPolicy
                .Entry(28, "Example.com"u8.ToArray()) // PinComplexityPolicyUrl
                .Entry(29, 33) // MaxPinLength
                .Entry(30, "encCredStoreStateByte"u8.ToArray()) // EncCredStoreState
                .Entry(31, new[] { 1, 2, 3 }); // AuthenticatorConfigCommands

            var encoded = cborMapWriter.Encode();
            return encoded;
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

            byte[] encodedData = new byte[]
            {
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

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
using System.Collections.Generic;
using Xunit;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class CredMgmtDataTests
    {
        private const int NumCredentials = 1;
        private const int RemainCount = 2;
        private const int RpId = 3;
        private const int RpName = 32;
        private const int RpIdHash = 4;
        private const int RpCount = 5;
        private const int UserId = 6;
        private const int UserName = 62;
        private const int UserDisplayName = 63;
        private const int CredIdId = 7;
        private const int CredIdType = 72;
        private const int CredIdTransports = 73;
        private const int PubKeyType = 8;
        private const int PubKeyAlg = 82;
        private const int PubKeyCurve = 83;
        private const int PubKeyX = 84;
        private const int PubKeyY = 85;
        private const int TotalCount = 9;
        private const int CredProtect = 10;
        private const int BlobKey = 11;

        [Fact]
        public void CredMgm_Decode_CorrectNumCredentials()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[NumCredentials];
            if (!(mgmtData.NumberOfDiscoverableCredentials is null) && expected is int unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.NumberOfDiscoverableCredentials;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectRemainCount()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[RemainCount];
            if (!(mgmtData.RemainingCredentialCount is null) && expected is int unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.RemainingCredentialCount;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectRpId()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[RpId];
            if (!(mgmtData.RelyingParty is null) && expected is string unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.RelyingParty.Id;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectRpName()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[RpName];
            if (!(mgmtData.RelyingParty is null) && expected is string unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.RelyingParty.Name;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectRpIdHash()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[RpIdHash];
            if (!(mgmtData.RelyingPartyIdHash is null) && expected is byte[] unboxedValue)
            {
                isCorrect = mgmtData.RelyingPartyIdHash.Value.Span.SequenceEqual(unboxedValue);
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectRpCount()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[RpCount];
            if (!(mgmtData.TotalRelyingPartyCount is null) && expected is int unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.TotalRelyingPartyCount;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectUserId()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[UserId];
            if (!(mgmtData.User is null) && expected is byte[] unboxedValue)
            {
                isCorrect = mgmtData.User.Id.Span.SequenceEqual(unboxedValue);
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectUserName()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[UserName];
            if (!(mgmtData.User is null) && expected is string unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.User.Name;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectUserDisplayName()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[UserDisplayName];
            if (!(mgmtData.User is null) && expected is string unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.User.DisplayName;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectCredIdId()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[CredIdId];
            if (!(mgmtData.CredentialId is null) && expected is byte[] unboxedValue)
            {
                isCorrect = mgmtData.CredentialId.Id.Span.SequenceEqual(unboxedValue);
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectCredIdType()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[CredIdType];
            if (!(mgmtData.CredentialId is null) && expected is string unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.CredentialId.Type;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectCredIdTransports()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[CredIdTransports];
            if (!(mgmtData.CredentialId is null) && expected is string[] unboxedValue)
            {
                if (!(mgmtData.CredentialId.Transports is null) &&
                    mgmtData.CredentialId.Transports.Count == unboxedValue.Length)
                {
                    var index = 0;
                    for (; index < unboxedValue.Length; index++)
                    {
                        if (mgmtData.CredentialId.Transports[index] != unboxedValue[index])
                        {
                            break;
                        }
                    }

                    isCorrect = index >= unboxedValue.Length;
                }
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectKeyType()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[PubKeyType];
            if (!(mgmtData.CredentialPublicKey is null) && expected is CoseKeyType unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.CredentialPublicKey.Type;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectKeyAlg()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[PubKeyAlg];
            if (!(mgmtData.CredentialPublicKey is null) && expected is CoseAlgorithmIdentifier unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.CredentialPublicKey.Algorithm;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectCurve()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[PubKeyCurve];
            if (!(mgmtData.CredentialPublicKey is null) && expected is CoseEcCurve unboxedValue)
            {
                if (mgmtData.CredentialPublicKey is CoseEcPublicKey pubKey)
                {
                    isCorrect = unboxedValue == pubKey.Curve;
                }
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectX()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[PubKeyX];
            if (!(mgmtData.CredentialPublicKey is null) && expected is byte[] unboxedValue)
            {
                if (mgmtData.CredentialPublicKey is CoseEcPublicKey pubKey)
                {
                    isCorrect = pubKey.XCoordinate.Span.SequenceEqual(unboxedValue);
                }
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectY()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[PubKeyY];
            if (!(mgmtData.CredentialPublicKey is null) && expected is byte[] unboxedValue)
            {
                if (mgmtData.CredentialPublicKey is CoseEcPublicKey pubKey)
                {
                    isCorrect = pubKey.YCoordinate.Span.SequenceEqual(unboxedValue);
                }
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectTotalCredentialCount()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[TotalCount];
            if (!(mgmtData.TotalCredentialsForRelyingParty is null) && expected is int unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.TotalCredentialsForRelyingParty;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectCredProtectPolicy()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[CredProtect];
            if (!(mgmtData.CredProtectPolicy is null) && expected is int unboxedValue)
            {
                isCorrect = unboxedValue == mgmtData.CredProtectPolicy;
            }

            Assert.True(isCorrect);
        }

        [Fact]
        public void CredMgm_Decode_CorrectLargeBlobKey()
        {
            var isCorrect = false;
            var mgmtData = GetFullCredMgmtData(out var expectedValues);
            var expected = expectedValues[BlobKey];
            if (!(mgmtData.LargeBlobKey is null) && expected is byte[] unboxedValue)
            {
                isCorrect = mgmtData.LargeBlobKey.Value.Span.SequenceEqual(unboxedValue);
            }

            Assert.True(isCorrect);
        }

        private CredentialManagementData GetFullCredMgmtData(out Dictionary<int, object> expectedValues)
        {
            expectedValues = new Dictionary<int, object>(capacity: 20);

            byte[] encodedData =
            {
                0xAB,
                0x01, 0x02,
                0x02, 0x17,
                0x03, 0xA2,
                0x62, 0x69, 0x64,
                0x64, 0x52, 0x70, 0x49, 0x64,
                0x64, 0x6E, 0x61, 0x6D, 0x65,
                0x66, 0x52, 0x70, 0x4E, 0x61, 0x6d, 0x65,
                0x04, 0x58, 0x20,
                0x11, 0x02, 0x0a, 0xcb, 0x3c, 0x67, 0x94, 0x8b, 0x61, 0xb4, 0x4f, 0xad, 0x3d, 0xa6, 0x24, 0x06,
                0x57, 0xd2, 0xb8, 0x3e, 0x8d, 0x1e, 0x8d, 0xec, 0xda, 0x57, 0x39, 0x82, 0xb8, 0xad, 0x55, 0xed,
                0x05, 0x01,
                0x06, 0xA3,
                0x62, 0x69, 0x64,
                0x46, 0x75, 0x73, 0x65, 0x72, 0x49, 0x64,
                0x64, 0x6E, 0x61, 0x6D, 0x65,
                0x68, 0x75, 0x73, 0x65, 0x72, 0x4E, 0x61, 0x6D, 0x65,
                0x6B, 0x64, 0x69, 0x73, 0x70, 0x6C, 0x61, 0x79, 0x4E, 0x61, 0x6D, 0x65,
                0x69, 0x55, 0x73, 0x65, 0x72, 0x20, 0x4E, 0x61, 0x6D, 0x65,
                0x07, 0xA3,
                0x62, 0x69, 0x64,
                0x44, 0x31, 0x32, 0x33, 0x34,
                0x64, 0x74, 0x79, 0x70, 0x65,
                0x6A, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 0x2d, 0x6b, 0x65, 0x79,
                0x6A, 0x74, 0x72, 0x61, 0x6e, 0x73, 0x70, 0x6f, 0x72, 0x74, 0x73,
                0x82,
                0x63, 0x75, 0x73, 0x62,
                0x63, 0x6e, 0x66, 0x63,
                0x08, 0xa5,
                0x01, 0x02,
                0x03, 0x38, 0x18,
                0x20, 0x01,
                0x21, 0x58, 0x20,
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F, 0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x22, 0x58, 0x20,
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C, 0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B,
                0x09, 0x02,
                0x0A, 0x02,
                0x0B, 0x58, 0x20,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC, 0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02, 0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            };

            expectedValues.Add(NumCredentials, value: 2);
            expectedValues.Add(RemainCount, value: 23);
            expectedValues.Add(RpId, "RpId");
            expectedValues.Add(RpName, "RpName");
            expectedValues.Add(RpIdHash, new byte[]
            {
                0x11, 0x02, 0x0a, 0xcb, 0x3c, 0x67, 0x94, 0x8b,
                0x61, 0xb4, 0x4f, 0xad, 0x3d, 0xa6, 0x24, 0x06,
                0x57, 0xd2, 0xb8, 0x3e, 0x8d, 0x1e, 0x8d, 0xec,
                0xda, 0x57, 0x39, 0x82, 0xb8, 0xad, 0x55, 0xed
            });
            expectedValues.Add(RpCount, value: 1);
            expectedValues.Add(UserId, new byte[] { 0x75, 0x73, 0x65, 0x72, 0x49, 0x64 });
            expectedValues.Add(UserName, "userName");
            expectedValues.Add(UserDisplayName, "User Name");
            expectedValues.Add(CredIdId, new byte[] { 0x31, 0x32, 0x33, 0x34 });
            expectedValues.Add(CredIdType, "public-key");
            expectedValues.Add(CredIdTransports, new[] { "usb", "nfc" });
            expectedValues.Add(PubKeyType, CoseKeyType.Ec2);
            expectedValues.Add(PubKeyAlg, CoseAlgorithmIdentifier.ECDHwHKDF256);
            expectedValues.Add(PubKeyCurve, CoseEcCurve.P256);
            expectedValues.Add(PubKeyX, new byte[]
            {
                0x8B, 0x1C, 0x84, 0x52, 0x7E, 0x02, 0x89, 0x9F,
                0x58, 0x5C, 0xFF, 0xDB, 0x35, 0x48, 0xC3, 0x6E,
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC,
                0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F
            });
            expectedValues.Add(PubKeyY, new byte[]
            {
                0x38, 0x08, 0x01, 0xD5, 0xC2, 0x31, 0x1E, 0x0C,
                0x9D, 0x79, 0x6A, 0x57, 0xDD, 0xD4, 0x42, 0x7B,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02,
                0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            });
            expectedValues.Add(TotalCount, value: 2);
            expectedValues.Add(CredProtect, value: 2);
            expectedValues.Add(BlobKey, new byte[]
            {
                0xBC, 0x29, 0xFC, 0xE7, 0xAC, 0x3E, 0x44, 0xCC,
                0xC4, 0x21, 0xFA, 0xCB, 0xAA, 0x98, 0x47, 0x5F,
                0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02,
                0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B
            });

            return new CredentialManagementData(encodedData);
        }
    }
}

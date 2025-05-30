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
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Commands
{
    public class GenPairResponseTests
    {
        [Fact]
        public void Constructor_NullResponseApdu_ThrowsException()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new GenerateKeyPairResponse(null, 0x87, PivAlgorithm.Rsa2048));
#pragma warning restore CS8625
        }

        [Theory]
        [InlineData(PivSlot.Pin)]
        [InlineData(PivSlot.Puk)]
        [InlineData(PivSlot.Management)]
        [InlineData(0x11)]
        public void Constructor_InvalidSlot_ThrowsException(byte slotNumber)
        {
            ResponseApdu responseApdu = GetResponseApdu(ResponseStatus.Success, KeyType.ECP256);

            _ = Assert.Throws<ArgumentException>(() => new GenerateKeyPairResponse(responseApdu, slotNumber, PivAlgorithm.Rsa2048));
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void GetData_InvalidKeyData_ThrowsException(KeyType keyType)
        {
            ResponseApdu responseApdu = GetBadDataResponseApdu(keyType);
            var response = new GenerateKeyPairResponse(responseApdu, 0x88, keyType.GetPivAlgorithm());
            _ = Assert.Throws<ArgumentException>(() => response.GetData());
        }

        [Theory]
        [InlineData(ResponseStatus.Success, SWConstants.Success)]
        [InlineData(ResponseStatus.AuthenticationRequired, SWConstants.SecurityStatusNotSatisfied)]
        [InlineData(ResponseStatus.Failed, SWConstants.FunctionNotSupported)]
        public void Constructor_SetsStatusWordCorrectly(ResponseStatus responseStatus, short expectedStatusWord)
        {
            ResponseApdu responseApdu = GetResponseApdu(responseStatus, KeyType.ECP384);
            var response = new GenerateKeyPairResponse(responseApdu, 0x89, KeyType.ECP384.GetPivAlgorithm());

            short StatusWord = response.StatusWord;

            Assert.Equal(expectedStatusWord, StatusWord);
        }

        [Theory]
        [InlineData(ResponseStatus.Success)]
        [InlineData(ResponseStatus.AuthenticationRequired)]
        [InlineData(ResponseStatus.Failed)]
        public void Constructor_SetsStatusCorrectly(ResponseStatus responseStatus)
        {
            ResponseApdu responseApdu = GetResponseApdu(responseStatus, KeyType.ECP256);
            var response = new GenerateKeyPairResponse(responseApdu, 0x8A, KeyType.ECP256.GetPivAlgorithm());

            ResponseStatus Status = response.Status;

            Assert.Equal(responseStatus, Status);
        }
        
        [Theory]
        [InlineData(PivSlot.Authentication)]
        [InlineData(PivSlot.Signing)]
        [InlineData(PivSlot.Retired1)]
        [InlineData(PivSlot.Retired20)]
        public void Constructor_SetsSlotNumCorrectly(byte slotNumber)
        {
            ResponseApdu responseApdu = GetResponseApdu(ResponseStatus.Success, KeyType.RSA2048);
            var response = new GenerateKeyPairResponse(responseApdu, slotNumber, KeyType.RSA2048.GetPivAlgorithm());

            byte getSlotNumber = response.SlotNumber;

            Assert.Equal(slotNumber, getSlotNumber);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void GetData_FailResponse_ThrowsException(KeyType keyType)
        {
            ResponseApdu responseApdu = GetResponseApdu(ResponseStatus.Failed, keyType);
            var response = new GenerateKeyPairResponse(responseApdu, 0x8E, keyType.GetPivAlgorithm());

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void GetData_AuthenticationRequiredResponse_ThrowsException(KeyType keyType)
        {
            ResponseApdu responseApdu = GetResponseApdu(ResponseStatus.AuthenticationRequired, keyType);
            var response = new GenerateKeyPairResponse(responseApdu, 0x8E, keyType.GetPivAlgorithm());

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void GetData_ReturnsCorrectData(KeyType keyType) // TODO Make test for CUrve keys
        {
            ResponseApdu responseApdu = GetResponseApdu(ResponseStatus.Success, keyType);
            byte[] keyData = GetCorrectEncodingForAlgorithm(keyType);

            var response = new GenerateKeyPairResponse(responseApdu, 0x8F, keyType.GetPivAlgorithm());
#pragma warning disable CS0618 // Type or member is obsolete
            PivPublicKey getData = response.GetData();
#pragma warning restore CS0618 // Type or member is obsolete

            var keyDataSpan = new ReadOnlySpan<byte>(keyData);
            bool compareResult = keyDataSpan.SequenceEqual(getData.PivEncodedPublicKey.Span);

            Assert.True(compareResult);
        }

        // If ResponseStatus is Success, build a response for the keyType. If
        // the keyType is not supported, use Rsa1024.
        // If ResponseStatus is AuthenticationRequired, ignore keyType, build
        // the response of 6982.
        // If ResponseStatus is Failed, ignore keyType, build the response of
        // 6A81.
        private static ResponseApdu GetResponseApdu(ResponseStatus responseStatus, KeyType keyType)
        {
            byte[] responseData;

            switch (responseStatus)
            {
                default:
                    byte[] errorResponse = new byte[2] { 0x6A, 0x81 };
                    responseData = errorResponse;
                    break;

                case ResponseStatus.AuthenticationRequired:
                    byte[] authResponse = new byte[2] { 0x69, 0x82 };
                    responseData = authResponse;
                    break;

                case ResponseStatus.Success:
                    responseData = BuildResponseForAlgorithm(keyType);
                    break;
            }

            return new ResponseApdu(responseData);
        }

        private static byte[] BuildResponseForAlgorithm(KeyType keyType)
        {
            byte[] encoding = GetCorrectEncodingForAlgorithm(keyType);
            byte[] returnValue = new byte[encoding.Length + 2];
            Array.Copy(encoding, returnValue, encoding.Length);
            returnValue[encoding.Length] = 0x90;
            returnValue[encoding.Length + 1] = 0x00;

            return returnValue;
        }

        private static byte[] GetCorrectEncodingForAlgorithm(KeyType keyType)
        {
            return keyType switch
            {
                KeyType.RSA2048 => new byte[] {
                    0x7F, 0x49, 0x82, 0x01, 0x09,
                        0x81, 0x82, 0x01, 0x00,
                            0xF1, 0x50, 0xBE, 0xFB, 0xB0, 0x9C, 0xAD, 0xFE,
                            0xF8, 0x0A, 0x3D, 0x10, 0x8C, 0x36, 0x92, 0xDC,
                            0x34, 0xB7, 0x09, 0x86, 0x42, 0xC9, 0xCD, 0x00,
                            0x55, 0xD1, 0xA4, 0xA0, 0x40, 0x61, 0x5A, 0x2A,
                            0x8A, 0xB4, 0x7D, 0xAC, 0xA1, 0x34, 0xA2, 0x2F,
                            0x0A, 0x36, 0xD2, 0x34, 0xB7, 0xD8, 0x72, 0x58,
                            0x20, 0xD6, 0x04, 0x66, 0x80, 0x7A, 0x7A, 0x0A,
                            0xD1, 0x03, 0x32, 0xA2, 0xD0, 0xC9, 0x92, 0x7E,
                            0x59, 0xB8, 0x63, 0xF8, 0xFD, 0xA3, 0x0F, 0xD0,
                            0xF1, 0xA1, 0x48, 0x50, 0xDF, 0x82, 0xDC, 0x4F,
                            0x9F, 0x7C, 0x18, 0x02, 0x29, 0x35, 0x72, 0xDD,
                            0x10, 0x54, 0x80, 0x12, 0x68, 0x89, 0x8F, 0x05,
                            0xCA, 0xA0, 0xEB, 0xD4, 0xF0, 0x82, 0x85, 0xB8,
                            0x67, 0xAD, 0xF3, 0xF7, 0x86, 0x2E, 0xD3, 0x6E,
                            0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53,
                            0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                            0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35,
                            0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                            0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07,
                            0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                            0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00,
                            0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                            0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29,
                            0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                            0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86,
                            0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                            0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59,
                            0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                            0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF,
                            0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFA,
                            0xEC, 0x48, 0xCC, 0xE7, 0xBA, 0x8B, 0xF7, 0x56,
                            0x6B, 0xDD, 0x7B, 0x56, 0x2A, 0x3B, 0xE7, 0xE9,
                        0x82, 0x03, 0x01, 0x00, 0x01
                },
                KeyType.ECP256 => new byte[] {
                    0x7F, 0x49, 0x43,
                        0x86, 0x41, 0x04, 0xC4, 0x17, 0x7F, 0x2B, 0x96,
                            0x8F, 0x9C, 0x00, 0x0C, 0x4F, 0x3D, 0x2B, 0x88,
                            0xB0, 0xAB, 0x5B, 0x0C, 0x3B, 0x19, 0x42, 0x63,
                            0x20, 0x8C, 0xA1, 0x2F, 0xEE, 0x1C, 0xB4, 0xD8,
                            0x81, 0x96, 0x9F, 0xD8, 0xC8, 0xD0, 0x8D, 0xD1,
                            0xBB, 0x66, 0x58, 0x00, 0x26, 0x7D, 0x05, 0x34,
                            0xA8, 0xA3, 0x30, 0xD1, 0x59, 0xDE, 0x66, 0x01,
                            0x0E, 0x3F, 0x21, 0x13, 0x29, 0xC5, 0x98, 0x56,
                            0x07, 0xB5, 0x26
                },
                KeyType.ECP384 => new byte[] {
                    0x7F, 0x49, 0x63,
                        0x86, 0x61, 0x04, 0xD5, 0x8A, 0xFF, 0x00, 0x3E,
                            0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E, 0x9D,
                            0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29, 0xA5,
                            0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15, 0xF3,
                            0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86, 0xBA,
                            0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A, 0xF9,
                            0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59, 0xF8,
                            0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5, 0x01,
                            0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF, 0x39,
                            0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFA, 0xEC,
                            0xA8, 0xA3, 0x30, 0xD1, 0x59, 0xDE, 0x66, 0x01,
                            0x48, 0xCC, 0xE7, 0xB7, 0xD8, 0x72, 0x58, 0x20,
                            0xD6, 0x04, 0x66
                },
                _ => new byte[] {
                    0x7F, 0x49, 0x81, 0x88,
                        0x81, 0x81, 0x80,
                            0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53,
                            0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                            0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35,
                            0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                            0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07,
                            0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                            0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00,
                            0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                            0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29,
                            0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                            0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86,
                            0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                            0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59,
                            0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                            0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF,
                            0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFB,
                        0x82, 0x03, 0x01, 0x00, 0x01
                },
            };
        }

        // Get a ResponseApdu that claims to be Success, but actually contains
        // data that is not in the correct form.
        // For example, return a TLV that is RSA: modulus || expo | modulus, or
        // ECC that is point || point.
        private static ResponseApdu GetBadDataResponseApdu(KeyType keyType)
        {
            byte[] responseData = keyType switch
            {
                KeyType.RSA2048 => new byte[] {
                    0x7F, 0x49, 0x82, 0x01, 0x04,
                        0x81, 0x82, 0x01, 0x00,
                            0xF1, 0x50, 0xBE, 0xFB, 0xB0, 0x9C, 0xAD, 0xFE,
                            0xF8, 0x0A, 0x3D, 0x10, 0x8C, 0x36, 0x92, 0xDC,
                            0x34, 0xB7, 0x09, 0x86, 0x42, 0xC9, 0xCD, 0x00,
                            0x55, 0xD1, 0xA4, 0xA0, 0x40, 0x61, 0x5A, 0x2A,
                            0x8A, 0xB4, 0x7D, 0xAC, 0xA1, 0x34, 0xA2, 0x2F,
                            0x0A, 0x36, 0xD2, 0x34, 0xB7, 0xD8, 0x72, 0x58,
                            0x20, 0xD6, 0x04, 0x66, 0x80, 0x7A, 0x7A, 0x0A,
                            0xD1, 0x03, 0x32, 0xA2, 0xD0, 0xC9, 0x92, 0x7E,
                            0x59, 0xB8, 0x63, 0xF8, 0xFD, 0xA3, 0x0F, 0xD0,
                            0xF1, 0xA1, 0x48, 0x50, 0xDF, 0x82, 0xDC, 0x4F,
                            0x9F, 0x7C, 0x18, 0x02, 0x29, 0x35, 0x72, 0xDD,
                            0x10, 0x54, 0x80, 0x12, 0x68, 0x89, 0x8F, 0x05,
                            0xCA, 0xA0, 0xEB, 0xD4, 0xF0, 0x82, 0x85, 0xB8,
                            0x67, 0xAD, 0xF3, 0xF7, 0x86, 0x2E, 0xD3, 0x6E,
                            0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53,
                            0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                            0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35,
                            0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                            0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07,
                            0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                            0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00,
                            0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                            0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29,
                            0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                            0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86,
                            0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                            0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59,
                            0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                            0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF,
                            0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD, 0xFA,
                            0xEC, 0x48, 0xCC, 0xE7, 0xBA, 0x8B, 0xF7, 0x56,
                            0x6B, 0xDD, 0x7B, 0x56, 0x2A, 0x3B, 0xE7, 0xE9,
                        0x90, 0x00
                },
                KeyType.ECP256 => new byte[] {
                    0x7F, 0x49, 0x44,
                        0x86, 0x42,
                            0x04, 0xC4, 0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C,
                            0x00, 0x0C, 0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB,
                            0x5B, 0x0C, 0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C,
                            0xA1, 0x2F, 0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96,
                            0x9F, 0xD8, 0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66,
                            0x58, 0x00, 0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3,
                            0x30, 0xD1, 0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F,
                            0x21, 0x13, 0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5,
                            0x26, 0x11,
                        0x90, 0x00
                },
                KeyType.ECP384 => new byte[] {
                    0x7F, 0x49, 0x62,
                        0x86, 0x60,
                            0xD5, 0x8A, 0xFF, 0x00, 0x3E, 0x88, 0xFB, 0xC1,
                            0xE1, 0xF8, 0x37, 0x8E, 0x9D, 0xDB, 0x5D, 0x45,
                            0x61, 0x1B, 0x29, 0x29, 0xA5, 0xB7, 0xC3, 0xE7,
                            0x38, 0xE9, 0x1A, 0x15, 0xF3, 0x58, 0xDD, 0xCA,
                            0xE2, 0xE1, 0x3D, 0x86, 0xBA, 0xBC, 0x63, 0xE2,
                            0xCD, 0xA4, 0x75, 0x3A, 0xF9, 0x9C, 0xD8, 0x23,
                            0x0F, 0xD8, 0x18, 0x59, 0xF8, 0x12, 0x29, 0x62,
                            0xAB, 0xDC, 0xBE, 0xA5, 0x01, 0xC5, 0x28, 0xC3,
                            0xE8, 0xA1, 0x65, 0xCF, 0x39, 0x30, 0x66, 0x18,
                            0x6A, 0xE5, 0xAD, 0xFA, 0xEC, 0xA8, 0xA3, 0x30,
                            0xD1, 0x59, 0xDE, 0x66, 0x01, 0x48, 0xCC, 0xE7,
                            0xB7, 0xD8, 0x72, 0x58, 0x20, 0xD6, 0x04, 0x66,
                        0x90, 0x00
                },
                _ => new byte[] {
                    0x7F, 0x49, 0x81, 0x86,
                        0x81, 0x7F,
                            0xC8, 0xE0, 0x46, 0xC4, 0x6C, 0x67, 0x57, 0x53,
                            0x47, 0xC7, 0x38, 0x84, 0xAC, 0xF4, 0xF4, 0x44,
                            0x81, 0xAB, 0xDB, 0x64, 0xEE, 0x53, 0xB5, 0x35,
                            0xAE, 0x92, 0xFF, 0x8E, 0xFE, 0x00, 0xA7, 0xA8,
                            0xB2, 0x86, 0x3B, 0x66, 0xDB, 0x8E, 0xA7, 0x07,
                            0xFF, 0x13, 0x28, 0x49, 0xE5, 0x9B, 0xD1, 0xC8,
                            0xD2, 0x2C, 0xF9, 0x84, 0xD5, 0x8A, 0xFF, 0x00,
                            0x3E, 0x88, 0xFB, 0xC1, 0xE1, 0xF8, 0x37, 0x8E,
                            0x9D, 0xDB, 0x5D, 0x45, 0x61, 0x1B, 0x29, 0x29,
                            0xA5, 0xB7, 0xC3, 0xE7, 0x38, 0xE9, 0x1A, 0x15,
                            0xF3, 0x58, 0xDD, 0xCA, 0xE2, 0xE1, 0x3D, 0x86,
                            0xBA, 0xBC, 0x63, 0xE2, 0xCD, 0xA4, 0x75, 0x3A,
                            0xF9, 0x9C, 0xD8, 0x23, 0x0F, 0xD8, 0x18, 0x59,
                            0xF8, 0x12, 0x29, 0x62, 0xAB, 0xDC, 0xBE, 0xA5,
                            0x01, 0xC5, 0x28, 0xC3, 0xE8, 0xA1, 0x65, 0xCF,
                            0x39, 0x30, 0x66, 0x18, 0x6A, 0xE5, 0xAD,
                        0x82, 0x03, 0x01, 0x00, 0x01,
                        0x90, 0x00
                },
            };
            return new ResponseApdu(responseData);
        }
    }
}

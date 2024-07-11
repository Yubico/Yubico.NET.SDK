﻿// Copyright 2021 Yubico AB
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
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    public class RegisterResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void action()
            {
                _ = new RegisterResponse(responseApdu: null);
            }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new RegisterResponse(responseApdu);

            Assert.Equal(SWConstants.Success, registerResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new RegisterResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, registerResponse.Status);
        }

        [Fact]
        public void Constructor_ConditionsNotSatisfiedResponseApdu_SetsStatusCorrectly()
        {
            var sw1 = (byte)(SWConstants.ConditionsNotSatisfied >> 8);
            var sw2 = unchecked((byte)SWConstants.ConditionsNotSatisfied);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new RegisterResponse(responseApdu);

            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, registerResponse.Status);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void GetData_BadResponseData_Throws(bool validPubKey, bool validKeyHandle)
        {
            var encoding = RegistrationDataTests.GetEncodedRegistration(validPubKey, validKeyHandle);
            var responseApdu = new ResponseApdu(encoding, SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            _ = Assert.Throws<ArgumentException>(() => registerResponse.GetData());
        }

        [Fact]
        public void GetData_GoodResponseData_Succeeds()
        {
            var encoding = RegistrationDataTests.GetEncodedRegistration(validPubKey: true, validKeyHandle: true);
            var responseApdu = new ResponseApdu(encoding, SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            _ = registerResponse.GetData();
        }

        [Fact]
        public void GetData_GoodResponseData_SetsUserPublicKeyCorrectly()
        {
            var encoding = RegistrationDataTests.GetEncodedRegistration(validPubKey: true, validKeyHandle: true);
            var responseApdu = new ResponseApdu(encoding, SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            var data = registerResponse.GetData();
            var pubKeyPoint = new ECPoint
            {
                X = data.UserPublicKey.Slice(start: 1, length: 32).ToArray(),
                Y = data.UserPublicKey.Slice(start: 33, length: 32).ToArray()
            };

            Assert.Equal(RegistrationDataTests.GetPubKeyX(), Hex.BytesToHex(pubKeyPoint.X));
            Assert.Equal(RegistrationDataTests.GetPubKeyY(), Hex.BytesToHex(pubKeyPoint.Y));
        }

        [Fact]
        public void GetData_GoodResponseData_SetsKeyHandleCorrectly()
        {
            var encoding = RegistrationDataTests.GetEncodedRegistration(validPubKey: true, validKeyHandle: true);
            var responseApdu = new ResponseApdu(encoding, SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            var data = registerResponse.GetData();
            var keyHandle = RegistrationDataTests.GetKeyHandle(isValid: true, out var _);
            Assert.Equal(keyHandle, Hex.BytesToHex(data.KeyHandle.ToArray()));
        }

        [Fact]
        public void GetData_GoodResponseData_SetsCertificateCorrectly()
        {
            var encoding = RegistrationDataTests.GetEncodedRegistration(validPubKey: true, validKeyHandle: true);
            var responseApdu = new ResponseApdu(encoding, SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            var data = registerResponse.GetData();
            var cert = RegistrationDataTests.GetAttestationCert();
            Assert.Equal(cert, Hex.BytesToHex(data.AttestationCert.RawData.ToArray()));
        }

        [Fact]
        public void GetData_GoodResponseData_SetsSignatureCorrectly()
        {
            var encoding = RegistrationDataTests.GetEncodedRegistration(validPubKey: true, validKeyHandle: true);
            var responseApdu = new ResponseApdu(encoding, SWConstants.Success);

            var registerResponse = new RegisterResponse(responseApdu);

            var data = registerResponse.GetData();
            Assert.Equal(RegistrationDataTests.GetRegSignature(), Hex.BytesToHex(data.Signature.ToArray()));
        }
    }
}

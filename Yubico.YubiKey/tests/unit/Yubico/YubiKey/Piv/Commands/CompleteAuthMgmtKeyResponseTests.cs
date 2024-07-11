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

namespace Yubico.YubiKey.Piv.Commands
{
    public class CompleteAuthMgmtKeyResponseTests
    {
        private const int ApduMutual = 0;
        private const int ApduSingle = 1;
        private const int ApduNoAuth = 2;
        private const int ApduError = 32;

        static readonly ReadOnlyMemory<byte> _yubiKeyAuthenticationExpectedResponse = new byte[8]
        {
            0xAC, 0x29, 0xA4, 0x5E, 0x1F, 0x42, 0x8A, 0x23
        };

        static readonly ReadOnlyMemory<byte> _wrongYubiKeyAuthenticationResponse = new byte[8]
        {
            0xAC, 0x29, 0xA4, 0x00, 0x1F, 0x42, 0x8A, 0x23
        };

        static readonly ReadOnlyMemory<byte> _empty = ReadOnlyMemory<byte>.Empty;

        [Fact]
        public void Constructor_NullResponseApdu_ThrowsException()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() =>
                new CompleteAuthenticateManagementKeyResponse(null, _yubiKeyAuthenticationExpectedResponse));
#pragma warning restore CS8625
        }

        [Theory]
        [InlineData(ApduSingle, ResponseStatus.Success)]
        [InlineData(ApduNoAuth, ResponseStatus.AuthenticationRequired)]
        [InlineData(ApduError, ResponseStatus.Failed)]
        public void ConstructorSingle_ResponseApdu_SetsStatusCorrectly(int responseFlag, ResponseStatus status)
        {
            ResponseApdu? responseApdu = GetResponseApdu(responseFlag);
            var response = new CompleteAuthenticateManagementKeyResponse(responseApdu, _empty);

            Assert.Equal(status, response.Status);
        }

        [Theory]
        [InlineData(ApduMutual, ResponseStatus.Success)]
        [InlineData(ApduNoAuth, ResponseStatus.AuthenticationRequired)]
        [InlineData(ApduError, ResponseStatus.Failed)]
        public void ConstructorMutual_ResponseApdu_SetsStatusCorrectly(int responseFlag, ResponseStatus status)
        {
            ResponseApdu? responseApdu = GetResponseApdu(responseFlag);
            var response =
                new CompleteAuthenticateManagementKeyResponse(responseApdu, _yubiKeyAuthenticationExpectedResponse);

            Assert.Equal(status, response.Status);
        }

        [Theory]
        [InlineData(ApduSingle, SWConstants.Success)]
        [InlineData(ApduNoAuth, SWConstants.ConditionsNotSatisfied)]
        [InlineData(ApduError, SWConstants.FunctionNotSupported)]
        public void ConstructorSingle_ResponseApdu_SetsStatusWordCorrectly(int responseFlag, short statusWord)
        {
            ResponseApdu? responseApdu = GetResponseApdu(responseFlag);
            var response = new CompleteAuthenticateManagementKeyResponse(responseApdu, _empty);

            Assert.Equal(statusWord, response.StatusWord);
        }

        [Theory]
        [InlineData(ApduMutual, SWConstants.Success)]
        [InlineData(ApduNoAuth, SWConstants.ConditionsNotSatisfied)]
        [InlineData(ApduError, SWConstants.FunctionNotSupported)]
        public void ConstructorMutual_ResponseApdu_SetsStatusWordCorrectly(int responseFlag, short statusWord)
        {
            ResponseApdu? responseApdu = GetResponseApdu(responseFlag);
            var response =
                new CompleteAuthenticateManagementKeyResponse(responseApdu, _yubiKeyAuthenticationExpectedResponse);

            Assert.Equal(statusWord, response.StatusWord);
        }

        [Fact]
        public void ErrorInput_GetData_ThrowException()
        {
            ResponseApdu? responseApdu = GetResponseApdu(ApduError);
            var response =
                new CompleteAuthenticateManagementKeyResponse(responseApdu, _yubiKeyAuthenticationExpectedResponse);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        [Fact]
        public void ConstructorSingle_GetDataNonEmptyYubiKeyAuthenticationResponse_ThrowsException()
        {
            ResponseApdu? responseApdu = GetResponseApdu(ApduSingle);
            var response =
                new CompleteAuthenticateManagementKeyResponse(responseApdu, _yubiKeyAuthenticationExpectedResponse);

            _ = Assert.Throws<MalformedYubiKeyResponseException>(() => response.GetData());
        }

        [Theory]
        [InlineData(ApduMutual, AuthenticateManagementKeyResult.MutualFullyAuthenticated)]
        [InlineData(ApduNoAuth, AuthenticateManagementKeyResult.MutualOffCardAuthenticationFailed)]
        public void ConstructorMutual_GetData_CorrectResult(
            int responseFlag,
            AuthenticateManagementKeyResult expectedAuth)
        {
            ResponseApdu? responseApdu = GetResponseApdu(responseFlag);
            var response =
                new CompleteAuthenticateManagementKeyResponse(responseApdu, _yubiKeyAuthenticationExpectedResponse);

            AuthenticateManagementKeyResult getData = response.GetData();

            Assert.Equal(expectedAuth, getData);
        }

        [Theory]
        [InlineData(ApduSingle, AuthenticateManagementKeyResult.SingleAuthenticated)]
        [InlineData(ApduNoAuth, AuthenticateManagementKeyResult.SingleAuthenticationFailed)]
        public void ConstructorSingle_GetData_CorrectResult(
            int responseFlag, AuthenticateManagementKeyResult expectedAuth)
        {
            ResponseApdu? responseApdu = GetResponseApdu(responseFlag);
            var response = new CompleteAuthenticateManagementKeyResponse(responseApdu, _empty);

            AuthenticateManagementKeyResult getData = response.GetData();

            Assert.Equal(expectedAuth, getData);
        }

        [Fact]
        public void YubiKeyFailAuth_GetData_CorrectResult()
        {
            ResponseApdu? responseApdu = GetResponseApdu(ApduMutual);
            var response =
                new CompleteAuthenticateManagementKeyResponse(responseApdu, _wrongYubiKeyAuthenticationResponse);

            AuthenticateManagementKeyResult getData = response.GetData();

            Assert.Equal(AuthenticateManagementKeyResult.MutualYubiKeyAuthenticationFailed, getData);
        }

        // Return an APDU based on responseFlag
        //  0: Success, mutual auth
        //  1: Success, single auth
        //  2: SW = 6985, mgmt key did not authenticate
        //  3: error
        //  any other value for responseFlag: error
        private static ResponseApdu GetResponseApdu(int responseFlag)
        {
            byte successSw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte successSw2 = unchecked((byte)SWConstants.Success);
            byte noAuthSw1 = unchecked((byte)(SWConstants.ConditionsNotSatisfied >> 8));
            byte noAuthSw2 = unchecked((byte)SWConstants.ConditionsNotSatisfied);
            byte errorSw1 = unchecked((byte)(SWConstants.FunctionNotSupported >> 8));
            byte errorSw2 = unchecked((byte)SWConstants.FunctionNotSupported);
            byte[] apduMutual = new byte[]
            {
                0x7C, 0x0A, 0x82, 0x08, 0xAC, 0x29, 0xA4, 0x5E, 0x1F, 0x42, 0x8A, 0x23, successSw1, successSw2
            };
            byte[] apduSingle = new byte[]
            {
                successSw1, successSw2
            };
            byte[] apduNoAuth = new byte[]
            {
                noAuthSw1, noAuthSw2
            };
            byte[] apduError = new byte[]
            {
                errorSw1, errorSw2
            };

            switch (responseFlag)
            {
                default:
                    return new ResponseApdu(apduError);

                case ApduMutual:
                    return new ResponseApdu(apduMutual);

                case ApduSingle:
                    return new ResponseApdu(apduSingle);

                case ApduNoAuth:
                    return new ResponseApdu(apduNoAuth);
            }
        }
    }
}

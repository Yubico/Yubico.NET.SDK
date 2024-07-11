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
using System.Text;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class ListCredentialsResponseTests
    {
        [Fact]
        public void Constructor_ReturnsObject()
        {
            ResponseApdu apdu = new ResponseApdu(new byte[0], SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            Assert.NotNull(response);
        }

        [Fact]
        public void GetData_ResponseStatusFailed_ThrowsInvalidOperationException()
        {
            ResponseApdu apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            Action action = () => response.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GetData_ResponseStatusFailed_ExceptionMessageMatchesStatusMessage()
        {
            ResponseApdu apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            try
            {
                _ = response.GetData();
            }
            catch (InvalidOperationException ex)
            {
                Assert.Equal(response.StatusMessage, ex.Message);
            }
        }

        [Fact]
        public void GetData_DataTagAlgorithm_ThrowsMalformedException()
        {
            byte[] dataWithoutSw = new byte[] { DataTagConstants.CryptographicKeyType, 1, 0 };
            ResponseApdu apdu = new ResponseApdu(dataWithoutSw, SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            Action action = () => response.GetData();

            _ = Assert.Throws<MalformedYubiKeyResponseException>(action);
        }

        [Fact]
        public void GetData_DataTagAlgorithm_ExceptionMessageInvalidDataTag()
        {
            string expectedMessage = $"The value {DataTagConstants.CryptographicKeyType} is not " +
                                     $"a data tag supported by the YubiKey application.";

            byte[] dataWithoutSw = new byte[] { DataTagConstants.CryptographicKeyType, 1, 0 };
            ResponseApdu apdu = new ResponseApdu(dataWithoutSw, SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            try
            {
                _ = response.GetData();
            }
            catch (MalformedYubiKeyResponseException ex)
            {
                Assert.Equal(expectedMessage, ex.Message);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65)]
        public void GetData_InvalidElementSize_ThrowsMalformedException(int labelLength)
        {
            // Algo - None
            // Touch - false
            // Label - string of given size
            // Retries - 0
            List<byte> credRetryData = new List<byte>
            {
                (byte)CryptographicKeyType.None,
                0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(new char[labelLength]));
            credRetryData.Add(0);

            List<byte> dataWithoutSw = new List<byte>
            {
                DataTagConstants.LabelList,
                (byte)credRetryData.Count
            };
            dataWithoutSw.AddRange(credRetryData);

            ResponseApdu apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            Action action = () => response.GetData();

            _ = Assert.Throws<MalformedYubiKeyResponseException>(action);
        }

        [Fact]
        public void GetData_ElementSize3_ExceptionMessageInvalidCredRetryDataLength()
        {
            string expectedMessage = $"Invalid size of credential and retry data.";

            // Algo - None
            // Touch - false
            // Label - string of given size
            // Retries - 0
            List<byte> credRetryData = new List<byte>
            {
                (byte)CryptographicKeyType.None,
                0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(new char[0]));
            credRetryData.Add(0);

            List<byte> dataWithoutSw = new List<byte>
            {
                DataTagConstants.LabelList,
                (byte)credRetryData.Count
            };
            dataWithoutSw.AddRange(credRetryData);

            ResponseApdu apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            string actualMessage = "";
            try
            {
                _ = response.GetData();
            }
            catch (MalformedYubiKeyResponseException ex)
            {
                actualMessage = ex.Message;
            }

            Assert.Equal(expectedMessage, actualMessage);
        }

        [Fact]
        public void GetData_ZeroElements_ReturnsEmptyList()
        {
            ResponseApdu apdu = new ResponseApdu(new byte[0], SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            List<CredentialRetryPair> credentialRetryPairs = response.GetData();

            Assert.Empty(credentialRetryPairs);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(64)]
        public void GetData_OneElement_MatchesExpectedCredRetryObject(int labelSize)
        {
            CryptographicKeyType expectedKeyType = CryptographicKeyType.Aes128;
            string expectedLabel = new string('a', labelSize);
            bool expectedTouch = false;
            byte expectedRetryCount = 0;

            // Algo - None
            // Touch - false
            // Label - string of given size
            // Retries - 0
            List<byte> credRetryData = new List<byte>
            {
                (byte)expectedKeyType,
                expectedTouch ? (byte)1 : (byte)0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(expectedLabel));
            credRetryData.Add(expectedRetryCount);

            List<byte> dataWithoutSw = new List<byte>
            {
                DataTagConstants.LabelList,
                (byte)credRetryData.Count
            };
            dataWithoutSw.AddRange(credRetryData);

            ResponseApdu apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            List<CredentialRetryPair> pairs = response.GetData();

            // Only one element
            _ = Assert.Single(pairs);

            foreach (CredentialRetryPair pair in pairs)
            {
                Assert.Equal(expectedKeyType, pair.Credential.KeyType);
                Assert.Equal(expectedTouch, pair.Credential.TouchRequired);
                Assert.Equal(expectedLabel, pair.Credential.Label);
                Assert.Equal(expectedRetryCount, pair.Retries);
            }
        }

        [Fact]
        public void GetData_TwoElements_MatchesExpectedCredRetryObjects()
        {
            CryptographicKeyType expectedKeyType = CryptographicKeyType.Aes128;
            string expectedLabel = new string('a', 3);
            bool expectedTouch = false;
            byte expectedRetryCount = 0;

            // Algo - None
            // Touch - false
            // Label - string of given size
            // Retries - 0
            List<byte> credRetryData = new List<byte>
            {
                (byte)expectedKeyType,
                expectedTouch ? (byte)1 : (byte)0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(expectedLabel));
            credRetryData.Add(expectedRetryCount);

            List<byte> dataWithoutSw = new List<byte>
            {
                // First element
                DataTagConstants.LabelList,
                (byte)credRetryData.Count
            };
            dataWithoutSw.AddRange(credRetryData);

            // Second element
            dataWithoutSw.Add(DataTagConstants.LabelList);
            dataWithoutSw.Add((byte)credRetryData.Count);
            dataWithoutSw.AddRange(credRetryData);

            ResponseApdu apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            ListCredentialsResponse response = new ListCredentialsResponse(apdu);

            List<CredentialRetryPair> pairs = response.GetData();

            // Two elements
            Assert.Equal(2, pairs.Count);

            foreach (CredentialRetryPair pair in pairs)
            {
                Assert.Equal(expectedKeyType, pair.Credential.KeyType);
                Assert.Equal(expectedTouch, pair.Credential.TouchRequired);
                Assert.Equal(expectedLabel, pair.Credential.Label);
                Assert.Equal(expectedRetryCount, pair.Retries);
            }
        }
    }
}

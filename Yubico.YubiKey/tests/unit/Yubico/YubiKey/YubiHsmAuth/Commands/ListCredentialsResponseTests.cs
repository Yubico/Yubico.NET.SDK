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
            var apdu = new ResponseApdu(new byte[0], SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

            Assert.NotNull(response);
        }

        [Fact]
        public void GetData_ResponseStatusFailed_ThrowsInvalidOperationException()
        {
            var apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            var response = new ListCredentialsResponse(apdu);

            Action action = () => response.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GetData_ResponseStatusFailed_ExceptionMessageMatchesStatusMessage()
        {
            var apdu = new ResponseApdu(new byte[0], SWConstants.AuthenticationMethodBlocked);

            var response = new ListCredentialsResponse(apdu);

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
            var dataWithoutSw = new byte[] { DataTagConstants.CryptographicKeyType, 1, 0 };
            var apdu = new ResponseApdu(dataWithoutSw, SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

            Action action = () => response.GetData();

            _ = Assert.Throws<MalformedYubiKeyResponseException>(action);
        }

        [Fact]
        public void GetData_DataTagAlgorithm_ExceptionMessageInvalidDataTag()
        {
            var expectedMessage = $"The value {DataTagConstants.CryptographicKeyType} is not " +
                                  "a data tag supported by the YubiKey application.";

            var dataWithoutSw = new byte[] { DataTagConstants.CryptographicKeyType, 1, 0 };
            var apdu = new ResponseApdu(dataWithoutSw, SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

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
            var credRetryData = new List<byte>
            {
                (byte)CryptographicKeyType.None,
                0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(new char[labelLength]));
            credRetryData.Add(item: 0);

            var dataWithoutSw = new List<byte>
            {
                DataTagConstants.LabelList,
                (byte)credRetryData.Count
            };
            dataWithoutSw.AddRange(credRetryData);

            var apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

            Action action = () => response.GetData();

            _ = Assert.Throws<MalformedYubiKeyResponseException>(action);
        }

        [Fact]
        public void GetData_ElementSize3_ExceptionMessageInvalidCredRetryDataLength()
        {
            var expectedMessage = "Invalid size of credential and retry data.";

            // Algo - None
            // Touch - false
            // Label - string of given size
            // Retries - 0
            var credRetryData = new List<byte>
            {
                (byte)CryptographicKeyType.None,
                0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(new char[0]));
            credRetryData.Add(item: 0);

            var dataWithoutSw = new List<byte>
            {
                DataTagConstants.LabelList,
                (byte)credRetryData.Count
            };
            dataWithoutSw.AddRange(credRetryData);

            var apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

            var actualMessage = "";
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
            var apdu = new ResponseApdu(new byte[0], SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

            var credentialRetryPairs = response.GetData();

            Assert.Empty(credentialRetryPairs);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(64)]
        public void GetData_OneElement_MatchesExpectedCredRetryObject(int labelSize)
        {
            var expectedKeyType = CryptographicKeyType.Aes128;
            var expectedLabel = new string(c: 'a', labelSize);
            var expectedTouch = false;
            byte expectedRetryCount = 0;

            // Algo - None
            // Touch - false
            // Label - string of given size
            // Retries - 0
            var credRetryData = new List<byte>
            {
                (byte)expectedKeyType,
                expectedTouch ? (byte)1 : (byte)0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(expectedLabel));
            credRetryData.Add(expectedRetryCount);

            var dataWithoutSw = new List<byte>
            {
                DataTagConstants.LabelList,
                (byte)credRetryData.Count
            };
            dataWithoutSw.AddRange(credRetryData);

            var apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

            var pairs = response.GetData();

            // Only one element
            _ = Assert.Single(pairs);

            foreach (var pair in pairs)
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
            var expectedKeyType = CryptographicKeyType.Aes128;
            var expectedLabel = new string(c: 'a', count: 3);
            var expectedTouch = false;
            byte expectedRetryCount = 0;

            // Algo - None
            // Touch - false
            // Label - string of given size
            // Retries - 0
            var credRetryData = new List<byte>
            {
                (byte)expectedKeyType,
                expectedTouch ? (byte)1 : (byte)0
            };
            credRetryData.AddRange(Encoding.UTF8.GetBytes(expectedLabel));
            credRetryData.Add(expectedRetryCount);

            var dataWithoutSw = new List<byte>
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

            var apdu = new ResponseApdu(dataWithoutSw.ToArray(), SWConstants.Success);

            var response = new ListCredentialsResponse(apdu);

            var pairs = response.GetData();

            // Two elements
            Assert.Equal(expected: 2, pairs.Count);

            foreach (var pair in pairs)
            {
                Assert.Equal(expectedKeyType, pair.Credential.KeyType);
                Assert.Equal(expectedTouch, pair.Credential.TouchRequired);
                Assert.Equal(expectedLabel, pair.Credential.Label);
                Assert.Equal(expectedRetryCount, pair.Retries);
            }
        }
    }
}

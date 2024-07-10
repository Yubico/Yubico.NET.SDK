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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    public class AttestCertResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new CreateAttestationStatementResponse(responseApdu: null));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            var responseData = GetResponseData();
            var responseApdu = new ResponseApdu(responseData);

            var response = new CreateAttestationStatementResponse(responseApdu);

            Assert.Equal(SWConstants.Success, response.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            var responseData = GetResponseData();
            var responseApdu = new ResponseApdu(responseData);

            var response = new CreateAttestationStatementResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void Constructor_ErrorResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = SWConstants.ReferenceDataUnusable >> 8;
            var sw2 = unchecked((byte)SWConstants.ReferenceDataUnusable);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new CreateAttestationStatementResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, response.Status);
        }

        [Fact]
        public void Constructor_ErrorResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = SWConstants.ReferenceDataUnusable >> 8;
            var sw2 = unchecked((byte)SWConstants.ReferenceDataUnusable);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new CreateAttestationStatementResponse(responseApdu);

            Assert.Equal(SWConstants.ReferenceDataUnusable, response.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_GetDataCorrect()
        {
            var responseData = GetResponseData();
            var responseApdu = new ResponseApdu(responseData);

            var response = new CreateAttestationStatementResponse(responseApdu);

            var theCert = response.GetData();

            var certData = theCert.Export(X509ContentType.Cert);

            var expectedData = responseData.SkipLast(count: 2);
            var compareResult = expectedData.SequenceEqual(certData);

            Assert.True(compareResult);
        }

        [Fact]
        public void GetData_FailResponseApdu_Exception()
        {
            byte sw1 = SWConstants.InvalidCommandDataParameter >> 8;
            var sw2 = unchecked((byte)SWConstants.InvalidCommandDataParameter);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });

            var response = new CreateAttestationStatementResponse(responseApdu);

            _ = Assert.Throws<InvalidOperationException>(() => response.GetData());
        }

        private static byte[] GetResponseData()
        {
            return new byte[]
            {
                0x30, 0x82, 0x03, 0x1E, 0x30, 0x82, 0x02, 0x06, 0xA0, 0x03, 0x02, 0x01, 0x02, 0x02, 0x10, 0x01,
                0xA9, 0x1B, 0x29, 0xDC, 0x21, 0xE7, 0x67, 0xA5, 0xAE, 0xE4, 0xAA, 0x5C, 0x8C, 0x51, 0xE3, 0x30,
                0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x30, 0x21,
                0x31, 0x1F, 0x30, 0x1D, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x16, 0x59, 0x75, 0x62, 0x69, 0x63,
                0x6F, 0x20, 0x50, 0x49, 0x56, 0x20, 0x41, 0x74, 0x74, 0x65, 0x73, 0x74, 0x61, 0x74, 0x69, 0x6F,
                0x6E, 0x30, 0x1E, 0x17, 0x0D, 0x31, 0x39, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33, 0x32, 0x32,
                0x32, 0x5A, 0x17, 0x0D, 0x32, 0x30, 0x30, 0x32, 0x31, 0x38, 0x31, 0x32, 0x33, 0x32, 0x32, 0x32,
                0x5A, 0x30, 0x25, 0x31, 0x23, 0x30, 0x21, 0x06, 0x03, 0x55, 0x04, 0x03, 0x0C, 0x1A, 0x59, 0x75,
                0x62, 0x69, 0x4B, 0x65, 0x79, 0x20, 0x50, 0x49, 0x56, 0x20, 0x41, 0x74, 0x74, 0x65, 0x73, 0x74,
                0x61, 0x74, 0x69, 0x6F, 0x6E, 0x20, 0x39, 0x64, 0x30, 0x82, 0x01, 0x22, 0x30, 0x0D, 0x06, 0x09,
                0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00, 0x03, 0x82, 0x01, 0x0F, 0x00,
                0x30, 0x82, 0x01, 0x0A, 0x02, 0x82, 0x01, 0x01, 0x00, 0xA5, 0x57, 0x2D, 0xFF, 0x51, 0x21, 0x1D,
                0x9D, 0xBC, 0x39, 0x58, 0x31, 0x1B, 0xCF, 0xDC, 0x9D, 0xD3, 0x84, 0x35, 0x39, 0x30, 0xC8, 0x50,
                0x0C, 0x5A, 0x21, 0xB8, 0x64, 0xE0, 0x92, 0x7F, 0xA3, 0xDB, 0xB3, 0x15, 0xEC, 0x8E, 0x54, 0xA4,
                0xA6, 0xE7, 0x79, 0x6B, 0x63, 0xAB, 0x70, 0xBB, 0xA7, 0x73, 0xCF, 0x50, 0xCE, 0x86, 0xCD, 0x49,
                0x36, 0x07, 0x75, 0x11, 0x2C, 0x39, 0x24, 0x6B, 0xF1, 0x8B, 0x4A, 0x60, 0x7A, 0x96, 0xB6, 0x6B,
                0x8E, 0xA3, 0x5A, 0x5B, 0x0B, 0xB5, 0xF3, 0x30, 0xF0, 0xFE, 0xBA, 0xB3, 0xBA, 0xD2, 0x31, 0x18,
                0x33, 0x7C, 0xB0, 0x46, 0xA7, 0x71, 0x37, 0x06, 0x7F, 0x15, 0x98, 0x6B, 0x3C, 0x2D, 0x13, 0x39,
                0xB9, 0x62, 0xFD, 0x03, 0xED, 0x67, 0x5E, 0x80, 0x6F, 0x4F, 0xAB, 0x18, 0xBE, 0x1F, 0xD9, 0x09,
                0x12, 0x7B, 0x6A, 0x59, 0x14, 0x94, 0x13, 0x9A, 0xDB, 0x41, 0x87, 0x82, 0x3C, 0x42, 0x3F, 0x93,
                0xF4, 0x91, 0x55, 0x74, 0x15, 0x7F, 0xF5, 0x30, 0xED, 0xB8, 0x2E, 0xEE, 0x8F, 0x00, 0x5A, 0xCD,
                0xC1, 0x04, 0x99, 0xBC, 0xB0, 0x52, 0x59, 0xFD, 0xB3, 0xBF, 0xE7, 0x36, 0x4E, 0xC6, 0x8D, 0xE5,
                0xEC, 0x17, 0xD3, 0x03, 0x25, 0xBA, 0xD1, 0x22, 0x01, 0x02, 0x1F, 0x8E, 0xEE, 0x70, 0xF2, 0x22,
                0x1D, 0x9A, 0x2D, 0xC8, 0x9D, 0x03, 0x49, 0x9A, 0x79, 0x97, 0x56, 0x74, 0x5A, 0x00, 0xFF, 0xED,
                0x46, 0x69, 0x4C, 0xF2, 0xF6, 0x3B, 0xB3, 0x25, 0x53, 0x70, 0xE9, 0x04, 0x1D, 0xA9, 0x9D, 0xFC,
                0x5C, 0x1A, 0xB1, 0x5E, 0xB2, 0x9C, 0x58, 0xA0, 0xD6, 0xE7, 0xB8, 0xD3, 0x5E, 0xF5, 0x6C, 0x85,
                0x3E, 0x4F, 0xB3, 0xAB, 0x20, 0xB1, 0xD3, 0xFF, 0xC2, 0xC1, 0x66, 0x37, 0x86, 0x89, 0x34, 0x39,
                0x10, 0x5F, 0xB4, 0x2B, 0x7C, 0x66, 0x65, 0x32, 0xB3, 0x02, 0x03, 0x01, 0x00, 0x01, 0xA3, 0x4E,
                0x30, 0x4C, 0x30, 0x11, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0xC4, 0x0A, 0x03, 0x03,
                0x04, 0x03, 0x05, 0x03, 0x01, 0x30, 0x14, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0xC4,
                0x0A, 0x03, 0x07, 0x04, 0x06, 0x02, 0x04, 0x00, 0xA7, 0xB0, 0x70, 0x30, 0x10, 0x06, 0x0A, 0x2B,
                0x06, 0x01, 0x04, 0x01, 0x82, 0xC4, 0x0A, 0x03, 0x08, 0x04, 0x02, 0x01, 0x01, 0x30, 0x0F, 0x06,
                0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0xC4, 0x0A, 0x03, 0x09, 0x04, 0x01, 0x01, 0x30, 0x0D,
                0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00, 0x03, 0x82, 0x01,
                0x01, 0x00, 0x52, 0x64, 0x58, 0x57, 0x8F, 0x8C, 0x60, 0xB3, 0x7B, 0x0F, 0xC1, 0x2C, 0xD0, 0xC1,
                0x0C, 0x4F, 0x57, 0x18, 0xD0, 0x99, 0x85, 0xC1, 0x36, 0x7F, 0xE8, 0xD8, 0xAC, 0x3B, 0x9E, 0x37,
                0x51, 0x7D, 0x79, 0xB0, 0x7A, 0xF2, 0xCC, 0xC7, 0xE5, 0xC4, 0xD3, 0x97, 0xE1, 0xBB, 0x0F, 0x81,
                0x62, 0xB6, 0x9F, 0x9A, 0x58, 0x64, 0x80, 0x20, 0x2B, 0xE7, 0xB7, 0x06, 0x3B, 0x32, 0xBE, 0xB8,
                0x65, 0xDF, 0xF9, 0x69, 0x5C, 0xA0, 0x79, 0x8E, 0x9F, 0xA7, 0xC6, 0x38, 0xBA, 0xDF, 0x82, 0x99,
                0x5C, 0xE8, 0xBC, 0xEA, 0x10, 0x93, 0xA3, 0x06, 0xC7, 0xD9, 0xE1, 0xD1, 0x59, 0xFF, 0xBE, 0xB9,
                0x09, 0x86, 0x3A, 0xC1, 0x53, 0xA1, 0x9C, 0xBD, 0x77, 0x08, 0x50, 0x16, 0x6A, 0x0B, 0x7F, 0x35,
                0x11, 0x74, 0x43, 0xD6, 0xE1, 0x31, 0x6F, 0xEC, 0x47, 0x71, 0xC1, 0x02, 0x6F, 0x68, 0xA6, 0xC4,
                0xDE, 0xB0, 0x40, 0xB0, 0x35, 0xE2, 0xCC, 0xEC, 0x0E, 0xF3, 0x1B, 0x2F, 0xDC, 0x2D, 0xB1, 0xFF,
                0x35, 0x6F, 0xA0, 0x31, 0x07, 0xC5, 0x76, 0x23, 0xC7, 0x01, 0x73, 0x95, 0x6C, 0x88, 0xF2, 0xDA,
                0xED, 0x86, 0xBC, 0x82, 0x27, 0xA4, 0x16, 0xFE, 0xB8, 0x8D, 0x61, 0xE9, 0x7E, 0x64, 0xD6, 0x9B,
                0x99, 0x21, 0x9F, 0xD8, 0xEF, 0x77, 0xF3, 0x06, 0x39, 0x0D, 0x51, 0x5F, 0x44, 0x89, 0xA1, 0x3C,
                0xCB, 0xC2, 0x78, 0xF6, 0xB0, 0xEE, 0x62, 0x6E, 0xC8, 0xB0, 0xCF, 0x5B, 0xB1, 0x42, 0x8B, 0x34,
                0xE9, 0xCB, 0xC4, 0xB2, 0x68, 0xB8, 0x4D, 0x6C, 0x5B, 0xBB, 0x02, 0x08, 0x29, 0x2A, 0x92, 0xC0,
                0x33, 0xD3, 0xBC, 0x4E, 0x6A, 0x84, 0x74, 0xB0, 0x02, 0x12, 0xEE, 0x51, 0x69, 0x6D, 0xB8, 0xB3,
                0x5C, 0x05, 0xA0, 0x13, 0x96, 0x72, 0x43, 0x74, 0xA0, 0xB4, 0x9E, 0x49, 0x9F, 0x73, 0x13, 0x1E,
                0x1F, 0xD1,
                0x90, 0x00
            };
        }
    }
}

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
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    public class PutDataCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            byte[] data = new byte[] {
                0x53, 0x06, 0xbc, 0x02, 0x31, 0x32, 0xFE, 0x00
            };
            var command = new PutDataCommand(PivDataTag.IrisImages, data);

            Assert.True(command is IYubiKeyCommand<PutDataResponse>);
        }

        [Fact]
        public void ProtectedConstructor_ValidObject()
        {
            byte[] data = new byte[] { 0x53, 0x03, 0x31, 0x32, 0x33 };
            var command = new PutDataCommand(0x005fff0A, data);

            Assert.True(command is IYubiKeyCommand<PutDataResponse>);
        }

        [Fact]
        public void NoArgConstructor_DerivedFromPivCommand_IsTrue()
        {
            byte[] data = PivCommandResponseTestData.PutDataEncoding(PivDataTag.IrisImages, true);
            var command = new PutDataCommand()
            {
                Tag = PivDataTag.IrisImages,
                EncodedData = data,
            };

            Assert.True(command is IYubiKeyCommand<PutDataResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            byte[] data = new byte[] { 0x53, 0x03, 0xA1, 0xA2, 0xA3 };
            var command = new PutDataCommand(0x5FC109, data);

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Theory]
        [InlineData(PivDataTag.Discovery)]
        [InlineData(PivDataTag.Printed)]
        public void DisallowedTag_ThrowsException(PivDataTag tag)
        {
            byte[] encoding = new byte[] { 0x53, 0x03, 0x39, 0x38, 0x37 };

            _ = Assert.Throws<ArgumentException>(() => new PutDataCommand(tag, encoding));
        }

        [Fact]
        public void NoArgConstructer_NoSet_CorrectException()
        {
            var command = new PutDataCommand();

            _ = Assert.Throws<InvalidOperationException>(() => command.CreateCommandApdu());
        }

        [Fact]
        public void NoArgConstructer_NoData_CorrectException()
        {
            var command = new PutDataCommand()
            {
                Tag = PivDataTag.Retired12,
            };

            _ = Assert.Throws<InvalidOperationException>(() => command.CreateCommandApdu());
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            byte[] encoding = new byte[] { 0x53, 0x03, 0x39, 0x38, 0x37 };
            var command = new PutDataCommand(0x5F0000, encoding);

            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            PutDataResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is PutDataResponse);
        }

        [Theory]
        [InlineData(PivDataTag.Chuid)]
        [InlineData(PivDataTag.Capability)]
        [InlineData(PivDataTag.IrisImages)]
        [InlineData(PivDataTag.Fingerprints)]
        public void Constructor_Property_Tag(PivDataTag tag)
        {
            byte[] encoding = PivCommandResponseTestData.PutDataEncoding(tag, true);
            var command = new PutDataCommand()
            {
                Tag = tag,
                EncodedData = encoding
            };

            PivDataTag getTag = command.Tag;

            Assert.Equal(tag, getTag);
        }

        [Theory]
        [InlineData(PivDataTag.Signature)]
        [InlineData(PivDataTag.Retired1)]
        [InlineData(PivDataTag.KeyHistory)]
        [InlineData(PivDataTag.PairingCodeReferenceData)]
        public void Constructor_Property_EncodedData(PivDataTag tag)
        {
            byte[] encoding = PivCommandResponseTestData.PutDataEncoding(tag, true);
            var expectedResult = new Span<byte>(encoding);
            var command = new PutDataCommand(tag, encoding);

            ReadOnlyMemory<byte> encodedData = command.EncodedData;

            bool compareResult = expectedResult.SequenceEqual(encodedData.Span);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x0000007F)]
        [InlineData(0x00001111)]
        [InlineData(0x015fff01)]
        public void ProtectedConstructor_InvalidTag_Exception(int tag)
        {
            byte[] encoding = new byte[] { 0x61, 0x62, 0x63 };
            _ = Assert.Throws<InvalidOperationException>(() => new PutDataCommand(tag, encoding));
        }

        [Theory]
        [InlineData(PivDataTag.Chuid)]
        [InlineData(PivDataTag.Capability)]
        [InlineData(PivDataTag.Authentication)]
        [InlineData(PivDataTag.Signature)]
        [InlineData(PivDataTag.KeyManagement)]
        [InlineData(PivDataTag.CardAuthentication)]
        [InlineData(PivDataTag.Retired1)]
        [InlineData(PivDataTag.Retired2)]
        [InlineData(PivDataTag.Retired3)]
        [InlineData(PivDataTag.Retired4)]
        [InlineData(PivDataTag.Retired5)]
        [InlineData(PivDataTag.Retired6)]
        [InlineData(PivDataTag.Retired7)]
        [InlineData(PivDataTag.Retired8)]
        [InlineData(PivDataTag.Retired9)]
        [InlineData(PivDataTag.Retired10)]
        [InlineData(PivDataTag.Retired11)]
        [InlineData(PivDataTag.Retired12)]
        [InlineData(PivDataTag.Retired13)]
        [InlineData(PivDataTag.Retired14)]
        [InlineData(PivDataTag.Retired15)]
        [InlineData(PivDataTag.Retired16)]
        [InlineData(PivDataTag.Retired17)]
        [InlineData(PivDataTag.Retired18)]
        [InlineData(PivDataTag.Retired19)]
        [InlineData(PivDataTag.Retired20)]
        [InlineData(PivDataTag.SecurityObject)]
        [InlineData(PivDataTag.IrisImages)]
        [InlineData(PivDataTag.FacialImage)]
        [InlineData(PivDataTag.Fingerprints)]
        [InlineData(PivDataTag.SecureMessageSigner)]
        [InlineData(PivDataTag.PairingCodeReferenceData)]
        public void DataTagConstructor_CmdApdu_Correct(PivDataTag tag)
        {
            var expected = new List<byte>();
            if (tag != PivDataTag.BiometricGroupTemplate)
            {
                expected = PivCommandResponseTestData.GetDataCommandExpectedApduData(tag);
            }
            byte[] encoding = PivCommandResponseTestData.PutDataEncoding(tag, true);

            expected.AddRange(encoding);

            var command = new PutDataCommand(tag, encoding);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            bool compareResult = expected.SequenceEqual(data.ToArray());

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(0x005FFF00)]
        [InlineData(0x005FFF01)]
        [InlineData(0x005FC109)]
        public void ProtectedConstructor_CmdApdu_Correct(int tag)
        {
            byte[] encoding = new byte[] { 0x53, 0x03, 0x71, 0x72, 0x73 };

            List<byte> expected = PivCommandResponseTestData.GetDataCommandExpectedApduDataInt(tag);
            expected.AddRange(encoding);

            var command = new PutDataCommand(tag, encoding);
            CommandApdu cmdApdu = command.CreateCommandApdu();

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            bool compareResult = expected.SequenceEqual(data.ToArray());

            Assert.True(compareResult);
        }
    }
}

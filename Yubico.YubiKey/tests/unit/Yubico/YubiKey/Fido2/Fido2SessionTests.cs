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
using NSubstitute;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.Fido2
{
    public class Fido2SessionTests
    {
        [Fact]
        void Constructor_NullYubiKeyDevice_ThrowsArgumentNullException()
        {
            static void Action()
            {
#pragma warning disable CS8625
                _ = new Fido2Session(null);
#pragma warning restore CS8625
            }

            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        void Constructor_ValidYubiKeyDevice_Succeeds()
        {
            var mockYubiKey = Substitute.For<IYubiKeyDevice>();
            var mockConnection = Substitute.For<IYubiKeyConnection>();
            var mockResponse = new GetInfoResponse(new ResponseApdu(Fido2InfoTests.GetSampleEncoded(), SWConstants.Success));

            _ = mockConnection.SendCommand(Arg.Any<IYubiKeyCommand<IYubiKeyResponse>>())
                .Returns(mockResponse);

            _ = mockYubiKey.Connect(YubiKeyApplication.Fido2)
                .Returns(mockConnection);

            var session = new Fido2Session(mockYubiKey);

            Assert.NotNull(session);
        }

        [Fact]
        void Constructor_GivenValidYubiKeyDevice_ConnectsToFido2Application()
        {
            var mockYubiKey = Substitute.For<IYubiKeyDevice>();
            var mockConnection = Substitute.For<IYubiKeyConnection>();
            var mockResponse = new GetInfoResponse(new ResponseApdu(Fido2InfoTests.GetSampleEncoded(), SWConstants.Success));

            _ = mockConnection.SendCommand(Arg.Any<IYubiKeyCommand<IYubiKeyResponse>>())
                .Returns(mockResponse);

            _ = mockYubiKey.Connect(YubiKeyApplication.Fido2)
                .Returns(mockConnection);

            var session = new Fido2Session(mockYubiKey);

            mockYubiKey.Received().Connect(YubiKeyApplication.Fido2);
        }

        [Fact]
        void GetAuthenticatorInfo_SendsGetInfoCommand()
        {
            var mockYubiKey = Substitute.For<IYubiKeyDevice>();
            var mockConnection = Substitute.For<IYubiKeyConnection>();
            var mockResponse = new GetInfoResponse(new ResponseApdu(Fido2InfoTests.GetSampleEncoded(), SWConstants.Success));

            _ = mockConnection.SendCommand(Arg.Any<IYubiKeyCommand<IYubiKeyResponse>>())
                .Returns(mockResponse);

            _ = mockYubiKey.Connect(YubiKeyApplication.Fido2)
                .Returns(mockConnection);

            var session = new Fido2Session(mockYubiKey);

            mockConnection.Received().SendCommand(Arg.Any<GetInfoCommand>());
        }
    }
}

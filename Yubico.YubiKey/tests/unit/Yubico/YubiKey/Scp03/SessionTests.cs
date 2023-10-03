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
using Yubico.YubiKey.Scp03.Commands;
using Yubico.Core.Iso7816;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Scp03
{
    public class SessionTests
    {
        private static byte[] GetChallenge() => Hex.HexToBytes("360CB43F4301B894");
        private static byte[] GetCorrectInitializeUpdate() => Hex.HexToBytes("8050000008360CB43F4301B894");
        private static byte[] GetInitializeUpdateResponse() => Hex.HexToBytes("010B001F002500000000FF0360CAAFA4DAC615236ADD5607216F3E115C9000");
        private static byte[] GetCorrectExternalAuthenticate() => Hex.HexToBytes("848233001045330AB30BB1A079A8E7F77376DB9F2C");

        private static StaticKeys GetStaticKeys()
        {
            return new StaticKeys();
        }

        private static Session GetSession()
        {
            return new Session();
        }

        [Fact]
        public void Constructor_GivenStaticKeys_Succeeds()
        {
            _ = GetSession();
        }

        [Fact]
        public void BuildInitializeUpdate_GivenNullHostChallenge_ThrowsArgumentNullException()
        {
            Session sess = GetSession();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => sess.BuildInitializeUpdate(0, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void BuildInitializeUpdate_GivenHostChallengeWrongLength_ThrowsArgumentException()
        {
            byte[] hostChallengeWrongLength = new byte[9];
            Session sess = GetSession();
            _ = Assert.Throws<ArgumentException>(() => sess.BuildInitializeUpdate(0, hostChallengeWrongLength));
        }

        [Fact]
        public void BuildInitializeUpdate_GivenHostChallenge_BuildsCorrectInitializeUpdate()
        {
            // Arrange
            Session sess = GetSession();

            // Act
            InitializeUpdateCommand initializeUpdateCommand = sess.BuildInitializeUpdate(0, GetChallenge());
            byte[] initializeUpdateCommandBytes = initializeUpdateCommand.CreateCommandApdu().AsByteArray();

            // Assert
            Assert.Equal(initializeUpdateCommandBytes, GetCorrectInitializeUpdate());
        }

        [Fact]
        public void LoadInitializeUpdate_GivenNullInitializeUpdateResponse_ThrowsArgumentNullException()
        {
            Session sess = GetSession();
            InitializeUpdateCommand initializeUpdateCommand = sess.BuildInitializeUpdate(0, GetChallenge());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => sess.LoadInitializeUpdateResponse(null, GetStaticKeys()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void LoadInitializeUpdate_GivenNullStaticKeys_ThrowsArgumentNullException()
        {
            Session sess = GetSession();
            InitializeUpdateCommand initializeUpdateCommand = sess.BuildInitializeUpdate(0, GetChallenge());
            var correctResponse = new InitializeUpdateResponse(new ResponseApdu(GetInitializeUpdateResponse()));
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => sess.LoadInitializeUpdateResponse(correctResponse, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void LoadInitializeUpdate_CalledBeforeBuildInitializeUpdate_ThrowsInvalidOperationException()
        {
            Session sess = GetSession();
            var correctResponse = new InitializeUpdateResponse(new ResponseApdu(GetInitializeUpdateResponse()));
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<InvalidOperationException>(() => sess.LoadInitializeUpdateResponse(correctResponse, GetStaticKeys()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Session_GivenInitializeUpdateResponse_BuildsCorrectExternalAuthenticate()
        {
            // Arrange
            Session sess = GetSession();
            InitializeUpdateCommand initializeUpdateCommand = sess.BuildInitializeUpdate(0, GetChallenge());
            sess.LoadInitializeUpdateResponse(initializeUpdateCommand.CreateResponseForApdu(new ResponseApdu(GetInitializeUpdateResponse())), GetStaticKeys());

            // Act
            ExternalAuthenticateCommand externalAuthenticateCommand = sess.BuildExternalAuthenticate();
            byte[] externalAuthenticateCommandBytes = externalAuthenticateCommand.CreateCommandApdu().AsByteArray();

            // Assert
            Assert.Equal(externalAuthenticateCommandBytes, GetCorrectExternalAuthenticate());
        }
    }
}

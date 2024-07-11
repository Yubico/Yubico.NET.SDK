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
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Scp03.Commands;

namespace Yubico.YubiKey.Scp03
{
    public class SessionTests
    {
        private static byte[] GetChallenge()
        {
            return Hex.HexToBytes("360CB43F4301B894");
        }

        private static byte[] GetCorrectInitializeUpdate()
        {
            return Hex.HexToBytes("8050000008360CB43F4301B894");
        }

        private static byte[] GetInitializeUpdateResponse()
        {
            return Hex.HexToBytes("010B001F002500000000FF0360CAAFA4DAC615236ADD5607216F3E115C9000");
        }

        private static byte[] GetCorrectExternalAuthenticate()
        {
            return Hex.HexToBytes("848233001045330AB30BB1A079A8E7F77376DB9F2C");
        }

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
            var sess = GetSession();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() =>
                sess.BuildInitializeUpdate(keyVersionNumber: 0, hostChallenge: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void BuildInitializeUpdate_GivenHostChallengeWrongLength_ThrowsArgumentException()
        {
            var hostChallengeWrongLength = new byte[9];
            var sess = GetSession();
            _ = Assert.Throws<ArgumentException>(() =>
                sess.BuildInitializeUpdate(keyVersionNumber: 0, hostChallengeWrongLength));
        }

        [Fact]
        public void BuildInitializeUpdate_GivenHostChallenge_BuildsCorrectInitializeUpdate()
        {
            // Arrange
            var sess = GetSession();

            // Act
            var initializeUpdateCommand = sess.BuildInitializeUpdate(keyVersionNumber: 0, GetChallenge());
            var initializeUpdateCommandBytes = initializeUpdateCommand.CreateCommandApdu().AsByteArray();

            // Assert
            Assert.Equal(initializeUpdateCommandBytes, GetCorrectInitializeUpdate());
        }

        [Fact]
        public void LoadInitializeUpdate_GivenNullInitializeUpdateResponse_ThrowsArgumentNullException()
        {
            var sess = GetSession();
            var initializeUpdateCommand = sess.BuildInitializeUpdate(keyVersionNumber: 0, GetChallenge());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() =>
                sess.LoadInitializeUpdateResponse(initializeUpdateResponse: null, GetStaticKeys()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void LoadInitializeUpdate_GivenNullStaticKeys_ThrowsArgumentNullException()
        {
            var sess = GetSession();
            var initializeUpdateCommand = sess.BuildInitializeUpdate(keyVersionNumber: 0, GetChallenge());
            var correctResponse = new InitializeUpdateResponse(new ResponseApdu(GetInitializeUpdateResponse()));
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() =>
                sess.LoadInitializeUpdateResponse(correctResponse, staticKeys: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void LoadInitializeUpdate_CalledBeforeBuildInitializeUpdate_ThrowsInvalidOperationException()
        {
            var sess = GetSession();
            var correctResponse = new InitializeUpdateResponse(new ResponseApdu(GetInitializeUpdateResponse()));
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<InvalidOperationException>(() =>
                sess.LoadInitializeUpdateResponse(correctResponse, GetStaticKeys()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Session_GivenInitializeUpdateResponse_BuildsCorrectExternalAuthenticate()
        {
            // Arrange
            var sess = GetSession();
            var initializeUpdateCommand = sess.BuildInitializeUpdate(keyVersionNumber: 0, GetChallenge());
            sess.LoadInitializeUpdateResponse(
                initializeUpdateCommand.CreateResponseForApdu(new ResponseApdu(GetInitializeUpdateResponse())),
                GetStaticKeys());

            // Act
            var externalAuthenticateCommand = sess.BuildExternalAuthenticate();
            var externalAuthenticateCommandBytes = externalAuthenticateCommand.CreateCommandApdu().AsByteArray();

            // Assert
            Assert.Equal(externalAuthenticateCommandBytes, GetCorrectExternalAuthenticate());
        }
    }
}

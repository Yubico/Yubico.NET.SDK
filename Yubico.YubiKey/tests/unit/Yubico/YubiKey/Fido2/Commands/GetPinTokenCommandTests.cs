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
using Xunit;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetPinTokenCommandTests
    {
        [Fact]
        public void Constructor_Succeeds()
        {
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolOne();
            protocol.Encapsulate(authenticatorPubKey);

            var command = new GetPinTokenCommand(protocol, currentPin);

            Assert.NotNull(command);
        }

        [Fact]
        public void UP_Constructor_Succeeds()
        {
            string rpId = "fake-rp.com";
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolTwo();
            protocol.Encapsulate(authenticatorPubKey);

            var command = new GetPinUvAuthTokenUsingPinCommand(
                protocol, currentPin, PinUvAuthTokenPermissions.MakeCredential, rpId);

            Assert.NotNull(command);
        }

        [Fact]
        public void UV_Constructor_Succeeds()
        {
            string rpId = "fake-rp.com";
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolTwo();
            protocol.Encapsulate(authenticatorPubKey);

            var command = new GetPinUvAuthTokenUsingUvCommand(
                protocol, PinUvAuthTokenPermissions.MakeCredential, rpId);

            Assert.NotNull(command);
        }

        [Fact]
        public void NullProtocol_ThrowsException()
        {
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            CoseKey authenticatorPubKey = GetSamplePubKey();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new GetPinTokenCommand(null, currentPin));
#pragma warning restore CS8625 // Justification: this tests null input
        }

        [Fact]
        public void UP_NullProtocol_ThrowsException()
        {
            string rpId = "fake-rp.com";
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolOne();
            protocol.Encapsulate(authenticatorPubKey);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new GetPinUvAuthTokenUsingPinCommand(
                null, currentPin, PinUvAuthTokenPermissions.MakeCredential, rpId));
#pragma warning restore CS8625 // Justification: this tests null input
        }

        [Fact]
        public void UV_NullProtocol_ThrowsException()
        {
            string rpId = "fake-rp.com";
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolOne();
            protocol.Encapsulate(authenticatorPubKey);

            _ = Assert.Throws<ArgumentNullException>(() => new GetPinUvAuthTokenUsingUvCommand(
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                null, PinUvAuthTokenPermissions.MakeCredential, rpId));
#pragma warning restore CS8625 // null is the purpose of the test.
        }

        [Fact]
        public void NoEncapsulate_ThrowsException()
        {
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolOne();

            _ = Assert.Throws<InvalidOperationException>(() => new GetPinTokenCommand(protocol, currentPin));
        }

        [Fact]
        public void UP_NoEncapsulate_ThrowsException()
        {
            string rpId = "fake-rp.com";
            byte[] currentPin = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolOne();

            _ = Assert.Throws<InvalidOperationException>(() => new GetPinUvAuthTokenUsingPinCommand(
                protocol, currentPin, PinUvAuthTokenPermissions.MakeCredential, rpId));
        }

        [Fact]
        public void UV_NoEncapsulate_ThrowsException()
        {
            string rpId = "fake-rp.com";
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolOne();

            _ = Assert.Throws<InvalidOperationException>(() => new GetPinUvAuthTokenUsingUvCommand(
                protocol, PinUvAuthTokenPermissions.MakeCredential, rpId));
        }

        [Fact]
        public void InvalidKey_ThrowsException()
        {
            byte[] currentPin = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolTwo();
            protocol.Encapsulate(authenticatorPubKey);

            _ = Assert.Throws<ArgumentException>(() => new GetPinTokenCommand(protocol, currentPin));
        }

        [Fact]
        public void UP_InvalidKey_ThrowsException()
        {
            byte[] currentPin = new byte[]
            {
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
                0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            string rpId = "fake-rp.com";
            CoseKey authenticatorPubKey = GetSamplePubKey();
            var protocol = new PinUvAuthProtocolTwo();
            protocol.Encapsulate(authenticatorPubKey);

            _ = Assert.Throws<ArgumentException>(() => new GetPinUvAuthTokenUsingPinCommand(
                protocol, currentPin, PinUvAuthTokenPermissions.MakeCredential, rpId));
        }

        private CoseKey GetSamplePubKey()
        {
            byte[] encodedKey = new byte[]
            {
                0xa5, 0x01, 0x02, 0x03, 0x38, 0x18, 0x20, 0x01, 0x21, 0x58, 0x20, 0xde, 0x01, 0x65, 0x7e, 0x79,
                0xf9, 0x84, 0x47, 0x4b, 0xa2, 0xee, 0xbd, 0x3a, 0x8c, 0x68, 0xf9, 0xe2, 0x5c, 0x7a, 0x67, 0x6d,
                0x38, 0x9b, 0xb3, 0x51, 0x95, 0x29, 0xec, 0x4e, 0x0b, 0x88, 0x1d, 0x22, 0x58, 0x20, 0x81, 0x1e,
                0xaf, 0x83, 0x7a, 0xc7, 0xee, 0x98, 0x25, 0xdf, 0x5e, 0xce, 0xf0, 0xf8, 0xe1, 0x52, 0x66, 0x29,
                0x40, 0x79, 0xbb, 0x2b, 0x2a, 0x28, 0x96, 0x4f, 0x6b, 0x15, 0x41, 0x95, 0xe7, 0x0f
            };

            return CoseKey.Create(encodedKey, out int _);
        }
    }
}

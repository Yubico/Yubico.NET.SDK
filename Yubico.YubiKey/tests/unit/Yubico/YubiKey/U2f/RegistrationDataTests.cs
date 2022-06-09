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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f
{
    public class RegistrationDataTests
    {
        [Fact]
        public void Constructor_GivenIncorrectUserPublicKey_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new RegistrationData(GetEncodedRegistration(false, true)));
        }

        [Fact]
        public void Constructor_IncorrectKeyHandle_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new RegistrationData(GetEncodedRegistration(true, false)));
        }

        [Fact]
        public void Constructor_GivenGoodData_SetsUserPublicKeyCorrectly()
        {
            RegistrationData registrationData = GetGoodRegistrationData();

            var pubKeyPoint = new ECPoint
            {
                X = registrationData.UserPublicKey.Slice(1, 32).ToArray(),
                Y = registrationData.UserPublicKey.Slice(33, 32).ToArray(),
            };
            Assert.Equal(GetPubKeyX(), Hex.BytesToHex(pubKeyPoint.X));
            Assert.Equal(GetPubKeyY(), Hex.BytesToHex(pubKeyPoint.Y));
        }

        [Fact]
        public void Constructor_GivenGoodData_SetsKeyHandle()
        {
            RegistrationData registrationData = GetGoodRegistrationData();

            string expected = GetKeyHandle(true, out string _);

            Assert.Equal(expected, Hex.BytesToHex(registrationData.KeyHandle.ToArray()));
        }

        [Fact]
        public void Constructor_GivenGoodData_SetsCertificate()
        {
            RegistrationData registrationData = GetGoodRegistrationData();

            Assert.Equal(GetAttestationCert(), Hex.BytesToHex(registrationData.AttestationCert.RawData));
        }

        [Fact]
        public void Constructor_GivenGoodData_SetsSignature()
        {
            RegistrationData registrationData = GetGoodRegistrationData();

            Assert.Equal(GetRegSignature(), Hex.BytesToHex(registrationData.Signature.ToArray()));
        }

        [Fact]
        public void IsSignatureValid_GivenBadClientDataHash_ThrowsArgumentException()
        {
            RegistrationData registrationData = GetGoodRegistrationData();

            _ = Assert.Throws<ArgumentException>(() => registrationData.VerifySignature(new byte[10], new byte[32]));
        }

        [Fact]
        public void IsSignatureValid_GivenBadAppId_ThrowsArgumentException()
        {
            RegistrationData registrationData = GetGoodRegistrationData();

            _ = Assert.Throws<ArgumentException>(() => registrationData.VerifySignature(new byte[32], new byte[10]));
        }

        [Fact]
        public void VerifySignature_GivenCorrectData_ReturnsTrue()
        {
            byte[] appId = Hex.HexToBytes(GetAppId(true));
            byte[] clientDataHash = Hex.HexToBytes(GetClientDataHash(true));

            RegistrationData registrationData = GetGoodRegistrationData();

            Assert.True(registrationData.VerifySignature(appId, clientDataHash));
        }

        [Fact]
        public void IsSignatureValid_GivenIncorrectData_ReturnsFalse()
        {
            byte[] appId = Hex.HexToBytes(GetAppId(false));
            byte[] clientDataHash = Hex.HexToBytes(GetClientDataHash(true));

            RegistrationData registrationData = GetGoodRegistrationData();

            Assert.False(registrationData.VerifySignature(appId, clientDataHash));
        }

        public static RegistrationData GetGoodRegistrationData()
        {
            return new RegistrationData(GetEncodedRegistration(true, true));
        }

        // Return a byte array containing an encoded RegistrationData.
        // If validPubKey is true, make sure the portion of the encoding that is
        // the pub key is valid. If false, make sure it is incorrect.
        // Same for the other valids.
        public static byte[] GetEncodedRegistration(bool validPubKey, bool validKeyHandle)
        {
            string keyHandle = GetKeyHandle(validKeyHandle, out string handleLength);
            string regData = "05" + GetPubKey(validPubKey) + handleLength + keyHandle + GetAttestationCert() + GetRegSignature();

            return Hex.HexToBytes(regData);
        }

        // Get a string that is the public key portion of encoded registration
        // data. If isValid is true, return a valid pub key. If false, return a
        // value with incorrect data
        public static string GetPubKey(bool isValid)
        {
            string prefix = "03";
            if (isValid)
            {
                prefix = "04";
            }

            return prefix + GetPubKeyX() + GetPubKeyY();
        }

        public static byte[] GetPubKeyArray(bool isValid)
        {
            return Hex.HexToBytes(GetPubKey(isValid));
        }

        public static string GetPubKeyX()
        {
            return "e85c5043280180756bf5a2c1076946779989b62ce0d5d8917679cd5947326805".ToUpper();
        }

        public static string GetPubKeyY()
        {
            return "a6bc2a6c70dba6a48c0f6945f92ce5412691034b302ecde89daf5d71e55f5347".ToUpper();
        }

        public static string GetAuthSignature(out U2fAuthenticationType controlByte, out int counter)
        {
            controlByte = U2fAuthenticationType.DontEnforceUserPresence;
            counter = 11;

            return "3045022009e9a47d8f6f296fcbcc4abd9fe028f9605f9d4f192aa12433c3e47906118672022100f4a62104629176f03067d51ca445f9c42bcd548332cd79c00a7dba9451bd76d4";
        }

        public static byte[] GetAuthSignatureArray(out U2fAuthenticationType controlByte, out int counter)
        {
            return Hex.HexToBytes(GetAuthSignature(out controlByte, out counter));
        }


        public static byte[] GetGoodAuthDataArray()
        {
            byte[] signature = RegistrationDataTests.GetAuthSignatureArray(
                out U2fAuthenticationType controlByte, out int counter);

            byte[] authData = new byte[signature.Length + 5];
            int userPresence = controlByte == U2fAuthenticationType.EnforceUserPresence ? 1 : 0;
            authData[0] = (byte)userPresence;
            authData[1] = (byte)(counter >> 24);
            authData[2] = (byte)(counter >> 16);
            authData[3] = (byte)(counter >>  8);
            authData[4] = (byte) counter;

            Array.Copy(signature, 0, authData, 5, signature.Length);

            return authData;
        }

        public static string GetKeyHandle(bool isValid, out string handleLength)
        {
            handleLength = "08";
            if (isValid)
            {
                handleLength = "40";
                return "366efad771661bbbabf5c91b113378297ca17c912a63a9eea8e4356db0c26993fdabba6bf46a60bf95df395e544c6cda70c43212a8a69e229abaf4630255395c".ToUpper();
            }

            return "0102030405060708";
        }

        public static byte[] GetKeyHandleArray(bool isValid, out byte handleLength)
        {
            byte[] returnValue = Hex.HexToBytes(GetKeyHandle(isValid, out string hLength));
            int numValue = int.Parse(hLength, System.Globalization.NumberStyles.HexNumber);
            handleLength = (byte)numValue;
            return returnValue;
        }

        public static string GetAttestationCert()
        {
            return "308202d8308201c0a003020102020900ffee6ca15b04fdd2300d06092a864886f70d01010b0500302e312c302a0603550403132359756269636f2055324620526f6f742043412053657269616c203435373230303633313020170d3134303830313030303030305a180f32303530303930343030303030305a306e310b300906035504061302534531123010060355040a0c0959756269636f20414231223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e3127302506035504030c1e59756269636f205532462045452053657269616c203536303738373139393059301306072a8648ce3d020106082a8648ce3d030107034200045dd1b33e6e691982dd87c60548a1c6b4468d723498c72ee1d5ceef83ca9ad53695f22708d2af34397c261a4947e9db806ec03443224cbcad09c97e92a7e21d08a38181307f3013060a2b0601040182c40a0d0104050403050402302206092b0601040182c40a020415312e332e362e312e342e312e34313438322e312e373013060b2b0601040182e51c0201010404030204303021060b2b0601040182e51c010104041204102fc0579f811347eab116bb5a8db9202a300c0603551d130101ff04023000300d06092a864886f70d01010b0500038201010038ec60c14228f5f3662339219d078bd2d4728366db0a5862fd409b69ca0bf38ab84955712d98bd6ed959739499f27d0e69e550b9d3e80e6398f78f37853b270ee0ea3fbc9f7262bf100632438411f2e9075879fd2c3356f5bb91b13f935f8ba946d6fefd560e85181ec81dc08b1e7f14424be391877ac4aed0e6be397dfe35a6a07504152462413c7338aa6ff9e3dfbbc8067d762a68a6b76ff51ee15e2ff8d4f31a9559f04600be972f5470a51c99d4692347876125f3a19ceacd8709d6b82f9a91dca62485da15313928a5afd1601f9e46496a30d94ec2e51722536fe71c582daf720ad68f91f4f5adbe13fb78b1c25df872684aa370173d4ca8cb3d9d5b19".ToUpper();
        }

        public static string GetRegSignature()
        {
            return "3044022056c900e6bef8710cc4515917774c30f3b38be0fe5ade2e49dc06aaec2fa73f400220351502ec7709217d26a20ba465ef92f2fc2853eee5b792aac8801ae31b229a2c".ToUpper();
        }

        public static string GetClientDataHash(bool isValid)
        {
            if (isValid)
            {
                return "A76CF9A4BDA5D0596D56612E71CDD954C38168954541522F7DCD9433666BA0F0";
            }

            return "F9A4BDA5D0596D56612E71CDD954C38168954541522F7DCD9433666BA0F0A76C";
        }

        public static byte[] GetClientDataHashArray(bool isValid)
        {
            return Hex.HexToBytes(GetClientDataHash(isValid));
        }

        public static string GetAppId(bool isValid)
        {
            if (isValid)
            {
                return "08D71B87CC11BF231245CC8C0A1B653FB5A47C2D9D66A8B94154AAB26C2FF670";
            }

            return "1B87CC11BF231245CC8C0A1B653FB5A47C2D9D66A8B94154AAB26C2FF67008D7";
        }

        public static byte[] GetAppIdArray(bool isValid)
        {
            return Hex.HexToBytes(GetAppId(isValid));
        }
    }
}

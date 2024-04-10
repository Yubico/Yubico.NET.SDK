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
            return "3c562031232863f6507c481bc6c3f7ef90840c7f9b77326f1eb4fa95d83ab29a".ToUpper();
        }

        public static string GetPubKeyY()
        {
            return "b5ab75b04ffe011faf4fea9609efcd7730a954ba61ee96c20430fe07c6246148".ToUpper();
        }

        public static string GetAuthSignature(out U2fAuthenticationType controlByte, out int counter)
        {
            controlByte = U2fAuthenticationType.DontEnforceUserPresence;
            counter = 17;

            return "304502206190bee2c2ed0dd524c88f0848ae0e779b1167f0524da2329d1e0c80117a608f022100a9e86d105f3d937469a0188be886aaad633e4c8a221ba01873d712cce97778bb";
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
            authData[3] = (byte)(counter >> 8);
            authData[4] = (byte)counter;

            Array.Copy(signature, 0, authData, 5, signature.Length);

            return authData;
        }

        public static string GetKeyHandle(bool isValid, out string handleLength)
        {
            handleLength = "08";
            if (isValid)
            {
                handleLength = "40";
                return "6c61ddbf52b4d1597926c467bf279a96fc1bce0f6044c1482c88dea1097e06ef6bc491cfc82e74b33d2fbc115f5c60046d9dbc1935d78bce98d22af92465e443".ToUpper();

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
            return "308202ac30820194a00302010202042403b72b300d06092a864886f70d01010b0500302e312c302a0603550403132359756269636f2055324620526f6f742043412053657269616c203435373230303633313020170d3134303830313030303030305a180f32303530303930343030303030305a306e310b300906035504061302534531123010060355040a0c0959756269636f20414231223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e3127302506035504030c1e59756269636f205532462045452053657269616c203630343232333237353059301306072a8648ce3d020106082a8648ce3d030107034200043f3d6bb53f02d5ef9aa271549ee8e4a6662c86215f0c252779d35c83388c4740ba8f2d004e5d6f14902e5c0b69f346c54163782e12a0fc05f7999749f4bf2c1ca35b3059302206092b0601040182c40a020415312e332e362e312e342e312e34313438322e312e353013060b2b0601040182e51c020101040403020520301006092b0601040182c40a0c0403020103300c0603551d130101ff04023000300d06092a864886f70d01010b050003820101007dc61eae9e8299f09aaac4c090132e5688d1a0d1799581d940716ef9e14c093f31c2a21a557e4227e29e66aaa2a3253b5ac017f47b305079338bbb7060e3997fc9355444722e0ba0d9eb94e55605aa9f1c3003c96128f993ee3d1d5c8206e8ca722ee875220c71255617c34ece5ff00f9bbdb9090863c4850350701e816d3bf52f1b477e031974b080bbe37e05c6f2fcbfc1f8d9739726b41dac1c471854aab55fb840fb496ac3fef8920d97acc901179226be49a08164635ca22cd0cbcab019b64c316a49478c6f72a4da938d5f353b9b10c5bac90869bc5e17e021f60f84b498d0d6d5e850edec6f2316871716af190deff6340a89d07ab824925fc91a8831".ToUpper();
        }

        public static string GetRegSignature()
        {
            return "3046022100d778d422916e47baf8935d8a4ac9e5a48cfc45a125ee12538d226be7e5ba73c4022100efd29f8b3b81d5f56d9f8f54586197f300b6211668ea4e53a36120e8cc0c2d11".ToUpper();
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

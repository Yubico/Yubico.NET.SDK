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
using System.Security.Cryptography;
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class MakeLargeBlobTests : NeedPinToken
    {
        public MakeLargeBlobTests()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio, null)
        {
        }

        [Fact]
        public void InitialLargeBlob_Succeeds()
        {
            var getInfoCmd = new GetInfoCommand();
            GetInfoResponse getInfoRsp = Connection.SendCommand(getInfoCmd);
            Assert.Equal(ResponseStatus.Success, getInfoRsp.Status);
            AuthenticatorInfo authInfo = getInfoRsp.GetData();
            Assert.NotNull(authInfo.Options);
            if (!(authInfo.Options is null))
            {
                int maxLargeBlobsLength = authInfo.MaximumSerializedLargeBlobArray ?? 0;
                Assert.NotEqual(0, maxLargeBlobsLength);
                bool isKey = authInfo.Options.TryGetValue("largeBlobs", out bool isAvailable);
                Assert.True(isKey);
                Assert.True(isAvailable);
            }

            var getBlobCmd = new GetLargeBlobCommand(0, 900);
            GetLargeBlobResponse getBlobRsp = Connection.SendCommand(getBlobCmd);
            Assert.Equal(ResponseStatus.Success, getBlobRsp.Status);
            ReadOnlyMemory<byte> blobData = getBlobRsp.GetData();
            Assert.NotEmpty(blobData.ToArray());
        }

        [Fact]
        public void SetLargeBlob_Succeeds()
        {
            byte[] dataToStore = new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58
            };

            using HashAlgorithm digester = CryptographyProviders.Sha256Creator();

            byte[] dataPlusDigest = BuildDataPlusDigest(dataToStore, 0, dataToStore.Length, digester);
            int offset = 0;
            int totalLength = dataToStore.Length;
            byte[] dataToAuth = BuildDataToAuth(dataPlusDigest, 0, dataPlusDigest.Length, offset, digester);

            var protocol = new PinUvAuthProtocolTwo();
            bool isValid = GetPinToken(
                protocol, PinUvAuthTokenPermissions.LargeBlobWrite, out byte[] pinToken);
            Assert.True(isValid);
            byte[] pinUvAuthParam = protocol.AuthenticateUsingPinToken(pinToken, dataToAuth);

            var setBlobCmd = new SetLargeBlobCommand(
                dataPlusDigest, offset, totalLength + 16, pinUvAuthParam, (int)protocol.Protocol);
            SetLargeBlobResponse setBlobRsp = Connection.SendCommand(setBlobCmd);
            Assert.Equal(ResponseStatus.Success, setBlobRsp.Status);

            var getBlobCmd = new GetLargeBlobCommand(0, 900);
            GetLargeBlobResponse getBlobRsp = Connection.SendCommand(getBlobCmd);
            Assert.Equal(ResponseStatus.Success, getBlobRsp.Status);
            ReadOnlyMemory<byte> blobData = getBlobRsp.GetData();
            Assert.NotEmpty(blobData.ToArray());
        }

        private byte[] BuildDataPlusDigest(byte[] inputData, int dataOffset, int dataLength, HashAlgorithm digester)
        {
            digester.Initialize();
            _ = digester.TransformFinalBlock(inputData, dataOffset, dataLength);
            if (digester.Hash is null)
            {
                throw new ArgumentNullException(nameof(digester.Hash));
            }

            byte[] dataPlusDigest = new byte[dataLength + 16];
            Array.Copy(inputData, 0, dataPlusDigest, 0, dataLength);
            Array.Copy(digester.Hash, 0, dataPlusDigest, dataLength, 16);

            return dataPlusDigest;
        }

        private byte[] BuildDataToAuth(
            byte[] inputData, int dataOffset, int dataLength, int offset, HashAlgorithm digester)
        {
            digester.Initialize();
            _ = digester.TransformFinalBlock(inputData, dataOffset, dataLength);
            if (digester.Hash is null)
            {
                throw new ArgumentException(nameof(digester.Hash));
            }

            byte[] dataToAuth = new byte[38 + digester.Hash.Length];
            int index = 0;
            for (; index < 32; index++)
            {
                dataToAuth[index] = 0xff;
            }
            dataToAuth[index] = 0x0C;
            index++;
            dataToAuth[index] = 0x00;
            index++;
            dataToAuth[index] = (byte)offset;
            index++;
            dataToAuth[index] = (byte)(offset >> 8);
            index++;
            dataToAuth[index] = (byte)(offset >> 16);
            index++;
            dataToAuth[index] = (byte)(offset >> 24);
            index++;
            Array.Copy(digester.Hash, 0, dataToAuth, index, digester.Hash.Length);

            return dataToAuth;
        }
    }
}

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
using System.Security.Cryptography;
using Xunit;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Scp03;

namespace Yubico.YubiKey.Pipelines
{
    [Obsolete("This class is obsolete and will be removed in a future release.")]
    public class PipelineFixture : IApduTransform
    {
        public ResponseApdu Invoke(CommandApdu command, Type commandType, Type responseType)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (command.AsByteArray().SequenceEqual(new SelectApplicationCommand(YubiKeyApplication.Piv).CreateCommandApdu().AsByteArray()))
            {
                return new ResponseApdu(Hex.HexToBytes("9000"));
            }
            else if (command.AsByteArray().SequenceEqual(Hex.HexToBytes("8050FF0008360CB43F4301B894")))
            {
                return new ResponseApdu(Hex.HexToBytes("010B001F002500000000FF0360CAAFA4DAC615236ADD5607216F3E115C9000"));
            }
            else if (command.AsByteArray().SequenceEqual(Hex.HexToBytes("848233001045330AB30BB1A079A8E7F77376DB9F2C")))
            {
                return new ResponseApdu(Hex.HexToBytes("9000"));
            }
            else if (command.AsByteArray().SequenceEqual(Hex.HexToBytes("84FD0000181CE4E3D8F32D986A886DDBC90C8DB22553C2C04391250CCE")))
            {
                return new ResponseApdu(Hex.HexToBytes("5F67E9E059DF3C52809DC9F6DDFBEF3E4C45691B2C8CDDD89000"));
            }
            else
            {
                string apduHex = Hex.BytesToHex(command.AsByteArray());
                throw new SecureChannelException($"Error: received unexpected APDU {apduHex}");
                // return new ResponseApdu(Hex.HexToBytes("6a80"));
            }
        }
        public void Setup()
        {

        }
        public void Cleanup()
        {

        }
    }

    public class RandomNumberGeneratorFixture : RandomNumberGenerator
    {
        private readonly byte[] bytesToGenerate = Hex.HexToBytes("360CB43F4301B894"); // host challenge
        public override void GetBytes(byte[] arr)
        {
            if (arr is null)
            {
                throw new ArgumentNullException(nameof(arr));
            }
            for (int i = 0; i < bytesToGenerate.Length; i++)
            {
                arr[i] = bytesToGenerate[i];
            }
        }
    }

    [Obsolete("Class is replaced by ScpApduTransform.")]
    public class Scp03ApduTransformTests
    {
        private static IApduTransform GetPipeline() => new PipelineFixture();
        private static StaticKeys GetStaticKeys() => new StaticKeys();

        [Fact]
        public void Constructor_GivenNullPipeline_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new Scp03ApduTransform(null, GetStaticKeys()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Constructor_GivenNullStaticKeys_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new Scp03ApduTransform(GetPipeline(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Invoke_GivenPriorSetup_CorrectlyEncodesCommand()
        {
            // Arrange
            var pipeline = new Scp03ApduTransform(GetPipeline(), GetStaticKeys());
            using var fakeRng = new RandomNumberGeneratorFixture();
            pipeline.Setup(fakeRng);

            // Act
            ResponseApdu responseApdu = pipeline.Invoke(new VersionCommand().CreateCommandApdu(), typeof(object), typeof(object));
            var versionResponse = new VersionResponse(responseApdu);
            FirmwareVersion fwv = versionResponse.GetData();

            // Assert
            Assert.Equal(5, fwv.Major);
        }
    }
}

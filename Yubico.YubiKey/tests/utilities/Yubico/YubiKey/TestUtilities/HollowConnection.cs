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
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Otp.Commands;
using Yubico.YubiKey.Piv.Commands;

namespace Yubico.YubiKey.TestUtilities
{
    // This is a class that implements IYubiKeyConnection. However, it is hollow,
    // because there's nothing in it. It can't really do anything.
    // But it can be used in testing.
    // Suppose you are testing a feature that requires an actual connection to a
    // YubiKey. As long as that test does not actually need to contact a real
    // YubiKey, it can use this Hollow object. It is possible to get an instance
    // of an object that implements IYubiKeyConnection without requiring an
    // actual YubiKey.
    // Now make a hollow connection and run the tests. With some exceptions, if
    // some operation tries to actually send a command using the hollow objects,
    // they will throw an exception.
    // One command this class will execute is SelectApplicationCommand. It
    // won't actually do anything, it will simply create an error response.
    // However, this command is used in session dispose methods, so we need for
    // it to return without throwing an exception.
    // Other commands include authenticating PIV mgmt key and verifying PIV PIN.
    // If the AlwaysAuthenticatePiv property is set to true, those commands will
    // always work. This allows us to test something that requires auth or
    // verification. If AlwaysAuthenticatePiv is false, those commands will throw
    // an exception.
    public sealed class HollowConnection : IYubiKeyConnection
    {
        private readonly FirmwareVersion _firmwareVersion;

        public HollowConnection(YubiKeyApplication yubikeyApplication, FirmwareVersion firmwareVersion)
        {
            Application = yubikeyApplication;
            _firmwareVersion = firmwareVersion;
        }

        public YubiKeyApplication Application { get; private set; }

        public bool AlwaysAuthenticatePiv { get; set; }

        public ISelectApplicationData? SelectApplicationData { get; set; }

        public TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
            where TResponse : IYubiKeyResponse
        {
            if (yubiKeyCommand is SelectApplicationCommand)
            {
                var responseData = new byte[] { 0x6A, 0x82 };
                var responseApdu = new ResponseApdu(responseData);
                return yubiKeyCommand.CreateResponseForApdu(responseApdu);
            }

            if (yubiKeyCommand is InitializeAuthenticateManagementKeyCommand)
            {
                if (AlwaysAuthenticatePiv)
                {
                    var responseData = new byte[]
                    {
                        0x7C, 0x0A, 0x80, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x90, 0x00
                    };
                    var responseApdu = new ResponseApdu(responseData);
                    return yubiKeyCommand.CreateResponseForApdu(responseApdu);
                }
            }

            if (yubiKeyCommand is CompleteAuthenticateManagementKeyCommand)
            {
                if (AlwaysAuthenticatePiv)
                {
                    var apdu = yubiKeyCommand.CreateCommandApdu();
                    var data = apdu.Data.ToArray();
                    var responseData = new byte[]
                    {
                        0x7C, 0x0A, 0x82, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x90, 0x00
                    };
                    var keyBytes = new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
                    };
                    Array.Copy(data, sourceIndex: 14, responseData, destinationIndex: 4, length: 8);

                    using var tripleDes = CryptographyProviders.TripleDesCreator();

                    tripleDes.Mode = CipherMode.ECB;
                    tripleDes.Padding = PaddingMode.None;
                    using var encryptor = tripleDes.CreateEncryptor(keyBytes, rgbIV: null);
                    _ = encryptor.TransformBlock(data, inputOffset: 14, inputCount: 8, responseData, outputOffset: 4);

                    var responseApdu = new ResponseApdu(responseData);
                    return yubiKeyCommand.CreateResponseForApdu(responseApdu);
                }
            }

            if (yubiKeyCommand is ReadStatusCommand)
            {
                var sw = new byte[sizeof(short)];
                BinaryPrimitives.WriteInt16BigEndian(sw, SWConstants.Success);
                var version = new[] { _firmwareVersion.Major, _firmwareVersion.Minor, _firmwareVersion.Patch };
                var responseApdu = new ResponseApdu(version.Concat(new byte[] { 100, 0, 0, sw[0], sw[1] }).ToArray());
                return yubiKeyCommand.CreateResponseForApdu(responseApdu);
            }

            if (yubiKeyCommand is GetDataCommand)
            {
                var sw = new byte[sizeof(short)];
                BinaryPrimitives.WriteInt16BigEndian(sw, SWConstants.DataNotFound);
                var responseApdu = new ResponseApdu(new[] { sw[0], sw[1] });
                return yubiKeyCommand.CreateResponseForApdu(responseApdu);
            }

            throw new NotImplementedException();
        }

        public void Dispose()
        {
            SelectApplicationData = null;
        }
    }
}

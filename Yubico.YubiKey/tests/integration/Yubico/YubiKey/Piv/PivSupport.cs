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
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv
{
    public static class PivSupport
    {
        public static bool ImportKey(PivSession pivSession, byte slotNumber)
        {
            if (pivSession is null)
            {
                return false;
            }

            if (pivSession.ManagementKeyAuthenticated == false)
            {
                if (pivSession.TryAuthenticateManagementKey() == false)
                {
                    return false;
                }
            }

            var priKey = PivPrivateKey.Create(new byte[] {
                0x06, 0x20,
                0xba, 0x29, 0x7a, 0xc6, 0x64, 0x62, 0xef, 0x6c, 0xd0, 0x89, 0x76, 0x5c, 0xbd, 0x46, 0x52, 0x2b,
                0xb0, 0x48, 0x0e, 0x85, 0x49, 0x15, 0x85, 0xe7, 0x7a, 0x74, 0x3c, 0x8e, 0x03, 0x59, 0x8d, 0x3a
            });

            var importCommand = new ImportAsymmetricKeyCommand(
                priKey, slotNumber, PivPinPolicy.Never, PivTouchPolicy.Never);
            ImportAsymmetricKeyResponse importResponse = pivSession.Connection.SendCommand(importCommand);

            return importResponse.Status == ResponseStatus.Success;
        }

        public static bool GenerateKey(PivSession pivSession, byte slotNumber)
        {
            if (pivSession is null)
            {
                return false;
            }

            if (pivSession.ManagementKeyAuthenticated == false)
            {
                if (pivSession.TryAuthenticateManagementKey() == false)
                {
                    return false;
                }
            }

            var genPairCommand = new GenerateKeyPairCommand(
                slotNumber, PivAlgorithm.EccP256, PivPinPolicy.Never, PivTouchPolicy.Never);
            GenerateKeyPairResponse genPairResponse =
                pivSession.Connection.SendCommand(genPairCommand);

            return genPairResponse.Status == ResponseStatus.Success;
        }

        public static bool ResetPiv(PivSession pivSession)
        {
            if (pivSession is null)
            {
                return false;
            }

            if (BlockPinOrPuk(pivSession, PivSlot.Pin) == true)
            {
                if (BlockPinOrPuk(pivSession, PivSlot.Puk) == true)
                {
                    var resetCommand = new ResetPivCommand();
                    ResetPivResponse resetResponse = pivSession.Connection.SendCommand(resetCommand);
                    return resetResponse.Status == ResponseStatus.Success;
                }
            }

            return false;
        }

        private static bool BlockPinOrPuk(PivSession pivSession, byte slotNumber)
        {
            int retriesRemaining;
            do
            {
                byte[] currentValue = new byte[] {
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
                };
                byte[] newValue = new byte[] {
                    0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22
                };
                var changeCommand = new ChangeReferenceDataCommand(slotNumber, currentValue, newValue);
                ChangeReferenceDataResponse changeResponse = pivSession.Connection.SendCommand(changeCommand);

                if (changeResponse.Status == ResponseStatus.Failed)
                {
                    return false;
                }

                retriesRemaining = changeResponse.GetData() ?? 1;

            } while (retriesRemaining > 0);

            return true;
        }
    }
}

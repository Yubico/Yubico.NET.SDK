// Copyright 2025 Yubico AB
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
using Yubico.YubiKey.Fido2.PinProtocols;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class NeedPinToken : SimpleIntegrationTestConnection
    {
        private readonly byte[] _pin = FidoSessionIntegrationTestBase.ComplexPin.ToArray();

        protected NeedPinToken(YubiKeyApplication application, StandardTestDevice device)
            : base(application, device)
        {
            
        }

        protected bool GetPinToken(
            PinUvAuthProtocolBase protocol,
            PinUvAuthTokenPermissions permissions,
            out byte[] pinToken)
        {
            pinToken = Array.Empty<byte>();
            GetPinUvAuthTokenResponse getTokenRsp;

            if (protocol.AuthenticatorPublicKey is null)
            {
                var getKeyCmd = new GetKeyAgreementCommand(protocol.Protocol);
                var getKeyRsp = Connection.SendCommand(getKeyCmd);
                if (getKeyRsp.Status != ResponseStatus.Success)
                {
                    return false;
                }

                protocol.Encapsulate(getKeyRsp.GetData());

                if (permissions == PinUvAuthTokenPermissions.None)
                {
                    var getTokenCmd = new GetPinTokenCommand(protocol, _pin);
                    getTokenRsp = Connection.SendCommand(getTokenCmd);
                }
                else
                {
                    var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(protocol, _pin, permissions, null);
                    getTokenRsp = Connection.SendCommand(getTokenCmd);
                }

                if (getTokenRsp.Status == ResponseStatus.Success)
                {
                    pinToken = getTokenRsp.GetData().ToArray();
                    return true;
                }

                if (getTokenRsp.StatusWord != 0x6F35)
                {
                    return false;
                }

                var setPinCmd = new SetPinCommand(protocol, _pin);
                var setPinRsp = Connection.SendCommand(setPinCmd);
                if (setPinRsp.Status != ResponseStatus.Success)
                {
                    return false;
                }
            }

            if (permissions == PinUvAuthTokenPermissions.None)
            {
                var getTokenCmd = new GetPinTokenCommand(protocol, _pin);
                getTokenRsp = Connection.SendCommand(getTokenCmd);
            }
            else
            {
                var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(protocol, _pin, permissions, null);
                getTokenRsp = Connection.SendCommand(getTokenCmd);
            }

            if (getTokenRsp.Status == ResponseStatus.Success)
            {
                pinToken = getTokenRsp.GetData().ToArray();
                return true;
            }

            return false;
        }
    }
}

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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.Fido2.PinProtocols;

namespace Yubico.YubiKey.Fido2;

internal static class WebAuthn
{
    public const string Create = "webauthn.create";
    public const string Get = "webauthn.get";

    public static MakeCredentialData MakeCredential(
        IYubiKeyDevice device,
        byte[] clientDataHash,
        MakeCredentialParameters makeParams)
    {
        PinUvAuthProtocolBase protocol = makeParams.Protocol switch
        {
            PinUvAuthProtocol.ProtocolOne => new PinUvAuthProtocolOne(),
            PinUvAuthProtocol.ProtocolTwo => new PinUvAuthProtocolTwo(),
            _ => throw new ArgumentException($"Unsupported protocol: {makeParams.Protocol}.", nameof(makeParams)),
        };

        using var connection = device.Connect(YubiKeyApplication.Fido2);
        using var pin = new ZeroingMemoryHandle(Encoding.UTF8.GetBytes("11234567").AsMemory());

        var token = GetPinUvAuthToken(connection, pin.Data, protocol, PinUvAuthTokenPermissions.MakeCredential, makeParams.RelyingParty.Id);
        makeParams.PinUvAuthParam = protocol.AuthenticateUsingPinToken(token, clientDataHash);

        var mcCommand = new MakeCredentialCommand(makeParams);
        var mcResponse = connection.SendCommand(mcCommand);
        if (mcResponse.Status != ResponseStatus.Success)
        {
            throw new InvalidOperationException($"MakeCredential failed with status: {mcResponse.Status}");
        }

        var mcData = mcResponse.GetData();
        bool isValid = mcData.VerifyAttestation(makeParams.ClientDataHash);
        if (!isValid)
        {
            throw new InvalidOperationException("Attestation signature is not valid.");
        }

        return mcData;
    }

    public static MakeCredentialParameters CreateMakeCredentialParameters(
        PinUvAuthProtocolBase protocol,
        string rpId,
        string rpName,
        byte[] userId,
        string userName,
        string userDisplayName,
        byte[] clientDataHash)
    {
        var rp = new RelyingParty(rpId)
        {
            Name = rpName,
        };

        var user = new UserEntity(new ReadOnlyMemory<byte>(userId))
        {
            Name = userName,
            DisplayName = userDisplayName,
        };

        var makeParams = new MakeCredentialParameters(rp, user)
        {
            ClientDataHash = clientDataHash,
            Protocol = protocol.Protocol
        };

        makeParams.AddOption(AuthenticatorOptions.rk, true);

        return makeParams;
    }

    private static ReadOnlyMemory<byte> GetPinUvAuthToken(
        IYubiKeyConnection connection,
        ReadOnlyMemory<byte> pin,
        PinUvAuthProtocolBase protocol,
        PinUvAuthTokenPermissions permissions = PinUvAuthTokenPermissions.None,
        string? rpId = null)
    {
        var publicKey = GetAuthenticatorPublicKey(connection, protocol);
        protocol.Encapsulate(publicKey);

        var getTokenCmd = new GetPinUvAuthTokenUsingPinCommand(protocol, pin, permissions, rpId);
        var getTokenRsp = connection.SendCommand(getTokenCmd);

        return getTokenRsp.GetData();
    }

    private static CoseEcPublicKey GetAuthenticatorPublicKey(
        IYubiKeyConnection connection,
        PinUvAuthProtocolBase protocol)
    {
        var getKeyAgreementCommand = new GetKeyAgreementCommand(protocol.Protocol);
        var getKeyAgreementResponse = connection.SendCommand(getKeyAgreementCommand);

        return getKeyAgreementResponse.GetData();
    }
}
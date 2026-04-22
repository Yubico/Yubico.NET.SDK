// Copyright 2026 Yubico AB
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

using System.Security.Cryptography;
using Yubico.YubiKit.Fido2;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.IntegrationTests;

// Test-only PIN constant. Not zeroed because it is reused across tests.
internal static class WebAuthnTestHelpers
{
    internal const string TestRpId = "example.com";
    internal const string TestOriginUrl = "https://example.com";
    internal static readonly byte[] KnownTestPin = [0x31, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37];

    internal static WebAuthnClient CreateClient(FidoSession session)
    {
        var backend = new FidoSessionWebAuthnBackend(session);
        WebAuthnOrigin.TryParse(TestOriginUrl, out var origin);

        return new WebAuthnClient(
            backend,
            origin!,
            isPublicSuffix: domain => domain is "com" or "org" or "net" or "co.uk");
    }

    internal static async Task NormalizePinAsync(FidoSession session)
    {
        var info = await session.GetInfoAsync();
        var pinIsSet = info.Options.TryGetValue("clientPin", out var v) && v;

        var protocolVersion = info.PinUvAuthProtocols.Contains(2) ? 2 : 1;
        IPinUvAuthProtocol protocol = protocolVersion == 2
            ? new PinUvAuthProtocolV2()
            : new PinUvAuthProtocolV1();

        using var clientPin = new ClientPin(session, protocol);

        if (!pinIsSet)
        {
            await clientPin.SetPinAsync(KnownTestPin);
            return;
        }

        if (info.ForcePinChange == true)
        {
            byte[] tempPin = KnownTestPin.Reverse().ToArray();
            await clientPin.ChangePinAsync(KnownTestPin, tempPin);
            await clientPin.ChangePinAsync(tempPin, KnownTestPin);
            CryptographicOperations.ZeroMemory(tempPin);
        }

        try
        {
            _ = await clientPin.GetPinTokenAsync(KnownTestPin);
        }
        catch (CtapException ex) when (ex.Status is CtapStatus.PinInvalid)
        {
            Skip.If(true, "FIDO2 PIN differs from known test PIN '11234567'.");
        }
        catch (CtapException ex) when (ex.Status is CtapStatus.PinBlocked or CtapStatus.PinAuthBlocked)
        {
            Skip.If(true, "FIDO2 PIN is blocked. Reset required: ykman fido reset");
        }
    }

    internal static async Task<bool> SupportsPreviewSignAsync(FidoSession session)
    {
        var info = await session.GetInfoAsync();
        return info.Extensions?.Contains("previewSign") == true;
    }
}
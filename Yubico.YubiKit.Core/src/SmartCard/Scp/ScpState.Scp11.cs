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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.SmartCard.Scp;

internal partial class ScpState
{
    public static async Task<ScpState> Scp11InitAsync(
        IApduProcessor processor,
        Scp11KeyParameters keyParams,
        CancellationToken cancellationToken = default)
    {
        // GPC v2.3 Amendment F (SCP11) v1.4 §7.1.1
        var kid = keyParams.KeyReference.Kid;
        var kvn = keyParams.KeyReference.Kvn;

        // Perform Security Operation for SCP11a and SCP11c to send certificate chain
        if (kid is ScpKid.SCP11a or ScpKid.SCP11c)
            await PerformSecurityOperation(processor, keyParams, cancellationToken);

        // Prepare Authenticate command (sd elliptic curve key agreement)
        var scpTypeParam = kid switch
        {
            ScpKid.SCP11a => Scp11aTypeParam,
            ScpKid.SCP11b => Scp11bTypeParam,
            ScpKid.SCP11c => Scp11cTypeParam,
            _ => throw new ArgumentException("Invalid SCP11 KID")
        };

        byte[] keyUsage = [Scp11KeyUsage]; // AUTHENTICATED | C_MAC | C_DECRYPTION | R_MAC | R_ENCRYPTION
        byte[] keyType = [Scp11KeyType]; // AES
        byte[] keyLen = [16]; // 128-bit

        // Host ephemeral key
        var pkSdEcka = keyParams.PkSdEcka;

        // Create ephemeral OCE key using the same curve as the SD public key
        var sdParameters = pkSdEcka.Parameters;

        // Create ephemeral OCE ECDH key pair using the same curve
        var (ephemeralOceEcka, epkOceEcka) = GetECDHs(sdParameters);

        // GPC v2.3 Amendment F (SCP11) v1.4 §7.6.2.3
        // Construct the host authentication command
        using var scpTypeTlv = new Tlv(0x90, [0x11, scpTypeParam]);
        using var keyUsageTlv = new Tlv(0x95, keyUsage);
        using var keyTypeTlv = new Tlv(0x80, keyType);
        using var keyLenTlv = new Tlv(0x81, keyLen);
        var innerTlvs = TlvHelper.EncodeList([scpTypeTlv, keyUsageTlv, keyTypeTlv, keyLenTlv]);

        using var outerTlv1 = new Tlv(0xA6, innerTlvs.Span);
        using var outerTlv2 = new Tlv(0x5F49, epkOceEcka.PublicPoint.Span);
        var outerTlvs = TlvHelper.EncodeList([outerTlv1, outerTlv2]);
        var hostAuthenticateTlvEncodedData = outerTlvs;

        var ins = kid == ScpKid.SCP11b
            ? InsInternalAuthenticate
            : InsExternalAuthenticate;

        var authCommand = new ApduCommand(
            0x80,
            ins,
            kvn,
            kid,
            hostAuthenticateTlvEncodedData);

        // Issue the host authentication command
        var response = await processor.TransmitAsync(authCommand, false, cancellationToken).ConfigureAwait(false);
        if (response.SW != SWConstants.Success)
            throw ApduException.FromResponse(response, authCommand, "SCP11 authentication failed");

        // Receive and process response (ephemeral SD public key and receipt)
        using var tlvs = TlvHelper.DecodeList(response.Data.Span);

        var epkSdEckaTlv = tlvs[0]; // YubiKey ephemeral SD public key
        var sdReceipt = tlvs[1].Value; // YubiKey receipt

        // Oce static host key private key (SCP11a/c), or ephemeral key again (SCP11b)
        var skOceEcka = keyParams.SkOceEcka?.ToECDiffieHellman() ?? ephemeralOceEcka;

        // GPC v2.3 Amendment F (SCP11) v1.3 §3.1.2 Key Derivation
        var sessionKeys = X964Kdf.X963KDF(
            pkSdEcka,
            ephemeralOceEcka,
            skOceEcka,
            sdReceipt,
            epkSdEckaTlv.AsMemory(),
            hostAuthenticateTlvEncodedData,
            keyUsage,
            keyType,
            keyLen);

        // TODO review disposal
        skOceEcka.Dispose();

        CryptographicOperations.ZeroMemory(hostAuthenticateTlvEncodedData.Span);

        return new ScpState(sessionKeys, sdReceipt.Span.ToArray());
    }

    private static async Task PerformSecurityOperation(IApduProcessor processor, Scp11KeyParameters keyParams,
        CancellationToken cancellationToken)
    {
        // GPC v2.3 Amendment F (SCP11) v1.4 §7.5
        ArgumentNullException.ThrowIfNull(keyParams.SkOceEcka);

        var oceRef = keyParams.OceKeyRef ?? new KeyReference(0, 0);
        var n = keyParams.Certificates.Count - 1;
        if (n < 0)
            throw new ArgumentException("SCP11a and SCP11c require a certificate chain");

        for (var i = 0; i <= n; i++)
        {
            var certData = keyParams.Certificates[i].GetRawCertData();
            var p2 = (byte)(oceRef.Kid | (i < n ? Scp11MoreFragmentsFlag : 0x00));
            var certCommand = new ApduCommand(
                0x80,
                InsPerformSecurityOperation,
                oceRef.Kvn,
                p2,
                certData);
            var resp = await processor.TransmitAsync(certCommand, false, cancellationToken).ConfigureAwait(false);
            if (resp.SW != SWConstants.Success)
                throw ApduException.FromResponse(resp, certCommand, "SCP11 PERFORM SECURITY OPERATION failed");
        }
    }

    private static (ECDiffieHellman ephemeralOceEcka, ECPublicKey epkOceEcka) GetECDHs(ECParameters sdParameters)
    {
        var test = true;
        if (!test)
        {
            var ephemeralOceEcka = ECDiffieHellman.Create(sdParameters.Curve);
            var epkOceEcka = ECPublicKey.CreateFromParameters(ephemeralOceEcka.ExportParameters(false));
            return (ephemeralOceEcka, epkOceEcka);
        }
        else
        {
            var ephemeralOceEcka = ECPrivateKey
                .CreateFromValue(
                    Convert.FromHexString("549D2A8A03E62DC829ADE4D6850DB9568475147C59EF238F122A08CF557CDB91"),
                    KeyType.ECP256).ToECDiffieHellman();
            var epkOceEcka = ECPublicKey.CreateFromParameters(ephemeralOceEcka.ExportParameters(false));

            return (ephemeralOceEcka, epkOceEcka);
        }
    }
}
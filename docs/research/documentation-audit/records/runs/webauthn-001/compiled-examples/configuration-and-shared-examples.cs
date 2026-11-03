global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.IO;
global using System.Net.Http;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Ctap2.Cose;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.WebAuthn;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.Authentication;
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Preferences;

using Yubico.YubiKit.WebAuthn.Extensions.PreviewSign;
internal interface IRpValidation
{
    bool IsRpValidForOrigin(WebAuthnOrigin origin, string rpId);
}
internal static class DocumentationFragments
{
    internal static async Task Intent(WebAuthnClient client, PublicKeyCredentialRequestOptions requestOptions, CancellationToken cancellationToken)
    {
using (UserPresenceIntent.BeginScope("approve the transfer of 500 EUR"))
{
    await client.GetAssertionAsync(requestOptions, cancellationToken: cancellationToken);
}

    }
    internal static async Task XmlIntent(WebAuthnClient client, PublicKeyCredentialRequestOptions requestOptions, CancellationToken cancellationToken)
    {
        using (UserPresenceIntent.BeginScope("approve the transfer"))
        {
            await client.GetAssertionAsync(requestOptions, cancellationToken: cancellationToken);
        }

    }
    internal static WebAuthnClientOptions Allowlist()
    {
var allowedPairs = new HashSet<(string Origin, string RpId)>
{
    ("https://login.example.com", "example.com"),
    ("https://login.example.com", "partner.test")
};
var allowlistOptions = new WebAuthnClientOptions
{
    RpIdValidator = (requestOrigin, rpId) => allowedPairs.Contains((requestOrigin.StringValue, rpId))
};

        return allowlistOptions;
    }
    internal static WebAuthnClientOptions Service(IRpValidation rpValidationService)
    {
// rpValidationService is your application's service. Its synchronous method returns bool
// and takes (WebAuthnOrigin origin, string rpId), validating RP syntax AND caller/RP binding.
// It uses trusted, already-loaded policy; the SDK does not validate domain/URI syntax for it.
var serviceOptions = new WebAuthnClientOptions
{
    RpIdValidator = rpValidationService.IsRpValidForOrigin
};

        return serviceOptions;
    }
    internal static void Signing(GeneratedSigningKey generatedKey, ReadOnlyMemory<byte> credentialId, ReadOnlyMemory<byte> toBeSigned, ReadOnlyMemory<byte>? algorithmSpecificAdditionalArgs)
    {
// Registration: request a generated signing key.
var registrationExtensions = new RegistrationExtensionInputs(
    PreviewSign: PreviewSignRegistrationInput.GenerateKey(CoseAlgorithm.Es256));

// Authentication: provide keyHandle, algorithm-specific tbs, and optional raw additionalArgs.
var signingParams = new PreviewSignSigningParams(
    keyHandle: generatedKey.KeyHandle,
    tbs: toBeSigned,
    additionalArgs: algorithmSpecificAdditionalArgs);

var authenticationExtensions = new AuthenticationExtensionInputs(
    PreviewSign: PreviewSignAuthenticationInput.CreateSignByCredential(
        new Dictionary<ReadOnlyMemory<byte>, PreviewSignSigningParams>
        {
            [credentialId] = signingParams
        }));

    }
}

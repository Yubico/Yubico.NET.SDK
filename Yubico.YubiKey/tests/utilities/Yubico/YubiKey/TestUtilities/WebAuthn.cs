using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Yubico.YubiKey.Fido2;

namespace Yubico.YubiKey.TestUtilities;

public static class WebAuthn
{
    public static byte[] GetClientDataHash(string type, string challenge, string origin, bool crossOrigin = false)
    {
        var clientData = new ClientData(type, challenge, origin, crossOrigin);
        return SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(clientData)));
    }

    // public static byte[] GetPinUvAuthParam(Fido2Session session, byte[] clientDataHash)
    // {
    //     // if (!GetPinToken(protocol, PinUvAuthTokenPermissions.None, out byte[] pinToken))
    //     // {
    //     //     return false;
    //     // }
    //     byte[] pinUvAuthParam = session.AuthProtocol.AuthenticateUsingPinToken(pinToken, clientDataHash);

    //     // makeParams.ClientDataHash = clientDataHash;
    //     // makeParams.Protocol = protocol.Protocol;
    //     // makeParams.PinUvAuthParam = pinUvAuthParam;

    //     // makeParams.AddOption(AuthenticatorOptions.rk, true);
    // }
}

public class ClientData(string type, string challenge, string origin, bool crossOrigin = false)
{
    public const string Create = "webauthn.create";
    public const string Get = "webauthn.get";
    public string Type { get; } = type;
    public string Challenge { get; } = challenge;
    public string Origin { get; } = origin;
    public bool CrossOrigin { get; } = crossOrigin;

    public override string ToString() => $"Type: {Type}, Challenge: {Challenge}, Origin: {Origin}, CrossOrigin: {CrossOrigin}";
}


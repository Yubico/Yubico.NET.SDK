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

namespace Yubico.YubiKey.Fido2;

internal readonly struct ClientData
{
    public string Type { get; }
    public string Challenge { get; }
    public string Origin { get; }
    public bool CrossOrigin { get; }


    internal ClientData(string type, string challenge, string origin, bool crossOrigin = false)
    {
        Type = type;
        Challenge = challenge;
        Origin = origin;
        CrossOrigin = crossOrigin;
    }

    public byte[] ComputeHash()
    {
        // Explicit JSON ordering - matches WebAuthn spec
        string json = $$"""{"type":"{{Type}}","challenge":"{{Challenge}}","origin":"{{Origin}}"{{(CrossOrigin ? ",\"crossOrigin\":true" : "")}}"}""";

        using var sha256 = CryptographyProviders.Sha256Creator();

        _ = sha256.TransformFinalBlock(Encoding.UTF8.GetBytes(json), 0, json.Length);
        return sha256.Hash;
    }

    public static (ClientData clientData, byte[] challengeBytes) CreateWithRandomChallenge(
        string type, 
        string origin, 
        bool crossOrigin = false)
    {
        byte[] challengeBytes = GenerateRandomChallenge();
        string challengeBase64 = Base64(challengeBytes);
        string challengeBase64Url = UrlEncode(challengeBase64);

        var clientData = new ClientData(type, challengeBase64Url, origin, crossOrigin);
        return (clientData, challengeBytes);
    }

    public static ClientData Create(string type, string challenge, string origin, bool crossOrigin = false) => new(type, challenge, origin, crossOrigin);

    public static ClientData Create(string type, byte[] challenge, string origin, bool crossOrigin = false)
    {
        string challengeBase64 = Base64(challenge);
        string challengeBase64Url = UrlEncode(challengeBase64);
        return new ClientData(type, challengeBase64Url, origin, crossOrigin);
    }

    private static byte[] GenerateRandomChallenge(int length = 32)
    {
        byte[] challenge = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(challenge);
        return challenge;
    }

    internal static string Base64(byte[] data) => Convert.ToBase64String(data);
    internal static string UrlEncode(string data) => data.TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public override string ToString() =>
        $"Type: {Type}, Challenge: {Challenge}, Origin: {Origin}, CrossOrigin: {CrossOrigin}";
}


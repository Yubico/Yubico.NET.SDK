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

using System.Formats.Cbor;
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;

namespace Yubico.YubiKit.Fido2.Cbor;

internal static class FidoSessionRequestEncoding
{
    internal static byte[] BuildMakeCredentialRequest(
        ReadOnlyMemory<byte> clientDataHash,
        PublicKeyCredentialRpEntity rp,
        PublicKeyCredentialUserEntity user,
        IReadOnlyList<PublicKeyCredentialParameters> pubKeyCredParams,
        MakeCredentialOptions? options)
    {
        if (options?.UserPresence is false)
        {
            throw new ArgumentException("makeCredential does not allow up=false (CTAP §6.1).", nameof(options));
        }

        ThrowIfDuplicateCredentialParameters(pubKeyCredParams);

        var builder = CtapRequestBuilder.Create(CtapCommand.MakeCredential)
            .WithValue(1, writer => writer.WriteByteString(clientDataHash.Span))
            .WithValue(2, rp.Encode)
            .WithValue(3, user.Encode)
            .WithArray(4, writer => WriteArray(writer, pubKeyCredParams, static (w, parameter) => parameter.Encode(w)));

        if (options?.ExcludeList is { Count: > 0 } excludeList)
        {
            builder.WithArray(5, writer => WriteArray(writer, excludeList, static (w, credential) => credential.Encode(w)));
        }

        if (options?.Extensions is { Length: > 0 } extensions)
        {
            builder.WithValue(6, writer => writer.WriteEncodedValue(extensions.Span));
        }

        if (HasMakeCredentialOptions(options))
        {
            builder.WithValue(7, writer => WriteMakeCredentialOptions(writer, options!));
        }

        if (options?.PinUvAuthParam is { Length: > 0 } pinUvAuthParam)
        {
            builder
                .WithValue(8, writer => writer.WriteByteString(pinUvAuthParam.Span))
                .WithInt(9, options.PinUvAuthProtocol ?? 2);
        }

        if (options?.EnterpriseAttestation.HasValue == true)
        {
            builder.WithInt(10, options.EnterpriseAttestation.Value);
        }

        return builder.Build();
    }

    internal static byte[] BuildGetAssertionRequest(
        string rpId,
        ReadOnlyMemory<byte> clientDataHash,
        GetAssertionOptions? options)
    {
        var builder = CtapRequestBuilder.Create(CtapCommand.GetAssertion)
            .WithString(1, rpId)
            .WithValue(2, writer => writer.WriteByteString(clientDataHash.Span));

        if (options?.AllowList is { Count: > 0 } allowList)
        {
            builder.WithArray(3, writer => WriteArray(writer, allowList, static (w, credential) => credential.Encode(w)));
        }

        if (options?.Extensions is { Length: > 0 } extensions)
        {
            builder.WithValue(4, writer => writer.WriteEncodedValue(extensions.Span));
        }

        if (HasGetAssertionOptions(options))
        {
            builder.WithValue(5, writer => WriteGetAssertionOptions(writer, options!));
        }

        if (options?.PinUvAuthParam is { Length: > 0 } pinUvAuthParam)
        {
            builder
                .WithValue(6, writer => writer.WriteByteString(pinUvAuthParam.Span))
                .WithInt(7, options.PinUvAuthProtocol ?? 2);
        }

        return builder.Build();
    }

    /// <summary>
    /// CTAP 2.1-2.3 §6.1 pubKeyCredParams: "The array is ordered from most preferred to least preferred
    /// and MUST NOT include duplicate entries." The requirement is on the platform, so the SDK enforces it.
    /// </summary>
    internal static void ThrowIfDuplicateCredentialParameters(
        IReadOnlyList<PublicKeyCredentialParameters> pubKeyCredParams,
        string paramName = "pubKeyCredParams")
    {
        var seen = new HashSet<(string Type, CoseAlgorithmIdentifier Algorithm)>();
        foreach (var parameter in pubKeyCredParams)
        {
            if (!seen.Add((parameter.Type, parameter.Algorithm)))
            {
                throw new ArgumentException(
                    $"Duplicate credential parameter ({parameter.Type}, {parameter.Algorithm}); pubKeyCredParams must not include duplicate entries (CTAP §6.1).",
                    paramName);
            }
        }
    }

    private static bool HasMakeCredentialOptions(MakeCredentialOptions? options) =>
        options?.ResidentKey.HasValue == true ||
        options?.UserPresence.HasValue == true ||
        options?.UserVerification.HasValue == true;

    private static void WriteMakeCredentialOptions(CborWriter writer, MakeCredentialOptions options)
    {
        var count = 0;
        if (options.ResidentKey.HasValue) count++;
        if (options.UserPresence.HasValue) count++;
        if (options.UserVerification.HasValue) count++;

        writer.WriteStartMap(count);

        if (options.ResidentKey.HasValue)
        {
            writer.WriteTextString("rk");
            writer.WriteBoolean(options.ResidentKey.Value);
        }

        if (options.UserPresence.HasValue)
        {
            writer.WriteTextString("up");
            writer.WriteBoolean(options.UserPresence.Value);
        }

        if (options.UserVerification.HasValue)
        {
            writer.WriteTextString("uv");
            writer.WriteBoolean(options.UserVerification.Value);
        }

        writer.WriteEndMap();
    }

    private static bool HasGetAssertionOptions(GetAssertionOptions? options) =>
        options?.UserPresence.HasValue == true ||
        options?.UserVerification.HasValue == true;

    private static void WriteGetAssertionOptions(CborWriter writer, GetAssertionOptions options)
    {
        var count = 0;
        if (options.UserPresence.HasValue) count++;
        if (options.UserVerification.HasValue) count++;

        writer.WriteStartMap(count);

        if (options.UserPresence.HasValue)
        {
            writer.WriteTextString("up");
            writer.WriteBoolean(options.UserPresence.Value);
        }

        if (options.UserVerification.HasValue)
        {
            writer.WriteTextString("uv");
            writer.WriteBoolean(options.UserVerification.Value);
        }

        writer.WriteEndMap();
    }

    private static void WriteArray<T>(
        CborWriter writer,
        IReadOnlyList<T> values,
        Action<CborWriter, T> writeValue)
    {
        writer.WriteStartArray(values.Count);
        foreach (var value in values)
        {
            writeValue(writer, value);
        }

        writer.WriteEndArray();
    }
}
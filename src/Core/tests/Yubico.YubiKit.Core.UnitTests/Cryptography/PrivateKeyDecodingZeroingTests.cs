// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Formats.Asn1;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class PrivateKeyDecodingZeroingTests
{
    [Fact]
    public void CreateRSAParameters_ZeroesPrivateArraysSupersededByNormalization()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var expected = rsa.ExportParameters(includePrivateParameters: true);
        RSAParameters actual = default;
        byte[][]? decoderArrays = null;

        try
        {
            actual = AsnPrivateKeyDecoder.CreateRSAParameters(
                pkcs8,
                parameters => decoderArrays = GetPrivateArrays(parameters));

            AssertAllZero(Assert.IsType<byte[][]>(decoderArrays));
            Assert.Equal(expected.D, actual.D);
            Assert.Equal(expected.P, actual.P);
            Assert.Equal(expected.Q, actual.Q);
            Assert.Equal(expected.DP, actual.DP);
            Assert.Equal(expected.DQ, actual.DQ);
            Assert.Equal(expected.InverseQ, actual.InverseQ);
            AssertContainsNonZero(Assert.IsType<byte[]>(actual.D));
        }
        finally
        {
            ZeroPrivateArrays(expected);
            ZeroPrivateArrays(actual);
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    [Fact]
    public void RSAPrivateKey_CreateFromPkcs8_ZeroesDecoderArraysAndPreservesCopiedKey()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var expected = rsa.ExportParameters(includePrivateParameters: true);
        byte[][]? decoderArrays = null;

        try
        {
            using var key = RSAPrivateKey.CreateFromPkcs8(
                pkcs8,
                parameters => decoderArrays = GetPrivateArrays(parameters));

            var orphanedArrays = Assert.IsType<byte[][]>(decoderArrays);
            AssertAllZero(orphanedArrays);

            var survivingD = Assert.IsType<byte[]>(key.Parameters.D);
            Assert.NotSame(orphanedArrays[0], survivingD);
            Assert.Equal(expected.D, survivingD);
            Assert.Equal(expected.P, key.Parameters.P);
            Assert.Equal(expected.Q, key.Parameters.Q);
            Assert.Equal(expected.DP, key.Parameters.DP);
            Assert.Equal(expected.DQ, key.Parameters.DQ);
            Assert.Equal(expected.InverseQ, key.Parameters.InverseQ);
            AssertContainsNonZero(survivingD);
        }
        finally
        {
            ZeroPrivateArrays(expected);
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    [Fact]
    public void ECPrivateKey_CreateFromPkcs8_ZeroesDecoderDAndPreservesCopiedKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pkcs8 = ecdsa.ExportPkcs8PrivateKey();
        var expected = ecdsa.ExportParameters(includePrivateParameters: true);
        byte[]? decoderD = null;

        try
        {
            using var key = ECPrivateKey.CreateFromPkcs8(
                pkcs8,
                parameters => decoderD = parameters.D);

            var orphanedD = Assert.IsType<byte[]>(decoderD);
            AssertAllZero([orphanedD]);

            var survivingD = Assert.IsType<byte[]>(key.Parameters.D);
            Assert.NotSame(orphanedD, survivingD);
            Assert.Equal(expected.D, survivingD);
            Assert.Equal(expected.Q.X, key.Parameters.Q.X);
            Assert.Equal(expected.Q.Y, key.Parameters.Q.Y);
            AssertContainsNonZero(survivingD);
        }
        finally
        {
            if (expected.D is not null)
            {
                CryptographicOperations.ZeroMemory(expected.D);
            }

            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    [Fact]
    public void GetCurve25519PrivateKeyData_TrailingDataThrowsAndZeroesDecodedScalar()
    {
        var privateKey = new byte[32];
        Array.Fill(privateKey, (byte)0x5A);
        var pkcs8 = BuildCurve25519Pkcs8WithTrailingData(privateKey);
        byte[]? decodedPrivateKey = null;

        try
        {
            Assert.Throws<AsnContentException>(() =>
                AsnPrivateKeyDecoder.GetCurve25519PrivateKeyData(
                    pkcs8,
                    decoded => decodedPrivateKey = decoded));

            AssertAllZero([Assert.IsType<byte[]>(decodedPrivateKey)]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    private static byte[][] GetPrivateArrays(RSAParameters parameters) =>
    [
        Assert.IsType<byte[]>(parameters.D),
        Assert.IsType<byte[]>(parameters.P),
        Assert.IsType<byte[]>(parameters.Q),
        Assert.IsType<byte[]>(parameters.DP),
        Assert.IsType<byte[]>(parameters.DQ),
        Assert.IsType<byte[]>(parameters.InverseQ)
    ];

    private static void AssertAllZero(IEnumerable<byte[]> arrays)
    {
        foreach (var array in arrays)
        {
            Assert.All(array, value => Assert.Equal(0, value));
        }
    }

    private static void AssertContainsNonZero(byte[] array) =>
        Assert.Contains(array, value => value != 0);

    private static void ZeroPrivateArrays(RSAParameters parameters)
    {
        foreach (var array in GetExistingPrivateArrays(parameters))
        {
            CryptographicOperations.ZeroMemory(array);
        }
    }

    private static IEnumerable<byte[]> GetExistingPrivateArrays(RSAParameters parameters)
    {
        if (parameters.D is not null)
        {
            yield return parameters.D;
        }

        if (parameters.P is not null)
        {
            yield return parameters.P;
        }

        if (parameters.Q is not null)
        {
            yield return parameters.Q;
        }

        if (parameters.DP is not null)
        {
            yield return parameters.DP;
        }

        if (parameters.DQ is not null)
        {
            yield return parameters.DQ;
        }

        if (parameters.InverseQ is not null)
        {
            yield return parameters.InverseQ;
        }
    }

    private static byte[] BuildCurve25519Pkcs8WithTrailingData(byte[] privateKey)
    {
        var innerWriter = new AsnWriter(AsnEncodingRules.DER);
        innerWriter.WriteOctetString(privateKey);

        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.WriteInteger(0);
        writer.PushSequence();
        writer.WriteObjectIdentifier(Oids.Ed25519);
        writer.PopSequence();
        writer.WriteOctetString(innerWriter.Encode());
        writer.WriteNull();
        writer.PopSequence();
        return writer.Encode();
    }
}
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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class PrivateKeyZeroingFollowupTests
{
    [Fact]
    public void EncodeToPkcs8_Rsa_ZeroesTemporaryBuffersAndPreservesCallerKey()
    {
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(includePrivateParameters: true);
        RSAParameters decoded = default;
        byte[]? pkcs8 = null;
        byte[]? encodedPkcs1 = null;
        List<byte[]> integerContents = [];

        try
        {
            pkcs8 = AsnPrivateKeyEncoder.EncodeToPkcs8(
                parameters,
                integerContentCreated: integerContents.Add,
                rsaKeyEncoded: value => encodedPkcs1 = value);

            Assert.Equal(8, integerContents.Count);
            AssertAllZero(integerContents);
            AssertAllZero([Assert.IsType<byte[]>(encodedPkcs1)]);

            var survivingD = Assert.IsType<byte[]>(parameters.D);
            Assert.NotSame(integerContents[2], survivingD);
            AssertContainsNonZero(survivingD);

            using var check = RSA.Create();
            check.ImportPkcs8PrivateKey(pkcs8, out _);
            decoded = check.ExportParameters(includePrivateParameters: true);

            Assert.Equal(parameters.D, decoded.D);
            Assert.Equal(parameters.P, decoded.P);
            Assert.Equal(parameters.Q, decoded.Q);
            Assert.Equal(parameters.DP, decoded.DP);
            Assert.Equal(parameters.DQ, decoded.DQ);
            Assert.Equal(parameters.InverseQ, decoded.InverseQ);
            AssertContainsNonZero(Assert.IsType<byte[]>(decoded.D));
        }
        finally
        {
            ZeroPrivateArrays(parameters);
            ZeroPrivateArrays(decoded);
            if (pkcs8 is not null)
            {
                CryptographicOperations.ZeroMemory(pkcs8);
            }
        }
    }

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
        CryptographicOperations.ZeroMemory(parameters.D);
        CryptographicOperations.ZeroMemory(parameters.P);
        CryptographicOperations.ZeroMemory(parameters.Q);
        CryptographicOperations.ZeroMemory(parameters.DP);
        CryptographicOperations.ZeroMemory(parameters.DQ);
        CryptographicOperations.ZeroMemory(parameters.InverseQ);
    }
}
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
using System.Numerics;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities;

public static class CryptoSupport
{
    // Perform raw RSA using the given public key. Create a new byte array
    // for the output.
    public static bool CSharpRawRsaPublic(
        TestKey rsaTestKey,
        byte[] dataToProcess,
        out byte[] processedData)
    {
        using var rsaObject = rsaTestKey.AsRSA();

        var rsaParams = rsaObject.ExportParameters(false);
        var bufferLength = rsaTestKey.KeyType == KeyType.RSA2048 ? 256 : 128;
        processedData = [];
        byte[] temp = [];

        try
        {
            var value = new BigInteger(dataToProcess, true, true);
            var expo = new BigInteger(rsaParams.Exponent, true, true);
            var mod = new BigInteger(rsaParams.Modulus, true, true);

            var result = BigInteger.ModPow(value, expo, mod);
            temp = result.ToByteArray(true, true);

            return ToFixedLengthArray(temp, bufferLength, out processedData);
        }
        finally
        {
            ClearRsaParameters(rsaParams);
            CryptographicOperations.ZeroMemory(temp);
        }
    }

    // Perform raw RSA using the given private key. Create a new byte array
    // for the output.
    public static bool CSharpRawRsaPrivate(
        TestKey rsaTestKey,
        byte[] dataToProcess,
        out byte[] processedData)
    {
        using var rsaObject = rsaTestKey.AsRSA();

        var rsaParams = rsaObject.ExportParameters(true);
        var bufferLength = rsaTestKey.KeyType == KeyType.RSA2048 ? 256 : 128;
        processedData = [];
        byte[] temp = [];

        try
        {
            var value = new BigInteger(dataToProcess, true, true);
            var expo = new BigInteger(rsaParams.D, true, true);
            var mod = new BigInteger(rsaParams.Modulus, true, true);

            var result = BigInteger.ModPow(value, expo, mod);
            temp = result.ToByteArray(true, true);

            return ToFixedLengthArray(temp, bufferLength, out processedData);
        }
        finally
        {
            ClearRsaParameters(rsaParams);
            CryptographicOperations.ZeroMemory(temp);
        }
    }

    // Write the data in the inputArray into a new byte array, one that is
    // exactly fixedLength bytes long.
    // If the inputArray is too long, strip any leading 00 bytes. If there
    // are not enough leading 00 bytes to strip, return false.
    // Id the inputArray is too short, prepend 00 bytes.
    private static bool ToFixedLengthArray(
        byte[] inputArray,
        int fixedLength,
        out byte[] outputArray)
    {
        outputArray = new byte[fixedLength];

        if (inputArray.Length <= fixedLength)
        {
            Array.Copy(inputArray, 0, outputArray, fixedLength - inputArray.Length, inputArray.Length);
        }
        else
        {
            var count = inputArray.Length - fixedLength;
            for (var index = 0; index < count; index++)
            {
                if (inputArray[index] == 0)
                {
                    continue;
                }

                outputArray = [];
                return false;
            }

            Array.Copy(inputArray, count, outputArray, 0, fixedLength);
        }

        return true;
    }

    public static void ClearRsaParameters(
        RSAParameters rsaParams)
    {
        CryptographicOperations.ZeroMemory(rsaParams.P);
        CryptographicOperations.ZeroMemory(rsaParams.Q);
        CryptographicOperations.ZeroMemory(rsaParams.DP);
        CryptographicOperations.ZeroMemory(rsaParams.DQ);
        CryptographicOperations.ZeroMemory(rsaParams.InverseQ);
        CryptographicOperations.ZeroMemory(rsaParams.D);
    }

    public static void ClearEccParameters(
        ECParameters eccParams)
    {
        CryptographicOperations.ZeroMemory(eccParams.D);
    }
}

// Copyright 2021 Yubico AB
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
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Oath.Commands
{
    /// <summary>
    /// Provides helper methods that are used to calculate challenge-response for the commands: 
    /// SetPassword, Validate, CalculateCredential, CalculateAllCredentials.
    /// </summary>
    public abstract class OathChallengeResponseBaseCommand
    {
        /// <summary>
        /// Generates 8 bytes challenge that can be used for TOTP credential calculation.
        /// </summary>
        /// <returns>
        /// 8 bytes challenge.
        /// </returns>
        protected static byte[] GenerateTotpChallenge(CredentialPeriod? period)
        {
            if (period is null)
            {
                period = CredentialPeriod.Period30;
            }

            ulong timePeriod = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (uint)period;
            byte[] bytes = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, timePeriod);

            return bytes;
        }

        [Obsolete("This method is obsolete. Call GenerateRandomChallenge instead.")]
        protected static byte[] GenerateChallenge() => GenerateRandomChallenge();

        /// <summary>
        /// Generates random 8 bytes that can be used as challenge for authentication.
        /// </summary>
        /// <returns>
        /// Random 8 bytes.
        /// </returns>
        protected static byte[] GenerateRandomChallenge()
        {
            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();

            byte[] randomBytes = new byte[8];
            randomObject.GetBytes(randomBytes);
            
            return randomBytes;
        }

        /// <summary>
        /// Passes a user-supplied UTF-8 encoded password through 1000 rounds of PBKDF2
        /// with the salt value (the deviceID returned in SelectResponse).
        /// </summary>
        /// <returns>
        /// 16 bytes secret for authentication.
        /// </returns>
        protected static byte[] CalculateSecret(ReadOnlyMemory<byte> password, ReadOnlyMemory<byte> salt)
        {
#pragma warning disable CA5379, CA5387 // Do Not Use Weak Key Derivation Function Algorithm
            using (var pbkBytes = new Rfc2898DeriveBytes(password.ToArray(), salt.ToArray(), 1000))
            {
                return pbkBytes.GetBytes(16);
            }
#pragma warning restore CA5379, CA5387 // Do Not Use Weak Key Derivation Function Algorithm
        }

        /// <summary>
        /// Calculates HMAC using SHA1 as a hash function.
        /// </summary>
        /// <returns>
        /// HMAC result.
        /// </returns>
        protected static byte[] CalculateResponse(ReadOnlyMemory<byte> secret, ReadOnlyMemory<byte> message)
        {
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
            using (var hmacSha1 = new HMACSHA1(secret.ToArray()))
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
            {
                return hmacSha1.ComputeHash(message.ToArray());
            }
        }

    }
}

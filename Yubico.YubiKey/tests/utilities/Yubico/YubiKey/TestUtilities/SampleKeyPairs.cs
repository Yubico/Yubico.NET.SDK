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
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities
{
    [Obsolete("obsolete")]
    public static class SampleKeyPairs
    {
        public static bool GetKeysAndCertPem(
            KeyType keyType,
            bool validAttest,
            out string? cert,
            out string? publicKey,
            out string? privateKey)
        {
            var testCert = TestKeys.GetTestCertificate(keyType, validAttest);
            var testPrivKey = TestKeys.GetTestPrivateKey(keyType);
            var testPubKey = TestKeys.GetTestPublicKey(keyType);

            cert = testCert.AsPemString();
            privateKey = testPrivKey.AsPemString();
            publicKey = testPubKey.AsPemString();
            return true;
        }
    }
}

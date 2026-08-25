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
using System.Linq;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2.Commands
{
    /// <summary>
    /// Builders for <c>authenticatorCredentialManagement</c> response encodings
    /// used by credential-management decoding tests.
    /// </summary>
    internal static class CredMgmtTestData
    {
        /// <summary>
        /// A COSE key type this SDK does not model.
        /// </summary>
        /// <remarks>
        /// Taken from a real extension-defined key type rather than invented, so
        /// the fixtures exercise a shape a YubiKey can actually return. The
        /// specific value does not matter to these tests; what matters is that
        /// it is outside the set the SDK models.
        /// </remarks>
        public const int UnmodeledKeyType = -65537;

        /// <summary>
        /// A COSE algorithm this SDK does not model. See
        /// <see cref="UnmodeledKeyType"/> for why these values were chosen.
        /// </summary>
        public const int UnmodeledAlgorithm = -65700;

        public static readonly byte[] UserIdBytes = { 0x75, 0x73, 0x65, 0x72, 0x49, 0x64 };
        public static readonly byte[] CredentialIdBytes = { 0x31, 0x32, 0x33, 0x34 };
        public const string UserName = "userName";
        public const string UserDisplayName = "User Name";

        /// <summary>
        /// Builds a credential-management response containing a single
        /// credential: user (0x06), credential ID (0x07), public key (0x08),
        /// total credentials (0x09) and credProtect (0x0A).
        /// </summary>
        public static byte[] BuildCredentialUserInfo(
            byte[] encodedPublicKey,
            int totalCredentials = 1,
            int credProtectPolicy = 1)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(5);

            cbor.WriteInt32(6);
            WriteUserEntity(cbor);

            cbor.WriteInt32(7);
            WriteCredentialId(cbor);

            cbor.WriteInt32(8);
            cbor.WriteEncodedValue(encodedPublicKey);

            cbor.WriteInt32(9);
            cbor.WriteInt32(totalCredentials);

            cbor.WriteInt32(10);
            cbor.WriteInt32(credProtectPolicy);

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// An EC2 P-256 key using an algorithm this SDK does not model.
        /// </summary>
        public static byte[] BuildUnsupportedCoseKey() =>
            BuildEc2Key((int)CoseKeyType.Ec2, -70000);

        /// <summary>
        /// A key whose type and algorithm are both outside the set this SDK
        /// models, and whose value is itself structured (two nested EC2 points).
        /// Mirrors the shape of a real extension-defined key rather than a
        /// trivially malformed one.
        /// </summary>
        public static byte[] BuildUnmodeledStructuredCoseKey()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(4);
            cbor.WriteInt32(1);
            cbor.WriteInt32(UnmodeledKeyType);
            cbor.WriteInt32(3);
            cbor.WriteInt32(UnmodeledAlgorithm);
            cbor.WriteInt32(-1);
            cbor.WriteEncodedValue(BuildEc2Key((int)CoseKeyType.Ec2, (int)CoseAlgorithmIdentifier.ES256, 0x44));
            cbor.WriteInt32(-2);
            cbor.WriteEncodedValue(BuildEc2Key((int)CoseKeyType.Ec2, (int)CoseAlgorithmIdentifier.ES256, 0x55));
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        /// <summary>
        /// A well-formed ES256 EC2 key.
        /// </summary>
        public static byte[] BuildEs256CoseKey() =>
            BuildEc2Key((int)CoseKeyType.Ec2, (int)CoseAlgorithmIdentifier.ES256);

        /// <summary>
        /// A modeled algorithm paired with the wrong key type. This is corrupt
        /// data, not a future algorithm.
        /// </summary>
        public static byte[] BuildMismatchedEs256OkpCoseKey() =>
            BuildEc2Key((int)CoseKeyType.Okp, (int)CoseAlgorithmIdentifier.ES256);

        private static byte[] BuildEc2Key(int keyType, int algorithm, byte fill = 0x11)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(5);
            cbor.WriteInt32(1);
            cbor.WriteInt32(keyType);
            cbor.WriteInt32(3);
            cbor.WriteInt32(algorithm);
            cbor.WriteInt32(-1);
            cbor.WriteInt32((int)CoseEcCurve.P256);
            cbor.WriteInt32(-2);
            cbor.WriteByteString(Enumerable.Repeat(fill, 32).ToArray());
            cbor.WriteInt32(-3);
            cbor.WriteByteString(Enumerable.Repeat((byte)(fill + 1), 32).ToArray());
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void WriteUserEntity(CborWriter cbor)
        {
            cbor.WriteStartMap(3);
            cbor.WriteTextString("id");
            cbor.WriteByteString(UserIdBytes);
            cbor.WriteTextString("name");
            cbor.WriteTextString(UserName);
            cbor.WriteTextString("displayName");
            cbor.WriteTextString(UserDisplayName);
            cbor.WriteEndMap();
        }

        private static void WriteCredentialId(CborWriter cbor)
        {
            cbor.WriteStartMap(2);
            cbor.WriteTextString("id");
            cbor.WriteByteString(CredentialIdBytes);
            cbor.WriteTextString("type");
            cbor.WriteTextString("public-key");
            cbor.WriteEndMap();
        }
    }
}

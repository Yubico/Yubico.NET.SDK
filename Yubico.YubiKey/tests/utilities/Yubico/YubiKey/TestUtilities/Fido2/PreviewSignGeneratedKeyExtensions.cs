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
using System.Formats.Cbor;
using CommunityToolkit.Diagnostics;
using Yubico.Core.Cryptography;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.TestUtilities.Fido2
{
    /// <summary>
    /// Extension methods for <see cref="PreviewSignGeneratedKey"/> that provide
    /// RP-side verification functionality for the ESP256-split-ARKG test path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These methods enable Relying Party (RP) side public key derivation and
    /// signature verification for generated ARKG-P256 previewSign keys. These are
    /// test/verification utilities and not part of the authenticator-side SDK surface.
    /// </para>
    /// <para>
    /// In production deployments, RP-side verification logic belongs on the
    /// application server, not in the YubiKey SDK. These methods are provided
    /// for integration testing and demonstration purposes.
    /// </para>
    /// </remarks>
    public static class PreviewSignGeneratedKeyExtensions
    {
        private const int CoseKeyTypeArkgPub = -65537;
        private const int CoseAlgorithmArkgP256 = -65700;

        /// <summary>
        /// Derives a public key using the ARKG-P256 algorithm.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs offline key derivation using the ARKG-P256 algorithm.
        /// The derived public key can be used to verify signatures created by the
        /// YubiKey when provided with the corresponding ARKG key handle and context.
        /// </para>
        /// <para>
        /// Multiple independent public keys can be derived from the same generated
        /// key by using different context strings. Each context produces a unique
        /// derived key pair.
        /// </para>
        /// <para>
        /// To request an ESP256-split-ARKG signature with this test helper, pass
        /// the returned <see cref="PreviewSignDerivedKey"/> to
        /// <see cref="GetAssertionParameters.AddPreviewSignExtension"/>.
        /// The YubiKey will produce a signature that can be verified using
        /// <see cref="PreviewSignDerivedKey.VerifySignature(byte[], byte[])"/>.
        /// </para>
        /// </remarks>
        /// <param name="generatedKey">The generated key from which to derive.</param>
        /// <param name="inputKeyingMaterial">
        /// Input keying material for ARKG-P256 derivation. This should be random data
        /// unique to the derivation context.
        /// </param>
        /// <param name="context">
        /// Context string for domain separation. Different contexts produce
        /// different derived keys from the same input keying material.
        /// </param>
        /// <returns>
        /// A <see cref="PreviewSignDerivedKey"/> containing the derived public key,
        /// ARKG key handle, device key handle, and context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="generatedKey"/>, <paramref name="inputKeyingMaterial"/>,
        /// or <paramref name="context"/> is null.
        /// </exception>
        public static PreviewSignDerivedKey DerivePublicKey(
            this PreviewSignGeneratedKey generatedKey,
            byte[] inputKeyingMaterial,
            byte[] context)
        {
            Guard.IsNotNull(generatedKey, nameof(generatedKey));
            Guard.IsNotNull(inputKeyingMaterial, nameof(inputKeyingMaterial));
            Guard.IsNotNull(context, nameof(context));

            return DerivePublicKey(generatedKey, (ReadOnlySpan<byte>)inputKeyingMaterial, (ReadOnlySpan<byte>)context);
        }

        /// <summary>
        /// Derives a public key using the ARKG-P256 algorithm.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs offline key derivation using the ARKG-P256 algorithm.
        /// The derived public key can be used to verify signatures created by the
        /// YubiKey when provided with the corresponding ARKG key handle and context.
        /// </para>
        /// <para>
        /// Multiple independent public keys can be derived from the same generated
        /// key by using different context strings. Each context produces a unique
        /// derived key pair.
        /// </para>
        /// <para>
        /// To request an ESP256-split-ARKG signature with this test helper, pass
        /// the returned <see cref="PreviewSignDerivedKey"/> to
        /// <see cref="GetAssertionParameters.AddPreviewSignExtension"/>.
        /// The YubiKey will produce a signature that can be verified using
        /// <see cref="PreviewSignDerivedKey.VerifySignature(byte[], byte[])"/>.
        /// </para>
        /// </remarks>
        /// <param name="generatedKey">The generated key from which to derive.</param>
        /// <param name="inputKeyingMaterial">
        /// Input keying material for ARKG-P256 derivation. This should be random data
        /// unique to the derivation context.
        /// </param>
        /// <param name="context">
        /// Context string for domain separation. Different contexts produce
        /// different derived keys from the same input keying material.
        /// </param>
        /// <returns>
        /// A <see cref="PreviewSignDerivedKey"/> containing the derived public key,
        /// ARKG key handle, device key handle, and context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="generatedKey"/> is null.
        /// </exception>
        public static PreviewSignDerivedKey DerivePublicKey(
            this PreviewSignGeneratedKey generatedKey,
            ReadOnlySpan<byte> inputKeyingMaterial,
            ReadOnlySpan<byte> context)
        {
            Guard.IsNotNull(generatedKey, nameof(generatedKey));

            var (blindingPublicKey, kemPublicKey) = ParseArkgCoseKey(generatedKey.PublicKey.ToArray());
            var (derivedPublicKey, arkgKeyHandle) = ArkgPrimitives.Create().DerivePublicKey(
                blindingPublicKey,
                kemPublicKey,
                inputKeyingMaterial,
                context);

            return new PreviewSignDerivedKey(
                derivedPublicKey,
                arkgKeyHandle,
                generatedKey.KeyHandle,
                context.ToArray());
        }

        private static (byte[] blindingPublicKey, byte[] kemPublicKey) ParseArkgCoseKey(byte[] coseEncoded)
        {
            var reader = new CborReader(coseEncoded, CborConformanceMode.Ctap2Canonical);
            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;

            bool? isArkgPubKey = null;
            bool? isArkgP256Key = null;
            byte[]? blindingPublicKey = null;
            byte[]? kemPublicKey = null;

            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                long key = reader.ReadInt64();
                if (key == 1)
                {
                    int keyType = reader.ReadInt32();
                    isArkgPubKey = keyType == CoseKeyTypeArkgPub;
                }
                else if (key == 3)
                {
                    int algorithm = reader.ReadInt32();
                    isArkgP256Key = algorithm == CoseAlgorithmArkgP256;
                }
                else if (key == -1)
                {
                    blindingPublicKey = ReadEc2PointAsSec1(reader);
                }
                else if (key == -2)
                {
                    kemPublicKey = ReadEc2PointAsSec1(reader);
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (isArkgPubKey != true || isArkgP256Key != true)
            {
                throw new Ctap2DataException(
                    "ESP256-split-ARKG test helper requires an ARKG-pub ARKG-P256 COSE key.");
            }

            if (blindingPublicKey is null || kemPublicKey is null)
            {
                throw new Ctap2DataException(
                    "ESP256-split-ARKG test helper COSE key missing blindingPublicKey (-1) or kemPublicKey (-2).");
            }

            return (blindingPublicKey, kemPublicKey);
        }

        private static byte[] ReadEc2PointAsSec1(CborReader reader)
        {
            int? subEntries = reader.ReadStartMap();
            int subCount = subEntries ?? int.MaxValue;

            bool? isEc2Key = null;
            bool? isP256Curve = null;
            byte[]? x = null;
            byte[]? y = null;
            for (int j = 0; j < subCount; j++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                long subKey = reader.ReadInt64();
                if (subKey == 1)
                {
                    isEc2Key = reader.ReadInt32() == (int)CoseKeyType.Ec2;
                }
                else if (subKey == 3)
                {
                    reader.SkipValue();
                }
                else if (subKey == -1)
                {
                    isP256Curve = reader.ReadInt32() == (int)CoseEcCurve.P256;
                }
                else if (subKey == -2)
                {
                    x = reader.ReadByteString();
                }
                else if (subKey == -3)
                {
                    y = reader.ReadByteString();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            if (isEc2Key != true || isP256Curve != true)
            {
                throw new Ctap2DataException(
                    "ESP256-split-ARKG test helper public-key components must be EC2 P-256 keys.");
            }

            if (x is null || y is null || x.Length != 32 || y.Length != 32)
            {
                throw new Ctap2DataException(
                    "previewSign EC2 point coordinates must be 32 bytes each.");
            }

            byte[] sec1 = new byte[65];
            sec1[0] = 0x04;
            Buffer.BlockCopy(x, 0, sec1, 1, 32);
            Buffer.BlockCopy(y, 0, sec1, 33, 32);
            return sec1;
        }
    }
}

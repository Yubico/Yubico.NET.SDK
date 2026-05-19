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
using System.Linq;
using System.Reflection;
using Xunit;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.TestUtilities.Fido2;

namespace Yubico.YubiKey.Fido2
{
    public class PreviewSignExtensionTests
    {
        // ------------------------------------------------------------------
        // CBOR encoder tests for the input maps
        // ------------------------------------------------------------------

        [Fact]
        public void EncodeGenerateKeyInput_WritesAlgorithmsAndFlags_AsCanonicalCbor()
        {
            // Expected CBOR map: {3:[-65539], 4:1}.
            // Definite-length 2-entry map = 0xA2; key 3 = 0x03; array(1) = 0x81;
            // -65539 (CBOR negative int encoding: 0x3A 0x00 0x01 0x00 0x02);
            // key 4 = 0x04; value 1 = 0x01.
            byte[] expected = { 0xA2, 0x03, 0x81, 0x3A, 0x00, 0x01, 0x00, 0x02, 0x04, 0x01 };

            byte[] actual = PreviewSignExtension.EncodeGenerateKeyInput(
                new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 },
                flags: PreviewSignOptions.RequireUserPresence);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EncodeSignInput_NoArgs_WritesFlatMap()
        {
            byte[] keyHandle = { 0xAA, 0xBB };
            byte[] tbs = { 0x11, 0x22, 0x33 };

            // {2: h'AABB', 6: h'112233'} — definite-length 2-entry map.
            byte[] expected =
            {
                0xA2,
                0x02, 0x42, 0xAA, 0xBB,
                0x06, 0x43, 0x11, 0x22, 0x33,
            };

            byte[] actual = PreviewSignExtension.EncodeSignInput(keyHandle, tbs, additionalArgs: null);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EncodeSignInput_WithAdditionalArgs_IncludesKey7()
        {
            byte[] keyHandle = { 0xAA };
            byte[] tbs = { 0x11 };
            byte[] additionalArgs = { 0xCC, 0xDD };

            byte[] actual = PreviewSignExtension.EncodeSignInput(keyHandle, tbs, additionalArgs);

            // Re-decode and assert structure rather than hand-crafting the
            // canonical bytes — the encoder ordering plus map header tag is
            // already covered by the no-args case.
            var reader = new CborReader(actual, CborConformanceMode.Ctap2Canonical);
            Assert.Equal(3, reader.ReadStartMap());
            Assert.Equal(2, reader.ReadInt32());
            Assert.Equal(keyHandle, reader.ReadByteString());
            Assert.Equal(6, reader.ReadInt32());
            Assert.Equal(tbs, reader.ReadByteString());
            Assert.Equal(7, reader.ReadInt32());
            Assert.Equal(additionalArgs, reader.ReadByteString());
            reader.ReadEndMap();
        }

        // ------------------------------------------------------------------
        // CTAP key 6 regression test (the headline of this whole port)
        // ------------------------------------------------------------------

        [Fact]
        public void ParseGenerateKeyFromUnsignedExtensions_KeyAt6()
        {
            byte[] previewSignPayload = BuildSyntheticGeneratedKeyPayload();
            byte[] response = BuildMakeCredentialResponseWithUnsignedExtensions(previewSignPayload);

            var data = new MakeCredentialData(response);

            Assert.NotNull(data.UnsignedExtensionOutputs);
            Assert.True(data.UnsignedExtensionOutputs!.ContainsKey(Extensions.PreviewSign));

            PreviewSignGeneratedKey? generated = data.GetPreviewSignGeneratedKey();
            Assert.NotNull(generated);
            Assert.Equal(CoseAlgorithmIdentifier.ArkgP256Esp256, generated!.DerivedKeyAlgorithm);
            Assert.Equal(65, generated.BlindingPublicKey.Length);
            Assert.Equal(65, generated.KemPublicKey.Length);
        }

        [Fact]
        public void ParseGenerateKeyFromAuthenticatorDataExtensions()
        {
            byte[] previewSignPayload = BuildSyntheticGeneratedKeyPayload();
            byte[] response = BuildMakeCredentialResponseWithSignedExtension(previewSignPayload);

            var data = new MakeCredentialData(response);

            PreviewSignGeneratedKey? generated = data.GetPreviewSignGeneratedKey();

            Assert.NotNull(generated);
            Assert.Equal(CoseAlgorithmIdentifier.ArkgP256Esp256, generated!.DerivedKeyAlgorithm);
            Assert.Equal(65, generated.BlindingPublicKey.Length);
            Assert.Equal(65, generated.KemPublicKey.Length);
        }

        [Fact]
        public void ParseGenerateKeyFromSplitUnsignedAndAuthenticatorDataExtensions()
        {
            byte[] unsignedPayload = BuildSyntheticGeneratedKeyPayload(algorithm: null);
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] response = BuildMakeCredentialResponseWithSplitPreviewSignOutput(unsignedPayload, signedPayload);

            var data = new MakeCredentialData(response);

            PreviewSignGeneratedKey? generated = data.GetPreviewSignGeneratedKey();

            Assert.NotNull(generated);
            Assert.Equal(CoseAlgorithmIdentifier.ArkgP256Esp256, generated!.DerivedKeyAlgorithm);
            Assert.Equal(65, generated.BlindingPublicKey.Length);
            Assert.Equal(65, generated.KemPublicKey.Length);
        }

        [Fact]
        public void ParseGenerateKey_Throws_WhenAlgorithmMissing()
        {
            byte[] previewSignPayload = BuildSyntheticGeneratedKeyPayload(algorithm: null);

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeGeneratedKey(previewSignPayload));
        }

        [Fact]
        public void ParseGenerateKey_Throws_WhenAlgorithmUnsupported()
        {
            byte[] previewSignPayload = BuildSyntheticGeneratedKeyPayload(CoseAlgorithmIdentifier.ES256);

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeGeneratedKey(previewSignPayload));
        }

        [Fact]
        public void ParseGenerateKey_AcceptsMinimalArkgCoseKeyShape()
        {
            byte[] previewSignPayload = BuildSyntheticGeneratedKeyPayload(
                CoseAlgorithmIdentifier.ArkgP256Esp256,
                includeArkgCoseKeyMetadata: false);

            PreviewSignGeneratedKey? generated = PreviewSignExtension.DecodeGeneratedKey(previewSignPayload);

            Assert.NotNull(generated);
            Assert.Equal(65, generated!.BlindingPublicKey.Length);
            Assert.Equal(65, generated.KemPublicKey.Length);
        }

        [Fact]
        public void ParseGenerateKey_AcceptsHardwareArkgCoseMetadata()
        {
            byte[] previewSignPayload = BuildSyntheticGeneratedKeyPayload(
                CoseAlgorithmIdentifier.ArkgP256Esp256,
                arkgKeyType: -65537,
                arkgAlgorithm: (CoseAlgorithmIdentifier)(-65700),
                blindingAlgorithm: (int)CoseAlgorithmIdentifier.ES256,
                kemAlgorithm: -25);

            PreviewSignGeneratedKey? generated = PreviewSignExtension.DecodeGeneratedKey(previewSignPayload);

            Assert.NotNull(generated);
            Assert.Equal(65, generated!.BlindingPublicKey.Length);
            Assert.Equal(65, generated.KemPublicKey.Length);
        }

        [Fact]
        public void ParseGenerateKey_Throws_WhenArkgCoseKeyMetadataConflicts()
        {
            byte[] previewSignPayload = BuildSyntheticGeneratedKeyPayload(
                CoseAlgorithmIdentifier.ArkgP256Esp256,
                arkgAlgorithm: CoseAlgorithmIdentifier.ES256);

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeGeneratedKey(previewSignPayload));
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_ReturnsByteString()
        {
            byte[] sigBytes = { 0x30, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x02 };
            byte[] previewSignAuthDataValue = BuildSignatureMap(sigBytes);

            byte[]? recovered = PreviewSignExtension.DecodeSignature(previewSignAuthDataValue);

            Assert.NotNull(recovered);
            Assert.Equal(sigBytes, recovered);
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_ReturnsNull_WhenValueIsNotMap()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteByteString(new byte[] { 0x30, 0x00 });

            byte[]? recovered = PreviewSignExtension.DecodeSignature(cbor.Encode());

            Assert.Null(recovered);
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_ReturnsNull_WhenSignatureMissing()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(7);
            cbor.WriteByteString(new byte[] { 0x01 });
            cbor.WriteEndMap();

            byte[]? recovered = PreviewSignExtension.DecodeSignature(cbor.Encode());

            Assert.Null(recovered);
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_Throws_WhenSignatureValueIsNotByteString()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(6);
            cbor.WriteTextString("not-a-signature");
            cbor.WriteEndMap();

            _ = Assert.Throws<InvalidOperationException>(
                () => PreviewSignExtension.DecodeSignature(cbor.Encode()));
        }

        // ------------------------------------------------------------------
        // Flags rule
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(PreviewSignOptions.RequireUserVerification, 0b101)]
        [InlineData(PreviewSignOptions.RequireUserPresence, 0b001)]
        public void EncodeFlags_ProducesExpectedBits(PreviewSignOptions options, int expectedBits)
        {
            byte[] encoded = PreviewSignExtension.EncodeGenerateKeyInput(
                new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 },
                flags: options);

            int flags = ReadFlagsFromGenerateKeyInput(encoded);
            Assert.Equal(expectedBits, flags);
        }

        [Fact]
        public void PreviewSignOptions_NotFlagsEnum_DoesNotSupportBitwiseOr()
        {
            // ISC-3: PreviewSignOptions represents mutually exclusive modes (UP=1, UV=5),
            // not orthogonal [Flags]. Verify [Flags] attribute is absent.
            Type enumType = typeof(PreviewSignOptions);
            bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
            Assert.False(hasFlagsAttribute);

            // Verify wire-format values are the fixed protocol values (not orthogonal flags).
            // UV=5 would incorrectly overlap with UP=1 if treated as [Flags] (1|4=5),
            // proving these are mutually exclusive modes, not composable flags.
            int upValue = (int)PreviewSignOptions.RequireUserPresence;
            int uvValue = (int)PreviewSignOptions.RequireUserVerification;
            Assert.Equal(1, upValue);
            Assert.Equal(5, uvValue);
        }

        // ------------------------------------------------------------------
        // IsExtensionSupported gates (regressions)
        // ------------------------------------------------------------------

        [Fact]
        public void AddPreviewSignGenerateKey_ThrowsWhenExtensionUnsupported()
        {
            var info = BuildAuthenticatorInfoWithoutPreviewSign();
            var parameters = new MakeCredentialParameters(
                new RelyingParty("rp.example"),
                new UserEntity(new byte[] { 0x01 }) { Name = "u" });

            _ = Assert.Throws<NotSupportedException>(
                () => parameters.AddPreviewSignGenerateKeyExtension(
                    info,
                    new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 }));
        }

        [Fact]
        public void MakeCredentialCborEncode_EmbedsPreviewSignGenerateKeyExtension()
        {
            var info = BuildAuthenticatorInfoWithPreviewSign();
            var parameters = new MakeCredentialParameters(
                new RelyingParty("rp.example"),
                new UserEntity(new byte[] { 0x01 }) { Name = "u" });
            parameters.AddPreviewSignGenerateKeyExtension(
                info,
                new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 },
                PreviewSignOptions.RequireUserVerification);

            byte[] encoded = parameters.CborEncode();
            byte[] previewSignValue = ReadTextExtensionValue(encoded, parameterExtensionsKey: 6, Extensions.PreviewSign);
            int flags = ReadFlagsFromGenerateKeyInput(previewSignValue);

            Assert.Equal((int)PreviewSignOptions.RequireUserVerification, flags);
        }

        [Fact]
        public void AddPreviewSignGenerateKey_ThrowsWhenAlgorithmListEmpty()
        {
            var info = BuildAuthenticatorInfoWithPreviewSign();
            var parameters = new MakeCredentialParameters(
                new RelyingParty("rp.example"),
                new UserEntity(new byte[] { 0x01 }) { Name = "u" });

            _ = Assert.Throws<ArgumentException>(
                () => parameters.AddPreviewSignGenerateKeyExtension(
                    info,
                    Array.Empty<CoseAlgorithmIdentifier>()));
        }

        [Theory]
        [InlineData((PreviewSignOptions)0)]
        [InlineData((PreviewSignOptions)0b100)]
        [InlineData((PreviewSignOptions)0b111)]
        public void AddPreviewSignGenerateKey_ThrowsWhenFlagsInvalid(PreviewSignOptions flags)
        {
            var info = BuildAuthenticatorInfoWithPreviewSign();
            var parameters = new MakeCredentialParameters(
                new RelyingParty("rp.example"),
                new UserEntity(new byte[] { 0x01 }) { Name = "u" });

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => parameters.AddPreviewSignGenerateKeyExtension(
                    info,
                    new[] { CoseAlgorithmIdentifier.ArkgP256Esp256 },
                    flags));
        }

        [Fact]
        public void AddPreviewSignExtension_ThrowsWhenAllowListEmpty()
        {
            var parameters = new GetAssertionParameters(
                new RelyingParty("rp.example"),
                new byte[32]);

            PreviewSignDerivedKey derivedKey = BuildDerivedKeyFixture();
            byte[] tbs = new byte[32];

            var ex = Assert.Throws<InvalidOperationException>(
                () => parameters.AddPreviewSignExtension(
                    derivedKey.DeviceKeyHandle,
                    derivedKey.ArkgKeyHandle,
                    derivedKey.Context,
                    tbs));

            Assert.Contains("AllowCredential", ex.Message);
        }

        [Fact]
        public void AddPreviewSignExtension_ThrowsWhenTbsIsNotSha256Digest()
        {
            var parameters = new GetAssertionParameters(
                new RelyingParty("rp.example"),
                new byte[32]);
            PreviewSignDerivedKey derivedKey = BuildDerivedKeyFixture();
            parameters.AllowCredential(new CredentialId { Id = derivedKey.DeviceKeyHandle.ToArray() });

            _ = Assert.Throws<ArgumentException>(
                () => parameters.AddPreviewSignExtension(
                    derivedKey.DeviceKeyHandle,
                    derivedKey.ArkgKeyHandle,
                    derivedKey.Context,
                    new byte[31]));
        }

        [Fact]
        public void GetAssertionCborEncode_EmbedsPreviewSignSignExtension()
        {
            var parameters = new GetAssertionParameters(
                new RelyingParty("rp.example"),
                new byte[32]);
            PreviewSignDerivedKey derivedKey = BuildDerivedKeyFixture();
            byte[] tbs = Enumerable.Repeat((byte)0xA5, 32).ToArray();
            parameters.AllowCredential(new CredentialId { Id = derivedKey.DeviceKeyHandle.ToArray() });

            parameters.AddPreviewSignExtension(
                derivedKey.DeviceKeyHandle,
                derivedKey.ArkgKeyHandle,
                derivedKey.Context,
                tbs);

            byte[] encoded = parameters.CborEncode();
            byte[] previewSignValue = ReadTextExtensionValue(encoded, parameterExtensionsKey: 4, Extensions.PreviewSign);

            var reader = new CborReader(previewSignValue, CborConformanceMode.Ctap2Canonical);
            Assert.Equal(3, reader.ReadStartMap());
            Assert.Equal(2, reader.ReadInt32());
            Assert.Equal(derivedKey.DeviceKeyHandle.ToArray(), reader.ReadByteString());
            Assert.Equal(6, reader.ReadInt32());
            Assert.Equal(tbs, reader.ReadByteString());
            Assert.Equal(7, reader.ReadInt32());

            var argsReader = new CborReader(reader.ReadByteString(), CborConformanceMode.Ctap2Canonical);
            Assert.Equal(3, argsReader.ReadStartMap());
            Assert.Equal(3, argsReader.ReadInt32());
            Assert.Equal((int)CoseAlgorithmIdentifier.ArkgP256Esp256, argsReader.ReadInt32());
            Assert.Equal(-1, argsReader.ReadInt32());
            Assert.Equal(derivedKey.ArkgKeyHandle.ToArray(), argsReader.ReadByteString());
            Assert.Equal(-2, argsReader.ReadInt32());
            Assert.Equal(derivedKey.Context.ToArray(), argsReader.ReadByteString());
            argsReader.ReadEndMap();
            reader.ReadEndMap();
        }

        // ==================================================================
        // Helpers
        // ==================================================================

        private static int ReadFlagsFromGenerateKeyInput(byte[] encoded)
        {
            var reader = new CborReader(encoded, CborConformanceMode.Ctap2Canonical);
            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;
            int flags = -1;
            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = reader.ReadInt32();
                if (key == 4)
                {
                    flags = reader.ReadInt32();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();
            return flags;
        }

        private static byte[] ReadTextExtensionValue(
            byte[] encodedParameters,
            int parameterExtensionsKey,
            string extensionName)
        {
            var reader = new CborReader(encodedParameters, CborConformanceMode.Ctap2Canonical);
            int? entries = reader.ReadStartMap();
            int count = entries ?? int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (reader.PeekState() == CborReaderState.EndMap)
                {
                    break;
                }

                int key = reader.ReadInt32();
                if (key != parameterExtensionsKey)
                {
                    reader.SkipValue();
                    continue;
                }

                int? extensionEntries = reader.ReadStartMap();
                int extensionCount = extensionEntries ?? int.MaxValue;
                for (int j = 0; j < extensionCount; j++)
                {
                    if (reader.PeekState() == CborReaderState.EndMap)
                    {
                        break;
                    }

                    string currentName = reader.ReadTextString();
                    if (currentName == extensionName)
                    {
                        return reader.ReadEncodedValue().ToArray();
                    }

                    reader.SkipValue();
                }

                reader.ReadEndMap();
            }

            throw new InvalidOperationException("Extension not found.");
        }

        private static byte[] BuildSignatureMap(byte[] sig)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(6);
            cbor.WriteByteString(sig);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildGeneratedKeyAlgorithmPayload()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(3);
            cbor.WriteInt32((int)CoseAlgorithmIdentifier.ArkgP256Esp256);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        // Builds a minimal-but-valid previewSign generated-key payload:
        //   { 3: ArkgP256Esp256, 7: <inner attestation object> }
        private static byte[] BuildSyntheticGeneratedKeyPayload(
            CoseAlgorithmIdentifier? algorithm = CoseAlgorithmIdentifier.ArkgP256Esp256,
            bool includeArkgCoseKeyMetadata = true,
            int arkgKeyType = (int)CoseKeyType.Ec2,
            CoseAlgorithmIdentifier arkgAlgorithm = CoseAlgorithmIdentifier.ArkgP256Esp256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.Esp256,
            int kemAlgorithm = (int)CoseAlgorithmIdentifier.Esp256)
        {
            byte[] innerAttestation = BuildInnerAttestationObject(
                includeArkgCoseKeyMetadata,
                arkgKeyType,
                arkgAlgorithm,
                blindingAlgorithm,
                kemAlgorithm);

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(algorithm.HasValue ? 2 : 1);
            if (algorithm.HasValue)
            {
                cbor.WriteInt32(3);
                cbor.WriteInt32((int)algorithm.Value);
            }

            cbor.WriteInt32(7);
            cbor.WriteByteString(innerAttestation);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildInnerAttestationObject(
            bool includeArkgCoseKeyMetadata = true,
            int arkgKeyType = (int)CoseKeyType.Ec2,
            CoseAlgorithmIdentifier arkgAlgorithm = CoseAlgorithmIdentifier.ArkgP256Esp256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.Esp256,
            int kemAlgorithm = (int)CoseAlgorithmIdentifier.Esp256)
        {
            byte[] authData = BuildSyntheticAuthDataWithArkgCoseKey(
                includeArkgCoseKeyMetadata,
                arkgKeyType,
                arkgAlgorithm,
                blindingAlgorithm,
                kemAlgorithm);
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(2);
            cbor.WriteInt32(1);
            cbor.WriteTextString("packed");
            cbor.WriteInt32(2);
            cbor.WriteByteString(authData);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        // Build authenticator-data: 32-byte rpIdHash || flags=0x40 || signCount(0)
        // || AAGUID(16) || credIdLen(2) || credId(L) || COSE-encoded ARKG seed.
        private static byte[] BuildSyntheticAuthDataWithArkgCoseKey(
            bool includeArkgCoseKeyMetadata = true,
            int arkgKeyType = (int)CoseKeyType.Ec2,
            CoseAlgorithmIdentifier arkgAlgorithm = CoseAlgorithmIdentifier.ArkgP256Esp256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.Esp256,
            int kemAlgorithm = (int)CoseAlgorithmIdentifier.Esp256)
        {
            byte[] rpIdHash = new byte[32];
            byte flags = 0x40; // AT bit
            byte[] signCount = { 0, 0, 0, 0 };
            byte[] aaguid = new byte[16];
            byte[] credId = { 0xDE, 0xAD, 0xBE, 0xEF };
            byte[] cose = BuildArkgCoseKey(
                includeArkgCoseKeyMetadata,
                arkgKeyType,
                arkgAlgorithm,
                blindingAlgorithm,
                kemAlgorithm);

            int credIdLen = credId.Length;
            byte[] result = new byte[32 + 1 + 4 + 16 + 2 + credIdLen + cose.Length];
            int offset = 0;
            Array.Copy(rpIdHash, 0, result, offset, 32);
            offset += 32;
            result[offset++] = flags;
            Array.Copy(signCount, 0, result, offset, 4);
            offset += 4;
            Array.Copy(aaguid, 0, result, offset, 16);
            offset += 16;
            result[offset++] = (byte)((credIdLen >> 8) & 0xFF);
            result[offset++] = (byte)(credIdLen & 0xFF);
            Array.Copy(credId, 0, result, offset, credIdLen);
            offset += credIdLen;
            Array.Copy(cose, 0, result, offset, cose.Length);
            return result;
        }

        // ARKG-P256 COSE key: { 1: EC2, 3: ARKG-P256, -1: pkBl_ec2, -2: pkKem_ec2 }
        private static byte[] BuildArkgCoseKey(
            bool includeMetadata = true,
            int arkgKeyType = (int)CoseKeyType.Ec2,
            CoseAlgorithmIdentifier arkgAlgorithm = CoseAlgorithmIdentifier.ArkgP256Esp256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.Esp256,
            int kemAlgorithm = (int)CoseAlgorithmIdentifier.Esp256)
        {
            byte[] x = Enumerable.Repeat((byte)0xAB, 32).ToArray();
            byte[] y = Enumerable.Repeat((byte)0xCD, 32).ToArray();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(includeMetadata ? 4 : 2);
            if (includeMetadata)
            {
                cbor.WriteInt32(1);
                cbor.WriteInt32(arkgKeyType);
                cbor.WriteInt32(3);
                cbor.WriteInt32((int)arkgAlgorithm);
            }

            cbor.WriteInt32(-1);
            WriteEc2Submap(cbor, x, y, includeMetadata, blindingAlgorithm);

            cbor.WriteInt32(-2);
            WriteEc2Submap(cbor, x, y, includeMetadata, kemAlgorithm);

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void WriteEc2Submap(
            CborWriter cbor,
            byte[] x,
            byte[] y,
            bool includeMetadata = true,
            int algorithm = (int)CoseAlgorithmIdentifier.Esp256)
        {
            cbor.WriteStartMap(includeMetadata ? 5 : 2);
            if (includeMetadata)
            {
                cbor.WriteInt32(1);
                cbor.WriteInt32((int)CoseKeyType.Ec2);
                cbor.WriteInt32(3);
                cbor.WriteInt32(algorithm);
                cbor.WriteInt32(-1);
                cbor.WriteInt32((int)CoseEcCurve.P256);
            }

            cbor.WriteInt32(-2);
            cbor.WriteByteString(x);
            cbor.WriteInt32(-3);
            cbor.WriteByteString(y);
            cbor.WriteEndMap();
        }

        // Build a synthetic MakeCredential response containing CTAP key 6
        // (UnsignedExtensionOutputs) with the previewSign payload.
        private static byte[] BuildMakeCredentialResponseWithUnsignedExtensions(byte[] previewSignPayload)
        {
            byte[] hostAuthData = BuildAuthDataWithEs256CredentialPublicKey();

            return BuildMakeCredentialResponse(hostAuthData, includeUnsignedExtensions: true, previewSignPayload);
        }

        // Build a synthetic MakeCredential response containing authenticator-data
        // extension outputs with the previewSign payload.
        private static byte[] BuildMakeCredentialResponseWithSignedExtension(byte[] previewSignPayload)
        {
            byte[] hostAuthData = BuildAuthDataWithEs256CredentialPublicKey(previewSignPayload);

            return BuildMakeCredentialResponse(hostAuthData, includeUnsignedExtensions: false, previewSignPayload);
        }

        private static byte[] BuildMakeCredentialResponseWithSplitPreviewSignOutput(
            byte[] unsignedPayload,
            byte[] signedPayload)
        {
            byte[] hostAuthData = BuildAuthDataWithEs256CredentialPublicKey(signedPayload);

            return BuildMakeCredentialResponse(hostAuthData, includeUnsignedExtensions: true, unsignedPayload);
        }

        private static byte[] BuildMakeCredentialResponse(
            byte[] hostAuthData,
            bool includeUnsignedExtensions,
            byte[] previewSignPayload)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(includeUnsignedExtensions ? 4 : 3);

            cbor.WriteInt32(1);
            cbor.WriteTextString("packed");

            cbor.WriteInt32(2);
            cbor.WriteByteString(hostAuthData);

            cbor.WriteInt32(3);
            cbor.WriteStartMap(2);
            cbor.WriteTextString("alg");
            cbor.WriteInt32(-7);
            cbor.WriteTextString("sig");
            cbor.WriteByteString(new byte[] { 0x01 });
            cbor.WriteEndMap();

            if (includeUnsignedExtensions)
            {
                cbor.WriteInt32(6);
                cbor.WriteStartMap(1);
                cbor.WriteTextString(Extensions.PreviewSign);
                cbor.WriteEncodedValue(previewSignPayload);
                cbor.WriteEndMap();
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        // The host AuthenticatorData ctor requires an Ec2 COSE credential public
        // key (any key works — we just need MakeCredentialData to construct).
        private static byte[] BuildAuthDataWithEs256CredentialPublicKey(byte[]? previewSignPayload = null)
        {
            byte[] x = Enumerable.Repeat((byte)0x11, 32).ToArray();
            byte[] y = Enumerable.Repeat((byte)0x22, 32).ToArray();

            var coseKey = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            coseKey.WriteStartMap(5);
            coseKey.WriteInt32(1);  // kty
            coseKey.WriteInt32(2);  // EC2
            coseKey.WriteInt32(3);  // alg
            coseKey.WriteInt32(-7); // ES256
            coseKey.WriteInt32(-1); // crv
            coseKey.WriteInt32(1);  // P-256
            coseKey.WriteInt32(-2); // x
            coseKey.WriteByteString(x);
            coseKey.WriteInt32(-3); // y
            coseKey.WriteByteString(y);
            coseKey.WriteEndMap();
            byte[] coseEncoded = coseKey.Encode();

            byte[] extensionBytes = Array.Empty<byte>();
            if (previewSignPayload is not null)
            {
                var extensions = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
                extensions.WriteStartMap(1);
                extensions.WriteTextString(Extensions.PreviewSign);
                extensions.WriteEncodedValue(previewSignPayload);
                extensions.WriteEndMap();
                extensionBytes = extensions.Encode();
            }

            byte[] credId = { 0xCA, 0xFE };
            byte[] result = new byte[32 + 1 + 4 + 16 + 2 + credId.Length + coseEncoded.Length + extensionBytes.Length];
            int offset = 0;
            offset += 32; // rpIdHash zeros
            result[offset++] = previewSignPayload is null ? (byte)0x40 : (byte)0xC0; // AT flag, optionally ED flag
            offset += 4; // signCount zeros
            offset += 16; // AAGUID zeros
            result[offset++] = 0x00;
            result[offset++] = (byte)credId.Length;
            Array.Copy(credId, 0, result, offset, credId.Length);
            offset += credId.Length;
            Array.Copy(coseEncoded, 0, result, offset, coseEncoded.Length);
            offset += coseEncoded.Length;
            Array.Copy(extensionBytes, 0, result, offset, extensionBytes.Length);
            return result;
        }

        // Builds a synthetic AuthenticatorInfo with NO previewSign in its
        // Extensions list.
        private static AuthenticatorInfo BuildAuthenticatorInfoWithoutPreviewSign() =>
            BuildAuthenticatorInfo("credBlob");

        private static AuthenticatorInfo BuildAuthenticatorInfoWithPreviewSign() =>
            BuildAuthenticatorInfo(Extensions.PreviewSign);

        private static AuthenticatorInfo BuildAuthenticatorInfo(string extensionName)
        {
            byte[] aaguid = new byte[16];
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);

            cbor.WriteInt32(1);
            cbor.WriteStartArray(1);
            cbor.WriteTextString("FIDO_2_1");
            cbor.WriteEndArray();

            cbor.WriteInt32(2);
            cbor.WriteStartArray(1);
            cbor.WriteTextString(extensionName);
            cbor.WriteEndArray();

            cbor.WriteInt32(3);
            cbor.WriteByteString(aaguid);

            cbor.WriteEndMap();
            return new AuthenticatorInfo(cbor.Encode());
        }

        private static byte[] BuildSec1Generator()
        {
            // Fixed P-256 generator — valid SEC1 point used as a placeholder
            // for tests that only need a syntactically valid PublicKey.
            return new byte[]
            {
                0x04,
                0x6B, 0x17, 0xD1, 0xF2, 0xE1, 0x2C, 0x42, 0x47,
                0xF8, 0xBC, 0xE6, 0xE5, 0x63, 0xA4, 0x40, 0xF2,
                0x77, 0x03, 0x7D, 0x81, 0x2D, 0xEB, 0x33, 0xA0,
                0xF4, 0xA1, 0x39, 0x45, 0xD8, 0x98, 0xC2, 0x96,
                0x4F, 0xE3, 0x42, 0xE2, 0xFE, 0x1A, 0x7F, 0x9B,
                0x8E, 0xE7, 0xEB, 0x4A, 0x7C, 0x0F, 0x9E, 0x16,
                0x2B, 0xCE, 0x33, 0x57, 0x6B, 0x31, 0x5E, 0xCE,
                0xCB, 0xB6, 0x40, 0x68, 0x37, 0xBF, 0x51, 0xF5,
            };
        }

        // PreviewSignDerivedKey has an internal ctor — invoke via reflection
        // so the test can build a fixture without running the full ARKG path.
        private static PreviewSignDerivedKey BuildDerivedKeyFixture()
        {
            return (PreviewSignDerivedKey)Activator.CreateInstance(
                typeof(PreviewSignDerivedKey),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    (ReadOnlyMemory<byte>)BuildSec1Generator(),
                    (ReadOnlyMemory<byte>)new byte[] { 0x01 },
                    (ReadOnlyMemory<byte>)new byte[] { 0x02 },
                    (ReadOnlyMemory<byte>)new byte[] { 0x03 },
                },
                culture: null)!;
        }
    }
}

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
using Yubico.YubiKey.Fido2.Cbor;
using Yubico.YubiKey.Fido2.Cose;
using Yubico.YubiKey.TestUtilities.Fido2;

namespace Yubico.YubiKey.Fido2
{
    public class PreviewSignExtensionTests
    {
        private const int CoseKeyTypeArkgPub = -65537;
        private const int CoseAlgorithmArkgP256 = -65700;

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
                new[] { PreviewSignParametersExtensions.ArkgP256ESP256 },
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

            byte[] expected =
            {
                0xA3,
                0x02, 0x41, 0xAA,
                0x06, 0x41, 0x11,
                0x07, 0x42, 0xCC, 0xDD,
            };
            Assert.Equal(expected, actual);
        }

        // ------------------------------------------------------------------
        // CTAP key 6 regression test
        // ------------------------------------------------------------------

        [Fact]
        public void ParseGenerateKeyFromUnsignedExtensions_KeyAt6()
        {
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation();
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] response = BuildMakeCredentialResponseWithPreviewSignOutputs(unsignedPayload, signedPayload);

            var data = new MakeCredentialData(response);

            Assert.NotNull(data.UnsignedExtensionOutputs);
            Assert.True(data.UnsignedExtensionOutputs!.ContainsKey(PreviewSignParametersExtensions.ExtensionName));
            var signedMap = new CborMap<int>(signedPayload);
            var unsignedMap = new CborMap<int>(unsignedPayload);
            Assert.True(signedMap.Contains(3));
            Assert.True(unsignedMap.Contains(7));

            PreviewSignGeneratedKey? generated = data.GetPreviewSignGeneratedKey();
            Assert.NotNull(generated);
            Assert.Equal(PreviewSignParametersExtensions.ArkgP256ESP256, generated!.Algorithm);
            Assert.NotEmpty(generated.PublicKey.Span.ToArray());
        }

        [Fact]
        public void UnsignedExtensionOutputs_PreservesOpaqueUnknownExtensionValue()
        {
            byte[] opaqueValue = { 0xA0 };
            byte[] response = BuildMakeCredentialResponseWithCustomUnsignedExtension(
                "unknownExtension", WriteCborEmptyMap);

            var data = new MakeCredentialData(response);

            Assert.NotNull(data.UnsignedExtensionOutputs);
            Assert.True(data.UnsignedExtensionOutputs!.TryGetValue("unknownExtension", out ReadOnlyMemory<byte> value));
            Assert.Equal(opaqueValue, value.ToArray());
        }

        [Fact]
        public void UnsignedExtensionOutputs_PreservesArrayExtensionValue()
        {
            byte[] arrayValue = { 0x82, 0x01, 0xA0 };
            byte[] response = BuildMakeCredentialResponseWithCustomUnsignedExtension(
                "futureExtension", WriteCborArrayWithMapElement);

            var data = new MakeCredentialData(response);

            Assert.NotNull(data.UnsignedExtensionOutputs);
            Assert.True(data.UnsignedExtensionOutputs!.TryGetValue("futureExtension", out ReadOnlyMemory<byte> value));
            Assert.Equal(arrayValue, value.ToArray());
        }

        [Fact]
        public void ParseGenerateKeyFromAuthenticatorDataExtensions_ReturnsNull_WhenUnsignedOutputMissing()
        {
            byte[] previewSignPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] response = BuildMakeCredentialResponseWithSignedExtension(previewSignPayload);

            var data = new MakeCredentialData(response);

            Assert.Null(data.GetPreviewSignGeneratedKey());
        }

        [Fact]
        public void ParseGenerateKeyFromAuthenticatorDataExtensions_Throws_WhenOutputIsNotMap()
        {
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation();
            byte[] signedPayload = BuildByteString(new byte[] { 0x01 });
            byte[] response = BuildMakeCredentialResponseWithPreviewSignOutputs(unsignedPayload, signedPayload);

            var data = new MakeCredentialData(response);

            _ = Assert.Throws<Ctap2DataException>(
                () => data.GetPreviewSignGeneratedKey());
        }

        [Fact]
        public void ParseGenerateKey_ReturnsNull_WhenExtensionAbsent()
        {
            byte[] response = BuildMakeCredentialResponse(
                BuildAuthDataWithEs256CredentialPublicKey(),
                includeUnsignedExtensions: false,
                previewSignPayload: Array.Empty<byte>());

            var data = new MakeCredentialData(response);

            Assert.Null(data.GetPreviewSignGeneratedKey());
        }

        [Fact]
        public void ParseGenerateKeyFromUnsignedExtensions_ReturnsNull_WhenSignedOutputMissing()
        {
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation();
            byte[] response = BuildMakeCredentialResponseWithUnsignedExtensions(unsignedPayload);

            var data = new MakeCredentialData(response);

            Assert.Null(data.GetPreviewSignGeneratedKey());
        }

        [Fact]
        public void ParseGenerateKeyFromUnsignedExtensions_Throws_WhenSignedAlgorithmOutputIsNotMap()
        {
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation();
            byte[] signedPayload = BuildByteString(new byte[] { 0x01 });
            byte[] response = BuildMakeCredentialResponseWithPreviewSignOutputs(unsignedPayload, signedPayload);

            var data = new MakeCredentialData(response);

            _ = Assert.Throws<Ctap2DataException>(
                () => data.GetPreviewSignGeneratedKey());
        }

        [Fact]
        public void ParseGenerateKey_Throws_WhenAlgorithmMissing()
        {
            byte[] signedPayload = BuildGeneratedKeyPayloadWithoutAlgorithm();
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation();

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload));
        }

        [Fact]
        public void DecodeGeneratedKey_Throws_WhenAlgorithmValueIsMalformed()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(3);
            cbor.WriteTextString("not-an-algorithm");
            cbor.WriteEndMap();
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation();

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeGeneratedKey(cbor.Encode(), unsignedPayload));
        }

        [Fact]
        public void ParseGenerateKey_Throws_WhenOutputIsNotMap()
        {
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] unsignedPayload = BuildByteString(new byte[] { 0x01 });

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload));
        }

        [Fact]
        public void ParseGenerateKey_PreservesAlgorithmValue()
        {
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload(CoseAlgorithmIdentifier.ES256);
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation();

            PreviewSignGeneratedKey? generated = PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload);

            Assert.NotNull(generated);
            Assert.Equal(CoseAlgorithmIdentifier.ES256, generated!.Algorithm);
        }

        [Fact]
        public void DecodeGeneratedKey_IgnoresUnknownOpaqueValues()
        {
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload(includeUnknownOpaqueValue: true);
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation(includeUnknownOpaqueValue: true);

            PreviewSignGeneratedKey? generated = PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload);

            Assert.NotNull(generated);
            Assert.Equal(PreviewSignParametersExtensions.ArkgP256ESP256, generated!.Algorithm);
            Assert.NotEmpty(generated.PublicKey.Span.ToArray());
        }

        [Fact]
        public void ParseGenerateKey_PreservesRawArkgPubCoseKeyWithoutParsingCoseKey()
        {
            byte[] expectedCoseKey = BuildArkgCoseKey();
            _ = Assert.Throws<NotSupportedException>(
                () => CoseKey.Create(expectedCoseKey, out _));
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation(
                credentialPublicKeyCose: expectedCoseKey);

            var generated = PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload);

            Assert.NotNull(generated);
            Assert.Equal(expectedCoseKey, generated!.PublicKey.ToArray());
        }

        [Fact]
        public void ParseGenerateKey_AcceptsHardwareArkgCoseMetadata()
        {
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation(
                arkgKeyType: -65537,
                arkgAlgorithm: -65700,
                blindingAlgorithm: (int)CoseAlgorithmIdentifier.ES256,
                kemAlgorithm: -25);

            var generated = PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload);

            Assert.NotNull(generated);
            Assert.NotEmpty(generated!.PublicKey.Span.ToArray());
        }

        [Fact]
        public void DerivePublicKey_Throws_WhenArkgCoseKeyMetadataConflicts()
        {
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation(
                arkgAlgorithm: CoseAlgorithmArkgP256 - 1);
            PreviewSignGeneratedKey? generated = PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload);

            _ = Assert.Throws<Ctap2DataException>(
                () => generated!.DerivePublicKey(new byte[32], new byte[] { 0x01 }));
        }

        [Fact]
        public void DerivePublicKey_Throws_WhenArkgCoseKeyTypeConflicts()
        {
            byte[] signedPayload = BuildGeneratedKeyAlgorithmPayload();
            byte[] unsignedPayload = BuildGeneratedKeyUnsignedPayloadWithNoneAttestation(
                arkgKeyType: (int)CoseKeyType.Ec2);
            PreviewSignGeneratedKey? generated = PreviewSignExtension.DecodeGeneratedKey(signedPayload, unsignedPayload);

            _ = Assert.Throws<Ctap2DataException>(
                () => generated!.DerivePublicKey(new byte[32], new byte[] { 0x01 }));
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_ReturnsByteString()
        {
            byte[] sigBytes = { 0x30, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x02 };
            byte[] previewSignAuthDataValue = BuildSignatureMap(sigBytes);

            byte[] recovered = PreviewSignExtension.DecodeSignature(previewSignAuthDataValue);

            Assert.Equal(sigBytes, recovered);
        }

        [Fact]
        public void DecodeSignature_IgnoresUnknownOpaqueValues()
        {
            byte[] sigBytes = { 0x30, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x02 };
            byte[] previewSignAuthDataValue = BuildSignatureMap(sigBytes, includeUnknownOpaqueValue: true);

            byte[] recovered = PreviewSignExtension.DecodeSignature(previewSignAuthDataValue);

            Assert.Equal(sigBytes, recovered);
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_Throws_WhenValueIsNotMap()
        {
            byte[] encodedSignatureOutput = BuildByteString(new byte[] { 0x30, 0x00 });

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeSignature(encodedSignatureOutput));
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_Throws_WhenSignatureMissing()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(7);
            cbor.WriteByteString(new byte[] { 0x01 });
            cbor.WriteEndMap();

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeSignature(cbor.Encode()));
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_Throws_WhenSignatureValueIsNotByteString()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(6);
            cbor.WriteTextString("not-a-signature");
            cbor.WriteEndMap();

            _ = Assert.Throws<Ctap2DataException>(
                () => PreviewSignExtension.DecodeSignature(cbor.Encode()));
        }

        [Fact]
        public void GetPreviewSignSignature_ReturnsNull_WhenExtensionAbsent()
        {
            var data = new AuthenticatorData(new byte[37]);

            Assert.Null(data.GetPreviewSignSignature());
        }

        // ------------------------------------------------------------------
        // Flags rule
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(PreviewSignOptions.Unattended, 0b000)]
        [InlineData(PreviewSignOptions.RequireUserVerification, 0b101)]
        [InlineData(PreviewSignOptions.RequireUserPresence, 0b001)]
        public void EncodeFlags_ProducesExpectedBits(PreviewSignOptions options, int expectedBits)
        {
            byte[] encoded = PreviewSignExtension.EncodeGenerateKeyInput(
                new[] { PreviewSignParametersExtensions.ArkgP256ESP256 },
                flags: options);

            int flags = ReadFlagsFromGenerateKeyInput(encoded);
            Assert.Equal(expectedBits, flags);
        }

        [Fact]
        public void PreviewSignOptions_NotFlagsEnum_DoesNotSupportBitwiseOr()
        {
            Type enumType = typeof(PreviewSignOptions);
            bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
            Assert.False(hasFlagsAttribute);

            int unattendedValue = (int)PreviewSignOptions.Unattended;
            int upValue = (int)PreviewSignOptions.RequireUserPresence;
            int uvValue = (int)PreviewSignOptions.RequireUserVerification;
            Assert.Equal(0, unattendedValue);
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
                    new[] { PreviewSignParametersExtensions.ArkgP256ESP256 }));
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
                new[] { PreviewSignParametersExtensions.ArkgP256ESP256 },
                PreviewSignOptions.RequireUserVerification);

            byte[] encoded = parameters.CborEncode();
            byte[] previewSignValue = ReadTextExtensionValue(encoded, parameterExtensionsKey: 6, PreviewSignParametersExtensions.ExtensionName);
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
        [InlineData((PreviewSignOptions)0b010)]
        [InlineData((PreviewSignOptions)0b011)]
        [InlineData((PreviewSignOptions)0b100)]
        [InlineData((PreviewSignOptions)0b110)]
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
                    new[] { PreviewSignParametersExtensions.ArkgP256ESP256 },
                    flags));
        }

        [Fact]
        public void AddArkgPreviewSignHelper_ThrowsWhenAllowListEmpty()
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
        public void AddArkgPreviewSignHelper_ThrowsWhenDigestIsNotSha256Length()
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
        public void GetAssertionCborEncode_EmbedsArkgPreviewSignHelperArgs()
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
            byte[] previewSignValue = ReadTextExtensionValue(encoded, parameterExtensionsKey: 4, PreviewSignParametersExtensions.ExtensionName);

            var previewSignMap = new CborMap<int>(previewSignValue);
            Assert.Equal(3, previewSignMap.Count);
            Assert.Equal(derivedKey.DeviceKeyHandle.ToArray(), previewSignMap.ReadByteString(2).ToArray());
            Assert.Equal(tbs, previewSignMap.ReadByteString(6).ToArray());

            var argsMap = new CborMap<int>(previewSignMap.ReadByteString(7));
            Assert.Equal(3, argsMap.Count);
            Assert.Equal((int)PreviewSignParametersExtensions.ArkgP256ESP256, argsMap.ReadInt32(3));
            Assert.Equal(derivedKey.ArkgKeyHandle.ToArray(), argsMap.ReadByteString(-1).ToArray());
            Assert.Equal(derivedKey.Context.ToArray(), argsMap.ReadByteString(-2).ToArray());
        }

        // ==================================================================
        // Helpers
        // ==================================================================

        private static int ReadFlagsFromGenerateKeyInput(byte[] encoded)
        {
            var map = new CborMap<int>(encoded);
            return map.ReadInt32(4);
        }

        private static byte[] ReadTextExtensionValue(
            byte[] encodedParameters,
            int parameterExtensionsKey,
            string extensionName)
        {
            var map = new CborMap<int>(encodedParameters);
            if (!map.Contains(parameterExtensionsKey))
            {
                throw new InvalidOperationException("Extension not found.");
            }

            var extensions = map.ReadMap<string>(parameterExtensionsKey);
            if (!extensions.Contains(extensionName))
            {
                throw new InvalidOperationException("Extension not found.");
            }

            return extensions.ReadEncodedValue(extensionName).ToArray();
        }

        private static byte[] BuildSignatureMap(
            byte[] sig,
            bool includeUnknownOpaqueValue = false)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(includeUnknownOpaqueValue ? 2 : 1);
            cbor.WriteInt32(6);
            cbor.WriteByteString(sig);
            if (includeUnknownOpaqueValue)
            {
                WriteUnknownOpaqueEntry(cbor);
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildByteString(byte[] value)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteByteString(value);
            return cbor.Encode();
        }

        private static byte[] BuildGeneratedKeyAlgorithmPayload(
            CoseAlgorithmIdentifier algorithm = PreviewSignParametersExtensions.ArkgP256ESP256,
            bool includeUnknownOpaqueValue = false)
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(includeUnknownOpaqueValue ? 2 : 1);
            cbor.WriteInt32(3);
            cbor.WriteInt32((int)algorithm);
            if (includeUnknownOpaqueValue)
            {
                WriteUnknownOpaqueEntry(cbor);
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] BuildGeneratedKeyPayloadWithoutAlgorithm()
        {
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1);
            cbor.WriteInt32(4);
            cbor.WriteInt32((int)PreviewSignOptions.RequireUserPresence);
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        // Builds the unsigned previewSign generated-key output. The generated
        // key attestation object is synthetic and intentionally uses fmt="none";
        // these tests cover previewSign envelope parsing, not attestation trust.
        private static byte[] BuildGeneratedKeyUnsignedPayloadWithNoneAttestation(
            int arkgKeyType = CoseKeyTypeArkgPub,
            int arkgAlgorithm = CoseAlgorithmArkgP256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.ESP256,
            int? kemAlgorithm = null,
            byte[]? credentialPublicKeyCose = null,
            bool includeUnknownOpaqueValue = false)
        {
            byte[] innerAttestation = BuildGeneratedKeyNoneAttestationObject(
                arkgKeyType,
                arkgAlgorithm,
                blindingAlgorithm,
                kemAlgorithm,
                credentialPublicKeyCose);

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(1 + (includeUnknownOpaqueValue ? 1 : 0));
            cbor.WriteInt32(7);
            cbor.WriteByteString(innerAttestation);
            if (includeUnknownOpaqueValue)
            {
                WriteUnknownOpaqueEntry(cbor);
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void WriteUnknownOpaqueEntry(CborWriter cbor)
        {
            cbor.WriteInt32(99);
            cbor.WriteStartMap(0);
            cbor.WriteEndMap();
        }

        private static byte[] BuildGeneratedKeyNoneAttestationObject(
            int arkgKeyType = CoseKeyTypeArkgPub,
            int arkgAlgorithm = CoseAlgorithmArkgP256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.ESP256,
            int? kemAlgorithm = null,
            byte[]? credentialPublicKeyCose = null)
        {
            byte[] authData = BuildSyntheticAuthDataWithArkgCoseKey(
                arkgKeyType,
                arkgAlgorithm,
                blindingAlgorithm,
                kemAlgorithm,
                credentialPublicKeyCose);
            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(3);
            cbor.WriteInt32(1);
            cbor.WriteTextString("none");
            cbor.WriteInt32(2);
            cbor.WriteByteString(authData);
            cbor.WriteInt32(3);
            cbor.WriteStartMap(0);
            cbor.WriteEndMap();
            cbor.WriteEndMap();
            return cbor.Encode();
        }

        // Build authenticator-data: 32-byte rpIdHash || flags=0x40 || signCount(0)
        // || AAGUID(16) || credIdLen(2) || credId(L) || COSE-encoded ARKG seed.
        private static byte[] BuildSyntheticAuthDataWithArkgCoseKey(
            int arkgKeyType = CoseKeyTypeArkgPub,
            int arkgAlgorithm = CoseAlgorithmArkgP256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.ESP256,
            int? kemAlgorithm = null,
            byte[]? credentialPublicKeyCose = null)
        {
            byte[] rpIdHash = new byte[32];
            byte flags = 0x40; // AT bit
            byte[] signCount = { 0, 0, 0, 0 };
            byte[] aaguid = new byte[16];
            byte[] credId = { 0xDE, 0xAD, 0xBE, 0xEF };
            byte[] cose = credentialPublicKeyCose ??
                BuildArkgCoseKey(
                    arkgKeyType,
                    arkgAlgorithm,
                    blindingAlgorithm,
                    kemAlgorithm);

            return BuildAttestedAuthData(rpIdHash, flags, signCount, aaguid, credId, cose);
        }

        // ARKG-P256 COSE key: { 1: ARKG-pub, 3: ARKG-P256, -1: pkBl_ec2, -2: pkKem_ec2 }
        private static byte[] BuildArkgCoseKey(
            int arkgKeyType = CoseKeyTypeArkgPub,
            int arkgAlgorithm = CoseAlgorithmArkgP256,
            int blindingAlgorithm = (int)CoseAlgorithmIdentifier.ESP256,
            int? kemAlgorithm = null)
        {
            byte[] x = Enumerable.Repeat((byte)0xAB, 32).ToArray();
            byte[] y = Enumerable.Repeat((byte)0xCD, 32).ToArray();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(4);
            cbor.WriteInt32(1);
            cbor.WriteInt32(arkgKeyType);
            cbor.WriteInt32(3);
            cbor.WriteInt32(arkgAlgorithm);

            cbor.WriteInt32(-1);
            WriteEc2Submap(cbor, x, y, blindingAlgorithm);

            cbor.WriteInt32(-2);
            WriteEc2Submap(cbor, x, y, kemAlgorithm);

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void WriteEc2Submap(
            CborWriter cbor,
            byte[] x,
            byte[] y,
            int? algorithm = null)
        {
            int metadataEntries = algorithm.HasValue ? 3 : 2;
            cbor.WriteStartMap(metadataEntries + 2);
            cbor.WriteInt32(1);
            cbor.WriteInt32((int)CoseKeyType.Ec2);
            if (algorithm.HasValue)
            {
                cbor.WriteInt32(3);
                cbor.WriteInt32(algorithm.Value);
            }

            cbor.WriteInt32(-1);
            cbor.WriteInt32((int)CoseEcCurve.P256);

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

        private static byte[] BuildMakeCredentialResponseWithPreviewSignOutputs(
            byte[] unsignedPayload,
            byte[] signedPayload)
        {
            byte[] hostAuthData = BuildAuthDataWithEs256CredentialPublicKey(signedPayload);

            return BuildMakeCredentialResponse(hostAuthData, includeUnsignedExtensions: true, unsignedPayload);
        }

        private static byte[] BuildMakeCredentialResponseWithCustomUnsignedExtension(
            string extensionName,
            Action<CborWriter> writeExtensionValue)
        {
            byte[] hostAuthData = BuildAuthDataWithEs256CredentialPublicKey();

            var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical, convertIndefiniteLengthEncodings: true);
            cbor.WriteStartMap(4);

            cbor.WriteInt32(1);
            cbor.WriteTextString("packed");

            cbor.WriteInt32(2);
            cbor.WriteByteString(hostAuthData);

            cbor.WriteInt32(3);
            cbor.WriteStartMap(2);
            cbor.WriteTextString("alg");
            cbor.WriteInt32(-7);
            cbor.WriteTextString("sig");
            cbor.WriteByteString(SampleDerEncodedEs256Signature());
            cbor.WriteEndMap();

            cbor.WriteInt32(6);
            cbor.WriteStartMap(1);
            cbor.WriteTextString(extensionName);
            writeExtensionValue(cbor);
            cbor.WriteEndMap();

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static void WriteCborEmptyMap(CborWriter cbor)
        {
            cbor.WriteStartMap(0);
            cbor.WriteEndMap();
        }

        private static void WriteCborArrayWithMapElement(CborWriter cbor)
        {
            cbor.WriteStartArray(2);
            cbor.WriteInt32(1);
            cbor.WriteStartMap(0);
            cbor.WriteEndMap();
            cbor.WriteEndArray();
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
            cbor.WriteByteString(SampleDerEncodedEs256Signature());
            cbor.WriteEndMap();

            if (includeUnsignedExtensions)
            {
                cbor.WriteInt32(6);
                cbor.WriteStartMap(1);
                cbor.WriteTextString(PreviewSignParametersExtensions.ExtensionName);
                cbor.WriteEncodedValue(previewSignPayload);
                cbor.WriteEndMap();
            }

            cbor.WriteEndMap();
            return cbor.Encode();
        }

        private static byte[] SampleDerEncodedEs256Signature()
        {
            byte[] signature = new byte[70];
            signature[0] = 0x30;
            signature[1] = 0x44;
            signature[2] = 0x02;
            signature[3] = 0x20;
            Array.Fill(signature, (byte)0x11, 4, 32);
            signature[36] = 0x02;
            signature[37] = 0x20;
            Array.Fill(signature, (byte)0x22, 38, 32);

            return signature;
        }

        // The host AuthenticatorData fixture carries a minimal ES256 credential
        // public key so MakeCredentialData can parse the outer response before
        // previewSign extension handling is exercised.
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
                extensions.WriteTextString(PreviewSignParametersExtensions.ExtensionName);
                extensions.WriteEncodedValue(previewSignPayload);
                extensions.WriteEndMap();
                extensionBytes = extensions.Encode();
            }

            byte[] credId = { 0xCA, 0xFE };
            return BuildAttestedAuthData(
                new byte[32],
                previewSignPayload is null ? (byte)0x40 : (byte)0xC0,
                new byte[4],
                new byte[16],
                credId,
                coseEncoded,
                extensionBytes);
        }

        private static byte[] BuildAttestedAuthData(
            byte[] rpIdHash,
            byte flags,
            byte[] signCount,
            byte[] aaguid,
            byte[] credentialId,
            byte[] credentialPublicKeyCose,
            byte[]? extensions = null)
        {
            extensions ??= Array.Empty<byte>();

            byte[] result = new byte[
                rpIdHash.Length +
                1 +
                signCount.Length +
                aaguid.Length +
                2 +
                credentialId.Length +
                credentialPublicKeyCose.Length +
                extensions.Length];
            int offset = 0;
            Array.Copy(rpIdHash, 0, result, offset, rpIdHash.Length);
            offset += rpIdHash.Length;
            result[offset++] = flags;
            Array.Copy(signCount, 0, result, offset, signCount.Length);
            offset += signCount.Length;
            Array.Copy(aaguid, 0, result, offset, aaguid.Length);
            offset += aaguid.Length;
            result[offset++] = (byte)((credentialId.Length >> 8) & 0xFF);
            result[offset++] = (byte)(credentialId.Length & 0xFF);
            Array.Copy(credentialId, 0, result, offset, credentialId.Length);
            offset += credentialId.Length;
            Array.Copy(credentialPublicKeyCose, 0, result, offset, credentialPublicKeyCose.Length);
            offset += credentialPublicKeyCose.Length;
            Array.Copy(extensions, 0, result, offset, extensions.Length);
            return result;
        }

        // Builds a synthetic AuthenticatorInfo with NO previewSign in its
        // Extensions list.
        private static AuthenticatorInfo BuildAuthenticatorInfoWithoutPreviewSign() =>
            BuildAuthenticatorInfo("credBlob");

        private static AuthenticatorInfo BuildAuthenticatorInfoWithPreviewSign() =>
            BuildAuthenticatorInfo(PreviewSignParametersExtensions.ExtensionName);

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
                    (ReadOnlyMemory<byte>)ArkgP256RegressionVectors.BlindingPublicKey,
                    (ReadOnlyMemory<byte>)new byte[] { 0x01 },
                    (ReadOnlyMemory<byte>)new byte[] { 0x02 },
                    (ReadOnlyMemory<byte>)new byte[] { 0x03 },
                },
                culture: null)!;
        }
    }
}

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
using Xunit;
using Yubico.YubiKey.Fido2.Cose;

namespace Yubico.YubiKey.Fido2
{
    public class PreviewSignExtensionTests
    {
        [Fact]
        public void EncodeGenerateKeyInput_WritesAlgorithmsAndFlags_AsCanonicalCbor()
        {
            // Phase 1: This test will fail with NotImplementedException
            // Expected CBOR: {3:[-9], 4:1} for Esp256 with RequireUserPresence
            // TODO Phase 5: implement encoder and verify byte-for-byte match
            throw new NotImplementedException();
        }

        [Fact]
        public void EncodeSignByCredentialInput_NoArgs_WritesFlatMap()
        {
            // Phase 1: This test will fail with NotImplementedException
            // Expected CBOR: {2:keyHandle, 6:toBeSigned}
            // TODO Phase 5: implement encoder and verify byte-for-byte match
            throw new NotImplementedException();
        }

        [Fact]
        public void EncodeSignByCredentialInput_WithAdditionalArgs_IncludesKey7()
        {
            // Phase 1: This test will fail with NotImplementedException
            // Expected CBOR: {2:keyHandle, 6:toBeSigned, 7:additionalArgs}
            // TODO Phase 5: implement encoder and verify byte-for-byte match
            throw new NotImplementedException();
        }

        [Fact]
        public void ParseGenerateKeyFromUnsignedExtensions_KeyAt6()
        {
            // Phase 1: This test will fail with NotImplementedException
            // CRITICAL REGRESSION TEST: Verifies CTAP response key 6, not 8
            // Matches yubikit-swift release/1.3.0, Java, Python SDKs
            // Build synthetic CTAP MakeCredential response with key 6 containing previewSign payload
            // Assert that MakeCredentialData.GetPreviewSignGeneratedKey() returns non-null
            // TODO Phase 5: implement parser and verify against key 6
            throw new NotImplementedException();
        }

        [Fact]
        public void ParseSignatureFromExtensionOutput_ReturnsByteString()
        {
            // Phase 1: This test will fail with NotImplementedException
            // Verifies key 6 in GetAssertion auth data extension output
            // TODO Phase 5: implement parser and verify
            throw new NotImplementedException();
        }

        [Fact]
        public void Flags_DerivedFromRequireUv_TrueProduces0b101()
        {
            // Phase 1: This test will fail with NotImplementedException
            // requireUv == true should produce flags 0b101 (RequireUserVerification)
            // TODO Phase 5: verify flag encoding
            throw new NotImplementedException();
        }

        [Fact]
        public void Flags_DerivedFromRequireUv_FalseProduces0b001()
        {
            // Phase 1: This test will fail with NotImplementedException
            // requireUv == false should produce flags 0b001 (RequireUserPresence)
            // TODO Phase 5: verify flag encoding
            throw new NotImplementedException();
        }

        [Fact]
        public void AddPreviewSignGenerateKey_ThrowsWhenExtensionUnsupported()
        {
            // Phase 1: This test will fail with NotImplementedException
            // Create AuthenticatorInfo whose Extensions does not contain "previewSign"
            // Call MakeCredentialParameters.AddPreviewSignGenerateKeyExtension
            // Assert NotSupportedException is thrown
            // TODO Phase 5: implement validation and verify exception
            throw new NotImplementedException();
        }

        [Fact]
        public void AddPreviewSignByCredential_ThrowsWhenExtensionUnsupported()
        {
            // Phase 1: This test will fail with NotImplementedException
            // CRITICAL REGRESSION TEST: Guards against Alan's missing-validation bug
            // Create AuthenticatorInfo whose Extensions does not contain "previewSign"
            // Call GetAssertionParameters.AddPreviewSignByCredentialExtension
            // Assert NotSupportedException is thrown
            // TODO Phase 5: implement validation and verify exception
            throw new NotImplementedException();
        }
    }
}

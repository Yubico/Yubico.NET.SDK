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

namespace Yubico.YubiKey.Fido2.Arkg
{
    public class ArkgP256Tests
    {
        [Fact]
        public void DerivePublicKey_AgainstRustKAT_ProducesExpectedPublicKey()
        {
            // Phase 1: Placeholder test - will fail with NotImplementedException
            // TODO Phase 4: Replace with real KAT extracted from cnh-authenticator-rs-extension/.../arkg.rs
            // Known-answer test verifying ARKG-P256 derivation against reference implementation

            byte[] pkBl = new byte[65];      // TODO: replace with real KAT blinding public key
            byte[] pkKem = new byte[65];     // TODO: replace with real KAT KEM public key
            byte[] ikm = new byte[32];       // TODO: replace with real KAT input keying material
            byte[] ctx = new byte[16];       // TODO: replace with real KAT context

            // This will throw NotImplementedException in Phase 1
            var (derivedPk, arkgKeyHandle) = ArkgP256.DerivePublicKey(pkBl, pkKem, ikm, ctx);

            // TODO Phase 4: Assert derivedPk matches expected KAT output
            // TODO Phase 4: Assert arkgKeyHandle matches expected KAT output
            Assert.NotNull(derivedPk);
            Assert.NotNull(arkgKeyHandle);
        }

        [Fact]
        public void DerivePublicKey_DifferentIkm_ProducesDifferentKeys()
        {
            // Phase 1: Placeholder test - will fail with NotImplementedException
            // TODO Phase 4: Replace with real test data
            // Verifies that different IKM values produce different derived keys

            byte[] pkBl = new byte[65];      // TODO: replace with real test data
            byte[] pkKem = new byte[65];     // TODO: replace with real test data
            byte[] ikm1 = new byte[32];      // TODO: replace with real test data
            byte[] ikm2 = new byte[32];      // TODO: replace with real test data (different from ikm1)
            byte[] ctx = new byte[16];       // TODO: replace with real test data

            // These will throw NotImplementedException in Phase 1
            var (derivedPk1, _) = ArkgP256.DerivePublicKey(pkBl, pkKem, ikm1, ctx);
            var (derivedPk2, _) = ArkgP256.DerivePublicKey(pkBl, pkKem, ikm2, ctx);

            // TODO Phase 4: Assert derivedPk1 != derivedPk2
            Assert.NotEqual(derivedPk1, derivedPk2);
        }

        [Fact]
        public void DerivePublicKey_DifferentCtx_ProducesDifferentKeys()
        {
            // Phase 1: Placeholder test - will fail with NotImplementedException
            // TODO Phase 4: Replace with real test data
            // Verifies that different context values produce different derived keys

            byte[] pkBl = new byte[65];      // TODO: replace with real test data
            byte[] pkKem = new byte[65];     // TODO: replace with real test data
            byte[] ikm = new byte[32];       // TODO: replace with real test data
            byte[] ctx1 = new byte[16];      // TODO: replace with real test data
            byte[] ctx2 = new byte[16];      // TODO: replace with real test data (different from ctx1)

            // These will throw NotImplementedException in Phase 1
            var (derivedPk1, _) = ArkgP256.DerivePublicKey(pkBl, pkKem, ikm, ctx1);
            var (derivedPk2, _) = ArkgP256.DerivePublicKey(pkBl, pkKem, ikm, ctx2);

            // TODO Phase 4: Assert derivedPk1 != derivedPk2
            Assert.NotEqual(derivedPk1, derivedPk2);
        }
    }
}

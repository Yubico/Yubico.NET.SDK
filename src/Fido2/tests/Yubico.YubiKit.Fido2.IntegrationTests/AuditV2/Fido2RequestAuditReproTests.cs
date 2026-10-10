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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Fido2.IntegrationTests.AuditV2;

public sealed class Fido2RequestAuditReproTests
{
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    [Trait("Audit", "YESDK-1618")]
    public async Task YESDK1618_MakeCredentialFalseUserPresenceReturnsInvalidOption(YubiKeyTestState state) =>
        await state.WithFidoSessionAsync(async session =>
        {
            CtapException error = await Assert.ThrowsAsync<CtapException>(() =>
                session.MakeCredentialAsync(
                    new byte[32],
                    new PublicKeyCredentialRpEntity("example.com"),
                    new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "audit", "Audit"),
                    FidoTestData.ES256Params,
                    new MakeCredentialOptions { UserPresence = false }));

            Assert.Equal(0x2C, (byte)error.Status);
            Assert.Equal(CtapStatus.InvalidOption, error.Status);
        });
}

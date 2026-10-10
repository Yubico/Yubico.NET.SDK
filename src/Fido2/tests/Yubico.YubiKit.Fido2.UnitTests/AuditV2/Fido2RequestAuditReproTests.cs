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
using Yubico.YubiKit.Core.Cryptography.Cose;
using Yubico.YubiKit.Fido2.Cbor;
using Yubico.YubiKit.Fido2.Credentials;

namespace Yubico.YubiKit.Fido2.UnitTests.AuditV2;

public sealed class Fido2RequestAuditReproTests
{
    [Fact]
    [Trait("Audit", "YESDK-1618")]
    public void YESDK1618_MakeCredentialDoesNotEncodeFalseUserPresence()
    {
        byte[] request = FidoSessionRequestEncoding.BuildMakeCredentialRequest(
            new byte[32],
            new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
            [new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256)],
            new MakeCredentialOptions { ResidentKey = true, UserPresence = false });

        var reader = new CborReader(request.AsMemory(1), CborConformanceMode.Ctap2Canonical);
        int? parameters = reader.ReadStartMap();
        bool foundOptions = false;
        for (int i = 0; i < parameters; i++)
        {
            int key = reader.ReadInt32();
            if (key != 7)
            {
                reader.SkipValue();
                continue;
            }

            foundOptions = true;
            int? optionCount = reader.ReadStartMap();
            for (int j = 0; j < optionCount; j++)
            {
                string option = reader.ReadTextString();
                bool value = reader.ReadBoolean();
                Assert.NotEqual("up", option);
                if (option == "rk") Assert.True(value);
            }
            reader.ReadEndMap();
        }
        reader.ReadEndMap();
        Assert.True(foundOptions);
    }

    [Fact]
    [Trait("Audit", "YESDK-1618")]
    public void YESDK1618_GetAssertionStillEncodesFalseUserPresence()
    {
        byte[] request = FidoSessionRequestEncoding.BuildGetAssertionRequest(
            "example.com", new byte[32], new GetAssertionOptions { UserPresence = false });

        var reader = new CborReader(request.AsMemory(1), CborConformanceMode.Ctap2Canonical);
        int? parameters = reader.ReadStartMap();
        bool foundFalseUp = false;
        for (int i = 0; i < parameters; i++)
        {
            int key = reader.ReadInt32();
            if (key != 5)
            {
                reader.SkipValue();
                continue;
            }

            Assert.Equal(1, reader.ReadStartMap());
            Assert.Equal("up", reader.ReadTextString());
            foundFalseUp = !reader.ReadBoolean();
            reader.ReadEndMap();
        }
        reader.ReadEndMap();
        Assert.True(foundFalseUp);
    }

    [Fact]
    [Trait("Audit", "NEW-FIDO2-DUPLICATE-ALGORITHMS")]
    public void NEW_MakeCredentialDoesNotEncodeDuplicateCredentialParameters()
    {
        var algorithm = new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256);
        byte[] request = FidoSessionRequestEncoding.BuildMakeCredentialRequest(
            new byte[32],
            new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
            [algorithm, algorithm],
            null);

        var reader = new CborReader(request.AsMemory(1), CborConformanceMode.Ctap2Canonical);
        int? parameters = reader.ReadStartMap();
        for (int i = 0; i < parameters; i++)
        {
            if (reader.ReadInt32() != 4)
            {
                reader.SkipValue();
                continue;
            }

            Assert.Equal(1, reader.ReadStartArray());
            reader.SkipValue();
            reader.ReadEndArray();
            return;
        }

        Assert.Fail("Missing pubKeyCredParams parameter");
    }
}

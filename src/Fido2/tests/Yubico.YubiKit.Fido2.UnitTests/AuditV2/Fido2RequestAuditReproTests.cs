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
    /// <summary>
    ///     CTAP 2.1-2.3 §6.1: the makeCredential "up" value "MUST be true if present" and false yields
    ///     CTAP2_ERR_INVALID_OPTION. The SDK rejects it before anything is encoded.
    /// </summary>
    [Fact]
    [Trait("Audit", "YESDK-1618")]
    public void YESDK1618_MakeCredentialRejectsFalseUserPresence()
    {
        var ex = Assert.Throws<ArgumentException>(() => FidoSessionRequestEncoding.BuildMakeCredentialRequest(
            new byte[32],
            new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
            [new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256)],
            new MakeCredentialOptions { ResidentKey = true, UserPresence = false }));

        Assert.Equal("options", ex.ParamName);
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

    /// <summary>
    ///     CTAP 2.1-2.3 §6.1 pubKeyCredParams: "MUST NOT include duplicate entries." The SDK rejects the list
    ///     before anything is encoded: rejected rather than deduplicated.
    /// </summary>
    [Fact]
    [Trait("Audit", "N1")]
    public void N1_MakeCredentialRejectsDuplicateCredentialParameters()
    {
        var algorithm = new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256);

        var ex = Assert.Throws<ArgumentException>(() => FidoSessionRequestEncoding.BuildMakeCredentialRequest(
            new byte[32],
            new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
            [algorithm, new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.EdDSA), algorithm],
            null));

        Assert.Equal("pubKeyCredParams", ex.ParamName);
    }

    /// <summary>Distinct entries are encoded in the caller's order (§6.1: "most preferred to least preferred").</summary>
    [Fact]
    [Trait("Audit", "N1")]
    public void N1_MakeCredentialEncodesDistinctCredentialParametersInOrder()
    {
        byte[] request = FidoSessionRequestEncoding.BuildMakeCredentialRequest(
            new byte[32],
            new PublicKeyCredentialRpEntity("example.com"),
            new PublicKeyCredentialUserEntity(new byte[] { 0x01 }, "alice", "Alice"),
            [new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.RS256), new PublicKeyCredentialParameters(CoseAlgorithmIdentifier.ES256)],
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

            Assert.Equal(2, reader.ReadStartArray());
            var algorithms = new List<int>();
            for (int entry = 0; entry < 2; entry++)
            {
                _ = reader.ReadStartMap();
                int? alg = null;
                while (reader.PeekState() != CborReaderState.EndMap)
                {
                    var key = reader.ReadTextString();
                    if (key == "alg") alg = reader.ReadInt32(); else reader.SkipValue();
                }
                reader.ReadEndMap();
                algorithms.Add(alg!.Value);
            }
            reader.ReadEndArray();
            Assert.Equal([(int)CoseAlgorithmIdentifier.RS256, (int)CoseAlgorithmIdentifier.ES256], algorithms);
            return;
        }

        Assert.Fail("Missing pubKeyCredParams parameter");
    }
}
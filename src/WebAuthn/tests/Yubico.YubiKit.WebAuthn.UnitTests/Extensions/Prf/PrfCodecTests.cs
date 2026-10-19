// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Extensions.Codec;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Extensions.Prf;

/// <summary>
///     Known-answer tests from WebAuthn Level 3 §16.17.1.2 "CTAP2 hmac-secret extension" (test vectors for the prf
///     extension). Every value below was taken from the W3C text and re-derived independently before use: the salts by
///     SHA-256, the encryptions by AES-256-CBC under the vector's shared secret, output1 as HMAC-SHA-256(CredRandom, salt1).
/// </summary>
public class PrfCodecTests
{
    // seed = UTF-8("WebAuthn PRF test vectors"); prf_eval_first = seed || 0x02; prf_eval_second = seed || 0x03
    private static readonly byte[] PrfEvalFirst = Convert.FromHexString("576562417574686e20505246207465737420766563746f727302");
    private static readonly byte[] PrfEvalSecond = Convert.FromHexString("576562417574686e20505246207465737420766563746f727303");
    private static readonly byte[] Salt1 = Convert.FromHexString("527413ebb48293772df30f031c5ac4650c7de14bf9498671ae163447b6a772b3");
    private static readonly byte[] Salt2 = Convert.FromHexString("d68ac03329a10ee5e0ec834492bb9a96a0e547baf563bf78ccbe8789b22e776b");
    private static readonly byte[] Output1 = Convert.FromHexString("3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae");
    private static readonly byte[] Output2 = Convert.FromHexString("a62a8773b19cda90d7ed4ef72a80a804320dbd3997e2f663805ad1fd3293d50b");

    // PIN/UV auth protocol 2 (shared_secret = HMAC key || AES key)
    private static readonly byte[] SharedSecretV2 = Convert.FromHexString(
        "0c63083de8170101d38bcf8bd72309568ddb4550867e23404b35d85712f7c20d8bc911ee23c06034cbc14290b9669bec07739053c5a416e313ef905c79955876");
    private static readonly byte[] SaltEncV2Single = Convert.FromHexString(
        "23dde5e3462daf36559b85c4ac5f9656aa9bfd81c1dc2bf8533c8b9f3882854786b4f500e25b4e3d81f7fc7c74236229");
    private static readonly byte[] OutputEncV2Single = Convert.FromHexString(
        "3bfaa48f7952330d63e35ff8cd5bca48d2a12823828915749287256ab146272f9fb437bf65691243c3f504bd7ea6d5e6");
    private static readonly byte[] SaltEncV2Two = Convert.FromHexString(
        "d9f4236403e0fe843a8e4e5be764d120904c198ad6e77b089876a3391961f183b0008b4ca66b91cd72aa35b6151ff981f6e5649f3c040e6615ad7dd8ae96ef23b229a5c97c3f0dcd8605eee166ce163a");
    private static readonly byte[] OutputEncV2Two = Convert.FromHexString(
        "90ee52f739043bc17b3488a74306d7801debb5b61f18662c648a25b5b5678ede482cdaff99a537a44f064fcb10ce6e04dfd27619dc96a0daff8507e499296b1eecf0981f7c8518b277a7a3018f5ec6fb");

    // PIN/UV auth protocol 1 (shared_secret = SHA-256(Z))
    private static readonly byte[] SharedSecretV1 = Convert.FromHexString("23e5ed7157c25892b77732fb9c8a107e3518800db2af4142f9f4adfacb771d39");
    private static readonly byte[] SaltEncV1Single = Convert.FromHexString("ab8c878bb05d04700f077ed91845ec9c503c925cb12b327ddbeb4243c397f913");
    private static readonly byte[] OutputEncV1Single = Convert.FromHexString("15d4e4f3f04109b492b575c1b38c28585b6719cf8d61304215108d939f37ccfb");

    /// <summary>L3 §10.1.4: salt = SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || input).</summary>
    [Fact]
    public void ComputeSalt_MatchesSpecVectors()
    {
        Assert.Equal(Salt1, PrfInput.ComputeSalt(PrfEvalFirst));
        Assert.Equal(Salt2, PrfInput.ComputeSalt(PrfEvalSecond));
    }

    /// <summary>The spec's saltEnc decrypts to salt1 (|| salt2) under the shared secret: our protocol speaks the same AES-CBC shape.</summary>
    [Theory]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public void SpecSaltEnc_DecryptsToTheSalts(int protocolVersion, bool twoInputs)
    {
        using var protocol = CreateProtocol(protocolVersion);
        var (sharedSecret, saltEnc) = (protocolVersion, twoInputs) switch
        {
            (2, false) => (SharedSecretV2, SaltEncV2Single),
            (2, true) => (SharedSecretV2, SaltEncV2Two),
            _ => (SharedSecretV1, SaltEncV1Single)
        };

        var plaintext = protocol.Decrypt(sharedSecret, saltEnc);

        Assert.Equal(twoInputs ? [.. Salt1, .. Salt2] : Salt1, plaintext);
    }

    /// <summary>CTAP §12.7 output is AES-CBC(sharedSecret, output1 [|| output2]); the codec decrypts and splits into results.</summary>
    [Theory]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public void DecryptResults_MatchesSpecOutputs(int protocolVersion, bool twoInputs)
    {
        using var protocol = CreateProtocol(protocolVersion);
        var (sharedSecret, outputEnc) = (protocolVersion, twoInputs) switch
        {
            (2, false) => (SharedSecretV2, OutputEncV2Single),
            (2, true) => (SharedSecretV2, OutputEncV2Two),
            _ => (SharedSecretV1, OutputEncV1Single)
        };

        var results = PrfCodec.DecryptResults(protocol, sharedSecret, outputEnc);

        Assert.NotNull(results);
        Assert.Equal(Output1, results.First.ToArray());
        if (twoInputs)
        {
            Assert.Equal(Output2, results.Second!.Value.ToArray());
        }
        else
        {
            Assert.Null(results.Second);
        }
    }

    /// <summary>Anything but one or two 32-byte blocks is not a PRF result (yubikit-android HmacSecretExtension: omit, don't fail).</summary>
    [Fact]
    public void DecryptResults_WrongLength_ReturnsNull()
    {
        using var protocol = new PinUvAuthProtocolV2();
        var ciphertext = protocol.Encrypt(SharedSecretV2, new byte[48]);

        Assert.Null(PrfCodec.DecryptResults(protocol, SharedSecretV2, ciphertext));
    }

    private static IPinUvAuthProtocol CreateProtocol(int version) =>
        version == 1 ? new PinUvAuthProtocolV1() : new PinUvAuthProtocolV2();
}
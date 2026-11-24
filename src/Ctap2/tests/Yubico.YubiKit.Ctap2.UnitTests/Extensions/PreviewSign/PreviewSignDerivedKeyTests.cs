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

using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Extensions;

namespace Yubico.YubiKit.Ctap2.UnitTests.Extensions.PreviewSign;

[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public class PreviewSignDerivedKeyTests
{
    [Fact]
    public void VerifySignature_RejectsBytesAfterTheDerSequenceOrItsComponents()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var point = ecdsa.ExportParameters(false).Q;
        byte[] publicKey = [4, .. (point.X ?? throw new InvalidOperationException()), .. (point.Y ?? throw new InvalidOperationException())];
        var derived = new PreviewSignDerivedKey(publicKey, default, default, default);
        var message = "previewSign test message"u8;
        byte[] signature = ecdsa.SignData(message, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        Assert.True(derived.VerifySignature(message, signature));
        Assert.False(derived.VerifySignature("other message"u8, signature));
        Assert.False(derived.VerifySignature(message, [.. signature, 0x05, 0x00]));

        byte[] withExtraComponent = [.. signature, 0x02, 0x01, 0x01];
        withExtraComponent[1] += 3;
        Assert.False(derived.VerifySignature(message, withExtraComponent));
    }
}
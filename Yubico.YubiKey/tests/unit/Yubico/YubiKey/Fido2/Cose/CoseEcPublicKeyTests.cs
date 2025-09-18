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
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Fido2.Cose;

public class CoseEcPublicKeyTests
{
    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void Encoding_Decoding_Key_Returns_ExpectedValues(
        string oid)
    {
        // Arrange
        var ecDsa = ECDsa.Create(ECCurve.CreateFromOid(Oid.FromOidValue(oid, OidGroup.PublicKeyAlgorithm)));
        var publicKey = ecDsa.ExportParameters(false);
        var coseKey = new CoseEcPublicKey(
            CoseEcCurve.P384,
            CoseAlgorithmIdentifier.ES384,
            publicKey.Q.X,
            publicKey.Q.Y);

        // Act
        var coseEncodedKey = coseKey.Encode(); // Encode
        var coseKey2 = new CoseEcPublicKey(coseEncodedKey); // Decode
        var publicKey2 = coseKey2.ToEcParameters();

        // Assert
        // The EC parameter values should be the same between the two keys.
        Assert.Equal(publicKey.Q.X, publicKey2.Q.X);
        Assert.Equal(publicKey.Q.Y, publicKey2.Q.Y);
    }

    [Theory]
    [InlineData(Oids.ECP256)]
    [InlineData(Oids.ECP384)]
    [InlineData(Oids.ECP521)]
    public void Constructor_with_EcParameters(
        string oid)
    {
        var ecDsa = ECDsa.Create(ECCurve.CreateFromOid(Oid.FromOidValue(oid, OidGroup.PublicKeyAlgorithm)));
        var publicKeyParams = ecDsa.ExportParameters(false);

        // Test EC Parameters constructor
        var coseKey = new CoseEcPublicKey(publicKeyParams);
        Assert.True(coseKey.XCoordinate.Span.SequenceEqual(publicKeyParams.Q.X));
    }
}

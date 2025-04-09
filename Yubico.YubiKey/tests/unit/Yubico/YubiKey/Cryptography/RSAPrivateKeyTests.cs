// Copyright 2024 Yubico AB
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

using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Piv.Converters;

namespace Yubico.YubiKey.Cryptography;

public class RSAPrivateKeyTests
{
    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);
        var privateKey = RSAPrivateKey.CreateFromParameters(parameters);

        // Act
        privateKey.Dispose();

        // Assert all bytes are zero
        Assert.True(privateKey.Parameters.Modulus?.All(b => b == 0) ?? true);
        Assert.True(privateKey.Parameters.Exponent?.All(b => b == 0) ?? true);
        Assert.True(privateKey.Parameters.P?.All(b => b == 0) ?? true);
        Assert.True(privateKey.Parameters.Q?.All(b => b == 0) ?? true);
        Assert.True(privateKey.Parameters.DP?.All(b => b == 0) ?? true);
        Assert.True(privateKey.Parameters.DQ?.All(b => b == 0) ?? true);
        Assert.True(privateKey.Parameters.InverseQ?.All(b => b == 0) ?? true);
    }

    [Fact]
    public void CreateFromPivEncoding_WithValidParameters_CreatesInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var testRsaParameters = rsa.ExportParameters(true);
        var pivPrivateKey = new PivRsaPrivateKey(
            testRsaParameters.P,
            testRsaParameters.Q,
            testRsaParameters.DP,
            testRsaParameters.DQ,
            testRsaParameters.InverseQ);

        // Act
        var rsaPrivateKey = PivKeyDecoder.CreateRSAPrivateKey(pivPrivateKey.EncodedPrivateKey);

        // Assert
        Assert.Equal(testRsaParameters.P, rsaPrivateKey.Parameters.P);
        Assert.Equal(testRsaParameters.Q, rsaPrivateKey.Parameters.Q);
        Assert.Equal(testRsaParameters.DP, rsaPrivateKey.Parameters.DP);
        Assert.Equal(testRsaParameters.DQ, rsaPrivateKey.Parameters.DQ);
        Assert.Equal(testRsaParameters.InverseQ, rsaPrivateKey.Parameters.InverseQ);
    }

    [Fact]
    public void CreateFromPkcs8_WithValidParameters_CreatesInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);

        // Act
        var privateKey = rsa.ExportPkcs8PrivateKey();
        RSAPrivateKey privateKeyParams = RSAPrivateKey.CreateFromPkcs8(privateKey);

        // Assert
        Assert.Equal(parameters.Modulus, privateKeyParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, privateKeyParams.Parameters.Exponent);
        Assert.Equal(parameters.P, privateKeyParams.Parameters.P);
        Assert.Equal(parameters.Q, privateKeyParams.Parameters.Q);
        Assert.Equal(parameters.D, privateKeyParams.Parameters.D);
        Assert.Equal(parameters.DP, privateKeyParams.Parameters.DP);
        Assert.Equal(parameters.DQ, privateKeyParams.Parameters.DQ);
        Assert.Equal(parameters.InverseQ, privateKeyParams.Parameters.InverseQ);
    }

    [Fact]
    public void CreateFromRsaParameters_WithValidParameters_CreatesInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);

        // Act
        var privateKeyParams = RSAPrivateKey.CreateFromParameters(parameters);

        // Assert
        Assert.Equal(parameters.Modulus, privateKeyParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, privateKeyParams.Parameters.Exponent);
        Assert.Equal(parameters.P, privateKeyParams.Parameters.P);
        Assert.Equal(parameters.Q, privateKeyParams.Parameters.Q);
        Assert.Equal(parameters.DP, privateKeyParams.Parameters.DP);
        Assert.Equal(parameters.DQ, privateKeyParams.Parameters.DQ);
        Assert.Equal(parameters.InverseQ, privateKeyParams.Parameters.InverseQ);

        Assert.Equal(rsa.ExportPkcs8PrivateKey(), privateKeyParams.ExportPkcs8PrivateKey());
    }

    [Fact]
    public void CreateFromRsaParameters_WithCRTParameters_CreatesInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);
        var crtParameters = new RSAParameters
        {
            P = parameters.P,
            Q = parameters.Q,
            DP = parameters.DP,
            DQ = parameters.DQ,
            InverseQ = parameters.InverseQ
        };

        // Act
        var privateKeyParams = RSAPrivateKey.CreateFromParameters(crtParameters);

        // Assert
        Assert.Equal(crtParameters.P, privateKeyParams.Parameters.P);
        Assert.Equal(crtParameters.Q, privateKeyParams.Parameters.Q);
        Assert.Equal(crtParameters.DP, privateKeyParams.Parameters.DP);
        Assert.Equal(crtParameters.DQ, privateKeyParams.Parameters.DQ);
        Assert.Equal(crtParameters.InverseQ, privateKeyParams.Parameters.InverseQ);
    }
}

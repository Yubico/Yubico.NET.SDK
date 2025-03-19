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

using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography;

public class RSAPrivateKeyParametersTests
{
    [Fact]
    public void CreateFromPivEncoding_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(KeyType.RSA2048);
        var pivPrivateKey = testKey.AsPivPrivateKey();
        var pivPrivateKeyEncoded = pivPrivateKey.EncodedPrivateKey;

        // Act
        var privateKeyParams = KeyParametersPivHelper.CreatePrivateParametersFromPivEncoding<RSAPrivateKeyParameters>(pivPrivateKeyEncoded);
        var parameters = privateKeyParams.Parameters;

        // Assert
        Assert.Equal(parameters.Modulus, privateKeyParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, privateKeyParams.Parameters.Exponent);
        Assert.Equal(parameters.P, privateKeyParams.Parameters.P);
        Assert.Equal(parameters.Q, privateKeyParams.Parameters.Q);
        Assert.Equal(parameters.DP, privateKeyParams.Parameters.DP);
        Assert.Equal(parameters.DQ, privateKeyParams.Parameters.DQ);
        Assert.Equal(parameters.InverseQ, privateKeyParams.Parameters.InverseQ);
    }
    
    [Fact]
    public void CreateFromPkcs8_WithValidParameters_CreatesInstance()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);

        // Act
        var privateKey = rsa.ExportPkcs8PrivateKey();
        RSAPrivateKeyParameters privateKeyParams = RSAPrivateKeyParameters.CreateFromPkcs8(privateKey);

        // Assert
        Assert.Equal(parameters.Modulus, privateKeyParams.Parameters.Modulus);
        Assert.Equal(parameters.Exponent, privateKeyParams.Parameters.Exponent);
        Assert.Equal(parameters.P, privateKeyParams.Parameters.P);
        Assert.Equal(parameters.Q, privateKeyParams.Parameters.Q);
        Assert.Equal(parameters.DP, privateKeyParams.Parameters.DP);
        Assert.Equal(parameters.DQ, privateKeyParams.Parameters.DQ);
        Assert.Equal(parameters.InverseQ, privateKeyParams.Parameters.InverseQ);
    }
}

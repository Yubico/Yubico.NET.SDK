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
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv.Commands;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class GetMetadataCmdTests : PivSessionIntegrationTestBase
{
    [Theory]
    [InlineData(KeyType.AES128)]
    [InlineData(KeyType.AES192)]
    [InlineData(KeyType.AES256)]
    public void AesKey_GetMetadata_CorrectAlgorithm(
        KeyType keyType)
    {
        Skip.If(!Device.HasFeature(YubiKeyFeature.PivAesManagementKey));

        using var pivSession = GetSession();
        var isValid = pivSession.TryAuthenticateManagementKey();
        Assert.True(isValid);

        byte[] keyData =
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68
        };

        var keyLength = keyType switch
        {
            KeyType.AES128 => 16,
            KeyType.AES192 => 24,
            _ => 32
        };

        var setCmd = new SetManagementKeyCommand(keyData.AsMemory()[..keyLength], PivTouchPolicy.Never,
            keyType.GetPivAlgorithm());
        var setRsp = pivSession.Connection.SendCommand(setCmd);
        Assert.Equal(ResponseStatus.Success, setRsp.Status);

        var getCmd = new GetMetadataCommand(PivSlot.Management);
        var getRsp = pivSession.Connection.SendCommand(getCmd);
        Assert.Equal(ResponseStatus.Success, getRsp.Status);

        var metadata = getRsp.GetData();
        Assert.Equal(keyType.GetPivAlgorithm(), metadata.Algorithm);
    }
}

// Copyright Yubico AB
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

using Yubico.YubiKit.Ctap2.Extensions;

namespace Yubico.YubiKit.Ctap2.UnitTests.Extensions;

[System.Diagnostics.CodeAnalysis.Experimental("YKEXP001")]
public class PreviewSignSigningParamsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("80")]
    [InlineData("A000")]
    [InlineData("A101")]
    [InlineData("A201020103")]
    public void SigningParams_RejectAdditionalArgsThatAreNotExactlyOneMap(string hex)
    {
        byte[] additionalArgs = Convert.FromHexString(hex);
        Assert.Throws<ArgumentException>(() => new PreviewSignSigningParams(new byte[] { 1 }, new byte[] { 2 }, additionalArgs));
        Assert.Throws<ArgumentException>(() => new PreviewSignSigningParams(new byte[] { 1 }, new byte[] { 2 }) { AdditionalArgs = additionalArgs });
    }

    [Fact]
    public void SigningParams_InitAcceptsEmptyHandleButNotEmptyTbs()
    {
        byte[] keyHandle = [1];
        byte[] tbs = [2];
        var emptyHandle = new PreviewSignSigningParams(keyHandle, tbs) { KeyHandle = default };

        Assert.True(emptyHandle.KeyHandle.IsEmpty);
        Assert.Throws<ArgumentException>(() => new PreviewSignSigningParams(keyHandle, tbs) { Tbs = default });
    }
}
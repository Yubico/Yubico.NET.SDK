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

using Xunit;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

public class PrfOutputTests
{
    [Fact]
    public void PrfOutput_FromHmacSecretOutput_ParsesSingleOutput()
    {
        var decrypted = new byte[32];
        Random.Shared.NextBytes(decrypted);

        var output = PrfOutput.FromHmacSecretOutput(decrypted, hasTwoOutputs: false);

        Assert.True(output.Enabled);
        Assert.NotNull(output.First);
        Assert.Equal(32, output.First.Value.Length);
        Assert.True(output.Second is null || output.Second.Value.IsEmpty);
    }

    [Fact]
    public void PrfOutput_FromHmacSecretOutput_ParsesTwoOutputs()
    {
        var decrypted = new byte[64];
        Random.Shared.NextBytes(decrypted);

        var output = PrfOutput.FromHmacSecretOutput(decrypted, hasTwoOutputs: true);

        Assert.True(output.Enabled);
        Assert.NotNull(output.First);
        Assert.NotNull(output.Second);
        Assert.Equal(32, output.First.Value.Length);
        Assert.Equal(32, output.Second.Value.Length);
    }

    [Fact]
    public void PrfOutput_FromHmacSecretOutput_ThrowsOnShortData()
    {
        var decrypted = new byte[16];

        Assert.Throws<ArgumentException>(
            () => PrfOutput.FromHmacSecretOutput(decrypted));
    }
}

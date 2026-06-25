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

public class PrfInputTests
{
    [Fact]
    public void PrfInput_ComputesSaltCorrectly()
    {
        var input = new byte[] { 1, 2, 3, 4, 5 };

        var salt = PrfInput.ComputeSalt(input);

        Assert.Equal(32, salt.Length);
        var salt2 = PrfInput.ComputeSalt(input);
        Assert.Equal(salt, salt2);
    }

    [Fact]
    public void PrfInput_DifferentInputsProduceDifferentSalts()
    {
        var input1 = new byte[] { 1, 2, 3 };
        var input2 = new byte[] { 4, 5, 6 };

        var salt1 = PrfInput.ComputeSalt(input1);
        var salt2 = PrfInput.ComputeSalt(input2);

        Assert.NotEqual(salt1, salt2);
    }
}

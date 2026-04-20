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

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class PwStatusTests
{
    [Fact]
    public void Parse_DefaultYubiKeyValues_ParsesCorrectly()
    {
        // Default YubiKey PW status: policy=Always, maxLen=127 for all, attempts=3/0/3
        byte[] encoded = [0x00, 0x7F, 0x7F, 0x7F, 0x03, 0x00, 0x03];

        var status = PwStatus.Parse(encoded);

        Assert.Equal(PinPolicy.Always, status.SignaturePinPolicy);
        Assert.Equal(127, status.MaxLenUser);
        Assert.Equal(127, status.MaxLenReset);
        Assert.Equal(127, status.MaxLenAdmin);
        Assert.Equal(3, status.AttemptsUser);
        Assert.Equal(0, status.AttemptsReset);
        Assert.Equal(3, status.AttemptsAdmin);
    }

    [Fact]
    public void Parse_OncePolicy_ParsesCorrectly()
    {
        byte[] encoded = [0x01, 0x40, 0x40, 0x40, 0x03, 0x03, 0x03];

        var status = PwStatus.Parse(encoded);

        Assert.Equal(PinPolicy.Once, status.SignaturePinPolicy);
        Assert.Equal(64, status.MaxLenUser);
    }

    [Fact]
    public void GetMaxLen_ReturnsCorrectValue()
    {
        byte[] encoded = [0x00, 0x7F, 0x20, 0x40, 0x03, 0x00, 0x03];
        var status = PwStatus.Parse(encoded);

        Assert.Equal(127, status.GetMaxLen(Pw.User));
        Assert.Equal(32, status.GetMaxLen(Pw.Reset));
        Assert.Equal(64, status.GetMaxLen(Pw.Admin));
    }

    [Fact]
    public void GetAttempts_ReturnsCorrectValue()
    {
        byte[] encoded = [0x00, 0x7F, 0x7F, 0x7F, 0x02, 0x01, 0x05];
        var status = PwStatus.Parse(encoded);

        Assert.Equal(2, status.GetAttempts(Pw.User));
        Assert.Equal(1, status.GetAttempts(Pw.Reset));
        Assert.Equal(5, status.GetAttempts(Pw.Admin));
    }

    [Fact]
    public void Parse_TooShort_Throws()
    {
        byte[] encoded = [0x00, 0x7F, 0x7F];
        Assert.Throws<ArgumentException>(() => PwStatus.Parse(encoded));
    }
}

// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.YubiHsm.UnitTests;

public class RetryExtractionTests
{
    [Theory]
    [InlineData(unchecked((short)0x63C0), 0)]
    [InlineData(unchecked((short)0x63C1), 1)]
    [InlineData(unchecked((short)0x63C5), 5)]
    [InlineData(unchecked((short)0x63C8), 8)]
    [InlineData(unchecked((short)0x63CF), 15)]
    public void ExtractRetries_ValidRetrySw_ReturnsCount(short sw, int expectedRetries)
    {
        var result = HsmAuthSession.ExtractRetries(sw);

        Assert.Equal(expectedRetries, result);
    }

    [Theory]
    [InlineData(unchecked((short)0x9000))]
    [InlineData(unchecked((short)0x6982))]
    [InlineData(unchecked((short)0x6300))]
    [InlineData(unchecked((short)0x6400))]
    [InlineData(unchecked((short)0x63D0))]
    public void ExtractRetries_NonRetrySw_ReturnsNull(short sw)
    {
        var result = HsmAuthSession.ExtractRetries(sw);

        Assert.Null(result);
    }
}

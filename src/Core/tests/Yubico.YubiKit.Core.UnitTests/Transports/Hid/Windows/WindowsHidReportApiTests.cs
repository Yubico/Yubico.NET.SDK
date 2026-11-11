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

using Yubico.YubiKit.Core.Transports.Hid.Windows;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Windows;

public class WindowsHidReportApiTests
{
    [Theory]
    [InlineData(1, 5u, 0u)]
    [InlineData(-1, 5u, 0u)]
    [InlineData(0, 997u, 0u)]
    [InlineData(0, 5u, 5u)]
    [InlineData(0, 995u, 995u)]
    [InlineData(0, 0u, 31u)]
    public void InterpretSubmission_AcceptsOnlySuccessOrPending(int nativeResult, uint error, uint expected)
    {
        Assert.Equal(expected, WindowsHidReportApi.InterpretSubmission(nativeResult, error).ErrorCode);
    }
}
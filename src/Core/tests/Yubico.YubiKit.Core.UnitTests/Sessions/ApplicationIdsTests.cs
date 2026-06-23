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

using Yubico.YubiKit.Core.Sessions;

namespace Yubico.YubiKit.Core.UnitTests.Sessions;

public class ApplicationIdsTests
{
    [Fact]
    public void Piv_UsesRidPlusPixAid()
    {
        Assert.Equal(9, ApplicationIds.Piv.Length);
        Assert.Equal(
            new byte[] { 0xA0, 0x00, 0x00, 0x03, 0x08, 0x00, 0x00, 0x10, 0x00 },
            ApplicationIds.Piv);
    }
}

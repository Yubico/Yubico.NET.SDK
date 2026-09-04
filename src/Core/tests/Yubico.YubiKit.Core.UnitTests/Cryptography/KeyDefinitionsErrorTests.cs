// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.UnitTests.Cryptography;

public class KeyDefinitionsErrorTests
{
    [Fact]
    public void GetByKeyType_UnsupportedValue_IdentifiesValueAndParameter()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            KeyDefinitions.GetByKeyType(KeyType.None));

        Assert.Contains(nameof(KeyType.None), exception.Message, StringComparison.Ordinal);
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void GetByCoseCurve_UnsupportedValue_IdentifiesValue()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            KeyDefinitions.GetByCoseCurve(CoseEcCurve.X448));

        Assert.Contains(nameof(CoseEcCurve.X448), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetByOid_UnsupportedValue_IdentifiesValue()
    {
        const string UnsupportedOid = "1.2.3.4.5";

        var exception = Assert.Throws<NotSupportedException>(() =>
            KeyDefinitions.GetByOid(UnsupportedOid));

        Assert.Contains(UnsupportedOid, exception.Message, StringComparison.Ordinal);
    }
}
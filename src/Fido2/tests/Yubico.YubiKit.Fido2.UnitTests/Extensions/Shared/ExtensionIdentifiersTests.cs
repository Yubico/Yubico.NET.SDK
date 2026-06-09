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

public class ExtensionIdentifiersTests
{
    [Fact]
    public void ExtensionIdentifiers_HaveCorrectValues()
    {
        Assert.Equal("hmac-secret", ExtensionIdentifiers.HmacSecret);
        Assert.Equal("hmac-secret-mc", ExtensionIdentifiers.HmacSecretMakeCredential);
        Assert.Equal("credProtect", ExtensionIdentifiers.CredProtect);
        Assert.Equal("credBlob", ExtensionIdentifiers.CredBlob);
        Assert.Equal("largeBlob", ExtensionIdentifiers.LargeBlob);
        Assert.Equal("largeBlobKey", ExtensionIdentifiers.LargeBlobKey);
        Assert.Equal("minPinLength", ExtensionIdentifiers.MinPinLength);
        Assert.Equal("prf", ExtensionIdentifiers.Prf);
    }
}
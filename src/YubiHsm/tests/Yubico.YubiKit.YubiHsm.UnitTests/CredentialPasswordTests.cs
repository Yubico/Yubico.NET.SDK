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

public class CredentialPasswordTests
{
    [Fact]
    public void FromUtf8_PadsEncodedBytesWithoutOwningTheSource()
    {
        byte[] source = [0xC3, 0xA9, 0xF0, 0x9F, 0x94, 0x91]; // é🔑
        byte[] expected = [.. source, .. new byte[10]];
        using var password = CredentialPassword.FromUtf8(source);
        var owned = password.Bytes;

        Assert.Equal(expected, owned.ToArray());
        password.Dispose();
        password.Dispose();
        Assert.Equal(new byte[16], owned.ToArray());
        Assert.Equal(expected[..6], source);
        Assert.Throws<ObjectDisposedException>(() => _ = password.Bytes);
    }

    [Fact]
    public void FromRaw_PreservesOpaqueBytesAndClonesTheSource()
    {
        byte[] source = [0xFF, .. new byte[14], 0xAB];
        using var password = CredentialPassword.FromRaw(source);
        source[15] = 0;
        Assert.Equal(new byte[] { 0xFF, 0xAB }, new byte[] { password.Bytes[0], password.Bytes[15] });
    }

    [Fact]
    public void Factories_EnforceEncodedWidthAndDistinguishRawFromText()
    {
        using var boundary = CredentialPassword.FromUtf8("éééééééé"u8);
        Assert.Equal("éééééééé"u8.ToArray(), boundary.Bytes.ToArray());
        Assert.Throws<ArgumentException>(() => CredentialPassword.FromUtf8("ééééééééa"u8));
        Assert.Throws<ArgumentException>(() => CredentialPassword.FromUtf8([0xFF]));
        Assert.Throws<ArgumentException>(() => CredentialPassword.FromRaw(new byte[15]));
        Assert.Throws<ArgumentException>(() => CredentialPassword.FromRaw(new byte[17]));
    }
}

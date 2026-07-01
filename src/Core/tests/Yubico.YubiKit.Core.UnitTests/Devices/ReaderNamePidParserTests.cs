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

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class ReaderNamePidParserTests
{
    [Theory]
    [InlineData("Yubico YubiKey OTP 00 00", (ushort)0x0401)]
    [InlineData("Yubico YubiKey FIDO 00 00", (ushort)0x0402)]
    [InlineData("Yubico YubiKey OTP+FIDO 00 00", (ushort)0x0403)]
    [InlineData("Yubico YubiKey CCID 00 00", (ushort)0x0404)]
    [InlineData("Yubico YubiKey OTP+CCID 00 00", (ushort)0x0405)]
    [InlineData("Yubico YubiKey FIDO+CCID 00 00", (ushort)0x0406)]
    [InlineData("Yubico YubiKey OTP+FIDO+CCID 00 00", (ushort)0x0407)]
    public void FromReaderName_Standard_MapsToPid(string name, ushort expected) =>
        Assert.Equal(expected, ReaderNamePidParser.FromReaderName(name));

    [Theory]
    [InlineData("Yubico YubiKey NEO OTP 00 00", (ushort)0x0110)]
    [InlineData("Yubico YubiKey NEO OTP+CCID 00 00", (ushort)0x0111)]
    [InlineData("Yubico YubiKey NEO CCID 00 00", (ushort)0x0112)]
    [InlineData("Yubico YubiKey NEO OTP+FIDO+CCID 00 00", (ushort)0x0116)]
    public void FromReaderName_Neo_MapsToPid(string name, ushort expected) =>
        Assert.Equal(expected, ReaderNamePidParser.FromReaderName(name));

    [Fact]
    public void FromReaderName_IsCaseInsensitive() =>
        Assert.Equal((ushort)0x0407, ReaderNamePidParser.FromReaderName("yubico yubikey otp+fido+ccid 00 00"));

    [Fact]
    public void FromReaderName_U2fAlias_CountsAsFido() =>
        Assert.Equal((ushort)0x0406, ReaderNamePidParser.FromReaderName("Yubico YubiKey U2F+CCID 00 00"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Generic Smart Card Reader")]
    [InlineData("ACME Contactless Reader 0")]
    [InlineData("Yubico YubiKey 00 00")] // USB Yubico reader but no recognizable interface combination
    public void FromReaderName_UnrecognizedOrNoInterfaces_ReturnsNull(string? name) =>
        Assert.Null(ReaderNamePidParser.FromReaderName(name));

    [Theory]
    [InlineData((ushort)0x0407, true)]
    [InlineData((ushort)0x0120, true)]
    [InlineData((ushort)0x0110, true)]
    [InlineData((ushort)0x0000, false)]
    [InlineData((ushort)0x9999, false)]
    public void IsKnownPid_MatchesTable(ushort pid, bool expected) =>
        Assert.Equal(expected, ReaderNamePidParser.IsKnownPid(pid));

    [Fact]
    public void IsSky_IdentifiesSecurityKeyPid()
    {
        Assert.True(ReaderNamePidParser.IsSky(0x0120));
        Assert.False(ReaderNamePidParser.IsSky(0x0407));
    }
}
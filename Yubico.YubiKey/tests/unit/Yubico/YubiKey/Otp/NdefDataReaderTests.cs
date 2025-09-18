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

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace Yubico.YubiKey.Otp;

public class NdefDataReaderTests
{
    [Fact]
    public void Constructor_GivenBufferWithFirstByteNonZero_ThrowsArgumentInvalidException()
    {
        var invalidBuffer = new byte[] { 1 };

        void Action()
        {
            _ = new NdefDataReader(invalidBuffer);
        }

        _ = Assert.Throws<ArgumentException>(Action);
    }

    [Fact]
    public void Constructor_BufferWithTypeLengthGreaterThanOne_ThrowsNotSupportedException()
    {
        var invalidBuffer = new byte[] { 0, 4, 0xD1, 2, 0, 0 };

        void Action()
        {
            _ = new NdefDataReader(invalidBuffer);
        }

        _ = Assert.Throws<NotSupportedException>(Action);
    }

    [Fact]
    public void Constructor_UnsupportedRecordType_ThrowsNotSupportedException()
    {
        var invalidBuffer = new byte[] { 0, 4, 0xD1, 1, 0, 0 };

        void Action()
        {
            _ = new NdefDataReader(invalidBuffer);
        }

        _ = Assert.Throws<NotSupportedException>(Action);
    }

    [Theory]
    [InlineData('T', NdefDataType.Text)]
    [InlineData('U', NdefDataType.Uri)]
    public void Constructor_GivenWellFormedData_SetsTypeProperty(
        char typeByte,
        NdefDataType expectedType)
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)typeByte, 4, 1, 2, 3 };

        var reader = new NdefDataReader(buffer);

        Assert.Equal(expectedType, reader.Type);
    }

    [Fact]
    public void Constructor_GivenWellFormedData_SetsDataProperty()
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)'U', 1, 2, 3, 4 };

        var reader = new NdefDataReader(buffer);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, reader.Data);
    }

    [Fact]
    public void ToText_GivenNonTextRecordType_ThrowsInvalidOperationException()
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)'U', 1, 2, 3, 4 };
        var reader = new NdefDataReader(buffer);

        void Action()
        {
            _ = reader.ToText();
        }

        _ = Assert.Throws<InvalidOperationException>(Action);
    }

    [Theory]
    [InlineData(0x80, NdefTextEncoding.Utf16)]
    [InlineData(0, NdefTextEncoding.Utf8)]
    public void ToText_GivenWellFormedData_ReturnsEncodingCorrectly(
        byte headerByte,
        NdefTextEncoding expectedEncoding)
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)'T', headerByte, 0, 1, 2 };
        var reader = new NdefDataReader(buffer);

        var ndefText = reader.ToText();

        Assert.Equal(expectedEncoding, ndefText.Encoding);
    }

    [Theory]
    [InlineData("")]
    [InlineData("en-US")]
    public void ToText_GivenWellFormedData_ReturnsLanguageCorrectly(
        string cultureString)
    {
        if (cultureString is null)
        {
            throw new ArgumentNullException(cultureString);
        }

        // Arrange
        var length = (byte)cultureString.Length;

        using var buffer = new MemoryStream();
        buffer.Write(
            new byte[] { 0, (byte)(5 + length), 0xD1, 1, (byte)(1 + length), (byte)'T', (byte)(length & 0x3F) });
        buffer.Write(Encoding.UTF8.GetBytes(cultureString));

        var reader = new NdefDataReader(buffer.ToArray());

        // Act
        var ndefText = reader.ToText();

        // Assert
        Assert.Equal(new CultureInfo(cultureString), ndefText.Language);
    }

    [Fact]
    public void ToText_GivenWellFormedData_ReturnsTextCorrectly()
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)'T', 0, (byte)'A', (byte)'B', (byte)'C' };
        var reader = new NdefDataReader(buffer);

        var ndefText = reader.ToText();

        Assert.Equal("ABC", ndefText.Text);
    }

    [Fact]
    public void ToText_GivenUtf16BeWithBom_ReturnsTextCorrectly()
    {
        // Arrange
        var expectedText = "Test";
        var bom = Encoding.BigEndianUnicode.GetPreamble();
        var text = Encoding.BigEndianUnicode.GetBytes(expectedText);
        var length = (byte)(bom.Length + text.Length);

        using var buffer = new MemoryStream();
        buffer.Write(new byte[] { 0, (byte)(5 + length), 0xD1, 1, (byte)(1 + length), (byte)'T', 0x80 });
        buffer.Write(bom);
        buffer.Write(text);

        var reader = new NdefDataReader(buffer.ToArray());

        // Act
        var ndefText = reader.ToText();

        // Assert
        Assert.Equal(expectedText, ndefText.Text);
    }

    [Fact]
    public void ToText_GivenUtf16LeWithBom_ReturnsTextCorrectly()
    {
        // Arrange
        var expectedText = "Test";
        var bom = Encoding.Unicode.GetPreamble();
        var text = Encoding.Unicode.GetBytes(expectedText);
        var length = (byte)(bom.Length + text.Length);

        using var buffer = new MemoryStream();
        buffer.Write(new byte[] { 0, (byte)(5 + length), 0xD1, 1, (byte)(1 + length), (byte)'T', 0x80 });
        buffer.Write(bom);
        buffer.Write(text);

        var reader = new NdefDataReader(buffer.ToArray());

        // Act
        var ndefText = reader.ToText();

        // Assert
        Assert.Equal(expectedText, ndefText.Text);
    }

    [Fact]
    public void ToText_GivenUtf16BeWithoutBom_ReturnsTextCorrectly()
    {
        // Arrange
        var expectedText = "Test";
        var text = Encoding.BigEndianUnicode.GetBytes(expectedText);
        var length = (byte)text.Length;

        using var buffer = new MemoryStream();
        buffer.Write(new byte[] { 0, (byte)(5 + length), 0xD1, 1, (byte)(1 + length), (byte)'T', 0x80 });
        buffer.Write(text);

        var reader = new NdefDataReader(buffer.ToArray());

        // Act
        var ndefText = reader.ToText();

        // Assert
        Assert.Equal(expectedText, ndefText.Text);
    }

    [Fact]
    public void ToText_GivenUtf16LeWithoutBom_ReturnsTextCorrectly()
    {
        // Arrange
        var expectedText = "Test";
        var text = Encoding.Unicode.GetBytes(expectedText);
        var length = (byte)text.Length;

        using var buffer = new MemoryStream();
        buffer.Write(new byte[] { 0, (byte)(5 + length), 0xD1, 1, (byte)(1 + length), (byte)'T', 0x80 });
        buffer.Write(text);

        var reader = new NdefDataReader(buffer.ToArray());

        // Act
        var ndefText = reader.ToText();

        // Assert
        Assert.Equal(expectedText, ndefText.Text);
    }

    [Fact]
    public void ToUri_GivenNonUriRecordType_ThrowsInvalidOperationException()
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)'T', 1, 2, 3, 4 };
        var reader = new NdefDataReader(buffer);

        void Action()
        {
            _ = reader.ToUri();
        }

        _ = Assert.Throws<InvalidOperationException>(Action);
    }

    [Fact]
    public void ToUri_GivenUnsupportedPrefix_ThrowsInvalidOperationException()
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)'U', 36, 1, 2, 3 }; // 36 is out of range
        var reader = new NdefDataReader(buffer);

        void Action()
        {
            _ = reader.ToUri();
        }

        _ = Assert.Throws<InvalidOperationException>(Action);
    }

    [Theory]
    [InlineData(0, "", "")]
    [InlineData(1, "http://www.", "/")]
    [InlineData(2, "https://www.", "/")]
    [InlineData(3, "http://", "/")]
    [InlineData(4, "https://", "/")]
    [InlineData(5, "tel:", "")]
    [InlineData(6, "mailto:", "")]
    [InlineData(7, "ftp://anonymous:anonymous@", "/")]
    [InlineData(8, "ftp://ftp.", "/")]
    [InlineData(9, "ftps://", "/")]
    [InlineData(10, "sftp://", "/")]
    [InlineData(11, "smb://", "/")]
    [InlineData(12, "nfs://", "/")]
    [InlineData(13, "ftp://", "/")]
    [InlineData(14, "dav://", "/")]
    [InlineData(15, "news:", "")]
    [InlineData(16, "telnet://", "/")]
    [InlineData(17, "imap:", "")]
    [InlineData(18, "rtsp://", "/")]
    [InlineData(19, "urn:", "")]
    [InlineData(20, "pop:", "")]
    [InlineData(21, "sip:", "")]
    [InlineData(22, "sips:", "")]
    [InlineData(23, "tftp:", "")]
    [InlineData(24, "btspp://", "/")]
    [InlineData(25, "btl2cap://", "/")]
    [InlineData(26, "btgoep://", "/")]
    [InlineData(27, "tcpobex://", "/")]
    [InlineData(28, "irdaobex://", "/")]
    [InlineData(29, "file://", "/")]
    [InlineData(30, "urn:epc:id:", "")]
    [InlineData(31, "urn:epc:tag:", "")]
    [InlineData(32, "urn:epc:pat:", "")]
    [InlineData(33, "urn:epc:raw:", "")]
    [InlineData(34, "urn:epc:", "")]
    [InlineData(35, "urn:nfc:", "")]
    public void ToUri_GivenValidPrefixAndString_ReturnsValidUri(
        int prefixCode,
        string prefix,
        string suffix)
    {
        var buffer = new byte[] { 0, 8, 0xD1, 1, 4, (byte)'U', (byte)prefixCode, (byte)'a', (byte)'b', (byte)'c' };
        var reader = new NdefDataReader(buffer);

        var value = reader.ToUri();

        Assert.Equal(prefix + "abc" + suffix, value.ToString());
    }

    [Fact]
    public void ToUri_CustomPrefix_ReturnsValidUri()
    {
        // Arrange
        var customUri = "ykprogram://customurl";
        var length = (byte)customUri.Length;

        using var buffer = new MemoryStream();
        buffer.Write(new byte[] { 0, (byte)(5 + length), 0xD1, 1, (byte)(1 + length), (byte)'U', 0 });
        buffer.Write(Encoding.UTF8.GetBytes(customUri));

        var reader = new NdefDataReader(buffer.ToArray());

        // Act
        var value = reader.ToUri();

        // Assert
        Assert.Equal(customUri + "/", value.ToString());
    }
}

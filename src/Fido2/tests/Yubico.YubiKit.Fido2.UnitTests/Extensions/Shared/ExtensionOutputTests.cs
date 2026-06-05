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

using System.Formats.Cbor;
using Xunit;
using Yubico.YubiKit.Fido2.Extensions;

namespace Yubico.YubiKit.Fido2.UnitTests.Extensions;

/// <summary>
/// Unit tests for the ExtensionOutput class.
/// </summary>
public class ExtensionOutputTests
{
    [Fact]
    public void Decode_EmptyData_ReturnsEmptyOutput()
    {
        var data = ReadOnlyMemory<byte>.Empty;

        var output = ExtensionOutput.Decode(data);

        Assert.False(output.HasExtensions);
        Assert.Empty(output.ExtensionIds);
    }

    [Fact]
    public void Decode_WithCredProtect_ParsesCorrectly()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString("credProtect");
        writer.WriteInt32(2);
        writer.WriteEndMap();
        var data = writer.Encode();

        var output = ExtensionOutput.DecodeWithRawData(data);

        Assert.True(output.HasExtensions);
        Assert.True(output.TryGetCredProtect(out var policy));
        Assert.Equal(CredProtectPolicy.UserVerificationOptionalWithCredentialIdList, policy);
    }

    [Fact]
    public void Decode_WithCredBlobStored_ParsesCorrectly()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString("credBlob");
        writer.WriteBoolean(true);
        writer.WriteEndMap();
        var data = writer.Encode();

        var output = ExtensionOutput.DecodeWithRawData(data);

        Assert.True(output.TryGetCredBlobStored(out var stored));
        Assert.True(stored);
    }

    [Fact]
    public void Decode_WithCredBlobData_ParsesCorrectly()
    {
        var blob = new byte[] { 10, 20, 30, 40 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString("credBlob");
        writer.WriteByteString(blob);
        writer.WriteEndMap();
        var data = writer.Encode();

        var output = ExtensionOutput.DecodeWithRawData(data);

        Assert.True(output.TryGetCredBlob(out var result));
        Assert.Equal(blob, result.ToArray());
    }

    [Fact]
    public void Decode_WithMinPinLength_ParsesCorrectly()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString("minPinLength");
        writer.WriteInt32(6);
        writer.WriteEndMap();
        var data = writer.Encode();

        var output = ExtensionOutput.DecodeWithRawData(data);

        Assert.True(output.TryGetMinPinLength(out var minLength));
        Assert.Equal(6, minLength);
    }

    [Fact]
    public void Decode_WithLargeBlobKey_ParsesCorrectly()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString("largeBlobKey");
        writer.WriteByteString(key);
        writer.WriteEndMap();
        var data = writer.Encode();

        var output = ExtensionOutput.DecodeWithRawData(data);

        Assert.True(output.TryGetLargeBlobKey(out var result));
        Assert.Equal(key, result.ToArray());
    }

    [Fact]
    public void Decode_WithHmacSecret_ParsesCorrectly()
    {
        var outputData = new byte[48];
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(1);
        writer.WriteTextString("hmac-secret");
        writer.WriteByteString(outputData);
        writer.WriteEndMap();
        var data = writer.Encode();

        var output = ExtensionOutput.DecodeWithRawData(data);

        Assert.True(output.TryGetHmacSecret(out var hmacOutput));
        Assert.NotNull(hmacOutput);
        Assert.Equal(48, hmacOutput.Output.Length);
    }

    [Fact]
    public void TryGetCredProtect_MissingExtension_ReturnsFalse()
    {
        var output = ExtensionOutput.Decode(ReadOnlyMemory<byte>.Empty);

        var result = output.TryGetCredProtect(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetMinPinLength_MissingExtension_ReturnsFalse()
    {
        var output = ExtensionOutput.Decode(ReadOnlyMemory<byte>.Empty);

        var result = output.TryGetMinPinLength(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetRawExtension_MissingExtension_ReturnsFalse()
    {
        var output = ExtensionOutput.Decode(ReadOnlyMemory<byte>.Empty);

        var result = output.TryGetRawExtension("unknown", out _);

        Assert.False(result);
    }

    [Fact]
    public void Decode_WithMultipleExtensions_ParsesAll()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(3);

        writer.WriteTextString("credBlob");
        writer.WriteBoolean(true);

        writer.WriteTextString("credProtect");
        writer.WriteInt32(3);

        writer.WriteTextString("minPinLength");
        writer.WriteInt32(4);

        writer.WriteEndMap();
        var data = writer.Encode();

        var output = ExtensionOutput.DecodeWithRawData(data);

        Assert.True(output.HasExtensions);
        Assert.Equal(3, output.ExtensionIds.Count());

        Assert.True(output.TryGetCredBlobStored(out var stored));
        Assert.True(stored);

        Assert.True(output.TryGetCredProtect(out var policy));
        Assert.Equal(CredProtectPolicy.UserVerificationRequired, policy);

        Assert.True(output.TryGetMinPinLength(out var minLength));
        Assert.Equal(4, minLength);
    }

    [Fact]
    public void ExtensionOutput_WithUnsupportedExtension_YieldsEmptyOutputMap()
    {
        var emptyExtensions = ReadOnlyMemory<byte>.Empty;

        var output = ExtensionOutput.Decode(emptyExtensions);

        Assert.False(output.HasExtensions);
        Assert.Empty(output.ExtensionIds);
        Assert.False(output.TryGetCredProtect(out _));
        Assert.False(output.TryGetMinPinLength(out _));
    }
}

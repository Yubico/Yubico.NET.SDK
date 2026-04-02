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
using Yubico.YubiKit.Fido2.Cbor;

namespace Yubico.YubiKit.Fido2.UnitTests.Cbor;

/// <summary>
/// Tests for CtapResponseParser utility methods.
/// </summary>
public class CtapResponseParserTests
{
    #region ReadIntKeyMap Tests
    
    [Fact]
    public void ReadIntKeyMap_ParsesMapCorrectly()
    {
        // Arrange - create CBOR map {1: 42, 2: "hello"}
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteInt32(1);
        writer.WriteInt32(42);
        writer.WriteInt32(2);
        writer.WriteTextString("hello");
        writer.WriteEndMap();
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var intValue = 0;
        var stringValue = "";
        
        // Act
        CtapResponseParser.ReadIntKeyMap(reader, (key, r) =>
        {
            switch (key)
            {
                case 1:
                    intValue = r.ReadInt32();
                    break;
                case 2:
                    stringValue = r.ReadTextString();
                    break;
                default:
                    r.SkipValue();
                    break;
            }
        });
        
        // Assert
        Assert.Equal(42, intValue);
        Assert.Equal("hello", stringValue);
    }
    
    [Fact]
    public void ReadIntKeyMap_HandlesEmptyMap()
    {
        // Arrange
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        var callCount = 0;
        
        // Act
        CtapResponseParser.ReadIntKeyMap(reader, (_, _) => callCount++);
        
        // Assert
        Assert.Equal(0, callCount);
    }
    
    [Fact]
    public void ReadIntKeyMap_ThrowsOnNullReader()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CtapResponseParser.ReadIntKeyMap(null!, (_, _) => { }));
    }
    
    [Fact]
    public void ReadIntKeyMap_ThrowsOnNullHandler()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        Assert.Throws<ArgumentNullException>(() =>
            CtapResponseParser.ReadIntKeyMap(reader, null!));
    }
    
    #endregion
    
    #region ReadTextKeyMap Tests
    
    [Fact]
    public void ReadTextKeyMap_ParsesMapCorrectly()
    {
        // Arrange - create CBOR map {"name": "test", "value": 123}
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(2);
        writer.WriteTextString("name");
        writer.WriteTextString("test");
        writer.WriteTextString("value");
        writer.WriteInt32(123);
        writer.WriteEndMap();
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var name = "";
        var value = 0;
        
        // Act
        CtapResponseParser.ReadTextKeyMap(reader, (key, r) =>
        {
            switch (key)
            {
                case "name":
                    name = r.ReadTextString();
                    break;
                case "value":
                    value = r.ReadInt32();
                    break;
                default:
                    r.SkipValue();
                    break;
            }
        });
        
        // Assert
        Assert.Equal("test", name);
        Assert.Equal(123, value);
    }
    
    #endregion
    
    #region ToNullableMemory Tests
    
    [Fact]
    public void ToNullableMemory_WithData_ReturnsMemory()
    {
        var data = new byte[] { 1, 2, 3 };
        var result = CtapResponseParser.ToNullableMemory(data);
        
        Assert.NotNull(result);
        Assert.Equal(data, result.Value.ToArray());
    }
    
    #endregion
    
    #region ReadArrayAsList Tests
    
    [Fact]
    public void ReadArrayAsList_ParsesArrayCorrectly()
    {
        // Arrange - create CBOR array [1, 2, 3]
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(3);
        writer.WriteInt32(1);
        writer.WriteInt32(2);
        writer.WriteInt32(3);
        writer.WriteEndArray();
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        // Act
        var result = CtapResponseParser.ReadArrayAsList(reader, r => r.ReadInt32());
        
        // Assert
        Assert.Equal([1, 2, 3], result);
    }
    
    [Fact]
    public void ReadArrayAsList_HandlesEmptyArray()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartArray(0);
        writer.WriteEndArray();
        
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        var result = CtapResponseParser.ReadArrayAsList(reader, r => r.ReadInt32());
        
        Assert.Empty(result);
    }
    
    #endregion
    
    #region ReadOptional Tests
    
    [Fact]
    public void ReadOptionalInt32_WithValue_ReturnsValue()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteInt32(42);
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var result = CtapResponseParser.ReadOptionalInt32(reader);
        
        Assert.Equal(42, result);
    }
    
    [Fact]
    public void ReadOptionalInt32_WithNull_ReturnsNull()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteNull();
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var result = CtapResponseParser.ReadOptionalInt32(reader);
        
        Assert.Null(result);
    }
    
    [Fact]
    public void ReadOptionalBoolean_WithValue_ReturnsValue()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteBoolean(true);
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var result = CtapResponseParser.ReadOptionalBoolean(reader);
        
        Assert.True(result);
    }
    
    [Fact]
    public void ReadOptionalByteString_WithValue_ReturnsValue()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteByteString(data);
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var result = CtapResponseParser.ReadOptionalByteString(reader);
        
        Assert.Equal(data, result);
    }
    
    [Fact]
    public void ReadOptionalTextString_WithValue_ReturnsValue()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteTextString("test");
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var result = CtapResponseParser.ReadOptionalTextString(reader);
        
        Assert.Equal("test", result);
    }
    
    [Fact]
    public void ReadOptionalTextString_WithNull_ReturnsNull()
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteNull();
        var reader = new CborReader(writer.Encode(), CborConformanceMode.Ctap2Canonical);
        
        var result = CtapResponseParser.ReadOptionalTextString(reader);
        
        Assert.Null(result);
    }
    
    #endregion
}

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
using Xunit;

namespace Yubico.YubiKey.Utilities;

public class ByteArrayExtensionsTests
{
    private readonly byte[] _arr1 = { 1, 2 };
    private readonly byte[] _arr2 = { 3, 4, 5 };
    private readonly byte[] _arr3 = { 6 };
    private readonly byte[] _emptyArr = Array.Empty<byte>();

    private readonly byte[] _expected1And2 = { 1, 2, 3, 4, 5 };
    private readonly byte[] _expected1And2And3 = { 1, 2, 3, 4, 5, 6 };

    #region byte[] Tests

    [Fact]
    public void Concat_ByteArray_WithMultipleInputs_ReturnsCorrectConcatenation()
    {
        // Act
        var result = _arr1.Concat(_arr2, _arr3);

        // Assert
        Assert.Equal(_expected1And2And3, result);
    }

    [Fact]
    public void Concat_ByteArray_WithNoOthers_ReturnsNewArrayInstance()
    {
        var result = _arr1.Concat();

        Assert.Equal(_arr1, result);
        Assert.NotSame(_arr1, result);
    }
    
    [Fact]
    public void Concat_ByteArray_WithNullOthers_ReturnsNewArrayInstance()
    {
        var result = _arr1.Concat(null!);

        Assert.Equal(_arr1, result);
        Assert.NotSame(_arr1, result);
    }

    [Fact]
    public void Concat_ByteArray_WithSomeEmptyInputs_ReturnsCorrectConcatenation()
    {
        // Act
        var result = _arr1.Concat(_emptyArr, _arr2);

        // Assert
        Assert.Equal(_expected1And2, result);
    }

    [Fact]
    public void Concat_ByteArray_WithAllEmptyInputs_ReturnsEmptyArray()
    {
        // Act
        var result = _emptyArr.Concat(_emptyArr, _emptyArr);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ReadOnlyMemory<byte> Tests

    [Fact]
    public void Concat_ReadOnlyMemory_WithMultipleInputs_ReturnsCorrectConcatenation()
    {
        // Arrange
        var mem1 = new ReadOnlyMemory<byte>(_arr1);
        var mem2 = new ReadOnlyMemory<byte>(_arr2);
        var mem3 = new ReadOnlyMemory<byte>(_arr3);

        // Act
        var result = mem1.Concat(mem2, mem3);

        // Assert
        Assert.Equal(_expected1And2And3, result);
    }

    [Fact]
    public void Concat_ReadOnlyMemory_WithNoOthers_ReturnsCopyOfFirst()
    {
        // Arrange
        var mem1 = new ReadOnlyMemory<byte>(_arr1);

        // Act
        var result = mem1.Concat();

        // Assert
        Assert.Equal(_arr1, result);
    }

    [Fact]
    public void Concat_ReadOnlyMemory_WithSomeEmptyInputs_ReturnsCorrectConcatenation()
    {
        // Arrange
        var mem1 = new ReadOnlyMemory<byte>(_arr1);
        var memEmpty = ReadOnlyMemory<byte>.Empty;
        var mem2 = new ReadOnlyMemory<byte>(_arr2);

        // Act
        var result = mem1.Concat(memEmpty, mem2);

        // Assert
        Assert.Equal(_expected1And2, result);
    }
    
    [Fact]
    public void Concat_ReadOnlyMemory_WithAllEmptyInputs_ReturnsEmptyArray()
    {
        // Arrange
        var memEmpty1 = ReadOnlyMemory<byte>.Empty;
        var memEmpty2 = ReadOnlyMemory<byte>.Empty;
        
        // Act
        var result = memEmpty1.Concat(memEmpty2);
        
        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ReadOnlySpan<byte> Tests

    [Fact]
    public void Concat_ReadOnlySpan_WithTwoNonEmpty_ReturnsCorrectConcatenation()
    {
        // Arrange
        var span1 = new ReadOnlySpan<byte>(_arr1);
        var span2 = new ReadOnlySpan<byte>(_arr2);

        // Act
        var result = span1.Concat(span2);

        // Assert
        Assert.Equal(_expected1And2, result);
    }

    [Fact]
    public void Concat_ReadOnlySpan_WithFirstEmpty_ReturnsCorrectArray()
    {
        // Arrange
        var spanEmpty = ReadOnlySpan<byte>.Empty;
        var span2 = new ReadOnlySpan<byte>(_arr2);

        // Act
        var result = spanEmpty.Concat(span2);

        // Assert
        Assert.Equal(_arr2, result);
    }

    [Fact]
    public void Concat_ReadOnlySpan_WithSecondEmpty_ReturnsCorrectArray()
    {
        // Arrange
        var span1 = new ReadOnlySpan<byte>(_arr1);
        var spanEmpty = ReadOnlySpan<byte>.Empty;

        // Act
        var result = span1.Concat(spanEmpty);

        // Assert
        Assert.Equal(_arr1, result);
    }

    [Fact]
    public void Concat_ReadOnlySpan_WithBothEmpty_ReturnsEmptyArray()
    {
        // Arrange
        var spanEmpty1 = ReadOnlySpan<byte>.Empty;
        var spanEmpty2 = ReadOnlySpan<byte>.Empty;

        // Act
        var result = spanEmpty1.Concat(spanEmpty2);

        // Assert
        Assert.Empty(result);
    }

    #endregion
}

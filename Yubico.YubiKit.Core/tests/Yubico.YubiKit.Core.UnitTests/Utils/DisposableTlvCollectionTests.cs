// Copyright (C) 2024 Yubico.
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

using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.UnitTests.Utils;

public class DisposableTlvCollectionTests
{
    [Fact]
    public void Constructor_WithParamsArray_CreatesCollectionWithCorrectCount()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);
        var tlv3 = new Tlv(0x03, [0x33]);

        // Act
        using var collection = new DisposableTlvCollection(tlv1, tlv2, tlv3);

        // Assert
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Constructor_WithIEnumerable_CreatesCollectionWithCorrectCount()
    {
        // Arrange
        var tlvs = new List<Tlv> { new(0x01, [0x11]), new(0x02, [0x22]) };

        // Act
        using var collection = new DisposableTlvCollection(tlvs);

        // Assert
        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void Constructor_WithEmptyArray_CreatesEmptyCollection()
    {
        // Act
        using var collection = new DisposableTlvCollection();

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void Indexer_ReturnsCorrectTlv()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);

        // Act
        using var collection = new DisposableTlvCollection(tlv1, tlv2);

        // Assert
        Assert.Equal(0x01, collection[0].Tag);
        Assert.Equal(0x02, collection[1].Tag);
    }

    [Fact]
    public void AsSpan_ReturnsSpanWithCorrectLength()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);

        // Act
        using var collection = new DisposableTlvCollection(tlv1, tlv2);
        var span = collection.AsSpan();

        // Assert
        Assert.Equal(2, span.Length);
    }

    [Fact]
    public void AsSpan_ContainsCorrectTlvs()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);

        // Act
        using var collection = new DisposableTlvCollection(tlv1, tlv2);
        var span = collection.AsSpan();

        // Assert
        Assert.Equal(0x01, span[0].Tag);
        Assert.Equal(0x02, span[1].Tag);
    }

    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);
        var tlv3 = new Tlv(0x03, [0x33]);

        // Act
        using var collection = new DisposableTlvCollection(tlv1, tlv2, tlv3);
        var tags = new List<int>();
        foreach (var tlv in collection) tags.Add(tlv.Tag);

        // Assert
        Assert.Equal([0x01, 0x02, 0x03], tags);
    }

    [Fact]
    public void Dispose_DisposesAllTlvs()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11, 0x22]);
        var tlv2 = new Tlv(0x02, [0x33, 0x44]);

        var collection = new DisposableTlvCollection(tlv1, tlv2);

        // Verify TLVs have non-zero values initially
        Assert.NotEqual(0, tlv1.Tag);
        Assert.NotEqual(0, tlv1.Length);
        Assert.NotEqual(0, tlv2.Tag);
        Assert.NotEqual(0, tlv2.Length);

        // Act
        collection.Dispose();

        // Assert - After disposal, Tlv properties should be zeroed
        Assert.Equal(0, tlv1.Tag);
        Assert.Equal(0, tlv1.Length);
        Assert.Equal(0, tlv2.Tag);
        Assert.Equal(0, tlv2.Length);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var tlv = new Tlv(0x01, [0x11]);
        var collection = new DisposableTlvCollection(tlv);

        // Act & Assert - Multiple dispose calls should not throw
        collection.Dispose();
        collection.Dispose();
        collection.Dispose();
    }

    [Fact]
    public void UsingDeclaration_AutomaticallyDisposesCollection()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);

        // Act
        {
            using var collection = new DisposableTlvCollection(tlv1, tlv2);
            // Collection is in scope here
            Assert.NotEqual(0, tlv1.Tag);
        }
        // Collection disposed here

        // Assert - TLVs should be disposed after using block
        Assert.Equal(0, tlv1.Tag);
        Assert.Equal(0, tlv1.Length);
        Assert.Equal(0, tlv2.Tag);
        Assert.Equal(0, tlv2.Length);
    }

    [Fact]
    public void Count_EmptyCollection_ReturnsZero()
    {
        // Act
        using var collection = new DisposableTlvCollection();

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void AsSpan_EmptyCollection_ReturnsEmptySpan()
    {
        // Act
        using var collection = new DisposableTlvCollection();
        var span = collection.AsSpan();

        // Assert
        Assert.Equal(0, span.Length);
    }

    [Fact]
    public void GetEnumerator_EmptyCollection_YieldsNoItems()
    {
        // Act
        using var collection = new DisposableTlvCollection();
        var count = 0;
        foreach (var _ in collection) count++;

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Constructor_WithLongFormTags_HandlesCorrectly()
    {
        // Arrange - Test with long form tags (multi-byte tags)
        var tlv1 = new Tlv(0x5F49, [0x01, 0x02, 0x03]); // Long form tag
        var tlv2 = new Tlv(0xA6, [0x04, 0x05]); // Short form tag

        // Act
        using var collection = new DisposableTlvCollection(tlv1, tlv2);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal(0x5F49, collection[0].Tag);
        Assert.Equal(0xA6, collection[1].Tag);
    }

    [Fact]
    public void Constructor_WithVariousLengthValues_HandlesCorrectly()
    {
        // Arrange
        var smallValue = new byte[10];
        var largeValue = new byte[500];
        Array.Fill(smallValue, (byte)0xAA);
        Array.Fill(largeValue, (byte)0xBB);

        var tlv1 = new Tlv(0x01, smallValue);
        var tlv2 = new Tlv(0x02, largeValue);

        // Act
        using var collection = new DisposableTlvCollection(tlv1, tlv2);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal(10, collection[0].Length);
        Assert.Equal(500, collection[1].Length);
    }

    [Fact]
    public void IReadOnlyList_Interface_IsImplementedCorrectly()
    {
        // Arrange
        var tlv1 = new Tlv(0x01, [0x11]);
        var tlv2 = new Tlv(0x02, [0x22]);

        // Act
        IReadOnlyList<Tlv> collection = new DisposableTlvCollection(tlv1, tlv2);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal(0x01, collection[0].Tag);
        Assert.Equal(0x02, collection[1].Tag);

        // Cleanup
        ((IDisposable)collection).Dispose();
    }
}
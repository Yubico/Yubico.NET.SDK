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

using Yubico.YubiKit.Core.Utils;

namespace Yubico.YubiKit.Core.UnitTests.Utils;

public class DisposableArrayPoolBufferTests
{
    [Fact]
    public void Constructor_ValidSize_CreatesBuffer()
    {
        // Arrange & Act
        using var buffer = new DisposableArrayPoolBuffer(32);

        // Assert
        Assert.Equal(32, buffer.Length);
        Assert.Equal(32, buffer.Memory.Length);
    }

    [Fact]
    public void Constructor_ZeroSize_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DisposableArrayPoolBuffer(0));
    }

    [Fact]
    public void Constructor_NegativeSize_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new DisposableArrayPoolBuffer(-1));
    }

    [Fact]
    public void CreateFromSpan_CopiesDataCorrectly()
    {
        // Arrange
        ReadOnlySpan<byte> source = [1, 2, 3, 4, 5];

        // Act
        using var buffer = DisposableArrayPoolBuffer.CreateFromSpan(source);

        // Assert
        Assert.Equal(5, buffer.Length);
        Assert.True(source.SequenceEqual(buffer.Memory.Span));
    }

    [Fact]
    public void Memory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var buffer = new DisposableArrayPoolBuffer(16);
        buffer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = buffer.Memory);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var buffer = new DisposableArrayPoolBuffer(16);

        // Act & Assert (should not throw)
        buffer.Dispose();
        buffer.Dispose();
        buffer.Dispose();
    }
}

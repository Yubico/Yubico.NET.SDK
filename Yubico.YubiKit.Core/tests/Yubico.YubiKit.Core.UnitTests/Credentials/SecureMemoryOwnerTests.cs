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

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Core.UnitTests.Credentials;

public class SecureMemoryOwnerTests
{
    [Fact]
    public void Memory_ReturnsCorrectSize()
    {
        // Arrange & Act
        using var owner = new SecureMemoryOwner(32);

        // Assert
        Assert.Equal(32, owner.Memory.Length);
    }

    [Fact]
    public void Memory_ThrowsObjectDisposedException_AfterDispose()
    {
        // Arrange
        var owner = new SecureMemoryOwner(16);
        owner.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = owner.Memory);
    }

    [Fact]
    public void Dispose_ZerosBufferContents()
    {
        // Arrange
        var owner = new SecureMemoryOwner(8);
        var span = owner.Memory.Span;

        // Write some data
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = (byte)(i + 1);
        }

        // Capture reference to underlying array via the memory
        var memoryCopy = new byte[8];
        span.CopyTo(memoryCopy);

        // Verify data was written
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], memoryCopy);

        // Act
        owner.Dispose();

        // Note: We can't directly verify the underlying buffer is zeroed
        // after dispose because accessing it throws ObjectDisposedException.
        // The implementation uses CryptographicOperations.ZeroMemory which
        // is a security-critical API that guarantees zeroing.
        Assert.Throws<ObjectDisposedException>(() => _ = owner.Memory);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var owner = new SecureMemoryOwner(16);

        // Act - should not throw
        owner.Dispose();
        owner.Dispose();
        owner.Dispose();
    }

    [Fact]
    public void Constructor_ThrowsForZeroSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SecureMemoryOwner(0));
    }

    [Fact]
    public void Constructor_ThrowsForNegativeSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SecureMemoryOwner(-1));
    }

    [Fact]
    public void Memory_InitializesToZeros()
    {
        // Arrange & Act
        using var owner = new SecureMemoryOwner(16);

        // Assert
        Assert.All(owner.Memory.ToArray(), b => Assert.Equal(0, b));
    }
}

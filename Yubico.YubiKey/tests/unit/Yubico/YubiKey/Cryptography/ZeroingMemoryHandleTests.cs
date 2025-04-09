// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKey.Cryptography;

public class ZeroingMemoryHandleTests
{
    [Fact]
    public void Dispose_ShouldClearArrayContent()
    {
        byte[] privateKeyData = new byte[] { 10, 20, 30, 40, 50 };

        using (var secureData = new ZeroingMemoryHandle(privateKeyData))
        {
            Assert.Equal(new byte[] { 10, 20, 30, 40, 50 }, secureData.Data);
        }

        Assert.All(privateKeyData, b => Assert.Equal(0, b)); // Ensure each byte is 0
    }

    [Fact]
    public void BasicUsage_ZeroesArrayWhenDisposed()
    {
        // Arrange
        byte[] sensitiveData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        using (var handle = new ZeroingMemoryHandle(sensitiveData))
        {
            // Verify data is accessible within scope
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, handle.Data);
        }

        // Assert
        Assert.All(sensitiveData, b => Assert.Equal(0, b));
    }

    [Fact]
    public void NestedUsage_ZeroesAllArraysWhenDisposed()
    {
        // Arrange
        byte[] keyData = new byte[] { 1, 2, 3, 4, 5 };
        byte[] ivData = new byte[] { 6, 7, 8, 9, 10 };
        byte[] resultData = new byte[5];

        // Act
        using (var keyHandle = new ZeroingMemoryHandle(keyData))
        {
            // Use key data for some operation
            using (var ivHandle = new ZeroingMemoryHandle(ivData))
            {
                // Simulate combining key and IV for an operation
                for (int i = 0; i < 5; i++)
                {
                    resultData[i] = (byte)(keyHandle.Data[i] ^ ivHandle.Data[i]);
                }
            }

            // IV should be zeroed here
            Assert.All(ivData, b => Assert.Equal(0, b));

            // Key should still be accessible
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, keyHandle.Data);
        }

        // Both key and IV should be zeroed
        Assert.All(keyData, b => Assert.Equal(0, b));
        Assert.All(ivData, b => Assert.Equal(0, b));

        // Result should be preserved since it wasn't in a handle
        Assert.NotEqual(new byte[] { 0, 0, 0, 0, 0 }, resultData);
    }

    [Fact]
    public void PassingToMethods_MaintainsAccess()
    {
        // Arrange
        byte[] sensitiveData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        using (var handle = new ZeroingMemoryHandle(sensitiveData))
        {
            // Simulate passing to a method that uses the handle
            ProcessSensitiveData(handle);

            // Data should still be accessible after the method call
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, handle.Data);
        }

        // Assert
        Assert.All(sensitiveData, b => Assert.Equal(0, b));
    }

    private void ProcessSensitiveData(
        ZeroingMemoryHandle handle)
    {
        // Simulates some processing of the sensitive data
        // Do something with data...
    }

    [Fact]
    public void AccessAfterDispose_ThrowsException()
    {
        // Arrange
        byte[] sensitiveData = new byte[] { 1, 2, 3, 4, 5 };
        var handle = new ZeroingMemoryHandle(sensitiveData);

        // Act
        handle.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => handle.Data);
    }
    
    [Fact]
    public void MultiLayerComponent_MaintainsSecurityThroughLayers()
    {
        // Arrange
        byte[] sensitiveData = new byte[] { 1, 2, 3, 4, 5 };
    
        // Act
        using (var handle = new ZeroingMemoryHandle(sensitiveData))
        {
            // First layer
            var layer1Result = FirstLayerProcessor.Process(handle);
        
            // Second layer, passing both the original handle and the layer1 result
            var layer2Result = SecondLayerProcessor.Process(handle, layer1Result);
        
            // Verify data is still accessible
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, handle.Data);
        }
    
        // Assert
        Assert.All(sensitiveData, b => Assert.Equal(0, b));
    }

// Mock processors for testing
    private static class FirstLayerProcessor
    {
        public static byte[] Process(ZeroingMemoryHandle handle)
        {
            // Simulate processing
            var result = new byte[handle.Data.Length];
            Buffer.BlockCopy(handle.Data, 0, result, 0, handle.Data.Length);
            return result;
        }
    }

    private static class SecondLayerProcessor
    {
        public static byte[] Process(ZeroingMemoryHandle originalHandle, byte[] intermediateResult)
        {
            // Simulate more processing
            for (int i = 0; i < originalHandle.Data.Length; i++)
            {
                intermediateResult[i] ^= originalHandle.Data[i];
            }
            return intermediateResult;
        }
    }
}

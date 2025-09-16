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

namespace Yubico.YubiKey.Utilities;

internal static class ByteArrayExtensions
{
    public static byte[] Concat(this ReadOnlyMemory<byte> first, params ReadOnlyMemory<byte>[] others) => ConcatCore(first, others);
    public static byte[] Concat(this byte[] arr, params byte[][] others)
    {
        if (others == null || others.Length == 0)
        {
            return (byte[])arr.Clone();
        }
       
        return ConcatCore(arr, Array.ConvertAll(others, item => (ReadOnlyMemory<byte>)item));
    }
    

    public static byte[] Concat(this ReadOnlySpan<byte> arr, ReadOnlySpan<byte> other)
    {
        int totalLength = arr.Length + other.Length;
        if (totalLength == 0)
        {
            return Array.Empty<byte>();
        }
    
        byte[] result = new byte[totalLength];
        arr.CopyTo(result);
        other.CopyTo(result.AsSpan(arr.Length));
        
        return result;
    }
    
    private static byte[] ConcatCore(this ReadOnlyMemory<byte> first, params ReadOnlyMemory<byte>[] others)
    {
        int totalLength = first.Length;
        foreach (var other in others)
        {
            totalLength += other.Length;
        }

        if (totalLength == 0)
        {
            return Array.Empty<byte>();
        }

        byte[] result = new byte[totalLength];
        var resultSpan = result.AsSpan();

        first.Span.CopyTo(resultSpan);
        int offset = first.Length;

        foreach (var other in others)
        {
            other.Span.CopyTo(resultSpan[offset..]);
            offset += other.Length;
        }

        return result;
    }
}

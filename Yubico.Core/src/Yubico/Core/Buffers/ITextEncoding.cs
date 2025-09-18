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

namespace Yubico.Core.Buffers;

/// <summary>
///     Interface for abstracting different means of encoding and decoding byte
///     collections.
/// </summary>
public interface ITextEncoding
{
    /// <summary>
    ///     Encode the byte collection into <paramref name="encoded" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When encoding sensitive data that should not be in an immutable
    ///         object, this method gives you the ability to encode directly
    ///         to memory.
    ///     </para>
    ///     <para>
    ///         The point of this method is to allow you to encode data to memory that
    ///         you own. Therefore, you must allocate this memory before calling this
    ///         method. The amount of memory required varies depending on the encoding.
    ///     </para>
    ///     <para>
    ///         For <see cref="Base16" /> and <see cref="ModHex" />, it is one character
    ///         per four bits of information, so two characters per byte.
    ///     </para>
    ///     <para>
    ///         For <see cref="Base32" />,
    ///         it is more complicated. Each character hold five bits of data. Plus, you
    ///         must account for padding at the end of the encoded data. There is a method
    ///         to calculate the space needed called <see cref="Base32.GetEncodedSize(int)" />
    ///         that will tell you how many bytes you need.
    ///     </para>
    /// </remarks>
    /// <param name="data">The data to be encoded.</param>
    /// <param name="encoded">A <see cref="Span{T}" /> to encode the data to.</param>
    void Encode(ReadOnlySpan<byte> data, Span<char> encoded);

    /// <summary>
    ///     Encode the byte collection into a string representation.
    /// </summary>
    /// <param name="data">The byte collection to encode.</param>
    /// <returns>A string representation of <paramref name="data" />.</returns>
    string Encode(ReadOnlySpan<byte> data);

    /// <summary>
    ///     Decode the string into <paramref name="data" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When decoding sensitive data that should not be in an immutable
    ///         object, this method gives you the ability to decode directly
    ///         to a char <see cref="Span{T}" />.
    ///     </para>
    ///     <para>
    ///         The point of this method is to allow you to decode data to a char collection
    ///         that you own. Therefore, you must allocate this collection before calling this
    ///         method.
    ///     </para>
    ///     <para>
    ///         For <see cref="Base16" /> and <see cref="ModHex" />, it is one character
    ///         per four bits of information, so two characters per byte.
    ///     </para>
    ///     <para>
    ///         For <see cref="Base32" />,
    ///         it is more complicated. Each character hold five bits of data. Plus, you
    ///         must account for padding at the end of the encoded data. There is a method
    ///         to calculate the space needed called <see cref="Base32.GetDecodedSize(ReadOnlySpan{char})" />
    ///         that will tell you how many bytes you need.
    ///     </para>
    /// </remarks>
    /// <param name="encoded">Encoded text.</param>
    /// <param name="data">A <see cref="Span{T}" /> to decode the data to.</param>
    void Decode(ReadOnlySpan<char> encoded, Span<byte> data);

    /// <summary>
    ///     Decode the string into a byte array.
    /// </summary>
    /// <param name="encoded">A string encoded with data to be decoded.</param>
    /// <returns>A byte collection resulting from decoding <paramref name="encoded" />.</returns>
    byte[] Decode(string encoded);
}

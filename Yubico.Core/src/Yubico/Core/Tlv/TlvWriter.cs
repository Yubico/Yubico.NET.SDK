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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Yubico.Core.Tlv;

/// <summary>
///     Use this class to build TLV (tag-length-value) constructions.
/// </summary>
/// <remarks>
///     See the User's Manual entry on
///     <xref href="UsersManualSupportTlv"> TLV</xref> for an in-depth discussion
///     of what TLV is and a general description of how to use this class.
/// </remarks>
public sealed class TlvWriter
{
    private readonly Stack<TlvNestedTlv> _nestedTlvStack;

    /// <summary>
    ///     Create a new TlvWriter object.
    /// </summary>
    public TlvWriter()
    {
        var initialNested = new TlvNestedTlv();
        _nestedTlvStack = new Stack<TlvNestedTlv>();
        _nestedTlvStack.Push(initialNested);
    }

    /// <summary>
    ///     Clear any data that had been copied from input.
    /// </summary>
    /// <remarks>
    ///     If any of the data to encode had been sensitive (such as private key
    ///     material), then call the Clear method after encoding to make sure it
    ///     is overwritten.
    ///     <para>
    ///         Call this only after the schema has been completely entered (the
    ///         outermost NestedTlv has been closed). If you call this before the schema
    ///         has been completely entered, it can throw an exception.
    ///     </para>
    /// </remarks>
    /// <exception cref="TlvException">
    ///     The method is called before a schema has been completely entered.
    /// </exception>
    public void Clear()
    {
        TlvNestedTlv initialNested = GetInitialNestedTlv();

        initialNested.Clear();
    }

    /// <summary>
    ///     Start a new Nested TLV.
    /// </summary>
    /// <remarks>
    ///     The way the caller is supposed to build Nested schemas using TlvWriter is
    ///     as follows.
    ///     <code language="csharp">
    ///   var writer = new TlvWriter();
    ///   using (writer.WriteNestedTlv(tag0))
    ///   {
    ///       writer.WriteValue(tag1, element1);
    ///       writer.WriteValue(tag2, element2);
    ///   }
    ///   byte[] encoding = tlvWriter.Encode();
    /// </code>
    ///     <para>
    ///         The using directive in this case means that when the variable goes out
    ///         of scope, the Dispose method will be called immediately. Furthermore,
    ///         when written this way (with the curly braces), the variable goes out of
    ///         scope upon completion of the close curly brace.
    ///     </para>
    ///     <para>
    ///         The WriteNestedTlv method returns an instance of TlvWriter.TlvScope.
    ///         So in the above construction, the variable for which the using is
    ///         constructed is the TlvWriter.TlvScope returned by the method.
    ///     </para>
    ///     <para>
    ///         Normally, a Dispose method overwrites sensitive data or releases
    ///         resources (files, internet connections, etc.). However, the Dispose in
    ///         this class simply makes sure the Nested TLV is closed. Any new calls to
    ///         <c>Write</c> will apply to the next level up.
    ///     </para>
    ///     <para>
    ///         There are certain calls you can make only after a schema has been
    ///         completely entered. To be completely entered means the outermost
    ///         Nested TLV has been closed (the closing curly brace).
    ///     </para>
    /// </remarks>
    /// <param name="tag">
    ///     The tag that will be written out when encoding the TLV.
    /// </param>
    public TlvScope WriteNestedTlv(int tag)
    {
        var nestedTlv = new TlvNestedTlv(tag);
        _nestedTlvStack.Push(nestedTlv);

        return new TlvScope(this);
    }

    /// <summary>
    ///     End the current NestedTlv, returning to the parent context.
    /// </summary>
    /// <exception cref="TlvException">
    ///     The method is called directly and there was no WriteNestedTlv, or the
    ///     EndNestedTlv for a Nested TLV had already been called.
    /// </exception>
    private void EndNestedTlv()
    {
        if (_nestedTlvStack.Count < 2)
        {
            throw new TlvException(ExceptionMessages.TlvInvalidSchema);
        }

        TlvNestedTlv nestedToEnd = _nestedTlvStack.Pop();
        TlvNestedTlv parent = _nestedTlvStack.Peek();
        parent.AddSubElement(nestedToEnd);
    }

    /// <summary>
    ///     Add a byte array as a value to be written out.
    /// </summary>
    /// <remarks>
    ///     When an Encode method is called, the tag and value given will be
    ///     written out as the T and V of the TLV.
    ///     <para>
    ///         If there is no data, pass an empty <c>Span</c>:
    ///         <c>ReadOnlySpan&lt;byte&gt;.Empty</c>. In that case, what is written
    ///         out is simply tag 00.
    ///     </para>
    /// </remarks>
    /// <param name="tag">
    ///     The tag that will be written out when this TLV is encoded.
    /// </param>
    /// <param name="value">
    ///     The byte array that is the value.
    /// </param>
    /// <exception cref="TlvException">
    ///     The tag is invalid, or the length is unsupported.
    /// </exception>
    public void WriteValue(int tag, ReadOnlySpan<byte> value)
    {
        var element = new TlvSubElement(tag, value);
        TlvNestedTlv currentNested = _nestedTlvStack.Peek();
        currentNested.AddSubElement(element);
    }

    /// <summary>
    ///     Add an encoded byte array to be written out.
    /// </summary>
    /// <param name="encodedTlv">
    ///     The encoded byte array that will be written out when this TLV is encoded.
    /// </param>
    /// <exception cref="TlvException">
    ///     The length is unsupported.
    /// </exception>
    public void WriteEncoded(ReadOnlySpan<byte> encodedTlv)
    {
        var element = new TlvSubElement(encodedTlv);
        TlvNestedTlv currentNested = _nestedTlvStack.Peek();
        currentNested.AddSubElement(element);
    }

    /// <summary>
    ///     Add a string as the value to be written out.
    /// </summary>
    /// <remarks>
    ///     The C# String object is essentially a character array. However, each
    ///     character is 16 bits. You can build strings using ASCII characters
    ///     (each of which is 8 bits long, with the most significant bit 0). For
    ///     example,
    ///     <code>
    ///    string someString = "ABCD"
    ///    represented internally as 00 41 00 42 00 43 00 44
    /// </code>
    ///     But suppose you want to write out a TLV with the value being the byte
    ///     array that is the ASCII character array.
    ///     <code>
    ///    tag 04 41 42 43 44
    ///   You don't want to simply copy the internal array, otherwise you
    ///   would get
    ///    tag 08 00 41 00 42 00 43 00 44
    /// </code>
    ///     <para>
    ///         To get a byte array that returns each character as a single byte, you
    ///         can use the System.Text.Encoding class. For the TlvWriter class,
    ///         this method (WriteString) will call on the Encoding class to convert
    ///         the input string into a byte array that will be the value in a TLV.
    ///         You only need supply the encoding scheme. The scheme you specify must
    ///         be in the form of a System.Text.Encoding object. It is easy to supply
    ///         such and object, simply pass in Encoding.ASCII, Encoding.UTF8, or any
    ///         of the other encoding schemes supported in that class (look at the
    ///         documentation for System.Text.Encoding, there are several properties
    ///         with the summary of "Gets an encoding for...").
    ///     </para>
    ///     <para>
    ///         For example:
    ///         <code language="csharp">
    ///    string someString = "ABCD";
    ///    writer.WriteString(0x81, someString, Encoding.ASCII);
    /// </code>
    ///     </para>
    ///     <para>
    ///         A string with non-ASCII characters will be stored internally with the
    ///         16-bit version of that character. For example, look at a string with
    ///         the "plus-minus" character.
    ///         <code language="csharp">
    ///    string someString = "A\u00B1B"; // this is A +or- B
    ///    represented internally as 00 41 00 B1 00 42
    /// </code>
    ///         Encoding that using the ASCII encoding scheme will not produce the
    ///         correct output. There are a couple of options:
    ///         <code>
    ///    writer.WriteString(tag, someString, Encoding.BigEndianUnicode);
    ///      tag 06 00 41 00 B1 00 42
    ///    writer.WriteString(tag, someString, Encoding.UTF8);
    ///      tag 04 41 C2 B1 42
    /// </code>
    ///     </para>
    /// </remarks>
    /// <param name="tag">
    ///     The tag that will be written out when this TLV is encoded.
    /// </param>
    /// <param name="value">
    ///     The string to be converted into a byte array.
    /// </param>
    /// <param name="encoding">
    ///     The encoding system to use to convert the string to a byte array,
    ///     such as System.Text.Encoding.ASCII or UTF8.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The encoding argument is null.
    /// </exception>
    /// <exception cref="TlvException">
    ///     The tag is invalid, or the length is unsupported.
    /// </exception>
    public void WriteString(int tag, string value, Encoding encoding)
    {
        if (encoding is null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        WriteValue(tag, encoding.GetBytes(value));
    }

    /// <summary>
    ///     Add a byte as the value to be written out.
    /// </summary>
    /// <param name="tag">
    ///     The tag that will be written out when this TLV is encoded.
    /// </param>
    /// <param name="value">
    ///     The byte to be converted into a byte array.
    /// </param>
    /// <exception cref="TlvException">
    ///     The tag is invalid.
    /// </exception>
    public void WriteByte(int tag, byte value)
    {
        byte[] valueArray = new[] { value };

        WriteValue(tag, valueArray);
    }

    /// <summary>
    ///     Add a 16-bit integer as the value to be written out.
    /// </summary>
    /// <param name="tag">
    ///     The tag that will be written out when this TLV is encoded.
    /// </param>
    /// <param name="value">
    ///     The short to be converted into a byte array.
    /// </param>
    /// <param name="bigEndian">
    ///     If true, write out the short as big endian, with the high order byte of
    ///     the value in the left most position in the byte array. If false,
    ///     write out the value as little endian, with the low order byte of the
    ///     value in the left most position in the byte array. The default is
    ///     true, so if no argument is given, the value will be written as big
    ///     endian.
    /// </param>
    /// <exception cref="TlvException">
    ///     The tag is invalid.
    /// </exception>
    public void WriteInt16(int tag, short value, bool bigEndian = true)
    {
        byte[] valueArray = new byte[2];

        if (bigEndian)
        {
            BinaryPrimitives.WriteInt16BigEndian(valueArray, value);
        }
        else
        {
            BinaryPrimitives.WriteInt16LittleEndian(valueArray, value);
        }

        WriteValue(tag, valueArray);
    }

    /// <summary>
    ///     Add an unsigned 16-bit integer as the value to be written out.
    /// </summary>
    /// <param name="tag">
    ///     The tag that will be written out when this TLV is encoded.
    /// </param>
    /// <param name="value">
    ///     The unsigned short to be converted into a byte array.
    /// </param>
    /// <param name="bigEndian">
    ///     If true, write out the short as big endian, with the high order byte of
    ///     the value in the left most position in the byte array. If false,
    ///     write out the value as little endian, with the low order byte of the
    ///     value in the left most position in the byte array.
    /// </param>
    [CLSCompliant(false)]
    public void WriteUInt16(int tag, ushort value, bool bigEndian = true)
    {
        byte[] valueArray = new byte[2];

        if (bigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(valueArray, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(valueArray, value);
        }

        WriteValue(tag, valueArray);
    }

    /// <summary>
    ///     Add a 32-bit integer as the value to be written out.
    /// </summary>
    /// <param name="tag">
    ///     The tag that will be written out when this TLV is encoded.
    /// </param>
    /// <param name="value">
    ///     The int to be converted into a byte array.
    /// </param>
    /// <param name="bigEndian">
    ///     If true, write out the int as big endian, with the high order byte of
    ///     the value in the left most position in the byte array. If false,
    ///     write out the value as little endian, with the low order byte of the
    ///     value in the left most position in the byte array.
    /// </param>
    /// <exception cref="TlvException">
    ///     The tag is invalid.
    /// </exception>
    public void WriteInt32(int tag, int value, bool bigEndian = true)
    {
        byte[] valueArray = new byte[4];

        if (bigEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(valueArray, value);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(valueArray, value);
        }

        WriteValue(tag, valueArray);
    }

    /// <summary>
    ///     Get the length the encoding will be.
    /// </summary>
    /// <remarks>
    ///     Note that this will only return the length if a full schema has been
    ///     entered. Otherwise it will thorw an exception.
    ///     <para>
    ///         Call this only after the schema has been completely entered (the
    ///         outermost Nested TLV has been closed). If you call this before the schema
    ///         has been completely entered, it can throw an exception.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The total length of the result of a call to Encode.
    /// </returns>
    /// <exception cref="TlvException">
    ///     The method is called before a schema has been completely entered.
    /// </exception>
    public int GetEncodedLength()
    {
        TlvNestedTlv initialNested = GetInitialNestedTlv();

        return initialNested.EncodedLength;
    }

    /// <summary>
    ///     Write out the encoding of the structure defined, returning a
    ///     new byte array containing the result.
    /// </summary>
    /// <remarks>
    ///     Call this only after the schema has been completely entered (the
    ///     outermost Nested TLV has been closed). If you call this before the schema
    ///     has been completely entered, it can throw an exception.
    /// </remarks>
    /// <returns>
    ///     A new byte array containing the encoding.
    /// </returns>
    /// <exception cref="TlvException">
    ///     The method is called before a schema has been completely entered.
    /// </exception>
    public byte[] Encode()
    {
        TlvNestedTlv initialNested = GetInitialNestedTlv();

        byte[] encoding = new byte[initialNested.EncodedLength];

        if (!initialNested.TryEncode(encoding, 0, out _))
        {
            throw new TlvException(ExceptionMessages.TlvInvalidSchema);
        }

        return encoding;
    }

    /// <summary>
    ///     Write out the encoding of the structure defined, placing the
    ///     result into the destination.
    /// </summary>
    /// <remarks>
    ///     This will try to write out the encoding. If the destination buffer is
    ///     not big enough, the method will return false (no data was written).
    ///     <para>
    ///         Call this only after the schema has been completely entered (the
    ///         outermost Nested TLV has been closed). If you call this before the schema
    ///         has been completely entered, it can throw an exception.
    ///     </para>
    /// </remarks>
    /// <param name="destination">
    ///     The Span into which the encoding will be placed.
    /// </param>
    /// <param name="bytesWritten">
    ///     On success, receives the number of bytes written into the destination.
    /// </param>
    /// <returns>
    ///     A bool, true if the method successfully encoded, false otherwise.
    /// </returns>
    /// <exception cref="TlvException">
    ///     The method is called before a schema has been completely entered.
    /// </exception>
    public bool TryEncode(Span<byte> destination, out int bytesWritten)
    {
        TlvNestedTlv initialNested = GetInitialNestedTlv();

        return initialNested.TryEncode(destination, 0, out bytesWritten);
    }

    /// <summary>
    ///     Return the initial Nested TLV object. This method will throw an exception
    ///     if the initial Nested TLV is not the only member in the stack.
    /// </summary>
    /// <remarks>
    ///     Note that this will not pop the initial Nested TLV from the stack. It
    ///     will simply verify that it is the only Nested TLV left in the stack and
    ///     then return a reference to it.
    /// </remarks>
    /// <returns>
    ///     The initial NestedTlv object created when the TlvWriter object was
    ///     built.
    /// </returns>
    /// <exception cref="TlvException">
    ///     The method is called before a schema has been completely entered.
    /// </exception>
    private TlvNestedTlv GetInitialNestedTlv()
    {
        if (_nestedTlvStack.Count != 1)
        {
            throw new TlvException(ExceptionMessages.TlvInvalidSchema);
        }

        return _nestedTlvStack.Peek();
    }

    #region Nested type: TlvScope

    /// <summary>
    ///     The struct that defines the scope of a Nested TLV. An instance of this
    ///     Struct is returned by a call to TlvWriter.WriteNestedTlv.
    /// </summary>
    /// <remarks>
    ///     The way the caller is supposed to build Nested schemas using TlvWriter is
    ///     as follows.
    ///     <code language="csharp">
    ///   var writer = new TlvWriter();
    ///   using (writer.WriteNestedTlv(tag0))
    ///   {
    ///       writer.WriteValue(tag1, element1);
    ///       writer.WriteValue(tag2, element2);
    ///   }
    ///   byte[] encoding = tlvWriter.Encode();
    /// </code>
    ///     <para>
    ///         The using directive in this case means that when the variable goes out
    ///         of scope, the Dispose method will be called immediately. Furthermore,
    ///         when written this way (with the curly braces), the variable goes out of
    ///         scope upon completion of the close curly brace.
    ///     </para>
    ///     <para>
    ///         The WriteNestedTlv method returns an instance of TlvWriter.TlvScope.
    ///         So in the above construction, the variable for which the using is
    ///         constructed is the TlvWriter.TlvScope returned by the method.
    ///     </para>
    ///     <para>
    ///         Normally, a Dispose method overwrites sensitive data or releases
    ///         resources (files, internet connections, etc.). However, the Dispose in
    ///         this class simply calls the TlvWriter.EndNestedTlv.
    ///     </para>
    ///     <para>
    ///         When we're building a schema, we want to make sure the elements that
    ///         belong under a particular NestedTlv are placed there and not anywhere
    ///         else. In order to do so, we have a method that says, Start Nested TLV.
    ///         Now, every Element that is added, until we hit the end, will be
    ///         subordinate to this Nested TLV. When we have added all the sub-elements
    ///         to a Nested TLV, we can call the End method. However, a decision was made
    ///         not to expose the End method, but to use this "using" construction.
    ///         The reason is so that code can be written to have a structure where
    ///         the indentations in the code match the schema.
    ///     </para>
    ///     <para>
    ///         Really, that's why it is done this way. To make the code have the same
    ///         visual style of the schema. For example:
    ///         <code>
    ///   Suppose you have an encoding that will look something like this:
    ///     7C len                Nested TLV
    ///        81 len value
    ///        82 len value
    ///        7D len             Nested TLV
    ///           83 len value
    ///           84 len value
    ///        85 len value
    /// </code>
    ///         Code following the pattern that uses "using" and TlvWriter.TlvScope
    ///         would look something like this.
    ///         <code language="csharp">
    ///   var writer = new TlvWriter();
    ///   using (writer.WriteNestedTlv(0x7C))
    ///   {
    ///       writer.WriteValue(0x81, value81);
    ///       writer.WriteValue(0x82, value82);
    ///       using (writer.WriteNestedTlv(0x7C))
    ///       {
    ///           writer.WriteValue(0x83, value83);
    ///           writer.WriteValue(0x84, value84);
    ///       }
    ///       writer.WriteValue(0x85, value85);
    ///   }
    ///   byte[] encoding = tlvWriter.Encode();
    /// </code>
    ///     </para>
    /// </remarks>

    //
    // When we're following a schema, we want to make sure the elements that
    // belong under a particular NestedTlv are placed there and not anywhere
    // else. In order to do so, we have a method that says, Start Nested TLV.
    // Now, every Element that is added, until we hit the end, will be
    // subordinate to this Nested TLV. When we have added all the sub-elements
    // to a Nested TLV, we can call the End method. However, a decision was made
    // not to expose the End method, but to use this "using" construction.
    // The reason is so that code can be written to have a structure where
    // the indntations in the code match the schema.
    //
    // Really, that's why it is done this way. To make the code have the same
    // visual style of the schema. For example:
    //
    //   Suppose you have an encoding that will look something like this:
    //     7C len                Nested TLV
    //        81 len value
    //        82 len value
    //        7D len             Nested TLV
    //           83 len value
    //           84 len value
    //        85 len value
    //
    // Code following the pattern that uses "using" and TlvWriter.TlvScope
    // would look something like this.
    //
    //   var writer = new TlvWriter();
    //   using (writer.WriteNestedTlv(0x7C))
    //   {
    //       writer.WriteValue(0x81, value81);
    //       writer.WriteValue(0x82, value82);
    //       using (writer.WriteNestedTlv(0x7C))
    //       {
    //           writer.WriteValue(0x83, value83);
    //           writer.WriteValue(0x84, value84);
    //       }
    //       writer.WriteValue(0x85, value85);
    //   }
    //   byte[] encoding = tlvWriter.Encode();
    //
    // We are making this class nested (inside the TlvWriter class for two
    // reasons. One, this is the pattern used by .NET's AsnWriter class. And
    // two, because we don't really want anyone other than TlvWriter to be
    // able to use it. We need to make it public so that we can return it to
    // the caller (it's the object that is referenced by the using
    // structure), but we don't want the public to do anything with it. We
    // just want them to have it in the using construction. If we make it
    // nested, we can make it public (disabling warnings, see below) but
    // make its constructor internal to discourage anyone other than
    // TlvWriter to build one.
    //
    // Note that we are disabling warnings CA1034 and CA1815. CA1034 says do
    // not make a nested class public, and CA1815 says a Struct must
    // implement Equals along with == and !=.
    //
    // The language specifies that a nested type can be public, it's just
    // recommended it not be done. In fact, .NET has documentation that
    // essentially says to never make a nested type public, and declares,
    // "Do not suppress a warning from this rule." However, the .NET
    // AsnWriter class suppresses the warning and the Tlv classes are
    // following the AsnWriter pattern.
    //
    // We are also suppressing CA1815, which demands that a struct override
    // the Equals method. Once again, the .NET AsnWriter class overrides
    // this warning, and we are following their pattern. In addition, the
    // .NET documentation does declare, "It is safe to suppress a warning
    // from this rule if instances of the value type will not be compared to
    // each other." The only use of this struct is to implement the Dispose
    // method and allow the using construction. Hence, there is no need to
    // compare instances. We could have made this a Class instead of a
    // Struct, but we are following the AsnWriter pattern.
    #pragma warning disable CA1034, CA1815 // see comments above
    public struct TlvScope : IDisposable
    {
        private TlvWriter? _writer;

        /// <summary>
        ///     Create a new TlvScope object connected to the TlvWriter object
        ///     provided.
        /// </summary>
        /// <param name="writer">
        ///     The TlvWriter object that was used to create the NestedTlv element
        ///     that is generating this Scope.
        /// </param>
        internal TlvScope(TlvWriter writer)
        {
            _writer = writer;
        }

        /// <summary>
        ///     When the Scope object goes out of scope, this method is called. It
        ///     will make sure the Nested TLV is ended and any new additions to the
        ///     TlvWriter object will be associated with the Nested TLV's parent.
        /// </summary>

        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to structs. There is no Finalizer
        // for a struct, because they are value types and not subject to
        // garbage collection. So the Dispose method will simply perform the
        // End process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_writer is not null)
            {
                _writer.EndNestedTlv();
                _writer = null;
            }
        }
    }
    #pragma warning restore CA1034, CA1815

    #endregion
}

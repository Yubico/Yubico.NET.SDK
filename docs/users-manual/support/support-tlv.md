---
uid: UsersManualSupportTlv
summary: *content
---

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# TLV

A "tag-length-value" (TLV) construction is a byte array that has a tag indicating what the
data is, a length specifying its length (in bytes/octets), and the value itself.

For example, here is one possible TLV.

```txt
    08 04 72 26 9A 33
    ^  ^  ^         ^
    |  |  |---------|
    |  |     value
    |  - length
    - tag
```

The TLV allows groups of variable-length data elements to be combined into one buffer. To
parse a collection of elements, a reader must know where one element ends and the next
begins, and which element is which.

A standard will specify a schema for the data. It is the programmer's job to "convert"
that schema into a byte array. For example, this could be a schema for an RSA public key.

```txt
    schema for RSA public key:
    { 51 L1 modulus || 52 L2 publicExponent }

    For this schema,
      the tag of 51 means modulus
      L1 is the length of the modulus
      the tag of 52 means public exponent
      L2 is the length of the publicExponent

    As a stream of octets, it might look something like this.

    51 81 80 A5 29 ... 3B 52 03 01 00 01

    The reader sees the 51 tag and knows the modulus follows
    The reader sees the 81 and knows the length is represented as the one following octet
    The reader sees the 80 and knows the length is 128 (0x80 = decimal 128)
    The reader can now read the next 128 bytes as the modulus
    The reader sees the 52 tag and knows the public exponent follows
    The reader sees the 03 and knows the length of the value is 3
      (because the first length octet is not 8x, then that is the length)
    The reader can now read the next 3 bytes as the public exponent
```

## ASN.1 and DER

There is a standard called ASN.1 (Abstract Syntax Notation One), and another standard
called DER (Distinguished Encoding Rules). These make up one possible TLV implementation.
The ASN.1 standard specifies how to make definitions of collections of data, and the DER
standard specifies how to "convert" those definitions into actual "bytes on the wire".

In other words, there is the concept of "TLV" and ASN.1 with DER is one specific way to
build a TLV system.

The TLV classes in the SDK are not specifically ASN.1 and DER, they are more general. The
SDK's TLV classes can be used to build and parse ASN.1/DER constructions. However, the
SDK needs to build and parse TLV constructions that do not adhere to the ASN.1 and DER 
standards.

For example, with ASN.1 and DER, a tag describes what type of data follows, such as
INTEGER, UTCTime, or UTF8String. An INTEGER always has a tag of `02`. For example, both
the RSA modulus and public exponent will have tags of `02`. However, in the standards
that the SDK follows, a tag often describes the data that follows, not its type. Hence, a
modulus can have a tag of `51` and the public exponent can have a tag of `52`.

## TLV classes in the SDK

The two classes needed to build and parse TLV constructions are

```csharp
    Yubico.Core.TlvWriter
    Yubico.Core.TlvReader
```

These classes build and parse TLV constructions where

* The tag is either one or two octets
* The length follows the DER standard for length construction and represents the number of
  bytes/octets of the value
* These classes build and parse two kinds of TLV constructions: concatenation and nested.

### Tag

The tag can be one or two octets. How does the writer class know whether to write one or
two octets? The input is an `int` (a 4-byte type). The minimum tag is `0x00000000` and the
maximum tag is `0x0000FFFF`. For example,

```csharp
    tlvWriter.WriteValue(0x0000725F, value);
```

In this case, the writer class knows this is a two-octet tag.

When reading a TLV, how does the reader class know a tag is one or two octets? For
example, if it sees `72 5f`, how does it know this is a two-octet tag or a one-octet tag
with a length of `5F`? The answer is that the caller must supply the expected tag. That
is, the TLV does not simply read, it reads according to the schema.

```csharp
    value = tlvWriter.ReadValue(0x725F);
```

In this case, the schema says that at this point in the encoding, the value has a tag of
`725F`. So when reading, your code says, "Read the next element, the expected tag is
`725F`."

There is a method, `PeekTag`, that allows you to look at the next tag before decoding. Use
this if the tag might be one of a number of values. Take a look at the tag, then if it is
one of the acceptable values, call the `ReadValue` method with the tag returned by `Peek`.

### Length

It is important to know that the length describes the number of bytes/octets, which is not
necessarily the number of items being represented. For example, if a schema specifies a tag
for two 32-bit integers, the TLV could be something like this.

```txt
    7F 08 00 00 01 00 ff ff ff ff
```

This represents two integers: `0x00000100 = decimal 256` and `0xffffffff = decimal -1`.
The length in the TLV is 8, meaning there are 8 octets. The length is not 2, even though
this TLV represents 2 things.

The length is constructed following the DER standard.

```txt
    actual length         encoded length           example
   --------------------------------------------------------------
     0x00 - 0x7f           one length octet    20 (decimal 32)
     0x80 - 0xFF             two octets:       81 81 (decimal 129)
                              81 length        81 A7 (decimal 167)
     0x0100 - 0xFFFF        three octets:      82 01 00 (decimal 256)
                              82 L1 L2         82 15 4B (decimal 5,451)
     0x010000 - 0xFFFFFF    four octets:       83 01 83 B0 (decimal 99,248)
                            83 L1 L2 L3
```

When reading, these rules mean

* If the first length octet `< 0x80`, that is the length.
* If the first length octet is `0x8x`, then the length is the next `x` octets.
  * DER allows for a 15 octet length (`8F` as the first length octet), however, virtually
    all implementatations will limit the number of octets that make up the length to 3, 4,
    or 5. The TLV classes in the SDK limit the length to three octets (e.g. `83 01 00 00`,
    decimal 65,536).
* If the first length octet is `> 0x8y`, where `y` is the maximum count for the
  implementation, that is an error. For the SDK, `y` is 3, so `84` is unsupported, but
  `0x92` or `0xC7` are also invalid.

Note that a length of zero is allowed. That means there is no following data.

### Concatenation and nested

With concatenation, the encoded data is simply

```txt
   TLV || TLV || ... || TLV

 For example:
   01 01 86 02 02 05 05 08 04 01 26 9A 33
     or for better visual clarity:
   01 01 86    02 02 05 05    08 04 01 26 9A 33
```

With nested, there is more of a tree structure, where a collection of elements is packaged
into a "parent" TL:

```txt
   TL { TLV || ... || TLV }

 For example:
   81 0D 01 01 86 02 02 05 05 08 04 01 26 9A 33
     or for better visual clarity:
   81 0D
      01 01
         86
      02 02
         05 05
      08 04
         01 26 9A 33
```

The nested is a representation of one collection of multiple elements. In the example
above, there was one thing with a tag of 81 and a length of `0D = decimal 13`. The 13
octets that made up the contents of that one thing happened to be 3 different elements.

It is certainly possible to have a nested TLV inside another nested TLV. For example, a
standard might specify a schema such as this.

```txt
    7A L1 { 01 01 algorithm, 7F L2 { 02 L3 challenge, 05 L4 response } }

    7A 19
       01 01
          07
       7F 14
          02 08
             38 86 D9 A9 0C 91 EE 71
          05 08
             81 1B 40 D5 70 AB 35 0F
```

### Build/Encode

To build a TLV construction (to create an encoding) using the SDK, use the `TlvWriter`
class.

#### Concatenation

Suppose the standard calls for a concatenation. For example, a schema might look like the
following.

```txt
   { 01 algorithm || 02 retry counts || 08 serial number }
```

The code to write it could look like this.

```csharp
   var tlvWriter = new TlvWriter();
   tlvWriter.WriteByte(0x01, 0x07);
   tlvWriter.WriteValue(0x02, retryCountArray);
   tlvWriter.WriteInt32(0x08, serialNumber);

   byte[] encoding = tlvWriter.Encode();
   tlvWriter.Clear()
```

In the example, you simply instantiate, then add each of the elements. There are a number
of ways to specify the value: as a byte, an int, a byte array, and more. The `Encode`
method will build the encoding. It will be able to compute all the lengths, knowing which
require a single byte (length < 0x80) and which require a longer length construction
(e.g. `81 94`).

If there is no data, pass in an empty `ReadOnlySpan`.

```csharp
    // Suppose the schema calls for a key name with tag `0x78`, but
    // there is no name.
    tlvWriter.WriteValue(0x78, ReadOnlySpan<byte>.Empty);

    // When this gets encoded, the element will be written as
    //   78 00
```

The `Clear` method is optional. This calls on the `TlvWriter` object to overwrite any data
it copied. This is discussed further later on.

#### Nested elements

A standard might specify a schema with nested elements, such as the following.

```csharp
    // 7A L1 { 01 01 algorithm, 02 L2 challenge }

    // Build a TlvWriter, specify the NestedTlv, and then add each of the elements.

    var tlvWriter = new TlvWriter();
    using (tlvWriter.WriteNestedTlv(0x7A))
    {
        tlvWriter.WriteByte(0x01, 0x07);
        tlvWriter.WriteValue(0x02, challengeArray);
    }
    byte[] encoding = tlvWriter.Encode();
    tlvWriter.Clear();
```

It is possible you have a Nested TLV inside another Nested TLV.

```csharp
    // 7A L1 { 01 01 algorithm, 7F L2 { 02 L3 challenge, 05 L4 response }, 09 01 digest }

    var tlvWriter = new TlvWriter();
    using (tlvWriter.WriteNestedTlv(0x7A))
    {
        tlvWriter.WriteByte(0x01, 0x07);
        using (tlvWriter.WriteNestedTlv(0x7F))
        {
            tlvWriter.WriteValue(0x02, challengeArray);
            tlvWriter.WriteValue(0x05, responseArray);
        }
        tlvWriter.WriteByte(0x09, 0x22);
    }
    byte[] encoding = tlvWriter.Encode();
    tlvWriter.Clear();
```

#### WriteEncoded

Sometimes you have something already encoded and you need to add it to an existing schema.
In that case, there is a `WriteEncoded`. This does not compute the length of an input
value, it simply copies the entire input into the existing schema.

For example, suppose you have a schema that specifies one element as a certificate. You
have code already that builds a certificate encoding. You don't want to copy that code
every place a certificate is needed. When you need to add a certifiate to a schema, call
the method that builds the encoding. You now have the full TLV of a certificate, not just
the value. Call `WriteEncoded`.

```csharp
    byte[] encodedCert = certObject.GetEncodedCertificate();

    var tlvWriter = new TlvWriter();
    using (tlvWriter.WriteNestedTlv(0x30))
    {
        tlvWriter.WriteString(0x0C, someName);
        tlvWriter.WriteValue(0x04, someReference);
        tlvWriter.WriteEncoded(encodedCert);
    }
    byte[] encoding = tlvWriter.Encode();
    tlvWriter.Clear();
```

If you had called `WriteValue` with the fully encoded certificate as the value, the
`TlvWriter` would have written out an "extra" tag and length.

#### Clear

Suppose you are encoding an RSA private key. You provide to the `TlvWriter` class the two
primes among other sensitive information. You want this data to appear in memory for as
short of a time as possible (see the User's Manual article on
[sensitive data](../sdk-programming-guide/sensitive-data.md)). Has the `TlvWriter` object
copied any of that data into a new buffer? If so, you want it to be overwritten.

Call the `Clear` method when you are done with the writer object. Any information it
copied will be overwritten. Any reference copies will be ignored.

### Parse/Decode

To parse a TLV construction (to decode an encoding) using the SDK, use the `TlvReader`
class.

#### Concatenation

Suppose the standard calls for a concatenation and you have a buffer that purportedly
contains an encoding of the definition.

```csharp
    // { 01 algorithm || 02 retry counts || 08 serial number }
    // 
    // Suppose the encoding is
    //   01 01 07 02 02 05 05 08 04 01 26 9A 33

    var tlvReader = new TlvReader(encoding);
    byte algorithm = tlvReader.ReadByte(0x01);
    ReadOnlyMemory<byte> retryCounts = tlvReader.ReadValue(0x02);
    int serialNumber = tlvReader.ReadInt32(0x08);
```

The `TlvReader` object you instantiate will copy a reference to the `encoding`. The object
begins life with an internal position of zero, the beginning of the `encoding`.

When you call the `ReadByte` method, the object will look at its internal position and see
if the tag there is `01`. If it is, it will read the byte that is the value of the TLV. It
will then move the position to the byte just beyond the current encoding. In this case, it
moves to position 3, where the first `02` is. Finally, it will return the byte it read.

The next call, `ReadValue`, will verify the next tag is what was expected, determine the
length of the value, build a return `ReadOnlyMemory<byte>`, move the internal position (to
position 7, the `08`), and return the newly created `ReadOnlyMemory` object.

This `ReadOnlyMemory` object will "point" to the input encoding array. That is, the reader
object will not copy the data, it will only point to where, in the encoding, the value
begins. If you ran the experiment where you decoded this element, then changed
`encoding[5]`, that change would be reflected in the return value.

```csharp
    // encoding = 01 01 07 02 02 05 05 08 04 01 26 9A 33

     . . .
    ReadOnlyMemory<byte> retryCounts = tlvReader.ReadValue(0x02);

    // encoding[5] is 05
    // retryCount.Span[0] is 05

    // set encoding[5] = 0x06
    // now look at retryCount.Span[0], it is also 06
```

Finally, read the last element as a 32-bit integer.

Note that the `ReadByte` method will fail if the length of the element it is reading is
not one, and the `ReadInt32` method will fail if the length of the element it is reading
is not exactly 4.

If there is no data (there is a tag and the length is `00`), the Read will return an empty
`ReadOnlyMemory<byte>` object (`value.Length` is 0).

Note also that there is no `Clear` method for `TlvReader`. That class never copies data,
it only copies references.

#### Nested

Suppose the standard calls for a nested and you have a buffer that purportedly contains an
encoding of the definition.

```csharp
    // 7A L1 { 01 01 algorithm, 02 L2 challenge }
    // 
    // Suppose the encoding is
    //   7A 0D 01 01 07 02 08 38 86 D9 A9 0C 91 EE 71
    // Or for better visual clarity
    //   7A 0D
    //      01 01
    //         07
    //      02 08
    //         38 86 D9 A9 0C 91 EE 71
```

You could read the entire encoding as a concatenation of one element.

```csharp
    var tlvReader = new TlvReader(encoding);
    ReadOnlyMemory<byte> value = tlvReader.ReadValue(0x7A);

    // The contents of value are
    //   01 01 07   02 08 38 86 D9 A9 0C 91 EE 71
```

You now have a new `ReadOnlyMemory` buffer, the value. This new buffer contains a simple
concatenation with two elements. You could create a new `TlvReader` with this data, and
read the two elements.

```csharp
    var tlvReader = new TlvReader(encoding);
    ReadOnlyMemory<byte> value = tlvReader.ReadValue(0x7A);

    TlvReader anotherReader = new TlvReader(value);
    byte algorithm = anotherReader.ReadByte(0x01);
    ReadOnlyMemory<byte> challenge = anotherReader.ReadValue(0x02);
```

There is a more efficient way to do this.

```csharp
    var tlvReader = new TlvReader(encoding);
    TlvReader nestedReader = tlvReader.ReadNestedTlv(0x7A);
    byte algorithm = nestedReader.ReadByte(0x01);
    ReadOnlyMemory<byte> challenge = nestedReader.ReadValue(0x02);
```

You create the `TlvReader` object for the entire encoding. When you read the nested
construction, it creates a new `TlvReader`, this one able to read only the nested data.
This means you don't have to create an intermediate `ReadOnlyMemory` and make a call to
create a new `TlvReader`. This will be even more efficient when there are nesteds in
nesteds.

After making the call to read a nested, you have a new reader object. Use that object to
read what is under the nested tag.

Upon instantiation, the original `tlvReader` has a reference to the encoding and its
internal position is zero. After calling `ReadNestedTlv`, it moves to the end of the
current element it is reading. The current element it is reading happens to be the entire
encoding, so it moves to the end (a call to `tlvReader.HasData` would return `false`).

The new `nestedReader` object also points to the encoding, but its internal position is 2,
pointing to the first element in the nested construction.

#### Multiple nesteds

Suppose you have an encoding like this.

```csharp
   // 30 17
   //    02 01
   //       01
   //    30 0A
   //       04 04
   //          11 22 33 44
   //       0C 02 
   //          38 36
   //    03 05
   //       00 77 88 99 AA BB

    var tlvReader = new TlvReader(encoding);
    // tlvReader points to position 0.

    TlvReader nestedReader = tlvReader.ReadNestedTlv(0x30);
    // tlvReader points to position 25 (0x19), beyond the end, HasData is false
    // nestedReader points to position 2

    byte version = nestedReader.ReadByte(0x02);
    // nestedReader points to position 5

    TlvReader internalReader = nestedReader.ReadNestedTlv(0x30);
    // nestedReader points to position 17 (0x11)
    // internalReader points to position 7

    ReadOnlyMemory<byte> valueA = internalReader.ReadValue(0x04);
    // internalReader points to position 13 (0x0D)
    // valueA is a Slice of encoding, from position 9 to 12

    ReadOnlyMemory<byte> valueB = internalReader.ReadValue(0x0C);
    // internalReader points to position 17 (0x11)
    // this is beyond the end of this element, so HasData is false
    // valueB is a Slice of encoding, from position 15 to 16

    ReadOnlyMemory<byte> valueC = nestedReader.ReadValue(03);
    // nestedReader points to position 25 (0x19)
    // this is beyond the end of this element, so HasData is false
    // valueC is a Slice of encoding, from position 19 to 24
```

When you read an element, you can read it as a value, even if it is nested. If you want
to read what is in a nested, you need to create a new reader object, either by reading the
value and calling `new TlvReader` yourself, or by calling `ReadNested`.

The `ReadNestedTlv` method builds a new `TlvReader` (e.g. internalNested), but this new
reader is only able to see the bytes that made up that element. It is looking at a Slice
of the original encoding.

#### Reading encoded

Another read method is `ReadEncoded`. This reads an entire element, returning a "pointer"
to the full TLV of that element, not just the V. For example, suppose we have this
encoding.

```csharp
   // 30 17
   //    02 01
   //       01
   //    30 0A
   //       04 04
   //          11 22 33 44
   //       0C 02 
   //          38 36
   //    03 05
   //       00 77 88 99 AA BB

    var tlvReader = new TlvReader(encoding);
    TlvReader nestedReader = tlvReader.ReadNestedTlv(0x30);

    byte version = nestedReader.ReadByte(0x02);

    ReadOnlyMemory<byte> toBeSigned = nestedReader.ReadEncoded(0x30);

    ReadOnlyMemory<byte> valueC = nestedReader.ReadValue(03);
```

Look inside the `toBeSigned` buffer and you will see

```txt
    30 0A 04 04 11 22 33 44 0C 02 38 36

    toBeSigned.Length will be 12
```

This might be useful if you have code already written to decode something very
complicated. For example, suppose one element is a certificate. You don't want to write
the certificate parsing code every time. You have a method for that. But that method needs
the entire certificate encoding. Or maybe the data to sign (or verify) is the full
encoding of something, and that something is an element in another encoding. You need to
extract its entire encoding: TLV, not just V.

#### Reading optional values

Suppose you are reading a construction where one or more of the elements are optional. Or
maybe you are reading a construction where the order of elements is optional. For example,
maybe the schema is

```txt
    { 81 L1 RsaModulus || 82 L2 RsaPublicExponent || 87 L3 EcPublicPoint }

    -- If a modulus is present, there must be a public exponent, and there must not
    be a public point.
    -- The order of modulus and public exponent is not prescribed.
    -- If a public point is present, there must not be a modulus or public exponent.
```

In this case, there are a number of valid encodings

```txt
   81 82 01 00 modulus 82 03 01 00 01
   82 03 01 00 01 81 82 01 00 modulus
   87 41 publicPoint
```

The following code looks correct, but it will lead to an exception.

```csharp
    var reader = TlvReader(encoding);
    ReadOnlyMemory<byte> modulus = reader.ReadValue(0x81);
    ReadOnlyMemory<byte> pubExpo = reader.ReadValue(0x82);
    ReadOnlyMemory<byte> pubPoint = reader.ReadValue(0x87);
```

Something is not going to be there, so at some point the `Read` will throw an exception,
because it will run out of data or an expected tag is missing.

In this case, you must find out what the next tag is before calling `ReadValue`. There is
a method in `TlvReader` to do that.

```csharp
    ReadOnlyMemory<byte> modulus;
    ReadOnlyMemory<byte> pubExpo;
    ReadOnlyMemory<byte> pubPoint;

    var reader = TlvReader(encoding);

    // If the next tag is modulus, read modulus, then expo, and
    // nothing else.
    // If the next tag is expo, read expo, then modulus, and
    // nothing else.
    // If the next tag is public point, read the public point and
    // nothing else.
    int nextTag = reader.PeekTag();
    switch (nextTag)
    {
        case 81:
            modulus = reader.ReadValue(0x81);
            pubExpo = reader.ReadValue(0x82);
            break;

        case 82:
            pubExpo = reader.ReadValue(0x82);
            modulus = reader.ReadValue(0x81);
            break;

        case 87:
            pubPoint = reader.ReadValue(0x87);
            break;

        default:
            ReportError();
            break;
    }
```

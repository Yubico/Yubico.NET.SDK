---
uid: TheFido2Pin
---

<!-- Copyright 2022 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# The FIDO2 PIN

The FIDO2 standards contain some special requirements on the PIN. In brief, the PIN must
be supplied as "... the UTF-8 representation of" the "Unicode characters in Normalization
Form C." What does that mean? How does one build such a PIN?

## Unicode characters

First, let's look at "Unicode characters". The Unicode standard specifies a number for
each character supported. For example, the number for cap-A is `U+0041` or `0x000041`.
The number for the lower case greek letter pi (&#960;) is `U+03C0`. There is no logical
limit to numbers, but currently the maximum Unicode number is `0x10FFFF` (21-bits, or 3
bytes).

Unfortunately, it is also possible to create "combinations". There is a "block" of unicode
numbers that are "combining diacritical marks", meaning that when they appear in an array
of characters, software that can render Unicode will know to combine them with the
previous character. For example, the Unicode for lower case `e` is `U+0065`, and the
Unicode for "acute accent" is `U+0301` (the acute accent is a small, diagonal line above a
letter, sort of like a single quote or forward slash). Combine the two

```csharp
    char[] eWithAcute = new char[] { '\u0065', '\u0301' };
```

and the result is a lower case `e` with an acute accent: &#233;.

There is also a Unicode number for an `e` with an acute accent: `U+00E9`. In other words,
there are two ways to represent this letter in Unicode.

```csharp
    char[] eWithAcute = new char[] { '\u0065', '\u0301' };
    char[] sameCharacter = new char[] { '\u00E9' };
```

## Normalization

In order to use a PIN, there has to be one and only one way to encode the characters.
Otherwise, someone could enter the correct PIN and if the underlying platform encodes it
differently than the original one, then it would not authenticate. So the second element
of the PIN is normalization. There is a standard that specifies how to "convert" most of
the combinations into single numbers. For example, normalization can convert
`0065 0301` into `00E9`.

Hence if your PIN is normalized, then there is only one set of numbers to represent it.
The standard specifies a number of ways to normalize, and FIDO2 has chosen the technique
described as "Form C".

## UTF-8

Once the PIN has been normalized, it is in essence an array of Unicode numbers. It would
be possible to specify that each character in the PIN be a 3-byte (big endian) number. It
would also be possible to specify that only 16-bit characters be allowed in a PIN and
encode it as an array of 2-byte values. However, the standard specifies encoding it as
UTF-8. In this encoding scheme, many characters can be expressed as a single byte, rather
than two or three. In addition, there are no `00` bytes in UTF-8. For example, cap-C is
`U+0043` and in UTF-8, it is `0x43`. The letter pi is `U+03C0`, and is encoded in UTF-8 as
`0xCB80`. In this way, it is possible to save space by "eliminating" many of the `00`
bytes.

Actually, the encoding scheme is efficient only in that it treats ASCII characters as
single bytes. There are non-ASCII Unicode characters that are only one byte (`U+00xx`),
and are UTF-8 encoded as two bytes, and some two-byte Unicode characters that are
encoded using three bytes, and three-byte Unicode encoded in four bytes. However, because
ASCII characters are the most-used characters, the efficienices usually outweigh the
inefficiencies.

## C# and Unicode

Your PIN collection code will likely include some code that does something like this.

```csharp
    while (someCheck)
    {
        ConsoleKeyInfo currentKeyInfo = Console.ReadKey();
        if (currentKeyInfo.Key == ConsoleKey.Enter)
        {
            break;
        }

        inputData = AppendChar(currentKeyInfo.KeyChar, inputData, ref dataLength);
    }
```

You read each character in the PIN as a `char` and append it to a `char[]`. You could use
the `string` class, but Microsoft recommends not using the `string` class to hold sensitive
data. This is because:

> System.String instances are immutable, operations that appear to modify an existing
> instance actually create a copy of it to manipulate. Consequently, if a String object
> contains sensitive information such as a password, credit card number, or personal data,
> there is a risk the information could be revealed after it is used because your
> application cannot delete the data from computer memory.

By reading each PIN as a `char`, you are limiting the characters you support to those that
can be represented as a 16-bit number in the Unicode space. You would not support
`U-10000` to `U+10FFFF`. This will almost certainly be no problem, because these numbers
almost exclusively represent emojis and other figures (e.g. U+1F994 is a hedgehog:
&#129428;), along with rare alphabets (e.g. U+14400 to U+14646 are for Anatolian
hieroglyphs).

You now have a char array to represent the PIN.

### C# and Normalization

At this point, you need to normalize. For example, suppose that someone has a German
keyboard and originally set a FIDO2 PIN that included a lower case `u` with an umlaut
(&#252;). That keyboard represented the character as `U+00FC`. But now this person is
using a keyboard that has no umlaut so uses the keystrokes `Option-U` followed by `u`.
Maybe the platform reads it as `U+00FC`, but maybe it reads it as `U+0075, U+0308`.

If the char array is normalized, `U+00FC` will stay `U+00FC`, but `U+0075, U+0308` will be
converted to `U+00FC`.

How does one normalize in C#? Unfortunately there are no good solutions. Here are three
possibilities: ignore the problem and assume no one will use a PIN that really needs
normalization, write your own normalization code (or obtain something from a vendor), or
use the `String.Normalize` method which would store the PIN in a new immutable string
instance.

#### Assume PINs will not need normalization

This might not be unsafe. While it is possible to have a PIN that when entered is not the
same as the normalized version, it is not likely.

First of all, a PIN that consists of only ASCII characters is normalized. Second, most
people will choose a PIN that does not contain unusual characters. And third, there is
a good chance that the keyboard or PIN-reading software will return the normalized version
of a character even if some other form is possible.

#### Write your own normalization code

To do so, you will likely reference the Unicode standard along with the Normalization
Annex to develop some class that can read a `char` array and convert those values to the
normalized form C. For example, your program might read all the characters and determine
if there are any characters from the "combining diacritical marks" block. If so, combine
them with the appropriate prior character and map to the normalized value.

Alternatively, you might want to use some Open Source normalization code or find some
other vendor with some module that can perform the appropriate operations.

```csharp
    char[] pinChars = CollectPin();
    char[] normalizedPinChars = PerformNormalization(pinChars);
```

#### Normalization using the `string` class

As we saw above, holding sensitive data in a `string` carries some risk. Whether or not
this is an acceptable risk for your application is something that you will need to
determine. If your application's risk profile would allow the use of the `string` class,
here's what you can do.

```csharp
    char[] pinChars = CollectPin();
    char[] normalizedPinChars = PerformNormalization(pinChars);
      . . .

public char[] PerformNormalization(char[] pinChars)
{
    string pinAsString = new string(pinChars);
    string normalizedPin = pinAsString.Normalize();
    return normalizedPin.ToCharArray();
}
```

### C# and UTF-8

Once you have an array of characters, you can convert that into UTF-8 using the C#
`Encoding` class.

```csharp
    byte[] utf8Pin = Encoding.UTF8.GetBytes(normalizedPinChars);
```

This byte array is what you pass to the
[SetPinCommand](xref:Yubico.YubiKey.Fido2.Commands.SetPinCommand).

If you are using the `string` class to normalize, your code could look something like
this.

```csharp
    char[] pinChars = CollectPin();
    string pinAsString = new string(pinChars);
    string normalizedPin = pinAsString.Normalize();
    byte[] utf8Pin = Encoding.UTF8.GetBytes(normalizedPin);
```

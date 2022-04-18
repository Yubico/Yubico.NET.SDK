---
uid: UsersManualPrivateKeys
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

# Private keys

You will likely need to know how to read or format private keys only if you will be
importing private keys onto a YubiKey. See, for example, the PIV operation
[Import Private Key](xref:Yubico.YubiKey.Piv.PivSession.ImportPrivateKey%2a).

# Encoded Private Key

One of the unfortunate problems of public key cryptography is the myriad ways to represent
private keys. Part of this is natural, due to the fact that different algorithms have
different elements. For example, an RSA private key can consist of three integers:

* modulus
* public exponent
* private exponent

or it can consist of five integers

* prime P
* prime Q
* exponent P
* exponent Q
* coefficient

or it can consist of all eight integers

* modulus
* public exponent
* private exponent
* prime P
* prime Q
* exponent P
* exponent Q
* coefficient

while an "Fp" Elliptic Curve (EC) private key consists of

* curve
  * prime
  * order
  * coefficients
    * a
    * b
    * c
  * base point
    * x-coordinate
    * y-coordinate
* public point
  * x-coordinate
  * y-coordinate
* private value

If the curve is a standard one (such as NIST P-256), then it can be represented by an
object identifier (OID).

* OID
* public point
  * x-coordinate
  * y-coordinate
* private value

or even only

* OID
* private value

The public point can be computed from the curve parameters and private value.

There are standards that have defined ways to represent public keys, but unfortunately
there are more than one. The most common definitions are `PrivateKeyInfo` from PKCS #8
("Public Key Cryptography Standard" number 8 is now an internet standard: RFC 5208) and
PEM (Privacy-Enhanced Mail, a standard that originally described how to use public key
cryptography to build secure email, but has elements that turned out to be useful,
including representation of keys).

Fortunately, there is some overlap. The vast majority of applications will use

* `PrivateKeyInfo`
* PEM "PRIVATE KEY" (this wraps a `PrivateKeyInfo`)

The reason `PrivateKeyInfo` is popular is that it contains algorithm information in
addition to the actual key data. That is, a private key in this format contains an
`AlgorithmIdentifier` specifying the algorithm and any parameters, as well as the key data
specific to that algorithm.

In addition, there are C# classes that will build or parse these structures, although
it will still require some work on your part. This article will describe how to build a
private key object the YubiKey can read from `PrivateKeyInfo` and PEM formats. Note that a
YubiKey will never return a private key, so there will be no need to convert from a
YubiKey-formatted private key to a `PrivateKeyInfo` or PEM format.

## PIV private keys

Unfortunately, PIV does not define its own format of encoding private keys, although
Yubico has defined an encoding that is very similar to the PIV public key format.
However, the SDK's PIV application APIs that work with a private keys require them to be
instances of the [PivPrivateKey](xref:Yubico.YubiKey.Piv.PivPrivateKey) class.
Hence, when importing a private key into a YubiKey, your application will need to be able
to "convert" from `PrivateKeyInfo` or PEM to `PivPrivateKey`.

### From PEM to YubiKey

Suppose you have a key in PEM format and need it as a `PivPrivateKey`. There are a number
of PEM formats. You know what the format is by its header and footer. For example, here
are a number of PEM header/footer private key combinations.

```
   -----BEGIN PRIVATE KEY-----
   -----END PRIVATE KEY-----

   -----BEGIN RSA PRIVATE KEY-----
   -----END RSA PRIVATE KEY-----

   -----BEGIN EC PRIVATE KEY-----
   -----END EC PRIVATE KEY-----
```

This article deals only with `-----BEGIN PRIVATE KEY-----`.

To convert from PEM to `PivPrivateKey`, first extract the `PrivateKeyInfo`, then convert
the `PrivateKeyInfo` to `PivPublicKey`. That conversion is described in the next section.

```csharp
// This method extracts the DER encoding of PrivateKeyInfo from the PEM
// PRIVATE KEY.
// For example,
//   byte[] privateKeyInfo = GetPrivateKeyInfo(pemKey);
//
public static byte[] GetPrivateKeyInfo(string pemKey)
{
    // Isolate the Base64.
    string b64EncodedKey = pemKey
        .Replace("-----BEGIN PRIVATE KEY-----\n", null)
        .Replace("\n-----END PRIVATE KEY-----", null);

    // Get the DER of the PrivateKeyInfo.
    return Convert.FromBase64String(b64EncodedKey);
}
```

### From `PrivateKeyInfo` to YubiKey

If you have a byte array that contains the `PrivateKeyInfo`, and want to build a
`PivPrivateKey`, you will need to first determine the algorithm. Once you know the
algorithm of the key, you can use the appropriate C# class to read the encoded data.
Remember, the algorithm of the key is specified in the key data itself.

Note that the .NET Base Class Library does not have a class that can parse
`PrivateKeyInfo` for either RSA or ECC and build the appropriate object. The only methods
that can read this encoding are in classes for the specific algorithms. That is, the `RSA`
class can read `PrivateKeyInfo` only if the input data is an RSA key, and the `ECDsa`
class can read it only if the input data is an ECC key.

One possible workaround would be to supply the encoded key to the `RSA` class and if it
works, we have an RSA key. If it does not work, give the encoded key to the `ECDsa` class.
However, if the `RSA` class gets an encoded key that is not RSA, it throws an exception.
That is, we would have to use exceptions to determine code flow, which is never a good
idea.

Hence, we need to open up the encoding ourselves and read the `AlgorithmIdentifier`.
Actually, we will only need to read a part of the `AlgorithmIdentifier`, the object
identifier (OID). Furthermore, because the YubiKey only supports RSA and ECC, and with ECC
the only curves are P-256 and P-384, we will be able to determine which algorithm by
looking at only one particular byte of the OID.

In order to find the OID, we will need to decode the DER encoding of `PrivateKeyInfo`.
Actually, we will only need to decode part of it.

Unfortunately, the C# language does not have any publicly available classes to read DER
encodings. There are indeed ASN.1/DER classes in the C# language, but they were not made
public until .NET 5.0.

But we can write a simple helper routine to read what we need.

First, `PrivateKeyInfo` is defined as

```
PrivateKeyInfo ::= SEQUENCE {
        version                   Version,
        privateKeyAlgorithm       AlgorithmIdentifier,
        privateKey                PrivateKey,
        attributes           [0]  IMPLICIT Attributes OPTIONAL }

Version ::= INTEGER

AlgorithmIdentifier ::=  SEQUENCE  {
        algorithm                 OBJECT IDENTIFIER,
        parameter                 ANY DEFINED BY algorithm OPTIONAL  }
```

What this means is that the DER encoding will look something like this:

```
    30 len
       02 01 00
       30 len
          06 len
             OID bytes
         etc.

    where the len octets might be one, two, or three bytes long.
```

To get to the OID, we just need to read the first `30 len`, then the INTEGER, then the
second `30 len`, then the `06 len`. Here's a method that can read the tag and length
octets (and optionally the value) of a DER element.

```csharp
// Read the tag in the buffer at the given offset. Then read the length
// octet(s). If the readValue argument is false, return the offset into
// the buffer where the value begins. If the readValue argument is true,
// skip the value (that will be length octets) and return the offset into
// the buffer where the next TLV begins.
// If the length octets are invalid, return -1.
public static int ReadTagLen(byte[] buffer, int offset, bool readValue)
{
    // Make sure there are enough bytes to read.
    if ((offset < 0) || (buffer.Length < offset + 2))
    {
        return -1;
    }

    // Skip the tag, look at the first length octet.
    // If the length is 0x7F or less, the length is one octet.
    // If the length octet is 0x80, that's BER and we shouldn't see it.
    // Otherwise the length octet should be 81, 82, or 83 (technically it
    // could be 84 or higher, but this method does not support anything
    // beyond 83). This says the length is the next 1, 2, or 3 octets.
    int length = buffer[offset + 1];
    int increment = 2;
    if ((length == 0x80) || (length > 0x83))
    {
        return -1;
    }
    if (length > 0x80)
    {
        int count = length & 0xf;
        if (buffer.Length < offset + increment + count)
        {
            return -1;
        }
        increment += count;
        length = 0;
        while (count > 0)
        {
            length <<= 8;
            length += (int)buffer[offset + increment - count] & 0xFF;
            count--;
        }
    }

    if (readValue)
    {
        if (buffer.Length < offset + increment + length)
        {
            return -1;
        }

        increment += length;
    }

    return offset + increment;
}
```

Using this support routine, we can now extract the algorithm from `PrivateKeyInfo`.

```csharp
// This method builds a new PivPublicKey from privateKeyInfo
//
PivPrivateKey GetPivPrivateKey(byte[] privateKeyInfo)
{
    PivPrivateKey privateKey;

    // Skip the encoding to get to the OID.
    int offset = ReadTagLen(privateKeyInfo, 0, false);
    offset = ReadTagLen(privateKeyInfo, offset, true);
    offset = ReadTagLen(privateKeyInfo, offset, false);
    offset = ReadTagLen(privateKeyInfo, offset, false);

    // encodedKey[offset] is where the OID begins.
    //   RSA: 2A 86 48 86 F7 0D 01 01 01
    //   ECC: 2A 86 48 CE 3D 02 01
    // For this sample code, we'll look at oid[3], if it's 86, RSA,
    // otherwise ECC. If it's something else, we'll get an exception.
    if (encodedKey[offset + 3] == 0x86)
    {
        var rsaObject = RSA.Create();
        rsaObject.ImportPkcs8PrivateKey(encodedKey, out _);

        // We need to get the private key elements. Those can be
        // found in the RSAParameters class.
        RSAParameters rsaParams = rsaObject.ExportParameters(true);

        var rsaPriKey = new PivRsaPrivateKey(
            rsaParams.P,
            rsaParams.Q,
            rsaParams.DP,
            rsaParams.DQ,
            rsaParams.InverseQ);
        privateKey = (PivPrivateKey)rsaPriKey;
    }
    else
    {
        var eccObject = ECDsa.Create();
        eccObject.ImportPkcs8PrivateKey(encodedKey, out _);
        // The KeySize gives the bit size, we want the byte size.
        int keySize = eccObject.KeySize / 8;

        // We need to build the private value and it must be exactly the
        // keySize.
        ECParameters eccParams = eccObject.ExportParameters(true);
        byte[] privateValue = new byte[keySize];
        offset = keySize - eccParams.D.Length;
        Array.Copy(eccParams.D, 0, privateValue, offset, eccParams.D.Length);

        var eccPriKey = new PivEccPrivateKey(privateValue);
        privateKey = (PivPrivateKey)eccPriKey;
    }

    return privateKey;
}
```

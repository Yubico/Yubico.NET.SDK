---
uid: UsersManualPublicKeys
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

# Public keys

One of the unfortunate problems of public key cryptography is the myriad ways to represent
public keys. Part of this is natural, due to the fact that different algorithms have
different elements. For example, an RSA public key consists of two integers:

* modulus
* public exponent

while an "Fp" Elliptic Curve (EC) public key consists of

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

If the curve is a standard one (such as NIST P-256), then it can be represented by an
object identifier (OID).

* OID
* public point
  * x-coordinate
  * y-coordinate

There are standards that have defined ways to represent public keys, but unfortunately
there are more than one. The most common definitions are in X.509 (the certificate
standard used by the vast majority of applications) and PEM (Privacy-Enhanced Mail, a
standard that originally described how to use public key cryptography to build secure
email, but has elements that turned out to be useful, including representation of keys).

Fortunately, there is some overlap. The vast majority of applications will use

* `SubjectPublicKeyInfo`
* PEM "PUBLIC KEY" (this wraps a `SubjectPublicKeyInfo`)

The reason `SubjectPublicKeyInfo` is popular is that it contains algorithm information in
addition to the actual key data. That is, a public key in this format contains an
`AlgorithmIdentifier` specifying the algorithm and any parameters, as well as the key data
specific to that algorithm.

In addition, there are C# classes that will build or parse these structures, although
it will still require some work on your part. This article will describe how to build
public keys in `SubjectPublicKeyInfo` and PEM formats from public keys returned by the
YubiKey, as well as building public keys a YubiKey can read from `SubjectPublicKeyInfo`
and PEM.

## PIV public keys

Unfortunately, PIV defines its own format of encoding public keys. However, the SDK's PIV
application APIs that work with public keys require them to be instances of the
[PivPublicKey](xref:Yubico.YubiKey.Piv.PivPublicKey) class. Hence, your application
will need to be able to "convert" between `SubjectPublicKeyInfo` and `PivPublicKey`.

### From YubiKey to `SubjectPublicKeyInfo`

When you generate a new key pair on the PIV application, you are given the public key. The
key is returned as an instance of the `PivPublicKey` class. From that class you can obtain
all the information about the key. The object has a property for algorithm:

* RSA 1024
* RSA 2048
* ECC P256
* ECC P384

The object also has properties for

* PIV-standard encoding
* YubiKey encoding.

If the key is RSA, this object will also contain properties for the modulus and public
exponent. If the key is ECC, this object will contain a property for the public point.

If you never need to work with any other key format, you can simply store the binary PIV
encoding. Then when you need to use the key again, just retrieve it and build the
`PivPublicKey` object. For example,

```csharp
using System;
using Yubico.YubiKey.Piv;

    using (var pivSession = new PivSession(yubiKey))
    {
        var collectorObj = new SomeKeyCollector();
        pivSession.KeyCollector = collectorObj.KeyCollectorDelegate;

        isValid = pivSession.TryGenerateKeyPair(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            out PivPublicKey? publicKey);

        // Assume there is some method that takes in the ReadOnlyMemory<byte> data
        // and stores it somewhere, a file maybe. It stores it agains some identifier.
        StorePublicKey(someId, publicKey.PivEncodedKey);
    }

    // Assume there is some mathod that finds the data associated with the identifier
    // and returns it.
    byte[] encodedPublicKey = RetrievePublicKey(someId);
    PivPublicKey publicKey = PivPublicKey.Create(encodedPublicKey);
```

But suppose you need to create a certificate request and that code requires the key be in
the format `SubjectPublicKeyInfo`. Fortunately, C# has classes that will build that
encoding. We will need to extract the individual elements of the public key from the
`PivPublicKey` and supply them to the C# class that can perform the encoding.

```csharp
using System;
using System.Security.Cryptography;
using Yubico.YubiKey.Piv;

// This method builds the DER encoding of SubjectPublicKeyInfo from the data inside
// the PivPublicKey.
// For example,
//   byte[] subjectPublicKeyInfo = GetSubjectPublicKeyInfo(publicKey);
//
public static byte[] GetSubjectPublicKeyInfo(PivPublicKey publicKey)
{
    byte[] encodedKey;

    // First, which C# classes you use depends on the algorithm.
    if ((publicKey.Algorithm == PivAlgorithm.Rsa1024) || (publicKey.Algorithm == PivAlgorithm.Rsa2048))
    {
        // If the Algorithm is Rsa, then the PivPublicKey is really an
        // instance of PivRsaPublicKey.
        PivRsaPublicKey rsaPubKey = (PivRsaPublicKey)publicKey;

        // The C# class called RSA can build the SubjectPublicKeyInfo.
        // We need to build an instance of that class and supply the key
        // data we want to encode.
        // The way to supply the key data is through the RSAParameters
        // class.

        // Build the RSAParameters object using the modulus and public
        // exponent.
        var rsaParams = new RSAParameters();
        rsaParams.Modulus = rsaPubKey.Modulus.ToArray();
        rsaParams.Exponent = rsaPubKey.PublicExponent.ToArray();

        // Build the RSA object that will be able to create the
        // SubjectPublicKeyInfo.
        using RSA rsaObject = RSA.Create(rsaParams);
        encodedKey = rsaObject.ExportSubjectPublicKeyInfo();
    }
    else
    {
        // If the Algorithm is Ecc, then the PivPublicKey is really an
        // instance of PivEccPublicKey.
        PivEccPublicKey eccPubKey = (PivEccPublicKey)publicKey;

        // The C# classes called ECDsa and ECDiffieHellman can build the
        // SubjectPublicKeyInfo.
        // We need to build an instance of either of those classes and
        // supply the key data we want to encode.
        // The way to supply the key data is through the ECParameters
        // class.

        // The public key consists of the curve and the public point (x-
        // and y-coordinate). To build the ECParameters class, therefore,
        // we need to supply those three things.

        // The curve is represented by the ECCurve class. If we use a
        // supported standard curve, we can build such an object using
        // the OID. Because the YubiKey supports only two curves, P-256
        // and P-384, and those curves are supported by C#, we can build
        // the ECCurve object using the OID.
        // The OID for P256 is 1.2.840.10045.3.1.7, and for P-384 it is
        // 1.3.132.0.34.
        string oidString = "1.2.840.10045.3.1.7";
        if (publicKey.Algorithm == PivAlgorithm.EccP384)
        {
            oidString = "1.3.132.0.34";
        }
        ECCurve eccCurve = ECCurve.CreateFromValue(oidString);

        // Now build the ECParameters object using the Curve and public
        // point.
        var eccParams = new ECParameters();
        eccParams.Curve = eccCurve;
        // In PIV, a public point is represented as
        // 04 || x-coordinate || y-coordinate
        // For the C# class we need to break it into the two coordinates.
        int coordLength = (eccPubKey.PublicPoint.Length - 1) / 2;
        eccParams.Q.X = eccPubKey.PublicPoint.Slice(1, coordLength).ToArray();
        eccParams.Q.Y = eccPubKey.PublicPoint.Slice(1 + coordLength, coordLength).ToArray();

        // Build the EC object that will be able to create the
        // SubjectPublicKeyInfo.
        using ECDsa eccObject = ECDsa.Create(eccParams);
        encodedKey = eccObject.ExportSubjectPublicKeyInfo();
    }

    return encodedKey;
}
```

Suppose you want the key in PEM form. The PEM form of a key is all ASCII characters. Maybe
you want to store the data in a human-readable file, or maybe there is some software you
must work with that requires keys to be in PEM format.

There are a number of PEM formats. You know what the format is by its header and footer.
For example, here are a number of PEM header/footer combinations.

```
   -----BEGIN PUBLIC KEY-----
   -----END PUBLIC KEY-----

   -----BEGIN RSA PUBLIC KEY-----
   -----END RSA PUBLIC KEY-----

   -----BEGIN EC PUBLIC KEY-----
   -----END EC PUBLIC KEY-----

   -----BEGIN CERTIFICATE REQUEST-----
   -----END CERTIFICATE REQUEST-----

   -----BEGIN CERTIFICATE-----
   -----END CERTIFICATE-----

   -----BEGIN PRIVACY-ENHANCED MESSAGE-----
   -----END PRIVACY-ENHANCED MESSAGE-----
```

The most robust PEM public key will be `PUBLIC KEY`. You will only have one format to work
with because the algorithm is specified in the key data itself, so there's only one label
for RSA and ECC. There is no encryption or passwords because there is no need to keep a
public key key private.

The contents of the PUBLIC KEY are simply the DER encoding of the `SubjectPublicKeyInfo`,
base-64 encoded.

```csharp
// This method builds the PEM encoding of the given public key.
// For example,
//   string pemKey = GetPemPublicKey(publicKey);
//
string GetPemPublicKey(PivPublicKey publicKey)
{
    byte[] encodedKey = GetSubjectPublicKeyInfo(publicKey);

    string b64EncodedKey = Convert.ToBase64String(encodedKey, Base64FormattingOptions.InsertLineBreaks);
    return "-----BEGIN PUBLIC KEY-----\n" + b64EncodedKey + "\n-----END PUBLIC KEY-----";
}
```

Here are what PEM keys look like.

```
This is the encoding of a 1024-bit RSA public key.

-----BEGIN PUBLIC KEY-----
MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQC0jN+3X53e9CcB99Rta3Wlzp2YhITRGr39CM6e
/2s+jC5gF+g/RJTopMX3cdIztFiAnhH1WeGcLDpSp7e+/lC6piBgysqa3VdpgD1161c1AsiqgsWU
VNbwxD4wWnheyGxaDnTxWbsxEWAufKPPdXsKnDkferDovEFzLaOjo+u+MQIDAQAB
-----END PUBLIC KEY-----

This is the encoding of a P-384 ECC public key

-----BEGIN PUBLIC KEY-----
MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAElxFSK2lukgHRDGFu3XmUnPTZ/+g44ClPeaom8TbXZ1qI
rDUGiEHlB1MfDIuXVLhxy+2yvBHPVZmyArSvohHkKOKPvwssTSWHyvyL02GSXyR2GhxZg4qIbwUa
w22VGAsK
-----END PUBLIC KEY-----
```

### From PEM to YubiKey

Suppose you have a key in PEM format and need it as a `PivPublicKey`. For example, maybe
you want to load a private and public key generated off the YubiKey into a PIV slot on the
YubiKey.

To convert from PEM to `PivPublicKey`, first extract the `SubjectPublicKeyInfo`, then
convert the `SubjectPublicKeyInfo` to `PivPublicKey`. That conversion is described in the
next section.

```csharp
// This method extracts the DER encoding of SubjectPublicKeyInfo from the PEM
// PUBLIC KEY.
// For example,
//   byte[] subjectPublicKeyInfo = GetSubjectPublicKeyInfo(pemKey);
//
byte[] GetSubjectPublicKeyInfo(string pemKey)
{
    // Isolate the Base64.
    string b64EncodedKey = pemKey
        .Replace("-----BEGIN PUBLIC KEY-----\n", null)
        .Replace("\n-----END PUBLIC KEY-----", null);

    // Get the DER of the SubjectPublicKeyInfo.
    return Convert.FromBase64String(b64EncodedKey);
}
```

### From `SubjectPublicKeyInfo` to YubiKey

If you have a byte array that contains the `SubjectPublicKeyInfo`, and want to build a
`PivPublicKey`, you will need to first determine the algorithm. Once you know the
algorithm of the key, you can use the appropriate C# class to read the encoded data.
Remember, the algorithm of the key is specified in the key data itself.

Note that the .NET Base Class Library does not have a class that can parse a
`SubjectPublicKeyInfo` for either RSA or ECC and build the appropriate object. The only
methods that can read this encoding are in classes for the specific algorithms. That is,
the `RSA` class can read `SubjectPublicKeyInfo` only if the input data is an RSA key, and
the `ECDsa` class can read it only if the input data is an ECC key.

One possible workaround would be to supply the encoded key to the `RSA` class and if it
works, we have an RSA key. If it does not work, give the encoded key to the `ECDsa` class.
However, if the `RSA` class gets an encoded key that is not RSA, it throws an exception.
That is, we would have to use exceptions to determine code flow, which is never a good
idea.

Hence, we need to open up the encoding ourselves and read the `AlgorithmIdentifier`.
Actually, we will only need to read a part of the `AlgorithmIdentifier`, the object
identifier (OID). Furthermore, because the YubiKey only supports RSA and ECC, and with ECC
the only curves are P-256 and P-384, we will be able to determine which algorithm by
looking at only one byte of the OID.

In order to find the OID, we will need to decode the DER encoding of
`SubjectPublicKeyInfo`. Actually, we will only need to decode part of it.

Unfortunately, the C# language does not have any publicly available code to read DER
encoding. There are indeed ASN.1/DER classes in the C# language, but they were not made
public until .NET 5.0.

But we can write a simple helper routine to read what we need.

First, `SubjectPublicKeyInfo` is defined as

```
SubjectPublicKeyInfo ::=  SEQUENCE  {
     algorithm            AlgorithmIdentifier,
     subjectPublicKey     BIT STRING  }

AlgorithmIdentifier ::=  SEQUENCE  {
     algorithm            OBJECT IDENTIFIER,
     parameter            ANY DEFINED BY algorithm OPTIONAL  }
```

What this means is that the DER encoding will look something like this:

```
    30 len
       30 len
          06 len
             OID bytes
         etc.

    where the len octets might be one, two, or three bytes long.
```

To get to the OID, we just need to read the first `30 len`, then the second `30 len`, then
the `06 len`. Here's a method that can read the tag and length octets of a DER element.

```csharp
// Read the tag in the buffer at the given offset. Then read the length
// octet(s). Return the offset into the buffer where the value begins.
// If the length octets are invalid, return -1.
public static int ReadTagLen(byte[] buffer, int offset)
{
    // Make sure there are enough bytes to read. This sample program will
    // work only if the data is a valid SubjectPublicKeyInfo, so we know
    // there must be a minimum number of bytes remaining.
    if ((offset < 0) || (buffer.Length < offset + 9))
    {
        return -1;
    }

    // Skip the tag, look at the first length octet.
    // If the length is 0x7F or less, the length is one octet.
    // If the length octet is 0x80, that's BER and we shouldn't see it,
    // but if so, the length is one octet.
    byte length = buffer[offset + 1];
    if (length <= 0x80)
    {
        return offset + 2;
    }

    // The first length octet should be 81 or 82. Technically it could be
    // 83, 84, or so on. But for this sample we're reading keys and we
    // should never see anything other than 81 or 82. Nonetheless, we'll
    // check for 81, 82, or 83. Anything else will be an error.
    if (length > 0x83)
    {
        return -1;
    }

    return offset + 2 + (int)(length & 0xf);
}
```

Using this support routine, we can now extract the algorithm from `SubjectPublicKeyInfo`.

```csharp
// This method builds a new PivPublicKey from subjectPublicKeyInfo
//
PivPublicKey GetPivPublicKey(byte[] subjectPublicKeyInfo)
{
    PivPublicKey pivKey;

    // Read the DER encoding to get to the value portion of the OID.
    int offset = ReadTagLen(subjectPublicKeyInfo, 0);
    offset = ReadTagLen(subjectPublicKeyInfo, offset);
    offset = ReadTagLen(subjectPublicKeyInfo, offset);

    if (offset < 0)
    {
        throw new ArgumentException(ExceptionMessages.InvalidPublicKeyData);
    }

    // subjectPublicKeyInfo[offset] is where the OID begins.
    //   RSA: 2A 86 48 86 F7 0D 01 01 01
    //   ECC: 2A 86 48 CE 3D 02 01
    // For this sample code, we'll look at oid[3], if it's 86, RSA,
    // if CE it's ECC.
    switch (encodedKey[offset + 3])
    {
        default:
            throw new ArgumentException(ExceptionMessages.InvalidPublicKeyData);
    
        case 0x86:
            RSA rsaObject = RSA.Create();
            rsaObject.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out int bytesRead);

            // We need to get the modulus and public exponent. Those can be
            // found in the RSAParameters class.
            RSAParameters rsaParams = rsaObject.ExportParameters(false);

            var rsaPubKey = new PivRsaPublicKey(rsaParams.Modulus, rsaParams.Exponent);
            pivKey = (PivPublicKey)rsaPubKey;
            break;

        case 0xCE:
            ECDsa eccObject = ECDsa.Create();
            eccObject.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out int bytesRead);

            // The KeySize gives the bit size, we want the byte size.
            int keySize = eccObject.KeySize / 8;

            // We need to build the public point as
            //  04 || x-coord || y-coord
            // Each coordinate must be the exact length.
            // Prepend 00 bytes if the coordinate is not long enough.
            ECParameters eccParams = eccObject.ExportParameters(false);
            byte[] point = new byte[(keySize * 2) + 1];
            point[0] = 4;
            offset = 1 + (keySize - eccParams.Q.X.Length);
            Array.Copy(eccParams.Q.X, 0, point, offset, eccParams.Q.X.Length);
            offset += keySize + (keySize - eccParams.Q.Y.Length);
            Array.Copy(eccParams.Q.Y, 0, point, offset, eccParams.Q.Y.Length);

            var eccPubKey = new PivEccPublicKey(point);
            pivKey = (PivPublicKey)eccPubKey;
            break;
    }

    return pivKey;
}
```

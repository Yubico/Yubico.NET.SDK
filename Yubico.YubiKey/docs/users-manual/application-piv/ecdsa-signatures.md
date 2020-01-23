---
uid: UsersManualEcdsaSignatures
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

# ECDSA signatures

A common misconception is that DSA and ECDSA, when generating the actual signature, will
perform an encryption operation. However, that is not the case. The DSA or ECDSA signing
operation will mathematically "combine" the digest of the data to sign with the private
key and a random value to generate two values, commonly called `r` and `s`.

There are mathematical operations that can then combine the digest of the signed data
along with the public key and the `s` value to generate `r` again. That is, if using the
digest, public key, and `s` in a particular way produces `r`, the signature verifies. If
not, the signature does not verify.

Back in the 1990s, the first DSA standards were specifying how the algorithm operated but
also how to present the signature. Someone verifying a DSA signature must know how the
signer has organized the `r` and `s`. There were generally two ways to do so.

```
(1)   Concatenation
      r || s   where r and s are the same length, prepend 00 bytes if necessary

   For example,

     e91a49c5147db1a9aaf244f05a434d6486931d2d00526db078b05edecbcd1eb4a208f3ae1617ae82
     |<---              r               --->||<---              s               --->|

(2)   DER/BER encoding of
         SEQUENCE {
            r   INTEGER,
            s   INTEGER }

   For example,

     DER:
     30 2c
        02 15
           00 e9 1a 49 c5 14 7d b1 a9 aa f2 44 f0 5a 43 4d 64 86 93 1d 2d 
        02 13
           52 6d b0 78 b0 5e de cb cd 1e b4 a2 08 f3 ae 16 17 ae 82

     BER:
     30 2e
        02 15
           00 e9 1a 49 c5 14 7d b1 a9 aa f2 44 f0 5a 43 4d 64 86 93 1d 2d 
        02 15
           00 00 52 6d b0 78 b0 5e de cb cd 1e b4 a2 08 f3 ae 16 17 ae 82

   Note that the DER encoding is variable length, some signatures can be longer, some
   shorter depending on the values. But it is possible to build a BER encoding that is a
   fixed length for each key size.
```

Virtually all standards chose to require the DER/BER encoding format. For example, if you
want to follow the PKCS 10 and X.509 standards for cert requests and certificates (RFCs
2986 and 5280), you will build and read DSA and ECDSA signatures as the BER encoding.

## .NET Base Class Libraries (BCL)

The way to build and verify ECDSA signatures in the BCL is with the `ECDsa` class. For
example, to create a signature, load the private key and call `SignData`.

```
    var eccCurve = ECCurve.CreateFromValue("1.2.840.10045.3.1.7");
    var eccParams = new ECParameters
    {
        Curve = (ECCurve)eccCurve
    };
    eccParams.Q.X = publicPointXCoord;
    eccParams.Q.Y = publicPointYCoord;
    eccParams.D = privateValue;

    using var ecdsaObject = ECDsa.Create(eccParams);

    byte[] signature = ecdsaObject.SignData(dataToSign, HashAlgorithmName.SHA256);
```

The `SignData` method will use the specified hash algorithm to digest the input
`dataToSign` and create an ECDSA signature using that digest along with the private key
and a random value. There is another method, `SignHash` that will create the signature
from the digest you provide.

The result of the signing operation is a byte array. But which format of signature is it?
The BCL does not document the format of the resulting signature, so you will have to
execute the method and examine the result to find out.

It turns out this method produces the concatenation of `r` and `s`, not the DER/BER
encoding standards specify.

Is it possible to get the BER/DER encoding? Yes and no. There is a method that allows you
to specify the format of the signature.

```
    byte[] signature = ecdsaObject.SignData(
        dataToSign, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
```

Note that this will produce the DER encoding, so its length is variable.

However, there is a problem with this method, namely it was not introduced until .NET 5.0.
That means if you are using, for example, .NET Standard 2.0 (as the .NET YubiKey SDK
does), then this method is not available.

Similarly, there are methods (`VerifyData` and `VerifyHash`) to verify signatures.

## The YubiKey ECDSA signature

When you call on the YubiKey to sign data using ECDSA, the result will be the DER
encoding.

## Converting the signature

If you have a signature in DER/BER form and need to use the BCL to verify, but are not
using .NET 5.0, you will need to convert to the concatentation that the `ECDsa` class
needs. Similarly, if you build a signature using the BCL and you need to send it in the
DER/BER format, you will need to convert.

The PIV sample code contains a class that can perform the conversion. See
`Yubico.YubiKey/examples/PivSampleCode/Converters/DsaSignatureConverter.cs`.

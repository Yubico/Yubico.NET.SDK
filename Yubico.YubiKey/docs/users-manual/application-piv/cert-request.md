---
uid: UsersManualPivCertRequest
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

# Building a certificate request for a PIV private key

You have generated a key pair in, or imported a private key into, a PIV slot. Now you need
a certificate. To obtain a certificate, you start by building a certificate request and
sending it to a Certificate Authority (CA). The CA will build a cert and return it to you.

This document describes how to build a cert request using a key in a YubiKey PIV slot.

Note that there is also some sample code that demonstrates this. Find it in

```txt
  .../Yubico.YubiKey/examples/PivSampleCode/CertificateOperations
```

Start with the file

```txt
  SampleCertificateOperations.cs
```

and the method

```txt
  GetCertRequest
```

## .NET Base Class Libraries

This document describes how to create a cert request using

```csharp
  System.Security.Cryptography.X509Certificates.CertificateRequest
```

This is not the only way to create a cert request. There are commercial products available
with certificate APIs. However, for this documentation (and the SDK sample code), only
classes available in the .NET BCL are examined.

## The `CertificateRequest` constructor

To build a cert request, start by building a `CertificateRequest` object. To do so, use
the constructor. There are several, but let's look at this one.

```csharp
    CertificateRequest(
        string subjectName,
        System.Security.Cryptography.RSA publicKey,
        System.Security.Cryptography.HashAlgorithmName hashAlgorithm,
        System.Security.Cryptography.RSASignaturePadding paddingScheme); 
```

### Subject name

A certificate is a binding between a name and a public key. So your cert request will need
to let the CA know what name and public key are to be in the certificate.

There are two ways to provide the subject name, as a `string` and as an instance of
`System.Security.Cryptography.X509Certificates.X500DistinguishedName`. The purpose of
this document is to describe how to build a cert request when the private key is on a
YubiKey. Hence, we will not describe how to build names, either by using the `string`
class or the `X500DistinguishedName` class. For this document, we're simply going to
use the string

```csharp
  string sampleName = "C=US,ST=CA,L=Palo Alto,O=Fake,CN=Fake Cert";
```

If you want to learn more about building a subject name, either by using a `string` or an
[`X500DistinguishedName`](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x500distinguishedname?view=net-5.0),
see the .NET documentation.

### Public key

When you generate a key pair on the YubiKey, a `PivPublicKey` is returned. The
`CertificateRequest` class needs that public key as an instance of the `RSA` class.

The `PivSampleCode.KeyConverter` class demonstrates how to get an `RSA` object from a
`PivPublicKey`. Your code might look something like this.

```csharp
    PivRsaPublicKey rsaPublic = pivSession.GenerateKeyPair(...);

    var rsaParams = new RSAParameters();
    rsaParams.Modulus = rsaPublic.Modulus.ToArray();
    rsaParams.Exponent = rsaPublic.PublicExponent.ToArray();

    RSA rsaPublicKeyObject = RSA.Create(rsaParams);
```

An `RSA` object can contain a public key only or both public and private keys. Later on,
we're going to sign the cert request using the private key partner to the public key
loaded in this step. Normally, that private key would need to be loaded into this `RSA`
object as well, because that's the object the cert request code would use to sign the
request. But the private key partner in this case is on the YubiKey and is not allowed to
leave the device. We can't load it into this `RSA` object. But we will still be able to
sign. How to do so will be described later.

### `HashAlgorithm`

To sign using RSA is to encrypt the hash of the data to sign. That is, RSA does not
operate directly on the data to sign, but rather the hash (message digest) of the data. So
we need to specify which hash algorithm the cert request code should use. Your best
choices are the following.

```csharp
  HashAlgorithmName.SHA256
  HashAlgorithmName.SHA384
  HashAlgorithmName.SHA512
```

There are other algorithms available: `MD5` and `SHA1`. However, researchers have found
weaknesses in them, so cryptographers universally recommend not using them any more. Use
them only for legacy systems.

Which algorithm should you use? Your application might be required to use a particular
digest based on a standard or protocol. If not, then you might choose SHA-256 simply
because it is the most widely used digest algorithm in RSA signatures. It is the one
that will likely have no interoperability issues.

Because a longer digest does add some security in digital signatures, you might choose to
use the SHA-384 with 1024-bit keys. If you try to use SHA-512, the operation will likely
fail because there is not enough space in a 1024-bit block (128 bytes, the size of the
signature) to contain a padded, 64-byte digest. If the RSA key is 2048 bits, you can use
SHA-512.

Note that there is an algorithm SHA-224, but the .NET BCL do not support it.

### `PaddingScheme`

Every standard that deals with RSA signatures requires the digest to be padded. There are
two available in the .NET BCL.

```csharp
  RSASignaturePadding.Pkcs1
  RSASignaturePadding.Pss
```

PSS is recommended over PKCS #1. It is possible to encounter a legacy system that requires
PKCS #1 and does not support PSS. However, if that is not the case, it is better to use
PSS.

An RSA signature is the encrypted digest. However, for security reasons, the actual data
to encrypt should be the same size as (or very close to the size of) the key itself. For
example, if the RSA key is 1024 bits (128 bytes), then the data to sign should also be a
128-byte block. A SHA-256 digest is only 32 bytes. Hence, to create a block to encrypt,
add pad bytes. The padding scheme used should be a standard one so that the verifier can
know which bytes are pad and which are the digest.

Both padding schemes supported in the .NET BCL require a minimum amount of padding. In
other words, a particular digest algorithm/padding scheme/key size might not be compatible
because the digest is very long and not many pad bytes are needed to complete a block.
That is why it is possible you will not be able to use SHA-512 with a 1024-bit key.

## Extensions

Now that you have the `CertificateRequest` object built, it is possible to add extensions.
To learn how to do that, see the .NET BCL documentation.

## `CreateSigningRequest`

When the `CertificateRequest` object has all the information you want, call the
appropriate `CreateSigningRequest` method.

The `CreateSigningRequest` method that takes no arguments will sign the request using the
private key inside the `RSA` object passed to the constructor. In this case, that object
has no private key, so we'll need to use

```csharp
  public byte[] CreateSigningRequest (
    System.Security.Cryptography.X509Certificates.X509SignatureGenerator signatureGenerator);
```

We will build an `X509SignatureGenerator`, which is an object that knows how to sign. The
sample code contains an example of one that uses a YubiKey to sign. See

```txt
  .../Yubico.YubiKey/examples/PivSampleCode/CertificateOperations

  YubiKeySignatureGenerator.cs
```

The `CreateSigningRequest` method will do much of the work to build up the cert request,
but will call on our `SignatureGenerator` to do work it can't.

### `X509SignatureGenerator`

This is an abstract class. We need to build a subclass that implements the specified
methods:

```csharp
  BuildPublicKey
  GetSignatureAlgorithmIdentifier
  SignData
```

We will pass an instance of our class to the `CreateSigningRequest` method.

Start with a "scaffold" class.

```csharp
public sealed class YubiKeySignatureGenerator : X509SignatureGenerator
{
    public YubiKeySignatureGenerator() { }

    protected override PublicKey BuildPublicKey() { }

    public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm) { }

    public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) { }
}
```

#### `BuildPublicKey`

When we first built the `CertificateRequest` object, we supplied the public key in the
`RSA` argument. It would seem that if the object (and the `CreateSigningRequest` method)
has access to the public key there should be no need for our `SignatureGenerator` to be
able to provide it. But nonetheless, we need to build this method.

Fortunately, the `X509SignatureGenerator` base class (the abstract class from which we
derive the class we are currently constructing) contains code that can be used to
accomplish this.

Call this `static` method

```csharp
    // Use the RSA object and padding scheme we created for the CertificateRequest
    // constructor.
    X509SignatureGenerator defaultGenerator = X509SignatureGenerator.CreateForRSA(
        rsaPublicKeyObject, RSASignaturePadding.Pss);
```

You now have an `X509SignatureGenerator` object. This happens to be the default. It is
what the `CreateSigningRequest()` (no arg version of this method) would use. However,
this object was built using an `RSA` object that contained no private key. So it won't be
able to sign. But it will be able to build a `PublicKey`.

We can update our class to take advantage of this object.

```csharp
public sealed class YubiKeySignatureGenerator : X509SignatureGenerator
{
    private readonly X509SignatureGenerator _defaultGenerator;
    private readonly RSASignaturePaddingMode _paddingMode;
    
    // Use the RSA object and padding scheme we created for the CertificateRequest
    // constructor.
    public YubiKeySignatureGenerator(RSA rsaPublicKeyObject, RSASignaturePadding paddingScheme)
    {
       _defaultGenerator = X509SignatureGenerator.CreateForRSA(rsaPublicKeyObject, paddingScheme);
       _paddingMode = paddingScheme.Mode;
    }

    protected override PublicKey BuildPublicKey()
    {
        return _defaultGenerator.PublicKey;
    }

    public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm) { }

    public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) { }
}
```

#### `GetSignatureAlgorithmIdentifier`

The point of this method is to return the DER encoding of the algorithm ID of the
algorithm that will be used to sign. The algID is part of the finished cert request.

Once again, the `CertificateRequest` object (and the `CreateSigningRequest` method) has
access to the `RSA` public key, the `HashAlgorithm`, and `RSASignaturePadding`. It would
seem that the object has everything it needs to build the "algID". But nonetheless, we
need to build this method.

Fortunately, the `_defaultGenerator` we built earlier can build the algID for us.

```csharp
public sealed class YubiKeySignatureGenerator : X509SignatureGenerator
{
    . . .

    public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
    {
        return _defaultGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
    }

    public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) { }
}
```

#### `SignData`

This is the method we will build that calls on the YubiKey to sign the data. The
`CertificateRequest` object is going to pass to our method the data to sign and the hash
algorithm it is expected to use. That means we need to digest the data using the specified
algorithm, then create a signature using that digest result. We need to pad the digest as
well.

In order to sign using a YubiKey, we need a `PivSession` and we need to know in which slot
the private key we're using resides.

```
    private readonly PivSession _pivSession;
    private readonly byte _slotNumber;
    private readonly int _keySizeBits;

    private readonly X509SignatureGenerator _defaultGenerator;
    private readonly RSASignaturePaddingMode _paddingMode;
    
    public YubiKeySignatureGenerator(
        PivSession pivSession,
        byte slotNumber,
        RSA rsaPublicKeyObject,
        RSASignaturePadding paddingScheme)
    {
        _pivSession = pivSession;
        _slotNumber = slotNumber;
        _keySizeBits = rsaPublicKeyObject.KeySize;
        _defaultGenerator = X509SignatureGenerator.CreateForRSA(rsaPublicKeyObject, paddingScheme);
        _paddingMode = paddingScheme.Mode;
    }

    protected override PublicKey BuildPublicKey()
    {
        return _defaultGenerator.PublicKey;
    }

    public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
    {
        return _defaultGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
    }

    public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
    {
        byte[] dataToSign = DigestData(data, hashAlgorithm);
        dataToSign = PadRsa(dataToSign, hashAlgorithm);

        return _pivSession.Sign(_slotNumber, dataToSign);
    }

    private byte[] DigestData(byte[] dataToDigest, HashAlgorithmName hashAlgorithm)
    {
        using HashAlgorithm digester = hashAlgorithm.Name switch
        {
            "SHA1" => CryptographyProviders.Sha1Creator(),
            "SHA256" => CryptographyProviders.Sha256Creator(),
            "SHA384" => CryptographyProviders.Sha384Creator(),
            "SHA512" => CryptographyProviders.Sha512Creator(),
            _ => throw new ArgumentException(),
        };

        byte[] digest = new byte[digester.HashSize / 8];

        _ = digester.TransformFinalBlock(data, 0, data.Length);
        Array.Copy(digester.Hash, 0, digest, 0, digest.Length);

        return digest;
    }

    private byte[] PadRsa(byte[] digest, HashAlgorithmName hashAlgorithm)
    {
        int digestAlgorithm = hashAlgorithm.Name switch
        {
            "SHA1" => RsaFormat.Sha1,
            "SHA256" => RsaFormat.Sha256,
            "SHA384" => RsaFormat.Sha384,
            "SHA512" => RsaFormat.Sha512,
            _ => 0,
        };

        if (_rsaPadding.Mode == RSASignaturePaddingMode.Pkcs1)
        {
            return RsaFormat.FormatPkcs1Sign(digest, digestAlgorithm, _keySizeBits);
        }

        return RsaFormat.FormatPkcs1Pss(digest, digestAlgorithm, _keySizeBits);
    }
```

## ECC

It is possible you will want to build a cert request for an ECC key pair. In that case,
you will need a `SignatureGenerator` that can sign using ECC. To do so, you will need to
build an ECC default `SignatureGenerator`, there are restrictions on the size of the
digest (it must match the key size), and there is no padding.

The sample code demonstrates how to build a `SignatureGenerator` that can sign using
either RSA or ECC.

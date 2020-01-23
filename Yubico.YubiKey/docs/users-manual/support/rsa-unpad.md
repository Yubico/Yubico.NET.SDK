---
uid: UsersManualRsaUnpad
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

# Attacks on RSA decryption unpad operations

There are attacks on RSA involving the unpad operation. This document describes those
attacks, whether the YubiKey and/or SDK is vulnerable, and SDK mitigations.

## Chosen ciphertext attack on RSA

Suppose an attacker is able to obtain an RSA ciphertext block from unknown plaintext. The
goal is to retrieve the plaintext of this original message.

The attacker creates a different ciphertext block mathematically related to the first. If
the attacker is able to somehow get the private key owner to decrypt this new block and
return the plaintext, they can deduce the original message.

That is, there exist

```txt
  P1  (plaintext 1, the unknown)
  C1  (ciphertext 1, known to the attacker)
  P2  (known)
  C2' (known)(the chosen ciphertext)
```

If they are mathematically related (in a particular way), the attacker "can solve for P1".

Incidentally, just to be pedantic, `C2'` (C-two-prime, the chosen ciphertext) is not the
ciphertext for `P2`. Rather, while there is a `C2` (the ciphertext for `P2`), the
ciphertext the attacker chooses is mathematically related to both C2 and C1. But it is not
C2 exactly.

There are two reasons this attack generally does not succeed in the real world. One,
protocols and applications are written so that results of private key operations are not
returned to outside querants. And two, even if the plaintext block were to be returned,
when the chosen ciphertext is decrypted, the "unpad" operation would undoubtedly fail and
an error, rather than the plaintext, would be returned. And even if the decrypted data
somehow survived the unpad operation (it looked like a properly padded block), the data
returned would be the unpadded data, not the entire block.

Nonetheless, this is the foundation of other attacks. This document describes these
attacks, when an application is susceptible (or not), and what mitigations are required.
Finally, this document describes some code inside the .NET YubiKey SDK that would
appear to be susceptible, and the mitigations employed to reduce its exposure.

> :exclamation: Note that this attack only allows the adversary to recover one message. It
> does not threaten the private key itself.

## PKCS 1 padding (aka PKCS #1 v.1.5 and P1.5)

To encrypt using RSA, each plaintext block must be numerically less than the RSA modulus.
However, if the plaintext is too small, then the RSA operation is not secure. Hence, to
build a block that is less than the modulus, but also not a small value, the block is
padded. The oldest standard padding scheme for RSA encryption is known as PKCS 1
(PKCS = Public Key Cryptography Standards, and the first of those is related to RSA).

In this scheme, a block of memory or a byte array the size of the modulus is created. It
is filled this way.

```txt
00 || 02 || pad bytes || 00 || plaintext
```

The pad bytes are random, non-zero bytes. The standard specifies that there must be at
least 8 pad bytes. For example, if the RSA key is 1024 bits (128 bytes) and the data to
encrypt is a 32-byte value (e.g. a 256-bit AES key), the block of data would be

```txt
00 || 02 || 93 random non-zero bytes || 00 || 32 plaintext bytes
```

This block is converted into a number and encrypted.

After decrypting the ciphertext, the private key owner will check to make sure the first
byte is 00 and the second is 02. If not, error, don't return any plaintext.

If the first two bytes are correct, search for the first occurrence of the 00 byte. If
there is none, error, don't return any plaintext.

If there is a 00 byte, make sure there are at least 8 bytes of pad (i.e. the 00 appears
after index 9). If not, error, don't return any plaintext.

If these checks all pass, return all the bytes after the first 00 byte.

Some applications and protocols are written with another check, namely, how big the
unpadded data must be. For example, it's possible the encrypted data must be either 16,
24, or 32 bytes (it is an AES key), or it must be 48 bytes (it is a master session key).
If not, error, don't return any plaintext.

## Bleichenbacher Attack

In 1998, Daniel Bleichenbacher published some results of his research, which included a
way to employ the chosen-ciphertext attack on RSA, even without knowing the full plaintext
block result. It relied on knowing where, in a decrypted block, the padding scheme failed.

Send the private key owner a chosen ciphertext block. The owner decrypts and then tries to
unpad. When that operation fails, the owner sends a response indicating what the failure
was. That is, the error message might be, "Decryption failed, block[0] not 0." Or maybe it
was "Decryption failed, block[1] not 2". And so on.

Although the attacker does not know all the bytes of the plaintext, they do know some of
them based on the error message.

Now send another chosen ciphertext. And another. And keep sending messages. Depending on
the private key size, thousands or millions of messages. Now based on the results, solve
for the original plaintext message.

This attack is not practical unless two conditions are met. One, the decryptor (private
key owner) must be willing to decrypt thousands or millions of messages in a timely
manner, no questions asked (the term in cryptography is "oracle"), and two, return
descriptive error messages.

These conditions were met in the real world. An SSL server likely responds to all
handshake requests and processes them automatically. There were some implementations that
did indeed return descriptive error messages. The target message to attack would be the
one in which a session key was encrypted using the server's public key. Once the attacker
knows the session key, they can read an entire session's messages.

### Mitigation

To prevent the attack, SSL server code stopped returning descriptive error messages. All
errors (failed decryption because of an unpad error or anything else) triggered a single
response message.

### Attack updated

Without the detailed error message, Bleichenbacher then timed the responses. How long did
it take for the SSL server to respond? If it was very quick, the error was the first byte
of padding. If it was a little longer, the error was the second byte. And so on.

Even though the server was not explicitly returning a descriptive error code, the amount
of time it was spending on the decryption was enough information to launch the attack.

The number of messages required was generally estimated to be around 1,000,000 for a
2048-bit key. 

### Mitigation updated

Run all unpad checks, no matter what. If the first byte was wrong, note that there is an
error, then check the next byte. And so on. In the end, if there were any errors, simply
return the generic error message.

#### Not enough

Simply performing all checks is not necessarily enough. If there is a variation in the
amount of time spent on the unpad operation, information is leaked. For example, if both
the first byte and the second byte are incorrect, and there is no 00 marker byte, maybe
the amount of time to process is greater than if the problem is in the second byte alone.
The original timing attack said a quicker time meant the error was in the first byte. But
for some particular implementation, a quicker time could mean the error was in the second
byte.

It's not enough to make the computation slower, it really needs to be uniform. No matter
what the error, the time to perform the unpad operation is the same.

## Another mitigation: OAEP

Another way to solve this problem is to simply not use P1.5 padding. In 1994, Mihir
Bellare and Phillip Rogaway had developed a different padding scheme called Optimal
Asymmetric Encryption Padding (OAEP). Because of the Bleichenbacher attack, standards and
protocols had incentive to adopt this existing algorithm.

With OAEP, the padded data was indistinguishable from random (to help prevent other
"side-channel" attacks), and it was much more difficult to launch a Bleichenbacher attack,
even if the timing was known. That is, it was still possible an implementation of OAEP
unpad would leak information about where the padding went wrong, but because of how the
scheme worked (comparing digests of data rather than the data itself), that information
did not correlate to what bits in the chosen plaintext were different from the original
plaintext. Furthermore, it was much easier to write code that was more uniform anyway.

### Attack on OAEP

In 2001, James Manger published his attack on OAEP. In this, the attacker needs to know if
the data decrypted from the chosen ciphertext is greater than or less than a particular
value (often called "B"). It will take a few thousand chosen-ciphertext messages, but
eventually the attacker will be able to recover the original message.

When verifying whether unpadded data is correct, there is a check to see if the most
significant byte is zero or not. If it is not zero, that's an error. Furthermore, if it is
not zero, the attacker knows the result is greater than B. So if the OAEP unpad code
checks the most significant byte, and then exits immediately, that's a quick response. In
that case, the attacker knows the result from the chosen ciphertext is greater than B. A
longer response means it is less than.

### Mitigation

Once again, make sure the OAEP unpad operation performs the entire process every time, and
make the total time (error or no error) as uniform as possible.

### One more mitigation

There is another possible mitigation: variable times. This would be something similar to
"RSA blinding".

In order to thwart timing attacks on the RSA algorithm itself (not the padding scheme), an
implementation could add some random amount of time to the process. This is called
"blinding". When this happens, a quick response with one ciphertext block does not
necessarily mean that the actual computation time is less than the actual computation time
of a slow response with another ciphertext block.

Now add in the unpad operation. An attacker likely knows only how long the total RSA
operation took (RSA decryption and unpad). If there is too much variation in the RSA
decryption time, then there is no way to tell how much of the total time was RSA and how
much was unpad.

It is also possible to build implementations of the P1.5 and OAEP unpad algorithms that
add a variable amount of time each time it is computed.

While variable-time RSA blinding implementations are used in the real world,
variable-length unpad schemes are rare.

## Signing

A digital signature using RSA involves performing the padding operation, then encrypting
that result using the RSA private key. The owner of the private key does not perform the
unpad operation, so the attacks listed here are not relevant.

## What the attacker needs

For these timing attacks on the unpad operation, the attacker needs two things:

* An oracle, namely, the owner of the private key must be willing to decrypt thousands or
  millions of messages in a timely manner, no questions asked.
* Accurate times for completion of the task.

## Susceptibility of the YubiKey to these timing attacks

The YubiKey itself does not perform the unpad operation. If you call on the YubiKey to
decrypt, it will perform "raw" RSA and return the still-padded result. It is the
responsibility of the calling application to unpad.

Hence, the YubiKey itself is not susceptible to this class of attack.

## Susceptibility of the .NET YubiKey SDK to these timing attacks

Because an application calling on the YubiKey to decrypt will need to perform the unpad
operation, the SDK provides a class,
[RsaFormat](xref:Yubico.YubiKey.Cryptography.RsaFormat), that can unpad the result
of RSA decryption.

If you use this class to unpad RSA decryption, will your application be susceptible to
these timing attacks?

### The oracle requirement

A YubiKey will almost certainly not be used in some application as an oracle. That is, the
YubiKey will likely not be running automatically, performing decryptions no questions
asked. Probably the only situation where this could happen is if someone wants to use a
YubiKey as a substitute for an HSM providing cryptographic services for an SSL server.
That is not recommended, by the way.

The most likely use case for a YubiKey performing decryption is for an individual user to
decrypt messages. In that case, it is extremely unlikely that an attacker will be able to
get the user to perform thousands or millions of decrypt operations in a timely manner.

### The accurate time requirement

Because the most likely use case for decrypting with a YubiKey involves user interaction,
including PIN entry and touch, the time for each RSA decryption is so varied it is
virtually useless.

Even though it is highly unlikely an attacker could mount an unpad timing attack on the
SDK's `RsaFormat` class when used in conjunction with the YubiKey, we will examine the
operation's time variation.

### [RsaFormat](xref:Yubico.YubiKey.Cryptography.RsaFormat)

With the .NET YubiKey SDK, you have two choices for unpadding. One, use the
[RsaFormat](xref:Yubico.YubiKey.Cryptography.RsaFormat) class in
`Yubico.YubiKey.Cryptography`. Or two, use an alternate implementation, such as one
you write yourself. Note that the Unpad code that the .NET Base Class Libraries use is not
publicly accessible.

The engineers at Yubico have taken care to make sure the unpadding operations following
P1.5 and OAEP are as uniform as possible. Yubico makes no guarantees that this code is
completely immune to timing attacks. However, tests that timed how long to unpad correct
versus incorrect values showed little variation. See the results below.

#### RsaFormat timing results

In the following tables, timing numbers are in microseconds:

```txt
0.372 microseconds = 0.000000372 seconds (372 nanoseconds, 0x000372 millisecond)

14.1 microseconds = 0.0000141 seconds (14,100 nanoseconds, 0.0141 millisecond)
```

These are averages over several timing iterations. Where applicable, results are given
based on the message size. For example, the baseline measurements (in the "Correct"
column) are for no-error unpad operations when the encrypted data (the unpadded message)
is 16, 24, 32, or 48 bytes long. For the "First byte wrong" column, the first byte was not
valid, but everything after that was correct, including the message of given length.

All timing numbers were taken on a computer with a 1.6 GHz Intel Core i5, 8th Gen chip,
running Windows 10.

Start with P1.5.

##### 1024-bit block PKCS 1 v1.5

Correct<br/>P1.5 Unpad | First Byte<br/>Wrong (1) | Second Byte<br/>Wrong (2) | Not Enough<br/> Pad (3) | No Zero Byte | 1, 2, and 3
:---: | :---: | :---: | :---: | :---: | :---:
16: 0.354 | 16: 0.346 | 16: 0.346 | 16: 0.341 | 0.339 | 16: 0.340
24: 0.370 | 24: 0.340 | 24: 0.336 | 24: 0.348 |       | 24: 0.345
32: 0.371 | 32: 0.335 | 32: 0.335 | 32: 0.349 |       | 32: 0.346
48: 0.379 | 48: 0.335 | 48: 0.336 | 48: 0.347 |       | 48: 0.345
**Overall average** |
0.369 | 0.339 | 0.338 | 0.346 | 0.339 | 0.344

##### 2048-bit block PKCS 1 v1.5

Correct<br/>P1.5 Unpad | First Byte<br/>Wrong (1) | Second Byte<br/>Wrong (2) | Not Enough<br/> Pad (3) | No Zero Byte | 1, 2, and 3
:---: | :---: | :---: | :---: | :---: | :---:
16: 0.676 | 16: 0.675 | 16: 0.670 | 16: 0.678 | 0.684 | 16: 0.669
24: 0.681 | 24: 0.672 | 24: 0.670 | 24: 0.681 |       | 24: 0.679
32: 0.692 | 32: 0.670 | 32: 0.668 | 32: 0.683 |       | 32: 0.678
48: 0.692 | 48: 0.671 | 48: 0.673 | 48: 0.685 |       | 48: 0.679
**Overall average** |
0.686 | 0.672 | 0.670 | 0.682 | 0.684 | 0.676

These numbers indicate that there is very little variance between times based on message
size. Secondly, there is very little variance based on error or no error. Lastly, there is
very little variance based on the type of error. For example, whether the error is an
incorrect first byte, or a combination of the first three errors, the amount of time the
`RsaFormat` method will take is very similar.

Next, let's look at OAEP.

##### 1024-bit block OAEP with SHA-256

Correct OAEP | First Byte<br/>Wrong (1) | Incorrect<br/>lHash (2) | Wrong<br/>Separator (3) | No<br/>Separator | 1, 2, and 3
:---: | :---: | :---: | :---: | :---: | :---:
16: 16.38 | 16: 16.48 | 16: 16.29 | 16: 16.19 | 16: 16.21 | 16: 16.19
24: 16.34 | 24: 16.36 | 24: 16.22 | 24: 16.14 | 24: 16.32 | 24: 16.14
32: 16.29 | 32: 16.68 | 32: 16.52 | 32: 16.18 | 32: 16.20 | 32: 16.11
48: 16.18 | 48: 16.24 | 48: 16.19 | 48: 16.11 | 48: 16.19 | 48: 16.03
**Overall average** |
16.29 | 16.44 | 16.30 | 16.15 | 16.23 | 16.11

##### 2048-bit block OAEP with SHA-256

Correct OAEP | First Byte<br/>Wrong (1) | Incorrect<br/>lHash (2) | Wrong<br/>Separator (3) | No<br/>Separator | 1, 2, and 3
:---: | :---: | :---: | :---: | :---: | :---:
16: 28.15 | 16: 28.36 | 16: 28.24 | 16: 28.07 | 16: 28.20 | 16: 28.06
24: 28.36 | 24: 28.82 | 24: 28.79 | 24: 28.19 | 24: 28.22 | 24: 28.07
32: 28.11 | 32: 28.99 | 32: 28.31 | 32: 28.18 | 32: 28.04 | 32: 28.13
48: 28.11 | 48: 28.32 | 48: 28.18 | 48: 28.31 | 48: 28.20 | 48: 28.08
**Overall average** |
28.18 | 28.62 | 28.38 | 27.18 | 28.16 | 28.08

Once again, we see very little variance between times based on message size. Secondly,
there is very little variance based on error or no error. Lastly, there is very little
variance based on the type of error.

The time it takes to perform OAEP is dependent on the digest algorithm chosen. The numbers
above are from timing exercises using SHA-256. The following numbers are averages when
using the other digest algorithms.

##### 1024-bit block OAEP with SHA-1, SHA-256, and SHA-384

Correct OAEP | First Byte<br/>Wrong (1) | Incorrect<br/>lHash (2) | Wrong<br/>Separator (3) | No<br/>Separator | 1, 2, and 3
:---: | :---: | :---: | :---: | :---: | :---:
**SHA-1** |
14.27 | 14.22 | 14.19 | 14.10 | 14.21 | 14.17
**SHA-256** |
16.29 | 16.44 | 16.30 | 16.15 | 16.23 | 16.11
**SHA-384** |
17.41 | 17.42 | 17.23 | 17.24 | 17.26 | 17.25

##### 2048-bit block OAEP with SHA-1, SHA-256, SHA-384, and SHA-512

Correct OAEP | First Byte<br/>Wrong (1) | Incorrect<br/>lHash (2) | Wrong<br/>Separator (3) | No<br/>Separator | 1, 2, and 3
:---: | :---: | :---: | :---: | :---: | :---:
**SHA-1** |
26.29 | 26.38 | 26.42 | 26.8 | 26.15 | 26.10
**SHA-256** |
28.18 | 28.62 | 28.38 | 27.18 | 28.16 | 28.08
**SHA-384** |
30.13 | 30.43 | 30.21 | 29.87 | 30.01 | 29.85
**SHA-512** |
32.26 | 32.77 | 32.39 | 32.21 | 32.41 | 32.30

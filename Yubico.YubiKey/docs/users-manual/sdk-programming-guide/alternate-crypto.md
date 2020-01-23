---
uid: UsersManualAlternateCrypto
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

# Providing alternate cryptographic implementations

During the course of operations, the SDK will need to perform cryptographic operations
such as random number generation, HMAC, AES, Triple-DES, or more. When that happens, the
SDK will use the default cryptography in the .NET environment, unless you provide an
alternative. This page describes how to provide an alternative.

## This does not replace the crypto on the YubiKey

First of all, this has nothing to do with the cryptography performed on the YubiKey
itself. When the YubiKey needs to perform some cryptographic operation (such as signing,
random number generation, or so on), it will perform the crypto it has, either in the
chip or the firmware. You cannot get the YubiKey to use alternate implementations.

Instead, this is about the times the SDK needs to perform cryptography in order to
communicate with the YubiKey. For example, in order to authenticate the PIV management
key, the YubiKey will perform some Triple-DES operations (as specified by the PIV
standard). The off-board application will also need to perform complementary Triple-DES
operations. The off-board application will simply call on the SDK to perform the
necessary actions, including the Triple-DES operations.

## It is not necessary to provide alternate implementations

If you do not provide alternate crypto, the SDK will work. It will simply use the
default cryptography found in .NET (see `System.Security.Cryptography`). This is a
completely acceptable choice for most applications.

## Why provide alternate implementations?

Even though it is not necessary to provide crypto (i.e. doing nothing will work), some
developers will still use alternate implementations. They might do so because they have
access to a hardware random number generator or a hardware accelarator, or their product
must use a FIPS-certified crypto library. Some developers trust only their own
implementations.

For those developers who want to replace the default crypto the SDK uses, then read on.
For developers who are happy to let the SDK use the default .NET implementations, it is
fine to skip this page.

## CryptoProviders class

Whenever the SDK needs to perform some crypto operation it calls on the
[CryptoProviders](xref:Yubico.YubiKey.Cryptography) static class. That class has a
number of properties that are actually delegates (function pointers). These functions
build the objects the SDK will use to perform the crypto.

Therefore, to replace the default implementations:

* Create a class that can perform the algorithm in question
* Write a function that can build the object (factory method)
* Load that function into the `CryptoProviders` class.

The class you build must be a subclass of a specific .NET class, and the function must
have the specific signature specified in the `CryptoProviders` class.

Why does the SDK use a delegate that builds an object (a factory method), rather than
loading an actual object itself? There are two main reasons.

First, an object contains state, which can include keys or plaintext. Each time the SDK
needs to perform any crypto, it will build an object, use it, and then immediately dispose
it so any sensitive data can be overwritten as soon as possible. If an object were to be
loaded, rather than a factory method, it would be much more difficult to guarantee every
secret inside the object were overwritten after using it.

Second, the default crypto is from the `System.Security.Cryptography` namespace, and all
the classes that will be used implement `IDisposable`. Any replacement object will need to
be a subclass and hence must also implement `IDisposable`. Because any object the SDK will
use is disposable, then the rules of ownership must be followed. That is, ownership of
the objects must be firmly established so that objects are not disposed before they might
be used. By building, and owning, new objects each time, the SDK will avoid any ownership
problems.

### Example: How the SDK uses the CryptographyProviders class for random number generation

Look at the `CryptoProviders` class. There is a property for the random number generator.

```
    public static class CryptographyProviders
    {
        public static Func<RandomNumberGenerator> RngCreator { get; set; }
    }
```

Suppose the SDK is performing an operation that needs random numbers. Here's what it will
do.

```
    using System.Security.Cryptography;

     . . .

    using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator()
    {
        randomObject.GetBytes(buffer);
    } 
```

### Example alternate implementation: RandomNumberGenerator

The SDK is built with the `RngCreator` property set to a function that will build and
return the default RNG. To change to a new RNG, set the `RngCreator` property to your
function (even though this is a static class, it is possible to set the properties).

Your function must have the following signature.

```
    System.Security.Cryptography.RandomNumberGenerator CreateAlternateRng();
```

#### The RandomNumberGenerator class

.NET defines a public abstract class `System.Security.Cryptography.RandomNumberGenerator`.
The SDK will expect to build an instance of this class and use it. In fact, the
`RngCreator` function loaded by the SDK will build an object using the
`RandomNumberGenerator.Create` method.

```
    // Creates an instance of the default implementation of a cryptographic random number
    // generator that can be used to generate random data.
    public static System.Security.Cryptography.RandomNumberGenerator Create();
```

The SDK's `RngCreator` function is simply one line of code:

```
    return RandomNumberGenerator.Create();
```

#### The alternate

Your replacement starts with a class that is a subclass of RandomNumberGenerator.

```
    public class AlternateRandom : System.Security.Cryptography.RandomNumberGenerator
    {
    }
```

Once you have a class that fulfills the requirements, you now build a function that can
create an instance of that class. This might be a static method inside the class itself,
or it might be in another class. It might look something like this.

```
    public static RandomNumberGenerator CreateAlternateRng()
    {
        return new AlternateRandom();
    }
```

#### Set the RngCreator property in CryptographyProviders

All you need to do is set the `RngCreator` property. After setting it to the new function,
every time the SDK creates a new `RandomNumberGenerator`, it will be calling your method.

```
    CryptographyProviders.RngCreator = AlternateRandom.CreateAlternateRng;
```

You will likely do this at the beginning of your program, such as in the `main`.

#### Possible extra information

Suppose your implementation needs some information to instantiate. For example, it might
need a handle to a hardware device or a handle to a FIPS library. That is, the constructor
is actually

```
   public AlternateRandom(SomeHandleType handle) { }
```

Your function that builds it would normally look like this.

```
    public static RandomNumberGenerator CreateAlternateRng(SomeHandleType handle)
    {
        return new AlternateRandom(handle);
    }
```

However, the function you provide to the SDK must be a method that has no input arguments.
There are a couple ways around this.

First, you could create a new handle each time.

```
    public static RandomNumberGenerator CreateAlternateRng()
    {
        SomeHandleType handle = SomeClass.BuildHwHandle();
        return new AlternateRandom(handle);
    }
```

It is possible you do not want to create a new handle every time, and besides, you might
need more information to build the handle, such as a hardware path.

A second way is to have your `RandomNumberGenerator` class hold the information, create an
instance of that class, and pass an instance method as the delegate.

```
using System.Security.Cryptography;

    public static AlternateRandom
    {
        public SomeHandleType Handle { get; set; }

        public AlternateRandom(SomeHandleType handle)
        {
            Handle = handle;
        }

        public RandomNumberGenerator CreateAlternateRng()
        {
            return this;
        }
    }
```

At the beginning of your program, you would do something like this.

```
    var handle = new SomeHandleType(info);

    var alternateRandom = new AlternateRandom(handle);
    CryptographyProviders.RngCreator = alternateRandom.CreateAlternateRng;
```

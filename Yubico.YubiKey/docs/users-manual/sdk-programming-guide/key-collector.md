---
uid: UsersManualKeyCollector
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

# The `KeyCollector`

In the SDK, there is the concept of a `KeyCollector`. This is a user-supplied delegate, a
callback method the SDK calls when it needs a PIN, password, key, or some other secret
value in order to complete verification or authentication. For example, with the PIV
application, you can make a call to sign data. That requires the PIN to be verified in
order to execute.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        byte[] signature = pivSession.Sign(PivSlot.Authentication, dataToSign);
    }
```

The SDK's `Sign` method will determine it needs the PIN, so it will make a call to
`SomeKeyCollector`, requesting the PIN. That method will do what it needs to get the PIN
from the end user, such as creating a new Window with a PIN box or writing/reading from
the command line. The `KeyCollector` returns the PIN and the SDK verifies it. The data can
now be signed.

It is also possible to call the `VerifyPin` method directly.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        pivSession.VerifyPin();
    }
```

As with the `Sign` method, the `VerifyPin` will call on the `KeyCollector` to retrieve the
PIN. Normally, an application would never call the `VerifyPin` method because there's no
need. The SDK will automatically make the necessary calls to verify a PIN when it needs
the PIN to be verified, and simply won't verify if it does not need it.

Once a PIN has been verified in a session, generally, there is no need to verify it again
in that session (there are exceptions [discussed below](#called-if-needed-more-than-once)).
For example, suppose you want to create two signatures.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        byte[] signature1 = pivSession.Sign(PivSlot.Authentication, dataToSign1);
        byte[] signature2 = pivSession.Sign(PivSlot.Authentication, dataToSign2);
    }

    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        pivSession.VerifyPin();

        byte[] signature1 = pivSession.Sign(PivSlot.Authentication, dataToSign1);
        byte[] signature2 = pivSession.Sign(PivSlot.Authentication, dataToSign2);
    }
```

In the first session above, the first call to `Sign` will, "under the covers", make a call
to verify the PIN. For the second call to `Sign`, there is no need, the PIN has already
been verified. In the second session above, the PIN has been verified by the call to
`VerifyPin`, so both calls to `Sign` will execute without calling the `VerifyPin` method
under the covers.

# Verification and authentication without the `KeyCollector`

The SDK contains methods to verify and authenticate where the secret value is provided by
the caller, rather than the `KeyCollector`. For example, with PIV, there are these
methods.

```csharp
    bool TryVerifyPin(ReadOnlyMemory<byte> pin, out int? retriesRemaining);
    bool TryChangePin(ReadOnlyMemory<byte> currentPin, ReadOnlyMemory<byte> newPin, out int? retriesRemaining);
    bool TryChangePuk(ReadOnlyMemory<byte> currentPuk, ReadOnlyMemory<byte> newPuk, out int? retriesRemaining);
    bool TryResetPin(ReadOnlyMemory<byte> puk, ReadOnlyMemory<byte> newPin, out int? retriesRemaining);
    bool TryChangePinAndPukRetryCounts(
            ReadOnlyMemory<byte> managementKey,
            ReadOnlyMemory<byte> pin,
            byte newRetryCountPin,
            byte newRetryCountPuk,
            out int? retriesRemaining);

    bool TryAuthenticateManagementKey(ReadOnlyMemory<byte> managementKey, bool mutualAuthentication = true);
    bool TryChangeManagementKey(
            ReadOnlyMemory<byte> currentKey,
            ReadOnlyMemory<byte> newKey,
            PivTouchPolicy touchPolicy = PivTouchPolicy.Default)
```

There is no need to build a `KeyCollector`. You simply verify the PIN and authenticate the
management key each session.

# The case for a `KeyCollector`

If you want to use the caller-supplied verify and authenticate methods, you are still
going to need to collect the PIN, password, or key. That is, your application most likely
contains a "Key Collector" component already. For example, you have code to collect the
PIN from the user, otherwise where did the PIN come from?

Because you probably already have code to collect the PIN/Password/Key, it is likely not
going to be difficult to extend it to fit within the SDK's `KeyCollector` framework.

Furthermore, there are advantages to using the `KeyCollector`.

## The SDK manages PIN, password, and key requirements

With PIV, for example, some operations require the PIN, other operations require the
management key, and still other operations that require both.

With a `KeyCollector`, you do not need to make sure your code is written to fulfill the
correct verify/auth logic. The SDK takes care of that for you.

## Collected only if needed

The SDK will only ask for a secret to be collected if it is needed. This reduces the
exposure to attack. See the User's Manual article on
[sensitive data](xref:UsersManualSensitive).

Either your application must manage the logic of when each secret is needed, or it can
simply use the caller-supplied verification methods at the beginning of each session.
Generally, once a secret has been verified in a session, there is no need to verify it
again. Hence, just make sure each secret is authenticated in each session and there is
no futher management needed.

For example, with PIV, simply do this.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        bool isAuth = pivSession.TryAuthenticateManagementKey(mgmtKey);
        bool isVerified = pivSession.TryVerifyPin(pin);

         . . .
    }
```

The downside to this is that you will be authenticating a secret even when it is not
needed. If your application will be doing something that needs the PIN, but it won't be
doing anything that needs the management key, you will be requiring the user to provide
the management key anyway, making them perform some task that is not needed. In additon,
ot increases the exposure to attack.

## The SDK manages retries

Suppose your application creates a PIV session and you will be doing something that
requires PIN verification. You collect the PIN from the user and call the following.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        bool isVerified = pivSession.TryVerifyPin(pin, out int? retriesRemaining);
    }
```

Suppose the return is `false`. Maybe the PIN is really `Paris16`, but the user
accidentally typed `Paris167`. Now what? How about the following?

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        ReadOnlyMemory<byte> pin = CollectPin();
        while (!pivSession.TryVerifyPin(pin, out int? retriesRemaining))
        {
            if (!CollectPin(someMessage, retriesRemaining, out ReadOnlyMemory<byte> pin))
            {
                throw OperationCanceledException(message);
            }
        }
    }
```

Much of this logic is already handled by the SDK. You can call the following.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = SomeKeyCollector;

        // false is returned if the user cancels.
        bool isVerified = pivSession.TryVerifyPin();

        // An exception is thrown if the user cancels.
        pivSession.VerifyPin();
    }
```

In these cases the SDK will handle the retries. It will call the `KeyCollector` until the
correct PIN is entered, the delegate returns canceled, or the retry count goes to zero.
The `KeyCollector` you provide will have to decide how to present the retry count to the
user and offer a way to cancel. But any solution will need to do that.

## The SDK tells the `KeyCollector` to `Release`

Once the secret has been authenticated, it is a good idea to overwrite sensitive data.
With a `KeyCollector` you write that code once. The SDK will call the `KeyCollector` with
`KeyEntryRequest.Release`, indicating that the SDK no longer needs the collected value.
The `KeyCollector` knows it can now overwrite any data and release any other resources.

If you don't use the `KeyCollector`, you will have to write the release code every time.
For example, here is a possibility.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        var pinData = new Memory<byte>(new byte[8]);

        try
        {
            int pinLength = CollectPin(pinData);
            while (!pivSession.TryVerifyPin(pinData.Slice(0, pinLength, out int? retriesRemaining))
            {
                pinLength = CollectPin(someMessage, retriesRemaining, pinData))
                if (pinLength == 0)
                {
                    throw OperationCanceledException(message);
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pinData.Span);
        } 
    }
```

## Called "just in time"

The SDK will not request a secret until it is needed. That means you don't need to collect
it and have it waiting around in memory, just in case it is needed. Of course, you can
collect it at the beginning of a session, authenticate it, and release it. But that has
its own [problems](#collected-only-if-needed).

## Called if needed more than once

There are rare cases where authentication of a secret is needed more than once per
session. In such a case, using a `KeyCollector` will be the most convenient.

For example, with PIV, it is possible to generate or load a private key with the PIN
policy "Always". This means the PIN must be verified each time it is used, even if the PIN
has already been verified in the session.

Under the covers, the [Verify command](../application-piv/commands.md#verify) must be the
only command executed before the
[Sign command](../application-piv/commands.md#authenticate-sign).

If using the `KeyCollector`, the SDK will make sure that happens. Without it, you will be
required to manage it.

## Might be needed for PIV PIN-only mode

Many applications will set a YubiKey to [PIN-only](../application-piv/pin-only.md). This
means that PIV operations that require management key authentication will be able to
execute with the caller supplying onl the PIN.

If a YubiKey is set to the PIN-only mode of `PinDerived`, the SDK will require a
`KeyCollector` to obtain the PIN. Note that Yubico recommends applications NOT use
`PinDerived`. It is provided only for backwards compatibility. You should only use
`PinProtected`.

If a YubiKey is set for `PinProtected`, it will generally be possible to use the
YubiKey without a `KeyCollector`. However, there are odd cases where a YubiKey can be
set for `PinProtected` and a `KeyCollector` is needed to obtain the PIN.

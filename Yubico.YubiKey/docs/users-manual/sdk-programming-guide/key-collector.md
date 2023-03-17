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

# The `KeyCollector` and alternatives 

> [!NOTE]
> The sample code contains sample key collectors. One sample key collector can be found at
> `Yubico.YubiKey/examples/PivSampleCode/KeyCollector`. It collects PINs and keys from the
> command line.
> See also the section below, [Building a KeyCollector](#building-a-keycollector).

In the SDK, there is the concept of a `KeyCollector`. This is a user-supplied
[delegate](delegates-in-sdk.md), a callback method the SDK calls when it needs a PIN,
password, key, or some other secret value in order to complete verification or
authentication.

> [!NOTE]
> The key collector is also used to notify the caller that touch or a fingerprint is
> needed. See the [article on the KeyCollector and touch](key-collector-touch.md) for a
> more detailed description of how to handle touch notifications.

For example, with the PIV application, you can make a call to sign data. That requires the
PIN to be verified in order to execute.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        pivSession.KeyCollector = CallerSuppliedKeyCollector;

        byte[] signature = pivSession.Sign(PivSlot.Authentication, dataToSign);
    }
```

The SDK's `Sign` method will determine it needs the PIN, so it will make a call to
`CallerSuppliedKeyCollector`, requesting the PIN. That method will do what it needs to get
the PIN from the end user, such as creating a new Window with a PIN box or writing/reading
from the command line. The `KeyCollector` returns the PIN and the SDK verifies it. The
data can now be signed.

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

One alternative is for your app to manage the logic of when each secret is needed. Another
alternative is to simply use the caller-supplied secret verification methods at the
beginning of each session. Generally, once a secret has been verified in a session, there
is no need to verify it again. Hence, just make sure each secret is authenticated in each
session and there is no futher management needed.

For example, with PIV, you could do this.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        bool isAuth = pivSession.TryAuthenticateManagementKey(mgmtKey);
        bool isVerified = pivSession.TryVerifyPin(pin);

         . . .
    }
```

One downside to this is that you will be authenticating a secret even when it is not
needed. If your application will be doing something that needs the PIN, but it won't be
doing anything that needs the management key, you will be requiring the user to provide
the management key anyway, making them perform some task that is not needed. In addition,
it increases the exposure to attack.

Another downside is that there are rare cases when a PIN might be needed more than once.
These exceptions are [discussed below](#called-if-needed-more-than-once).

## The SDK manages retries

Suppose your application creates a PIV session and you will be doing something that
requires PIN verification. You collect the PIN from the user and call the following.

```csharp
    using (var pivSession = new PivSession(yubiKey))
    {
        bool isVerified = pivSession.TryVerifyPin(pin, out int? retriesRemaining);
    }
```

Suppose the return is `false`. Maybe the user typed `Paris167, but the PIN is really
`Paris16` Now what? How about the following?

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

# Building a KeyCollector

At a minimum, your key collector will be a single method with this signature.

```csharp
   public bool KeyCollectingFunction(KeyEntryData keyEntryData);
```

The return is a `boolean`, if it was able to collect the value or values requested, return
`true`. If not, return `false`. Almost always, a `false` means the user canceled the
operation.

It's certainly possible to write a "standalone" method (e.g. a static method in a static
class), but more likely, your key collector will be a class.

```csharp
using Yubico.YubiKey;

    public class MyKeyCollector
    {
        // fields and properties

        // Create a new KeyCollector object that pops up a window.
        // This window will be a child of the parentWindow and will
        // have boxes for entering the PINs and keys, along with OK
        // and Cancel buttons.
        public MyKeyCollector(Handle parentWindow)
        {
        }

        // Create a new KeyCollector object that pops up a window.
        // This window will be a standalone window, no parent. It
        // will have boxes for entering the PINs and keys, along
        // with OK and Cancel buttons.
        public MyKeyCollector()
        {
        }

        // This is the method passed to the Yubico SDK as the
        // KeyCollector delegate.
        public bool KeyCollectorDelegate(KeyEntryData keyEntryData)
        {
        }
    }
```

Using this class would look something like this.

```csharp
        var keyCollectorObject = new MyKeyCollector(parentWindow);

         . . .

        using (var pivSession = new PivSession(yubiKey))
        {
            pivSession.KeyCollector = keyCollectorObject.KeyCollectorDelegate;

             . . .
        }
```

There's a good chance you already have some class that is a key collector. It might not be
called "key collector", but it is likely you already have a class built to collect PINs,
passwords or other such user-supplied secrets. If so, you will possibly need to simply add
a method that fulfills the SDK's delegate requirement.

## The KeyCollector delegate

Your method, at its foundation, will be something like this.

```csharp
        public bool KeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    // Do release work.

                case KeyEntryRequest.VerifyPivPin:
                    // Collect a PIN to be used to verify a PIV session.

                case KeyEntryRequest.ChangePivPin:
                    // Collect two PINs, the current and a new one, to be used
                    // to change the PIV PIN.

                case KeyEntryRequest.ChangePivPuk:
                    // Collect two PUKs, the current and a new one, to be used
                    // to change the PIV PUK.

                case KeyEntryRequest.ResetPivPinWithPuk:
                    // Collect the PUK and a new PIN to be used to recover the
                    // PIV PIN.

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    // Collect a management key to be used to authenticate a
                    // PIV session.

                case KeyEntryRequest.ChangePivManagementKey:
                    // Collect two management keys, the current and a new one,
                    // to be used to change the PIV management key.
            }
        }
```

When the SDK calls your key collector, it will pass an enum parameter indicating what is
requested. Your code will now know what it has to present to the user. Will it be a
message saying, "Enter the PIV PIN"? Or a message saying, "Enter the current PIV PIN and a
new PIN, in order to change the PIN"?

There are at least eight enum values indicating what the SDK needs the key collector to
collect (and probably more in the future as more features are added to the SDK). Your key
collector does not have to support all of them. If you build a key collector that only
verifies or changes a PIV PIN, then your switch statement only needs to support those
values (and Release).

The value indicating the request is the `KeyEntryData.Request` property. There is more
information in the `KeyEntryData` object.

```csharp
    public sealed class KeyEntryData
    {
        public KeyEntryRequest Request { get; set; }
        public bool IsRetry { get; set; }
        public int? RetriesRemaining { get; set; }
    }
```

Suppose the `Request` is `KeyEntryRequest.VerifyPivPin`. Your code now knows that the SDK
wants you to collect the PIV PIN. But your code can also look at the `IsRetry` property.
If that is `true`, your code now knows that a PIN had already been collected, but it was
incorrect. Maybe you want to present a message to the user, "The previous PIN attempt
failed. Do you want to try again?" Furthermore, you can look at the `RetriesRemaining`
property. You can let the user know how many retries they have before the PIN is blocked.

```txt
   Enter the PIV PIN
   The previous PIN attempt failed.
   You have  4 attempts remaining before
   the PIN is blocked.

   PIN:_______________

   OK        CANCEL
```

If the caller decides to cancel, your key collector delegate can return `false`.

Once your code has collected the PIN, it must return it. That is done using the
`SubmitValue` and `SubmitValues` methods inside the `KeyEntryData`.

Just as the `KeyEntryData` has information letting you know all about what is being
requested, it contains methods that allow you to return the values collected. If you are
to return one value (e.g. a PIV PIN for verification), then return that value using
`KeyEntryData.SubmitValue`. If you are to return two values (e.g. a current and a new PIV
PIN), return them using `KeyEntryData.SubmitValues`.

```csharp
using System.Security.Cryptography;
using Yubico.YubiKey;

    public class MyKeyCollector
    {
        private byte[] _currentValue = new byte[MaxValueLength]
        private int _currentLength;
        public Memory<byte> CurrentValue = new Memory<byte>(_currentValue);

        public bool SampleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            switch (keyEntryData.Request)
            {
                case KeyEntryRequest.Release:
                    CryptographicOperations.ZeroMemory(CurrentValue.Span)
                    break;

                case KeyEntryRequest.VerifyPivPin:
                    // The CollectValue method will collect the PIN and store
                    // it in the CurrentValue property.
                    isCollected = CollectValue(
                        "PIN", keyEntryData.IsRetry, keyEntryData.RetriesRemaining);
                    if (isCollected)
                    {
                        keyEntryData.SubmitValue(CurrentValue.Slice(0, _currentLength).Span);
                    }
                    break;
            }

            return isCollected;
        }
    }
```

### Release

One possible value of `KeyEntryRequest` is `Release`. This is how the SDK tells the
delegate that it has used the PIN (or key or whatever was requested), and your code can
release any resources. At this point, the delegate will likely overwrite sensitive data,
close handles, and so on.

Your `KeyCollector` delegate MUST NOT throw an exception when the request is `Release`.
Most `KeyCollector` delegates will likely be written to never throw an exception in any
situation (just return `false` if something goes wrong), but it is vitally important that
it never throw an exception when the request is `Release`. The `Release` is called from
inside a `finally` block, and it is a bad idea to throw exceptions from inside `finally`.

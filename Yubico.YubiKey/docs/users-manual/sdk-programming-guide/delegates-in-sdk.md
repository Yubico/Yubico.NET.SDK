---
uid: UsersManualDelegatesInSdk
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

# Delegates (callbacks) in the SDK

There are a number of places in the SDK where the developer must supply a `delegate`,
the C# version of a `callback`. This page discusses what they are, and how to write your
own.

## A callback

A callback is simply a function you provide the SDK. In turn, the SDK will call that
function when it needs to do some work that only your application can do. The easiest way
to illustrate this is with an example: the PIN collector.

Suppose you are building a PIV application, and want to sign some data. That operation
requires the PIN to be verified (it is possible to turn off the PIN requirement, but for
now, let's just look at the case where the PIN is required). In order to verify the PIN,
the SDK, of course, needs the PIN. How should that be provided?

While it is possible to simply require the caller enter the PIN as a byte array, the SDK
requires a PIN-collecting function. In this way, when the SDK needs the PIN, it will call
this function, use the PIN it collects, then release it. Following this pattern, the PIN
appears in memory the least amount of time (see the User's Manual entry on
[sensitive data](sensitive-data.md)).

The SDK does not have a PIN-collecting class or method. That's because how your
application collects the PIN is up to you. Maybe you collect it at the command line, maybe
you launch a new Window, or have a "sub-window" of the main window. Maybe you collect it
once at the beginning and simply keep it in a buffer somewhere (not recommended).

When the sign operation needs the PIN, it will call a PIN-collecting function. But because
there is no such method in the SDK, it will be your responsibility to provide one.

## The PIN-collecting callback

The PIN-collecting callback is defined as follows.

```
    public Func<KeyEntryData, bool>? KeyCollector
```

Notice that it is called `KeyCollector`, because it collects PINs, PUKs, management keys,
passwords, and any other sensitive data. The term `Key` is used as a general term and to
highlight the fact that this collects sensitive data.

You must provide a method with this signature.

```
   public bool PinCollectingFunction(KeyEntryData keyEntryData);
```

### The [KeyEntryData](xref:Yubico.YubiKey.KeyEntryData)

This is an object that contains information about the request. One property is a `Request`
that describes what the SDK wants the delegate to do.

One possible value of `Request` is `KeyEntryRequest.Release`. This is how the SDK tells
the delegate that it has used the PIN (or key or whatever was requested), and your code
can release any resources. At this point, the delegate will likely overwrite sensitive
data, close handles, and so on.

Note that the `KeyCollector` delegate MUST NOT throw an exception when the request is
`Release`. Most `KeyCollector` delegates will likely be written to never throw an
exception in any situation (just return `false` if something goes wrong), but it is
vitally important that it never throw an exception when the request is `Release`. The
`Release` is called from inside a `finally` block, and it is a bad idea to throw
exceptions from inside `finally`.

Inside the `KeyEntryData` class is also a property `IsRetry`. If true, this indicates that
a PIN (or key or whatever) that was returned the last call, did not succeed, and the user
needs to try again. The class also contains an `int` reporting the number of retries
remaining. This property is nullable so that if there is no count, it is null, and if
there is one, it contains the number of retries remaining before the element is blocked.

### The return

The `KeyCollector` returns a boolean. `False` means "cancel" (`true` means success or
"don't cancel"). If the SDK calls the `KeyCollector` and it returns `false`, it will stop
the operation it is performing and return.

For example, suppose your application calls on the SDK to perform a sign operation. The
SDK discovers that it needs the PIN, so calls the `KeyCollector`. The user enters a PIN,
the `KeyCollector` returns that value (in the `KeyEntryRequest` object) and returns
`true`. The SDK tries to verify the PIN, but learns it is not the correct value. The SDK
now calls the `KeyCollector` again, till indicating that it is asking for the PIN, but
for this attempt `IsRetry` is `true`. The SDK also passes on the remaining tries count.

At this point, your application creates a new window informing the user that the PIN
entered was incorrect and there are "x" number of retries remaining before the PIN is
blocked. The user has the option of cancelling the request. If the user cancels the
request, your `KeyCollector` function will return `false`.

### Example

Your application might do something such as this.

```
    // This class contains methods and data necessary to collect a PIN.
    class PinCollectionInfo
    {
        public WindowHandle Handle { get; set; }
        public UserInformation UserInfo { get; set; }
        
        private WindowInfo _windowInfo;

        public bool PinCollector(KeyEntryData keyEntryData);
    }

    // Now in the code that is calling the SDK:
    var pinInfo = new PinCollectionInfo()
    {
        Handle = someHandle,
        UserInfo = currentUserInfo,
    }

    using var pivSession = new PivSession(yubiKey)
    {
        KeyCollector = pinInfo.PinCollector,
    }
```

### One or two values

The `KeyCollector` will be called upon to collect either a single value, or two values. A
single value is requested when the SDK needs a PIN or key to verify, and it needs two
values when it is changing a PIN or other element. It needs the current value and a new
value.

### Inside the KeyCollector

The SDK is going to call your delegate when it needs the PIN (or PUK, or management key,
or some other user-supplied secret). When it does, it will pass to you a `KeyEntryData`
object. For example, see the
[PivSession's `KeyCollector`](xref:Yubico.YubiKey.Piv.PivSession.KeyCollector%2a).

The delegate you write will read the `KeyEntryData` to determine what the SDK is
requesting.

```
    switch (keyEntryData.Request)
    {
        default:
            return false;

        case KeyEntryRequest.Release:
            OverwriteBuffers();
            ReleaseResources();
            break;

        case KeyEntryRequest.VerifyPivPin:
            SetWindowVerifyPin(_windowInfo);
            break;

        case KeyEntryRequest.ChangePivPin:
            SetWindowChangePin(_windowInfo);
            break;

         . . .
    }

    WindowProcess.CollectKeyData(Handle, _windowInfo);
```

When your code has the value, it will call the `KeyEntryData.SubmitValue` or
`KeyEntryData.SubmitValues` method to load the value or values (two values whn changing
the PIN or key) into the `KeyEntryData` object. The input is a `ReadOnlyMemory<byte>`, but
passing a `byte[]` will work (it is automatically cast to the `ReadOnlyMemory<byte>`). The
SDK will be able to read the value or values, but will not be able to alter your buffer.
Furthermore, the `KeyEntryData` object will make a copy of the data (it won't simply copy
a reference to your buffer). It will be your responsibility to overwrite that buffer when
it is no longer needed.

When the SDK is done with the key collection process, it will call the delegate with the
`Request` of `KeyEntryRequest.Release`. Remember, your delegate must never throw an
exception if the request is `Release`.

Note that in this example, the `KeyCollector` needed a `Handle` and `UserInfo`. Your
collecting method might need something in order to work. Maybe it is a window handle,
because your PIN-collector will launch a window with a message and text-entry box. Or
maybe it is a database handle, or something else. It is whatever your PIN-collecting
function will need.

The delegate you supply must have access to that data. In the example, the collecting
method was an instance method of a class that held the necessary data.

The process is

* create a class that holds the information needed, along with a method that collects the
PIN. This method has the same signature as the delegate.
* In the application, create an instance of that class and set it with the needed data.
* Pass the PIN-collecting instance method as the delegate.

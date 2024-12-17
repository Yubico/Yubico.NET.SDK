---
uid: UsersManualKeyCollectorTouch
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

# The KeyCollector's touch notification

Normally, the KeyCollector is used to collect PINs, keys, or other secret values.
However, it is also used to notify the caller that an operation requires touch or a
fingerprint in order to complete.

For example, suppose your code is calling the SDK in order to make a FIDO2 credential on a
YubiKey. The SDK will call on the YubiKey to perform the `MakeCredentialCommand`. That
command will be able to complete the task only if user touches the gold contact at the
right time.

But when exactly is the user supposed to touch the contact? You could write your code to
guess when touch is needed, which might work. However, if you supply a KeyCollector, the
SDK will notify your application (which can then notify the user) exactly when it is
needed.

## What the SDK does

When the SDK needs the user to touch the YubiKey's contact, it will call the KeyCollector
delegate with the `KeyEntryRequest.TouchRequest`.

```csharp
    var keyEntryData = new KeyEntryData()
    {
        Request = KeyEntryRequest.TouchRequest
    };

    _ = KeyCollector(keyEntryData);
```

The return from the KeyCollector is a boolean. A `false` return indicates the SDK should
cancel the operation. However, this call is informative only, there is no cancelling.
Hence, the SDK will ignore the return value.

The SDK will make the KeyCollector call on a new thread, which allows work on the main
communication thread between the SDK and YubiKey to proceed without interruption.

Later on, when the operation has completed (the user touched the contact), timed out (the
user did not touch the contact in time), or there was an error, the SDK will call the
KeyCollector again, this time with the `KeyEntryRequest.Release`. This call to release
will be on the same thread as the call to request touch.

## What your KeyCollector should do

When the SDK calls your KeyCollector delegate with the touch request, it needs to do two
things: notify the user that touch is needed and return to the SDK "immediately".

"Immediately" means your code should not wait for an indeterminate operation to complete
before returning. An example of an indeterminate operation is waiting for a user to click
an "OK" button; it might happen quickly, it might not happen for several seconds, it is
possible it doesn't happen for days or even ever.

You might accomplish this by writing the touch notification to the command line (e.g.
`Console.WriteLine`), launching a modeless notification window, or launching a modal
notification window on a new thread (e.g. `Task` class). In all three of these
possibilities, your code will make calls that perform an operation for which the caller
(the SDK) does not need to wait.

This call to the KeyCollector notifies your code that user interaction with the YubiKey is
needed. It is possible to write a KeyCollector that requires further user interaction as
well. For example, you could write a KeyCollector that launches a window with a message
indicating the YubiKey needs touch, plus an "OK" button (or similar). That is, there are
two acts of user interaction here: touch the YubiKey's contact and click the "OK' button.
The window is not closed until the button is clicked.

However, because the touch notification is informative only, your KeyCollector most likely
should not require further user interaction. Hence, your best option is likely a modeless
notification window, such as Example 2 below.

When the YubiKey's operation is complete (either timeout, error, or the user did indeed
touch, and it executed successfully), the SDK calls your KeyCollector with the Release
request. You will have the opportunity to close any windows or perform any other cleanup
necessary.

### Example 1: Console

```csharp
    public class SampleKeyCollector
    {
        . . .

        public bool Fido2SampleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            . . .

            switch (keyEntryData.Request)
            {
                case KeyEntryRequest.TouchRequest:
                    // This call does not wait for any other action (such as user interaction)
                    // to return. It writes to the console and returns "immediately".
                    Console.WriteLine("Touch the YubiKey's contact to complete the operation.\n");
                    return true;

                case KeyEntryRequest.Release:
                    // There's no cleanup necessary with a call to Console.WriteLine.
                    return true;
            }
        }

        . . .
    }
```

### Example 2: Modeless window

In this example, the KeyCollector will launch a window, which will remain until its
`Visible` property is set to `false`. That happens when the SDK calls with `Release`. The
procedure can be described as follows:

* The SDK discovers that touch is needed.
* The SDK creates a new thread (call it thread T) and calls the KeyCollector on this
  thread with the request `TouchRequest`.
* The KeyCollector launches a window with a message. This call returns "immediately" to
  the KeyCollector.
* The KeyCollector in turn returns "immediately" to the SDK.
* When the operation completes (most likely the user touched the YubiKey's contact), the
  SDK, on Thread T, calls the KeyCollector with the request `Release`.
* The KeyCollector takes down the window and returns.
* Thread T ends.

This is what it looks like from the user's perspective:

* A window appears indicating they need to touch the YubiKey's contact.
* They touch the contact.
* The Window disappears.

```csharp
using System.Windows.Forms;

    // This class contains fields of Form and Label, which implement
    // IDisposable, which means it is likely your KeyCollector will also need
    // to implement IDisposable, and in your Dispose, call the Form and Label
    // Dispose methods.
    public class SampleKeyCollector : IDisposable
    {
        private Form _form;
        private Label _label;
        private bool _disposed;

        . . .

        public bool Fido2SampleKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            . . .

            switch (keyEntryData.Request)
            {
                case KeyEntryRequest.TouchRequest:
                    _form = new Form
                    {
                        Text = "User Action Required",
                        MaximizeBox = false,
                        MinimizeBox = false,
                        ControlBox = false,
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    _label = new Label
                    {
                        Text = "Touch the YubiKey's contact to complete the operation.",
                        AutoSize = true
                    };
                    _form.Controls.Add(_label);

                    // This call launches the window with the title "User Action Required"
                    // and the message in the client area "Touch the YubiKey's contact ...".
                    // It is modeless so does not wait for any other action (such as user
                    // interaction) to return. It simply launches the window and returns,
                    // just as a call to Console.WriteLine does. Hence, it will return to
                    // the SDK "immediately". The window will remain visible until it is
                    // closed.
                    _form.Show();
                    _label.Refresh();
                    return true;

                case KeyEntryRequest.Release:
                    // This will be called on the same thread as the TouchRequest was made.
                    // Now that we know the YubiKey has completed its operation, we can
                    // close the window.
                    if (!(_label is null))
                    {
                        _label.Visible = false;
                        _label.Dispose();
                        _label = null;
                    }
                    if (!(_form is null))
                    {
                        _form.Visible = false;
                        _form.Dispose();
                        _form = null;
                    }
                    return true;
            }
        }

        . . .
    }
```

In this example, a window is built using the `Form` class and launched using the `Show`
method. By launching using the `Show` method, it is a modeless window. It is possible to
launch it as a modal window (requiring user interaction) by calling the `ShowDialog`
method.

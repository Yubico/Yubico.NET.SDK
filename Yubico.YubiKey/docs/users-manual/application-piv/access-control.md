---
uid: UsersManualPivAccessControl
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

# PIV access control

There are some PIV commands or operations that require management key authentication or
PIN verification to execute. Furthermore, the auth/verification will be valid only during
a session. Which commands and operations require which access control element? How does
one write code to perform the auth/verification?

## PIV Session

First of all, if you use the [PivSession](xref:Yubico.YubiKey.Piv.PivSession)
class, all you really need to do is provide a
[`KeyCollector` delegate](xref:UsersManualDelegatesInSdk). The `PivSession`
object will determine when a management key is necessary, and when a PIN (or PUK) is
necessary. It will then call on the `KeyCollector` loaded to obtain the appropriate
element, and perform the auth/verification.

For example, suppose you have some code to generate a key pair.

```C#
    using (var pivSession = new PivSession(yubiKeyToUse))
    {
        pivSession.KeyCollector = SomeKeyCollector;
        PivPublicKey publicKey = pivSession.GenerateKeyPair(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            PivPinPolicy.Once,
            PivTouchPolicy.Once);
    }
```

When this is run, the `GenerateKeyPair` code will know that it needs management key
authentication only (no PIN or PUK). It will determine if the management key has been
authenticated or not in the current session. If it has already been authenticated, the
`GenerateKeyPair` method will not call for the management key to be authenticated again.
If not, it will call the `KeyCollector`, requesting the management key. Once it has the
management key, it will make the appropriate calls to authenticate.

Later on, suppose you have this code.

```C#
    using (var pivSession = new PivSession(yubiKeyToUse))
    {
        pivSession.KeyCollector = SomeKeyCollector;
        byte[] signature = pivSession.Sign(PivSlot.Authentication, digestData);
    }
```

Is the PIN required? The management key? The `PivSession` object will know that the PIN is
required and determine if the PIN has been verified in the session or not. If not, it will
call the `KeyCollector`, requesting the PIN, then make the appropriate calls to verify.

It is possible the private key in question was generated with the PIN policy set to
`Never`. In that case, the `Sign` method will determine that no PIN is required and simply
perform the signing operation. Note that it is possible on older YubiKeys for a PIN policy
to be `Never`, and the method still requests the PIN. This happens when the touch policy
is `Always` or `Cached` and the user does not touch.

With a `KeyCollector`, there is no need to worry about management key authentication, or
PIN/PUK verification. However, it is still possible to directly call methods to perform
these operations.

See [PivSession.AuthenticateManagementKey](xref:Yubico.YubiKey.Piv.PivSession.TryAuthenticateManagementKey%2a),
[PivSession.TryVerifyPin](xref:Yubico.YubiKey.Piv.PivSession.TryVerifyPin%2a), and
[PivSession.TryResetPin](xref:Yubico.YubiKey.Piv.PivSession.TryResetPin%2a).

If you want to auth/verify without a `KeyCollector`, you must call the commands directly.
How to do so is described in the next section.

## Authenticating with commands

If you are not familiar with the APDU, visit [this page](xref:UsersManualApdu) first.

Some PIV commands require a PIN (Personal Identification Number), PUK (PIN Unblocking
Key), management key, or maybe even two of those elements in combination. That is, access
to some commands is controlled by authenitication by PIN, PUK, or management key.

For example:

* To generate a key pair, the caller must authenticate the management key.
* To sign using a private key, the caller must authenticate the PIN.
* To set the PIN retry count, the caller must authenticate both the management key and the
  PIN.

How does one provide this authentication? There are two ways to verify the PIN:

* Supply the PIN or PUK as part of the command
* Verify the PIN using the [Verify PIN](commands.md#verify) command, and all commands
  executed in the current session that need the PIN verified will work.

Note that how you verify the PIN is not your choice. Some commands include the PIN and/or
PUK in the command data, and others do not. If the command data includes the PIN or PUK,
you must supply the PIN or PUK with the command. You cannot rely on a previous
verification command to authenticate. Similarly, some commands are defined as not
including the PIN in the data, so you must verify first.

For example, the [Change Reference Data](commands.md#change-reference-data) command
can change the PIN. To do so, the command must contain the current PIN and the new PIN.
Even if the [Verify PIN](commands.md#verify) command had been successfully executed
earlier in the session, this command requires the current PIN.

Similarly, the [Key Agreement](commands.md#authenticate-key-agreement) command
requires the PIN in order to perform the operation (as long as the private key was
generated or installed with the PIN policy set to something other than "Never"). In order
for it to work, the PIN must have been successfully verified earlier in the session using
the [Verify PIN](commands.md#verify) command. Even if the Change Reference Data
command had been successfully executed earlier in the session, the Verify PIN command
still must be successfully completed in order to perform the Key Agreement operation.

There is only one way to authenticate the management key:

* Authenticate using the [Authenticate: management key](commands.md#authenticate-management-key)
  commands (there are two, both of which need to be successful for the management key to be
  authenticated), and all commands executed in the current session that need the management
  key authenticated will work.

What is a session? That is, how do you know your code is operating in the same session for
which the PIN or management key was authenticated? If it is operating in the same session,
the code does not need to call the Verify or Authenticate commands again. But you also
want to know when a session ends, so you can know when it is necessary to verify or
authenticate.

If you create a `PivSession`, then when that object goes out of scope, the session will be
closed.

If you don't use a `PivSession`, then a session is closed whenever a YubiKey is
disconnected, or another YubiKey application is launched (e.g., your application creates a
new session for OTP).

The safest thing to do is use the `PivSession` with the `using` keyword.

```C#
    using (var pivSession = new PivSession(yubiKeyToUse))
    {
        pivSession.KeyCollector = SomeKeyCollector;
          . . .
    }
```

In this way, there is less of a chance you leave a session accidentally open.

## Part of the command

The [Reset retry](commands.md#reset-retry-recover-the-pin) command is an example of
this method of authenticating. This command resets the PIN using the PUK. You supply the
PUK and the new PIN. The command's data includes both the PIN and PUK.

CLA | INS | P1 | P2 | Lc | Data | Le
:---: | :---: | :---: | :---: | :---: | :---:
00 | 2C | 00 | 80 | 10 | *current PUK and new PIN* | (absent)

```txt
 00 2C 00 80 10 31 32 33 34 35 36 37 38 31 32 33 34 35 36 37 ff
               |<-------  PUK  ------->|<-------  PIN  ------->|
```

The YubiKey will authenticate the PUK as part of the command

## Verify the PIN using a separate command

The [Get Data](commands.md#get-data) command retrieves various elements out of a
YubiKey. For some elements, the PIN is required. In those cases, the Get Data command will
not work unless the PIN had been verified earlier in the session. Use the
[Verify PIN](commands.md#verify) command.

Once you verify the PIN once in a session, the YubiKey will be able to perform all PIV
commands in that session that require PIN authorization.

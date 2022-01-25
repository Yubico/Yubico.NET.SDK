---
uid: UsersManualPinPukMgmtKey
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

# The PIV PIN, PUK, and management key

Per the standard, there are three keys/secret values in PIV (Personal Identity
Verification):

* PIN (Personal Identification Number)
* PUK (PIN Unblocking Key)
* Management key

The main purpose of the PIN is to authenticate the user for signing and decrypting,
although there are other operations that need it as well. The standard specifies that it
is a 6- to 8-byte value, each of the bytes an ASCII number ('0' to '9', which in ASCII is
`0x30` to `0x39`). The YubiKey allows the PIN to be any ASCII character: numbers, letters
(upper- and lower-case), and even non-alphanumeric characters such as !, %, or # (among
others).

The PUK is used to unblock the PIN (see the section below on Blocking). The standard
specifies that it is to be an 8-byte value, each of the bytes any binary value (`0x00` -
`0xFF`). If your application uses the keyboard to insert the PUK, you might limit the user
to ASCII characters, but the YubiKey will accept any byte value in the PUK. In addition,
the YubiKey will allow the PUK to be 6, 7, or 8 bytes long.

The management key is used to authenticate the entity allowed to perform many YubiKey
management operations, such as generating a key pair. It is a Triple-DES key, which means
it is 24 bytes long. It is also binary (each byte is a value from 0x00 to 0xFF). Although
the key data is 192 bits long, because of the "parity bits" in a Triple-DES key, only 168
bits supply the key's strength. In addition, because of certain attacks on Triple-DES, the
actual effective bit strength of a key is 112.

The YubiKey is manufactured with the standard default PIN, PUK, and managment key values:

* PIN: "123456"
* PUK: "12345678"
* Management Key: 0x010203040506070801020304050607080102030405060708\
0102030405060708 three times

Note that the PIV standard specifies these default/initial values.

Upon receipt of the YubiKey, it is a good idea to change them from the default values. See
[Change Reference Data](commands.md#change-reference-data) and
[Set Management Key](commands.md#set-management-key)

### Entering binary data

If your application takes PIN, PUK, and management key input from the user typing on a
keyboard, how do they enter binary data? If the PIN is "90TFmv" that's easy to enter at a
keyboard. But if the PUK is 0x8F2B00CA716A, how does one type that at a keyboard?

The byte 0x2B is '+', 0x71 is 'q' and 0x6A is 'j', but what about 0x8F, 0x00, and 0xCA?

One solution is to simply limit your application's PUK to be only ASCII characters. Even
though the standard allows any binary byte, you will allow only keyboard characters. This
will likely be acceptable because there is a retry count, which limits the attacker's
ability to launch a brute-force attack.

But what about the management key? You could specify only keyboard characters there, 24
bytes and 24 keystrokes. But this is very problematic because this severely limits the
number of possible Triple-DES keys, and there is no retry limit on the management key.

The answer is to simply require the user enter the data as hex. For example, if the
management key is the default (0x010203...), then the user enters "0102030405060708...".

They enter 48 ASCII characters ('0' '1' '0' '2' ... which is 0x30 31 30 32 ...) but your
application reads it as 24 bytes.

But whatever you do, please document it. Don't simply put a box into the UI and say,
"Enter PUK" or "Enter management key". Let the user know they must enter the data as hex
values. Or if the PUK is limited to keyboard characters, let them know. Even for the PIN,
you can document that the input can be letters, numbers, or even some set of special
characters.

Otherwise the user might enter something that is incorrect, get an error message ("invalid
PUK" or "management key not 24 bytes") and have to figure it out through trial and error.

## Blocking

A PIN or PUK can be blocked. This happens when the wrong value is entered too many times
in a row. How many is too many? Upon manufacture, the number (the "retry count") is 3 for
both the PIN and PUK. It is possible to change the retry count using
[Set PIN retries](commands.md#set-pin-retries). The retry count can be any number from
1 to 255 (inclusive).

When a PIN is blocked, any operation that requires the PIN will not work, even if you
supply the correct PIN. You can unblock the PIN using the PUK (PIN Unblocking Key) in the
[PivSession.TryResetPin](xref:Yubico.YubiKey.Piv.PivSession.TryResetPin) method.

If you try to reset the PIN using the PUK, and provide the wrong PUK its retry count times
in a row, the PUK will be blocked. At that point, you cannot unblock the PIN, even if you
supply the correct PUK. There is no way to unblock the PUK, not even with the management
key.

When both the PIN and PUK are blocked, there is not much useful work you can do with the
YubiKey's PIV application. At this point, all you can do is reset the PIV application:
[PivSession.Reset](xref:Yubico.YubiKey.Piv.PivSession.ResetApplication). This
deletes the keys in all PIV slots and resets the PIN, PUK, and management key to their
defaults. Note that this has no effect on the other YubiKey applications (OTP, FIDO,
etc.).

The management key cannot be blocked. If an attacker wants to try to break your management
key, they can try the Triple-DES modified brute-force attacks (try every possible key
until "stumbling" onto the correct one). That will take thousands of years.

## Changing the retry counts

The YubiKey is manufactured with a retry count of three for both the PIN and PUK. If you
would like to change that, call the
[Change retry counts](xref:Yubico.YubiKey.Piv.PivSession.ChangePinAndPukRetryCounts%2a)
method. This call will change the retry counts for both the PIN and PUK.

> [!NOTE]
> You must change the retry counts of both the PIN and PUK. There is no way to change the
> retry count for only one secret.

> [!WARNING]
> Changing the retry counts will also reset the PIN and PUK to their default values.
> Even if you do not reset the application or change the PIN and PUK, after changing
> the retry counts, the PIN will be "123456" and the PUK will be "12345678".

The minimum retry count is one, and the maximum retry count is 255. A retry count of one
means there are no retries. If the user enters the wrong PIN or PUK just once, the secret
is blocked.

Because changing the retry count will reset the PIN and PUK to their default values, it is
a good idea to combine this operation with changing the PIN and PUK. That is, instead of
three options or menu items of "change retry count", "change PIN", and "change PUK", your
application could have one option or menu item that changes the PIN, PUK, and retry count
all at once. Alternatively, your application could set the retry count once during user
initiation, when the PIN, PUK, and management key are first changed from the default. Then
never offer the option of changing the retry count again.

## Operations that require the management key

* [Put data](commands.md#put-data)
* [Generate a new key pair](xref:Yubico.YubiKey.Piv.PivSession.GenerateKeyPair%2a)
* [Import a private key](xref:Yubico.YubiKey.Piv.PivSession.ImportPrivateKey%2a)
* [Import a certificate](xref:Yubico.YubiKey.Piv.PivSession.ImportCertificate%2a)
* [Change the retry counts](xref:Yubico.YubiKey.Piv.PivSession.ChangePinAndPukRetryCounts%2a)\
also requires the PIN
* [Change the management key](xref:Yubico.YubiKey.Piv.PivSession.ChangeManagementKey%2a)

## Operations that require the PIN

* [Verify the PIN](xref:Yubico.YubiKey.Piv.PivSession.TryVerifyPin%2a)
* [Change the PIN](xref:Yubico.YubiKey.Piv.PivSession.TryChangePin%2a)
* [Change the retry counts](xref:Yubico.YubiKey.Piv.PivSession.ChangePinAndPukRetryCounts%2a)\
also requires the management key
* [Sign](xref:Yubico.YubiKey.Piv.PivSession.Sign%2a)
* [Decrypt](xref:Yubico.YubiKey.Piv.PivSession.Decrypt%2a)
* [Key Agreement](xref:Yubico.YubiKey.Piv.PivSession.KeyAgree%2a)
* Get data for some data objects: [Get data](commands.md#get-data)

Note that it is possible, on YubiKey 4.0 and later, to change the PIN policy of the sign
and decrypt operations, so that the PIN is not required. See the user's manual entry on
[Pin and touch policies](pin-touch-policies.md).

## Operations that require the PUK

* [Change the PUK](xref:Yubico.YubiKey.Piv.PivSession.TryChangePuk%2a)
* [Reset the PIN](xref:Yubico.YubiKey.Piv.PivSession.TryResetPin%2a)

## Examples

Suppose the "retry count" for the PIN is 4. You try to sign but enter the wrong PIN. The
operation fails and the "remaining count" is decremented to 3. The retry count is still 4,
but you have only 3 tries remaining. Try again with the correct PIN, the operation
succeeds, and the remaining count is restored to 4.

The PIN retry count (and remaining count) is 4, but you know you have forgotten the PIN,
so you don't even try to verify it. You simply use the PUK to reset/unblock the PIN. This
works, because it is not necessary for the remaining count to go to zero before you use
the PUK to reset the PIN.

You changed the PIN and PUK when you first got the YubiKey. Now, you decide you want to
change the PIN again. This is certainly allowed, simply call the
[Change Reference Data command](commands.md#change-reference-data).

## PIN only

It is possible to set a YubiKey to be PIN only. This means the YubiKey is configured to
require he PIN only when performing operations, even those functions that normally require
management key authentication.

> [!Warning]
> A YubiKey in PIN only mode is less secure than one that requires the management
> key. However, there are applications for which entering a management key is simply not
> possible, so this feature is offered.

There are two ways to achieve this: PIN-protected and PIN-derived. It is possible to have
both methods active for a YubiKey.

> [!Warning]
> PIN-derived mode is not secure. You should not use this technique. It is offered only
> for backwards compatibility.

Yubico recommends that if you must set a YubiKey to PIN only, you set it to use only
PIN-protected.

### Minidriver compatibility

The Yubico minidriver will configure a YubiKey to PIN-protected mode. Hence, if you know
that your application will be running alongside Microsoft Windows machines using the
YubiKey Minidriver, you should strongly consider adding support for setting YubiKeys to
PIN-protected mode.

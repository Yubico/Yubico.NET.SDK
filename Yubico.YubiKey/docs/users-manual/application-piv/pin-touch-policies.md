---
uid: UsersManualPivPinTouchPolicy
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

# PIV PIN and touch policies

Suppose you want to use one of the PIV keys to sign or decrypt. The application running on
your host device will call one or more commands to perform the operation. Do you need to
enter the PIN to perform the operation? Do you need to touch the YubiKey?

Suppose you want to to generate a new key pair, you need to authenticate the management
key to perform that operation. But do you need to touch the YubiKey as well?

The PIN and touch policies answer those questions.

## Related articles

[PIV commands access control](access-control.md)

[The PIV PIN, PUK, and management key](pin-puk-mgmt-key.md)

## What are the possible policies?

#### PIN policies

* Never: the PIN is never needed
* Always: the PIN needed for every use
* Once: the PIN is needed once per session

#### Touch policies

* Never: a touch is never needed
* Always: a touch is needed for every use

For YubiKeys 4.3 and later, there is one more possible policy

* Cached: a touch is not needed if the YubiKey had been touched in the last 15 seconds,
  otherwise a touch is needed

> [!WARNING]
> It is important to point out that setting the PIN policy to "never" reduces security
> dramatically. This feature was added only because of customer demand for convenience.
> Yubico recommends setting the PIN policy to "always" or "once".

Note that if you do not specify a PIN or touch policy, there is a default. What the
default is will be described below.

Note also that with management keys there is only a touch policy. The PIN is never needed
to perform a management key operation (with the exception of
[Set PIN Retries](commands.md#set-pin-retries), but in this case the PIN is needed
becasue that is a command related to the PIN itself).

## Older YubiKeys (prior to YubiKey 4)

The ability to use PIN and touch policies other than the default was not available prior
to YubiKey 4. What this means is that when using a PIV key in a YubiKey, there was a
default policy only and no way to generate or import a key to use a different policy.

## Default policy

The default policies are programmed into the YubiKey upon manufacture. All YubiKeys,
before version 4 and after, are programmed with the same default policies. In the future,
there could be a YubiKey with a different default policy. But for now, the default PIN and
touch policies are the following.

* Slot 9C PIN policy: Always (the PIN is required before each private key operation)
* PIN policy: Once (the PIN is required once per session to use a private key to sign,
  decrypt, or perform key agreement)
* Touch policy: Never (touch is never required to use any PIV key, private or management)

> Note:
>
> The default PIN policy for slot 9C is different from the default for the other slots.
> This is from the PIV standard. So remember that if you generate a key in slot 9C and set
> the PIN policy to default, the actual PIN policy will be Always. It is a good idea to
> simply always specify the PIN policy you want, Never or Once, rather than Default.

> Note:
>
> Touch is not a part of the PIV standard. That is why the first YubiKeys that supported
> PIV did not have the option of touch when using a PIV key. This non-standard ability to
> require touch was added to YubiKey in version 4 to augment security.

## Changing the policy: management key (slot 9B)

If you want a touch policy different from the default for the management key, use the
[Set Management Key command](commands.md#set-management-key). This will set the actual
key value as well as the touch policy. With this command you can enter the current key
along with a different touch policy to change the policy only, or enter the same touch
policy with a new key to change the key only, or change both key and policy.

## Setting keys to a non-default policy (all slots other than 80, 81, 9B, F9)

If you want a policy different from the default for a private key, you must specify that
policy when the key is [generated](commands.md#generate-asymmetric) or
[imported](commands.md#import-asymmetric). Once the key is on the YubiKey there is no
way to change the policy.

Note that you can specify different policies for keys in different slots (if the YubiKey
has the option of setting policies). For example, you can generate a new key in slot 9A
that has a PIN policy of "always", while a key imported into slot 86 has a PIN policy of
"once".

> It is important to point out that setting the PIN policy to "never" reduces security
> dramatically. This feature was added only because of customer demand for convenience.
> Yubico recommends setting the PIN policy to "always" or "once".

## Examples

### Management key

You have a new YubiKey and one of the first things you do is change the management key
from the default. You call the
[Set Management Key command](commands.md#set-management-key) and provide the new key
data and specify the touch policy. Suppose you set the policy to "always".

Now whenever you call the
[Authenticate management key](commands.md#authenticate-management-key) command, the
authentication won't be complete until the YubiKey has been touched.

### Private key

Suppose you generate a new key pair for slot 9C using the
[Generate asymmetric key pair](commands.md#generate-asymmetric) command. You set
the PIN policy to never and the touch policy to always. Now when you call the
[Authenticate: sign](commands.md#authenticate-sign) command, you won't need to
combine it with [PIN verification](commands.md#verify) to make it work, but the
YubiKey won't complete the signing process until the YubiKey has been touched.

Suppose you generate a new key pair for slot 9D. You set the PIN policy to once, and the
touch policy to never. Now when you first decrypt using that key in a session, you will
need to authenticate the PIN, but won't need to touch. The next time you decrypt in the
session, you will not need the PIN nor touch.

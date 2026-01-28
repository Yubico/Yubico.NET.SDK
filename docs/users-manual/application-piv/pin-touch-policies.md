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

# PIV PIN, touch, and biometric policies

The YubiKey's PIN, touch, and biometric policies determine when PIN verification, biometric verification, and touch are required in order to perform an operation with a private key from one of the YubiKey's PIV slots (including the management key).

These policies apply to operations such as signing, decrypting, performing a key agreement, and generating a new key pair. For a full list of operations affected by PIN and touch policies, see [Operations that require the PIN](xref:UsersManualPinPukMgmtKey#operations-that-require-the-pin) and [Operations that require the management key](xref:UsersManualPinPukMgmtKey#operations-that-require-the-management-key).

## Policy properties

PIN and touch policies can be configured when a key is generated or imported into a YubiKey PIV slot. Once the policies have been set during key generation/import, they cannot be changed. If a PIN and/or touch policy is not specified at that time, the slot's default policies will be applied to the key. Default policies vary depending on the slot and are programmed into the YubiKey's firmware. Note that policy configuration is supported on YubiKeys with firmware version 4 and later; earlier YubiKeys are limited to the usage of slots' default policies.

A key's policies, whether explicitly configured or not, are properties of the key itself. This means that you can move a key from one slot to another (for example, from slot 9A to one of the retired key slots), and its policies do not change.

Biometric policies are a type of PIN policy and can only be used with YubiKey Bio Series — Multi-protocol Edition keys (YubiKey Bio Series – FIDO Edition key do not support PIV).

The management key (slot 9B) only has a touch policy. The PIN is never needed to perform a management key operation (with the exception of [Set PIN Retries](commands.md#set-pin-retries), but in this case the PIN is needed because this is a command related to the PIN itself). The PIN and PUK (slots 80 and 81) do not have PIN *or* touch policies.

Keys generated/imported into the following slots have both PIN **and** touch policies:

- 9A (Authentication)
- 9C (Digital Signature)
- 9D (Key Management)
- 9E (Card Authentication)
- F9 (Attestation)
- 82-95 (Retired Key Slots)

## PIN policy options

The three main PIN policy options are as follows:

* ``Never``: the PIN is never needed.
* ``Always``: the PIN is needed for every key operation.
* ``Once``: the PIN is needed once per session.

Due to PIV card activation requirements in section 4.3 of the [FIPS 201-3 specification](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.201-3.pdf), YubiKey FIPS Series keys with firmware version 5.7.1 or later cannot set PIN policy to ``Never``.

> [!WARNING]
> It is important to note that setting the PIN policy to ``Never`` reduces security
> dramatically. This feature was added only because of customer demand for convenience.
> Yubico recommends setting the PIN policy to ``Always`` or ``Once``.

### Biometric policy options

Biometric policies are a type of PIN policy that are available for YubiKey Bio Series — Multi-protocol Edition keys with firmware 5.7 or later. There are two biometric policy options:

* Match Once: a biometric or PIN verification is required for each session.
* Match Always: a biometric or PIN verification is required on every object access.

## Touch policy options

There are three touch policy options:

* Never: a touch is never needed.
* Always: a touch is needed for every key operation.
* Cached: if more than 15 seconds have elapsed since the last time the YubiKey was touched, a touch is needed (only available for YubiKeys with firmware version 4.3 and later).

## Default policies

The default PIN and touch policies are programmed into the YubiKey's firmware upon manufacture. Starting with firmware version 4, YubiKeys (with the exception of YubiKey FIPS Series keys with firmware version 5.7.1 or later) have the following default policies:

* Slot 9C PIN policy: Always
* Slot 9E PIN policy: Never
* General PIN policy: Once
* Touch policy: Never

Due to PIV card activation requirements in section 4.3 of the [FIPS 201-3 specification](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.201-3.pdf), **YubiKey FIPS Series keys with firmware version 5.7.1 or later have a default 9E PIN policy of ``Once``**. The other default polices listed above are the same for FIPS keys.

It's also important to note that slots 9C and 9E have different default PIN policies than all other slots due to the requirements mandated by the PIV standard (see [NIST SP 800-73pt1, section 3](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-73pt1-5.pdf)). Touch is not a part of the PIV standard, which is why the default touch policy is ``Never``. The ability to require touch was added to the YubiKey in firmware version 4 to augment security.

When generating or importing a key into one of the PIV slots, these default policies will be applied to the key unless otherwise specified.

## Setting keys to a non-default policy (all slots other than 80, 81, 9B, and F9)

If you want a policy different from the default for a private key, you must specify that
policy when the key is [generated](commands.md#generate-asymmetric) or
[imported](commands.md#import-asymmetric). Once the key is on the YubiKey, there is no
way to change the policy.

Note that you can specify different policies for keys in different slots (if the YubiKey
has the option of setting policies). For example, you can generate a new key in slot 9A
that has a PIN policy of "always", while a key imported into slot 86 has a PIN policy of
"once".

> It is important to point out that setting the PIN policy to "never" reduces security
> dramatically. This feature was added only because of customer demand for convenience.
> Yubico recommends setting the PIN policy to "always" or "once".

### Example scenarios

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

## Changing the touch policy for the management key (slot 9B)

Unlike other slots, the management key (slot 9B) only has a touch policy, which by default is ``Never``. However, when changing the management key, you can set this policy to one of the other touch policy options (``Always`` or ``Cached``). Once reconfigured with one of these other policies, the user will be required to touch the YubiKey at the specified frequency to perform management key operations.

To change the management key with the SDK, we have two options: the ``PivSession`` method, ``TryChangeManagementKey()``, or the lower-level ``SetManagementKeyCommand()``.

### TryChangeManagementKey() example

To change the management key and set a new touch policy using the ``PivSession``, simply call``TryChangeManagementKey()`` and provide the current management key, the new management key, and the desired touch policy. In this example, let's set the touch policy to ``Always``:

```csharp
using (PivSession pivtest = new PivSession(yubiKey))
{
    // currentKey and newKey set elsewhere.
    pivtest.TryChangeManagementKey(currentKey, newKey, PivTouchPolicy.Always);
}
```

Note that ``TryChangeManagementKey()`` is an overloaded method. In addition to specifying the new key's touch policy, you can also specify a particular algorithm. If these properties are not specified, the slot's default touch policy and default algorithm will be used for the new management key. Also, if you call the method without providing the current and new management keys directly, the SDK will call upon your KeyCollector to fetch them from the user.

### SetManagementKeyCommand() example

Unlike the ``PivSession``, using the PIV command classes to change the management key requires three steps: first we must initiate the PIV management key authentication process with ``InitializeAuthenticateManagementKeyCommand``, then we can finish management key authentication with ``CompleteAuthenticateManagementKeyCommand()``, and finally we can set the new management key and touch policy via ``SetManagementKeyCommand()``:

```csharp
IYubiKeyConnection connection = yubiKey.Connect(YubiKeyApplication.Piv);

// When initializing management key authentication with the command classes, we must specify the current management key's algorithm. (If you aren't sure about the algorithm, retrieve it from the key's metadata first.)
InitializeAuthenticateManagementKeyCommand initAuthManKeyCommand = new InitializeAuthenticateManagementKeyCommand(PivAlgorithm.Aes192);
InitializeAuthenticateManagementKeyResponse initAuthManKeyResponse = connection.SendCommand(initAuthManKeyCommand);

// Complete the management key authentication by passing in the initialization response and the current management key (currentKey set elsewhere).
CompleteAuthenticateManagementKeyCommand authManKeyCommand = new CompleteAuthenticateManagementKeyCommand(initAuthManKeyResponse, currentKey);
CompleteAuthenticateManagementKeyResponse authManKeyResponse = connection.SendCommand(authManKeyCommand);

// When setting the new management key and its touch policy, we must also specify the new key's algorithm.
SetManagementKeyCommand setManKeyCommand = new SetManagementKeyCommand(newKey, PivTouchPolicy.Always, PivAlgorithm.Aes192);
SetManagementKeyResponse setManKeyResponse = connection.SendCommand(setManKeyCommand);
```

In this example, we set the management key's touch policy to ``Always``. This means that future calls to ``CompleteAuthenticateManagementKeyCommand`` will not complete until the YubiKey has been touched.

## Retrieving an existing key's PIN and touch policies

Once a key has been generated or imported into a slot, you can check the PIN and touch policies it was configured with via the key's PIV metadata. Note that this feature is only available for YubiKeys with firmware version 5.3 and later. On older YubiKeys, there is no way to retrieve a key's policies after configuration.

To check a key's policies, create an instance of a ``PivSession`` with the desired YubiKey, call ``GetMetadata`` on a specific PIV slot, and extract the ``PinPolicy`` and ``TouchPolicy`` properties:

```csharp
using (PivSession pivtest = new PivSession(yubiKey))
{
    // Get the metadata from the key in slot 9A.
    PivMetadata metadata = pivtest.GetMetadata(0x9A);

    // Extract the properties from the metadata.
    PivPinPolicy pinPolicy = metadata.PinPolicy;
    PivTouchPolicy touchPolicy = metadata.TouchPolicy;
}
```

Note that if the slot does not contain a key, the SDK will throw an exception when trying to call ``GetMetadata``. Slots 80 and 81 (PIN and PUK) do not have PIN or touch policies and will always return ``None`` for those properties in the metadata. The management key (9B), on the other hand, has a touch policy but no PIN policy. In order to maintain consistency with the data format, the YubiKey will return the undefined value "0" for 9B's PIN policy. This is not a valid PIN policy, and the SDK translates it to ``Default`` in the metadata.

## Related articles

[PIV access control](access-control.md)

[The PIV PIN, PUK, and management key](pin-puk-mgmt-key.md)
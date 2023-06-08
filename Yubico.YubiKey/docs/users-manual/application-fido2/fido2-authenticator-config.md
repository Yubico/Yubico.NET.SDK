---
uid: Fido2AuthenticatorConfig
---

<!-- Copyright 2023 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 Authenticator Configuration

The CTAP 2.1 standard, in section 6.11, defines "authenticatorConfig" operations:

* Enable enterprise attestation
* Toggle "always UV"
* Set minimum PIN length
* Vendor prototype 

The SDK supports the first three, but no YubiKey currently supports "vendorPrototype", so
for now the SDK does not support it either.

## YubiKey support

Not all YubiKeys support authenticatorConfig operations. There are two ways to know if a
desired operation is supported:

* Call the appropriate Fido2Session `Try` method, if it returns `true`, it is supported
and the operation was successfully performed
* Check the `Options` in the
[AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.Fido2Session.AuthenticatorInfo).

For example, suppose you want to set the minimum PIN length. Just try to set it.

```csharp
      if (!fido2Session.TrySetPinConfig(8, null, null))
      {
          // Set min PIN length not supported.
      }
```

Or you can check the "setMinPINLength" option.

```csharp
    // Get the "setMinPINLength" option to know if it is possible to set the minimum PIN length.
    OptionValue setMinPinLenValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.setMinPINLength);

    // If the option is True, then it is supported, it is possible to set the min PIN length.
    if (setMinPinLenValue == OptionValue.True)
    {
        return fido2Session.TrySetPinConfig(8, null, null);
    }
    // Any other OptionValue and the operation is not supported.
```

For enterprise attestation, call the `Try` method or check the `ep` option.

```csharp
      if (!fido2Session.TryEnableEnterpriseAttestation())
      {
          // Enable enterprise attestation not supported.
      }
```

```csharp
    // Get the "ep" option to know if enterprise attestation is supported.
    OptionValue epValue = fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.ep);

    // If the OptionValue is True, then the operation is supported and enterprise
    // attestation is enabled.
    if (epValue == OptionValue.True)
    {
        // No need to call enable.
        return true;
    }
    // If the OptionValue is False, then the operation is supported but enterprise
    // attestation is not enabled.
    if (epValue == OptionValue.False)
    {
        return fido2Session.TryEnableEnterpriseAttestation();
    }
    // If the OptionValue is anything else (NotSupported or Unknown), then the
    // operation is not supported.
    return false;
```

For toggling always UV, call the `Try` method or check the `alwaysUv` option. Note that
this operation will set "alwaysUv" to `true` if it is `false` and vice versa. So if you
want to make sure the value is `true` or `false`, then you will likely want to determine
its state before toggling.

```csharp
      if (!fido2Session.TryToggleAlwaysUv())
      {
          // Toggling always UV not supported.
      }
```

```csharp
    // Get the "alwaysUv" option to know if toggle always UV is supported.
    OptionValue alwaysUvValue = AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.alwaysUv);

    // If this option is True, then it is supported and the YubiKey is currently set
    // to always require UV. If that's what you want, don't toggle.
    if (alwaysUvValue == OptionValue.True)
    {
        return true;
    }
    // If this option is False, then it is supported and the YubiKey is not currently set
    // to always require UV. If you want it set to be always require UV, then toggle.
    if (alwaysIvValue == OptionValue.False)
    {
        return fido2Session.TryToggleAlwaysUv();
    }
    // Anything else and the operation is not supported.
```

There is an option called "authnrCfg". If that option is present and True, then
authenticatorConfig is supported.

```csharp
    // Get the "authnrCfg" option to know if authenticatorConfig is supported.
    OptionValue authnrCfgValue = fido2Session.AuthenticatorInfo.GetOptionValue(AuthenticatorOptions.authnrCfg);
```

However, even if that is True, an individual operation might not be supported. So you might
as well ignore "authnrCfg" and just check the specific option.

### vendorPrototype

Currently, no YubiKey supports the `vendorPrototype` command. However, if future versions
of the YubiKey firmware supports this feature, you will be able to check if a particular
YubiKey has this feature by looking at the `VendorPrototypeConfigCommands` property of the                     
[AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.Fido2Session.AuthenticatorInfo) class. If it
is null, then the operation is not supported.

## Commands and Fido2Session methods

The SDK offers two ways to perform the "authenticator config" operations: call the command
classes directly or call methods inside the
[Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session) class. This article will discuss
the Fido2Session class and its methods.

* [TryEnableEnterpriseAttestation](xref:Yubico.YubiKey.Fido2.Fido2Session.TryEnableEnterpriseAttestation)
* [TryToggleAlwaysUv](xref:Yubico.YubiKey.Fido2.Fido2Session.TryToggleAlwaysUv)
* [TrySetPinConfig](xref:Yubico.YubiKey.Fido2.Fido2Session.TrySetPinConfig%2a)

Each of these methods will return `false` if the connected YubiKey does not support that
operation. You could simply call the method, and if it returns `true`, the operation is
supported and it was just executed. It it returns `false`, then it is not supported.

## AuthToken

In order to perform these operations, it is necessary to have a
[PinUvAuthToken](fido2-auth-tokens.md) with the
[AuthenticatorConfiguration](xref:Yubico.YubiKey.Fido2.Commands.PinUvAuthTokenPermissions.AuthenticatorConfiguration)
permission.

The Fido2Session class will obtain the AuthToken automatically if you supply a
KeyCollector. If you don't want to build a
[KeyCollector](../sdk-programming-guide/key-collector.md), make sure you perform a
`Verify` operation with the appropriate permission before calling any of the
authenticatorConfig methods. For example:

```csharp
    bool isVerified = fido2Session.TryVerifyPin(PinUvAuthTokenPemissions.AuthenticatorConfiguration);
```

## Enable enterprise attestation

Before discussing the enable enterprise attestation option, we should look at these two
topics:

* Attestation
* Enterprise attestation

### What is attestation?

Attestation is simply a process of providing some sort of data that has two features:

* Information identifying the source
* A way a recipient can verify the data, and hence, trust the information

The data in this process is called an attestation statement.

The most common attestation statement is a certificate. A certificate contains information
about the source (in the name, extensions, etc.), is signed, and chains to a root allowing
recipients to verify the contents.

With FIDO2, an attestation statement is built when making a credential. A credential is a
public key. The attestation statement contains that public key, information about the
relying party (RP) for whom the credential is made, a signature, and a certificate that
can be used to verify the public key. The private key partner to the public key in the
attestation statement is the key used to sign the attestation statement, so this is
similar to a self-signed cert.

The RP that receives the attestation statement stores that public key and uses it to
verify assertions. But before the RP can trust that public key, it will verify the
attestation statement. It does so by first verifying the contents (e.g. is the RP
correct?), then verifying the signature. If the public key in the statement verifies the
statement, then the contents are correct. Now the RP can use the cert to verify the public
key. Of course, the cert itself must be verified, and the RP does that by making sure the
contents contain the correct information (e.g. the cert should contain the YubiKey's
serial number, and the name should contain something such as "Authenticator Attestation"),
and that it chains to the Yubico root.

### What is enterprise attestation?

With enterprise attestation, the information in the attestation statement and the cert is
specific to an enterprise. For example, it might contain a cert that chains to the
enterprise's root, and the cert's name can contain the enterprise's name.

When making a credential, it is possible to use this enterprise attestation instead of the
"default". However, there are a number of conditions that must be met before this is done.

One, the YubiKey must support enterprise attestation (see the section above on
[YubiKey support](#yubikey-support)).

Two, the YubiKey must have a list of RPs for which enterprise attestation is possible.
This list is, as the standard says, " 'burned into' the authenticator by the vendor." In
other words, Yubico must manufacture these YubiKeys with the list, and that list cannot
be changed (no additions or subtractions), even if the YubiKey's FIDO2 application is
reset.

Three, the RP for which the credential is being made must be on that list.

Four, the make credential parameters must include the instruction to create an
enterprise attestation statement.

And finally, five, the enterprise attestation feature must be enabled. This is what
calling `TryEnableEnterpriseAttestation` will do.

If any of those five conditions are not met, then the YubiKey will generate a default
attestation statement.

## Toggle alwaysUv

Generally when making a credential or getting an assertion, the user must enter the PIN
or supply a fingerprint. These are the user verification (UV) operations.

There are cases, however, where UV is not required. For example, an RP can set UV to
"Discouraged" in a WebAuthn request.

On the other hand, some standards, such as FIPS certification, require UV to happen every
time, no matter what. Hence, it is possible to override "UV not required" cases by setting
the `alwaysUv` option to True.

Note that the toggleAlwaysUv will set it from False to True, if it is False, but it will
also set it from True to False if it is True. So make sure you check the "alwaysUv" option
before calling `TryToggleAlwaysUv`.

## Set minimum PIN length (and other PIN configuration operations)

A YubiKey is manufactured with a default minimum PIN length. It will likely be 4 on
regular and 6 on FIPS series YubiKeys.

It is possible an organization wants to make sure that users set longer PINs. Call the
`TrySetPinConfig` method to do that.

While the `setMinPINLength` subcommand defined in the FIDO2 standard can change the
minimum PIN length, it can do two other operations as well: specify a list of relying
parties that are allowed to see the minimum PIN length, and require that the PIN be
changed before any operation that requires UV be allowed to execute.

Note that you can call the `TrySetPinConfig` method to do any combination of one, two,
or all three of the operations it can perform.

### Minimum PIN length

You can know what the current minimum PIN length is by looking at the
[AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo.MinimumPinLength).
If your oganization requires longer PINs, then call `TrySetPinConfig`.

However, note that this minimum length is measured in code points. See the user's manual
entry on [The FIDO2 PIN](fido2-pin.md) for more information on how to count the number of
code points.

### Relying parties that can see the minimum PIN length

Normally, an RP is not allowed to know what the minimum PIN length is. However, some RPs
might want to accept credentials only from YubiKeys that meet certain security thresholds,
such as minimum PIN length.

During the make credential operation, the RP will request the minimum PIN length and the
client will pass that request along to the YubiKey. At that point, the YubiKey will look
through its lists (there are two lists described below) of RPs that are allowed to see the
minimum PIN length. If the requester is on one of the lists, the YubiKey will return that
number with the credential.

So how are these lists populated? The first list is set when the YubiKey is manufactured.
This list is immutable, entries cannot be added or removed, even if the YubiKey's FIDO2
application is reset. Most YubiKeys are not manufactured with such a list; this feature is
available through a special order process.

The second list is set when you supply a list of RPs in your call to `TrySetPinConfig`.
This list is mutable, meaning that each time you set this list, all previous entries in
that list are replaced. Note that the immutable list is not affected by this second list.

This mutable list has a limit to the number of RPs you can supply. The `AuthenticatorInfo`
object has a property,
[MaximumRpidsForSetMinPinLength](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo.MaximumRpidsForSetMinPinLength),
which specifies the maximum number of RPs you can provide in the list.

It is not possible to retrieve the RP IDs on either list. That is, there is no command you
can call to have the YubiKey return the RP IDs that have permission to know the minimum
PIN length.

#### How the client requests the minPINLength

When the client calls on the YubiKey to make a credential, it has the option of supplying
extensions. One of the extensions is "minPinLength". Pass in that extension with a value
of `true`.

```csharp
    var mcParams = new MakeCredentialParameters(rp, user)
    {
        ClientDataHash = clientDataHash
    };
    mcParams.AddOption(AuthenticatorOptions.rk, true);
    mcParams.AddExtension("minPinLength", new byte[] { 0xF5 });
```

If the YubiKey finds the given RP ID on one of its lists, it will return the credential
with the minimum PIN length. It will be in the
`MakeCredentialData.AuthenticatorData.Extensions` property. If the given RP ID is not on
one of the lists, the YubiKey will return the credential, but that exension will not be in
the `Extensions` property.

```csharp
    MakeCredentialData mcData = fido2Session.MakeCredential(mcParams);

    if (!(mcData.AuthenticatorData.Extensions is null))
    {
        if (mcData.AuthenticatorData.Extensions!.TryGetValue("minPinLength", out byte[]? eValue))
        {
            minPinLength = eValue![0];
        }
    }

```

When making a credential, the RP will make a request for the minimum PIN length. The
client will likely call on the YubiKey to make the credential, including the extension in
the parameters, and if the RP is on a list, the minimum PIN length is returned and passed
on, and if it is not on a list, the client sends the credential without the minimum PIN
length to the RP. It's now up to the RP to decide whether it will accept the credential.

If the RP receives a credential without the minimum PIN length, it might reject that
credential and the client can now try again. It can contact the user and say the RP is
requesting the minimum PIN length and ask if the user is willing to send it. If so, the
client can call `TrySetPinConfig` with the RP ID, delete the previous credential (see
the [credential management article](fido2-cred-mgmt.md)), and make a new one. Note that
to run `TrySetPinConfig`, the PIN (or fingerprint) must be verified.

### Forcing the PIN to be changed

The last operation the `TrySetPinConfig` method can perform is to force the user to
change the PIN. This can be done because the minimum PIN length has been changed, or
simply because the enterprise wants the PIN to be changed every so often.

If you are increasing the minimum PIN length, it might not be necessary to force a PIN
change because the YubiKey will force the PIN be changed when the new minimum PIN length
is longer than the current PIN.

For example, suppose the current minimum PIN length is 4 (the default). Suppose also that
the current PIN's length is 6. Let's say the minimum PIN length is set to 6. The YubiKey
will not force a PIN change. But if the minimum PIN length is set to 8, the YubiKey will
require a PIN change.

But if you want to require a PIN change, no matter what, call the `TrySetPinConfig`
method with a `forceChangePin` arg of `true`.

If the PIN is forced to be changed, no operation that requires user verification (PIN
or fingerprint) will be executed until the PIN is changed. For example, if the PIN is
forced to be changed, and you call `Fido2Session.EnumerateRelyingParties`, it will not
execute. You would need an [AuthToken](fido2-auth-tokens.md), but you can't get one until
the PIN has changed. You can call one of the `VerifyPin` methods, or even one of the
`VerifyUv` methods (verify a fingerprint), but they would not work until the PIN has been
changed. Once it has been changed, of course, the YubiKey will no longer require a PIN
change in order to perform an operation.

It is possible to know whether the PIN must be changed. The `AuthenticatorInfo` contains a
property, [ForcePinChange](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo.ForcePinChange),
that will be `true` if the PIN must be changed, either because the YubiKey is forcing it,
or you called the `TrySetPinConfig` method with a `forceChangePin` arg of `true`. Once
the PIN has been changed, that property will be reset to `false`.

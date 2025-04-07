---
uid: Fido2MinPinLength
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

# The minimum PIN length

This article discusses two topics: increasing the minimum PIN length and returning the
minimum PIN length to a relying party.

## Increasing the minimum PIN length

The FIDO2 standard specifies that the PIN must be at least four code points (see the
[User's Manual entry](fido2-pin.md) on the PIN). It also specifies that the manufacturer
of the authenticator can require a longer PIN and that the authenticator can offer the
option of increasing this minimum PIN length.

You can check the [AuthenticatorInfo](xref:Yubico.YubiKey.Fido2.AuthenticatorInfo) to
find a YubiKey's current minimum PIN length. Also, you can check the
`AuthenticatorInfo.Options` property to determine if the YubiKey supports increasing the
minimum PIN length.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        OptionValue optionValue = fido2Session.AuthenticatorInfo.GetOptionValue(
            AuthenticatorOptions.setMinPINLength);
        if (optionValue == OptionValue.True)
        {
            // Code to increase min PIN length here.
        }
    }
```

If the option is not supported on a YubiKey, then there simply is no way to increase the
minimum PIN length.

In addition, it is not possible to decrease the minimum PIN length outside of resetting
the entire FIDO2 application, which deletes all credentials and sets the application,
including the minimum PIN length, back to its original state.

To set the minimum PIN length in the SDK, use
[Fido2Session.TrySetPinConfig](xref:Yubico.YubiKey.Fido2.Fido2Session.TrySetPinConfig%2a).
That method can do any combination of three things:

* Increase the minimum PIN length
* Force the user to change the PIN
* Set the list of relying parties that can see the minimum PIN length

Suppose you want to only increase the minimum PIN length to six characters. Your code
would look something like this:

```csharp
        bool isValid = fido2Session.TrySetPinConfig(6, null, null);
        if (!isValid)
        {
            // The connected YubiKey does not support changing the
            // minimum PIN length.
        }
    }
```

The only way that method returns `false` is if the connected YubiKey does not support
changing the minimum PIN length. If the YubiKey supports this feature, and there is some
error (e.g. the provided new minimum PIN length is shorter than the current minimum), then
this method will throw an exception.

After you increase the minimum PIN length, it is possible the current PIN is not long
enough. In that case, the YubiKey requires the PIN be changed before it will perform
another operation that requires an AuthToken. To verify whether a YubiKey's PIN needs to
be changed following a minimum PIN length increase, check the
`AuthenticatorInfo.ForcePinChange` property.

For example, suppose for a YubiKey the minimum PIN length is 4, and a 4-character PIN is
set. Now suppose you change the minimum PIN length to 6. At this point, the
`fido2Session.AuthenticatorInfo.ForcePinChange` property is `true`. Now suppose you make a
call to an operation that requires an AuthToken, such as `MakeCredential`,
`EnumerateRelyingParties`, or even `TrySetPinConfig`. Such a call will throw an exception.

Your application must have the user change the PIN (e.g., call
`fido2Session.TryChangePin`). You can let the user know how long the PIN must be by
reporting the `fido2Session.AuthenticatorInfo.MinimumPinLength` property.

Now suppose the minimum PIN length is 4, and a 6-character PIN is set. You change the
minimum PIN length to 6. At this point, the YubiKey will not require the PIN be changed.

### Force a PIN change

If you want to make sure the user changes the PIN, either because you have changed the
minimum PIN length or there is some other reason to require a new PIN (e.g. a company
policy that specifies PINs be updated periodically), then call `TrySetPinConfig` with a
`forceChangePin` arg of `true`.

This forces a PIN change, even if the current PIN is of a length that is at least the
minimum PIN length.

```csharp
        // Force the PIN change while setting the minimum PIN length,
        // even if the current PIN is 6 characters long.
        isValid = fido2Session.TrySetPinConfig(6, null, true);

        // The following call forces the PIN change without setting the
        // minimum PIN length. It's forcing a PIN change for some reason
        // other than minimum PIN length.
        isValid = fido2Session.TrySetPinConfig(null, null, true);
```

At this point, the `fido2Session.AuthenticatorInfo.ForcePinChange` property is `true`. Now
suppose you make a call to an operation that requires an AuthToken, such as
`GetAssertions`, `DeleteCredential`, or `TrySetPinConfig`. Such a call will throw an
exception.

## Returning the minimum PIN length to a relying party

When you make a credential, you return it to the client, which forwards it to the relying
party. At that point, the RP can accept it or reject it. One reason it might reject it is
if the YubiKey's minimum PIN length is not sufficient (e.g. a requirement of an RP's
security policy).

In order for an RP to know a YubiKey's minimum PIN length, the YubiKey has to send it.
However, the YubiKey will not send the minimum PIN to an RP unless it is on one of the
"authorized RPIDs" lists.

That is, the RP can request the minimum PIN length, but when the YubiKey gets that
request, it will check to see if that RP is on one of its authorized RPIDs lists. If it
is, the YubiKey will build and return the credential with the minimum PIN length embedded
therein. If the RP is not on one of the lists, the YubiKey will build and return the
credential without the minimum PIN length.

Fortunately, it is easy to place an RP onto one of the lists.

### Authorized RPIDs lists

There are two such lists: the pre-configured, unchanging list, and the caller-defined,
changeable list.

The standard specifies that an authenticator manufacturer is allowed to "pre-load" a list
of RPs that are allowed to see the minimum PIN length. This list will never change, even
if a YubiKey is reset. You can't add to it or remove and entry from it. This will almost
certainly be a special order. For example, suppose the Acme company is distributing
authenticators to each employee and wants to make sure that the RP "acme.employees.com"
is allowed to see the minimum PIN length. The likely reason the company wants to make sure
the RP can see the minimum PIN length is so that the RP can verify that the minimum PIN
length on each authenticator is following company policy. Otherwise, a credential can be
rejected and an employee will not be able to log in until that issue is fixed. In this
case, Acme will make a special ordert from the manufacturer to program "acme.employee.com"
into the pre-configured list.

The second list is one you can create. If you want a particular RP to be able to see the
minimum PIN length, set this second list to contain that RP.

To set the caller-defined list, call `TrySetPinConfig`.

```csharp
        // Assume there is a variable called rp that is an instance of
        // the RelyingParty class.
        var rpidList = new List<string>(1);
        rpidList.Add(rp.Id);
        isValid = fido2Session.TrySetPinConfig(null, rpidList, null);
```

Any client or application is allowed to make this call. However, this call requires user
approval. No RPID can be placed onto this caller-defined list unless the caller has an
AuthToken with the `AuthenticatorConfiguration` permission. The only way to obtain the
AuthToken is if the user enters the PIN or supplies a valid fingerprint.

#### Not possible to view the lists

There is no way to call into the YubiKey and find out what, if any, RPIDs are on either
list. If you want to know what RPIDs are on the pre-configured list, you will likely need
to contact the manufacturer.

One strategy your application can take is to never worry about these lists. If an RP wants
to know the minimum PIN length, it can ask. If that RP is on the pre-configured list, then
it will get the value. If the RP is not on that list, it will still receive the
credential, but it won't get the minimum PIN length. Your application is simply enforcing
a policy that says only RPIDs the company has originally specified are allowed to see the
minimum PIN length.

Note that another application can set the caller-defined list, all that is needed is for
the user to supply the PIN or fingerprint when the application builds an AuthToken with
`AuthenticatorConfiguration` permission. Hence, it would be possible an RP is not on the
pre-configured list and is nonetheless able to obtain the minimum PIN length.

#### Setting the caller-defined list replaces the previous list

Suppose a YubiKey already has a caller-defined list. When you call `TrySetPinConfig` with
a list of RPIDs, it does not add or edit the existing list. The new list replaces the
previous one. Even if the previous list is longer than the new list, the previous list is
deleted and the YubiKey is set with the new list.

This will not replace the pre-configured list. There is nothing anyone can do to add to or
remove entries from that list, not even resetting the FIDO2 application.

Because your application will almost certainly not be the only application with access to
the YubiKey, and you can't see what RPIDs are on either list, you can't really know for
sure whether any particular RPID is on the caller-defined list or not.

What this means is that if your application is making a credential, and you want the RP to
see the minimum PIN length (and you know this RP is not on the pre-configured list), a
good strategy is to simply always set the caller-defined list before making the
credential.

#### Maximum number of entries in the caller-defined list

The standard does not specify a required number of available entries for either list.
However, it does allow the authenticator to declare the maximum number of entries for the
caller-defined list.

This number is given by the `AuthenticatorInfo.MaximumRpidsForSetMinPinLength` property.
This is the maximum number of RPIDs that can be passed to the authenticator during a call
to `TrySetPinConfig`. Because any setting a new list always replaces a current list, this
is the maximum number of RPIDs the caller-defined list will hold.

For the YubiKey, this number is likely to be one. There is logic to this number being one.
Because you cannot see the contents of the RPID list, and because more than one
application can have access to a YubiKey, you can never know whether an RPID is on the
list or not, even if your application placed it onto the list previously. So your best bet
is to simply always set the caller-defined list to the RP for which you are currently
making a credential (assuming you want to allow that RP to see the minimum PIN length).

#### Requesting the minimum PIN length

If the RP wants to know the minimum PIN length, then the client will pass that information
on to the YubiKey during the process of making a credential.

With the SDK, that means specifying this request in the `MakeCredentialParameters`.

```csharp
        // Assume there is a variable called rp that is an instance of
        // the RelyingParty class.
        var rpList = new List<string>(1)
        {
            rp.Id
        };
        bool isSupported = fido2Session.TrySetPinConfig(null, rpList, null))

        // Assume we have a UserEntity and a client data hash.
        var mcParams = new MakeCredentialParameters(rp, userEntity)
        {
            ClientDataHash = clientDataHash
        };
        mcParams.AddOption(AuthenticatorOptions.rk, true);
        if (isSupported)
        {
            mcParams.AddMinPinLengthExtension(fido2Session.AuthenticatorInfo);
        }
```

When the credential is made, the minimum PIN length is returned in the
`MakeCredentialData.AuthenticatorData.Extensions`.

```csharp
        MakeCredentialData mcData = fido2Session.MakeCredential(mcParams);

        // If the RP is allowed to see the minimum PIN length, the return will
        // be the value. If the RP is not allowed to see it, the return will be
        // null.
        int? minPinLen = mcData.AuthenticatorData.GetMinPinLengthExtension();
```

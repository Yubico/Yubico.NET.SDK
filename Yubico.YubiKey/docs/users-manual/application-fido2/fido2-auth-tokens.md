---
uid: Fido2AuthTokens
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

# How authentication is achieved: AuthTokens, permissions, PIN/UV, and AuthParams

Usually a PIN "unlocks" an application, but with FIDO2, the PIN is used to verify that the
caller is allowed to perform a requested command. That is, with FIDO2, the PIN is used to
authenticate a command.

The mechanism to do this begins when the PIN is used to retrieve an "AuthToken". Although
an AuthToken is not what is ultimately used to authenticate (an AuthParam is, see below),
most of the work in authentication is in retrieving AuthTokens from a YubiKey. It is the
foundation of the process. Hence, most of this document is about AuthTokens.

When you use the SDK, you will likely never make a call directly to retrieve an AuthToken
most of the work is done "under the covers" or "automatically". However, you must
understand the AuthToken in order to provide appropriate arguments to various method
calls. Furthermore, it is important to understand many of the (sometimes very) complex
rules in the FIDO2 standard that require building multiple AuthTokens in a single session.

That is, there can be use cases where a PIN must be entered multiple times in a single
session, and that is not a bug, or an SDK failure, but requirements of the standard. This
knowledge will help you better design your product.

## PIN, UV, AuthToken, AuthParam

In order to perform many FIDO2 operations, one must pass an AuthParam to the YubiKey along
with a command. An AuthParam is built from an AuthToken. The AuthToken is retrieved from
the YubiKey using the PIN or UV, along with the shared secret and possibly some other
info, which can include a list of permissions and a relying party ID.

The permissions are values that specify for which operations an AuthToken can be used. It
is also possible (and sometimes required) to specify for which relying party the operation
will work. That is, you can have many credentials on a YubiKey, but by specifying the
relying party, a command is only allowed to operate on those credentials associated with
that relying party. The section below on [permissions](#permissions) has more information
on this topic.

The shared secret is computed by performing the ECDH protocol between the client and
YubiKey.

shared secret + PIN [+ permissions] --> AuthToken --> AuthParam, where AuthParam used to authenticate a command

Here's another way to look at it.

* [Shared Secret] perform ECDH shared secret protocol between client and YubiKey
* [AuthToken] the client retrieves the AuthToken from the YubiKey using one of the following methods
    * PIN + shared secret => PinToken
    * PIN + shared secret + permissions => AuthToken
    * UV (fingerprint) + shared secret + permissions => AuthToken
* [AuthParam] the client builds the AuthParam
    * PinToken + message => AuthParam
    * AuthToken + message => AuthParam
* The client sends a command to the YubiKey
    * var cmd = new SomeCommand(info, AuthParam)

With the SDK, either all of this work is performed automatically (there is nothing you
have to do other than supply a KeyCollector), or you make one or two calls if you want
more control, and to possibly reduce the number of PIN/UV collections.

### How an AuthToken is retrieved

In the SDK, an AuthToken is retrieved by calling one of the `Verify` methods, such as
[VerifyPin](xref:Yubico.YubiKey.Fido2.Fido2Session.VerifyPin%2a) and
[VerifyUv](xref:Yubico.YubiKey.Fido2.Fido2Session.VerifyUv%2a). These
`Verify` methods will carry out the following actions:

* Perform ECDH to obtain a shared secret key
* Perform either PIN only, PIN with permissions, or UV with permissions
    * PIN only
        * If the PIN is not provided, call the KeyCollector
        * Digest the PIN
        * Encrypt the digest of the PIN using the shared secret key
        * Send a command to the YubiKey containing the encrypted "PinHash" and other info
        * If the PIN was correct, the YubiKey verifies the "PinHash"
        * The YubiKey returns a PinToken, which is the AuthToken to use
    * PIN with permissions
        * The caller supplies the permissions
        * If the PIN is not provided, call the KeyCollector
        * Digest the PIN
        * Encrypt the digest of the PIN using the shared secret key
        * Send a command to the YubiKey containing the encrypted "PinHash", permissions, and other info
        * If the PIN was correct, the YubiKey verifies the "PinHash"
        * The YubiKey returns a PinUvAuthToken (with permissions attached), which is the AuthToken to use
    * PIN with permissions
        * The caller supplies the permissions
        * Send a command to the YubiKey containing permissions, and other info
        * Upon receiving the command, the YubiKey will wait for the user to verify the fingerprint
            * The SDK sends a message to the KeyCollector announcing the YubiKey is waiting for the fingerprint
        * If the fingerprint is provided and it is correct, the YubiKey verifies it
        * The YubiKey returns a PinUvAuthToken, which is the AuthToken to use

In the SDK, if you call a `Verify` method, the result will be an AuthToken. The
`Fido2Session` class contains a property for the most recent AuthToken retrieved. For
example,

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        // The fido2Session.AuthToken property starts out as null.
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;
        fido2Session.VerifyPin();
        // If this succeeds, the fido2Session.AuthToken property is not null.
    }
```

At this point, there is an AuthToken that can be used to authenticate some operations.
However, it is important to note that the FIDO2 application was not "unlocked". It is not
the FIDO2 application itself that was verified, but only the PIN. And that AuthToken can
be used to authenticate only those operations for which it has permissions.

Notice that while you can call a `Verify` method, the SDK will perform the ECDH key
agreement, digest the PIN, encrypt the "PinHash", build the command message, send it to
the YubiKey, and parse the response. Although all those tasks are required to obtain an
AuthToken, your code only called the `Verify` method.

It is also important to note that it is not necessary to call a `Verify` method directly.
The SDK will call one if it is trying to perform some operation for which the existing
AuthToken won't work. See the section below on
[automatic verification](#sdk-automatic-verification) for more information on that topic
and when to let the SDK do this work and when you should intercede in the process.

Note also that in the above example, the code did not specify any permissions. In that
case, what the YubiKey returns is a PinToken. The sections below on
[AuthToken](#authtokens) provide more information on the difference between PinTokens
and PinUvAuthTokens.

### Using an AuthToken more than once

In some situations it is possible to build one AuthToken and use it to build several
AuthParams, either for multiple commands or multiple calls to the same command. However,
it is also possible an AuthToken can be used to build an AuthParam for one command and
not another. There are two reasons that happens (1) permissions and (2) expiry.

An AuthToken has permissions attached to it. If the set of permissions does not include
the operation you have requested, the AuthParam built from that AuthToken will not work.
It is even possible to have an AuthToken with the correct permissions not work because
a particular permission is associated with a relying party and the operation cannot
execute for that RP. See the section below on [permissions](#permissions).

An AuthToken's permissions can also expire. There are a number of ways to expire, but the
most common is when the YubiKey, upon receiving an AuthParam for one command, will
"expire" an AuthToken, and it cannot be used again (in the FIDO2 terminology, "expire" is
an active verb, it is something the YubiKey does to the PinToken). To perform another
command requires the client retrieve a new AuthToken. In still other cases an AuthToken
can be "partially expired", where it can be reused for some commands, but not for others.

Note that "expire" will usually have nothing to do with time. That is, a YubiKey will
expire an AuthToken, not because some amount of time has passed, but because some other
condition has been met. See the section below on [expiry](#expiry).

### Only one AuthToken is valid at any one time

It is not possible to retrieve several AuthTokens at one time and then use each one as
needed.

When a client requests an AuthToken from a YubiKey, the YubiKey builds one and stores it
(along with its permissions and relying party ID, if there are any) somewhere inside the
FIDO2 application space. That AuthToken is used to verify any AuthParam the client sends.

If a new AuthToken is requested, according to the standard, the YubiKey must perform
`resetPinUvAuthToken`, and "all existing pinUvAuthTokens are invalidated".

### AuthToken and PIN/UV collection

Each time the SDK builds a new AuthToken, it must have the PIN or the user must perform
the UV operation (on the YubiKey Bio that's fingerprint). The SDK will not keep a local
copy of the PIN, so if a PIN is needed, the SDK will call the KeyCollector. If the
KeyCollector does not keep a local copy of the PIN, it will have to ask the user to enter
it again.

There is no way, of course, to keep a local copy of a fingerprint, so if that is the way
the user authenticates, then they will have to perform that operation each time an
AuthToken is needed.

Unfortunately, because of the standard, depending on what your application does and how it
calls the SDK, it can be unavoidable to require multiple collections in the same session.

### Reusing AuthParams

The SDK will build a new AuthParam each time one is needed. There are some rare cases
where an AuthParam can be used more than once. That is, it is possible to store an
AuthParam and check to see if it can be reused. But it is much easier and more efficient
to simply build a new AuthParam each time.

## AuthTokens

There are three kinds of AuthTokens:

* PinToken
* PinUvAuthToken Using PIN
* PinUvAuthToken Using UV (fingerprint)

### PinToken

This is the AuthToken from FIDO2 version 2.0. There are no permissions associated with a
PinToken. Even though it is a version 2.0 constuction, it can be used in FIDO2 version
2.1. However, while some commands will successfully execute using an AuthParam built from
a PinToken, there are other commands that will return an error if provided an AuthParam
built from a PinToken.

On a FIDO2 version 2.0 YubiKey, the PinToken can be used many times and is generally not
expired. Hence, the client collects one PinToken from the YubiKey and uses it for several
commands or several calls to the same command.

On a FIDO2 version 2.1 YubiKey, the PinToken can only be used to perform MakeCredential
and GetAssertion. Furthermore, it can be expired, and in fact, it can be used to build an
AuthParam that will authenticate only call to one command.

### PinUvAuthToken

This is the FIDO2 version 2.1 AuthToken. It is built with permissions. That means an
AuthParam built from such an AuthToken will only authenticate commands specified in the
permissions. For example, it is possible to retrieve an AuthToken with permissions set to
"credential management". A client could use that AuthToken to build an AuthParam for
"get assertion" (that is, it's possible to write code to produce output), but that
AuthParam would not actually authenticate the GetAssertion command. The YubiKey is able to
know which AuthToken was used, and would know the permissions associated with that
AuthToken and authentication would fail.

It is possible to request an AuthToken with multiple permissions. That AuthToken could be
used for several different commands.

PinUvAuthTokens can be expired easily. See the section below on [expiry](#expiry).

If you use a PinToken on a FIDO2 version 2.1 YubiKey, the permissions are considered to
be "make credential" and "get assertion". That is, even though a PinToken does not have
the concept of permissions, a YubiKey that supports FIDO2 version 2.1, will act as if the
AuthToken has those two permissions. Hence, a PinToken cannot be used to perform
Credential Management, Bio Enrollment, Large Blobs, and others. Furthermore, a PinToken
can expire on a FIDO2 version 2.1 device.

### PinUvAuthToken using PIN or UV

It is possible to get an AuthToken using the PIN or UV (user verification, for YubiKey
that is fingerprint). It doesn't matter how the user "authenticates", the YubiKey will
return a PinUvAuthToken. There is no difference between an AuthToken retrieved using PIN
or UV. They work exactly the same. It is just a matter of how you authenticate yourself to
the YubiKey.

Note that in FIDO2 version 2.0, there was no way to perform UV to get a PinToken, there is
only the PIN. Fingerprint authentication was added in FIDO2 version 2.1. The FIDO2
standard currently specifies only fingerprints, but other methods, such as face
recognition, could be added in the future.

> [!NOTE]
> When a YubiKey would like the user to verify a fingerprint, the SDK will notify your
> application. See the
> [article on the KeyCollector and touch](../sdk-programming-guide/key-collector-touch.md)
> for a more detailed description of how to handle fingerprint notifications.

To perform UV, the fingerprint must be registered with the FIDO2 application on the
YubiKey. In FIDO2 this is called enrollment.

There are ways to discover whether the YubiKey has fingerprint capabilities, and if so,
whether one has been enrolled. The standard says, the client "SHOULD" try UV first, and if
that fails, use PIN (how to know a UV fails is a topic for another doc). Actually, it is
not that simple, there are further conditions, but for the most part, if the YubiKey has
fingerprints, and one has been enrolled, the client should call on the code that uses UV
for authentication, and use the PIN only if the UV fails.

However, that is "SHOULD" not "SHALL". Hence, it is possible a client only calls on the
code that uses the PIN. Or tries PIN first, then UV.

## Permissions

FIDO2 version 2.0 does not have permissions. That means that if your application is
connected to a YubiKey that supports only version 2.0, you will retrieve a PinToken and
that PinToken will have the permission to call on the YubiKey to do anything it supports.

Permissions were introduced in FIDO2 version 2.1. The standard lists six possible
permissions:

* Make Credential
* Get Assertion
* Credential Management
* Bio Enrollment
* Large Blob Write
* Authenticator Configuration

If you are programming for a YubiKey that supports FIDO2 version 2.1, then when you obtain
an AuthToken, you should get a PinUvAuthToken with permissions. For example, if you know
you will want to perform Bio Enrollment, get an AuthToken with that permission. Or if you
know you will want to get an assertion and write data to the [large blob](large-blobs.md),
get an AuthToken with those two permissions.

To get an AuthToken with permissions, call one of the `VerifyPin` or VerifyUv` methods.
For example

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;
        fido2Session.VerifyPin(PinUvAuthTokenPermissions.CredentialManagement);
    }
```

However, there are a number of complexities to be aware of.

### Relying Party ID

The standard specifies that for MakeCredential and GetAssertion, the permission must be
accompanied with a relying party ID. For example, if you want to get an assertion, you
must know for which relying party the assertion is. Then you get an AuthToken with the
permission of GetAssertion for that relying party. That AuthToken can be used only for
getting an assertion associated with the specified relying party. If you want to get an
assertion for a different relying party, you must get a new AuthToken.

The standard also states that with CredentialManagement, a relying party is optional. This
happens to be a fairly complex topic and is fully described below in the section on
[CredentialManagement permission](#credentialmanagement-permission).

For the last three permissions (BioEnrollment, LargeBlobWrite,
AuthenticatorConfiguration), the relying party is ignored. When you specify any of these
permissions, you can include a relying party, but it won't matter.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;
        fido2Session.VerifyPin(PinUvAuthTokenPermissions.GetAssertion, "RelyingPartyIdOfInterest");

        // or
        fido2Session.VerifyPin(
            PinUvAuthTokenPermissions.GetAssertion | PinUvAuthTokenPermissions.LargeBlobWrite,
            "RelyingPartyIdOfInterest");
    }
```

### PinToken on a FIDO2 version 2.1 device

The PinToken is from FIDO2 version 2.0, but it is possible it will still work on a FIDO2
version 2.1 device. An authenticator that supports FIDO2 version 2.1 is allowed by the
standard not to support FIDO2 version 2.0. If that happens, the authenticator is allowed
not to support the PinToken.

The PinToken has no concept of permissions. However, if you are connected to a YubiKey
that supports FIDO2 versions 2.0 and 2.1, a PinToken will behave as if it has the
permissions MakeCredential and GetAssertion. That means you cannot perform
CredentialManagement, BioEnrollment, etc. using a PinToken.

The MakeCredential and GetAssertion permissions are required to be accompanied by the
relying party ID, but there is no place in a PinToken for a relying party. If you have a
PinToken, then you can make a credential or get an assertion from any relying party. You
will be allowed, though, to perform only one operation using that PinToken. Once you make
a credential or get an assertion, the PinToken will no longer be valid.

### CredentialManagement permission

There are five CredentialManagement operations:

* GetCredentialMetadata
* EnumerateRelyingParties
* EnumerateCredentials
* DeleteCredential
* UpdateUserInformation

According to the standard, "The rpId parameter is optional, if it is present, the
pinUvAuthToken can only be used for Credential Management operations on Credentials
associated with that RP ID".

However, that is not accurate. Because the standard also declares that if you want to get
credential metadata or enumerate relying parties, then the AuthToken must not have an
associated relying party. This means that if you want to get credential metadata, you must
have an AuthToken with the CredentialManagement permission and no relying party. Or if you
want to enumerate the relying parties (get information on all relying parties represented
in all the [discoverable credentials](fido2-credentials.md)), you must not supply a
relying party.

If you want to enumerate the credentials, delete a credential, or update the user
information on one credential, you must get an auth token with the
CredentialManagement permission, but it will work whether or not it has an associated
relying party. If a relying party is specified, the operations will work only on
credentials associated with that relying party.

Suppose your application wants to list all the relying parties, then list all the
credentials for each relying party. You would need to retrieve an AuthToken with the
CredentialManagement permission and no relying party.

Suppose you know your application will want to get credential metadata and get an
assertion. You could get an AuthToken with both GetAssertion and CredentialManagement
permissions. However, the GetAssertion permission requires a relying party and the get
metadata operation requires no relying party. If you do not supply a relying party ID when
retrieving the AuthToken, the YubiKey would return an error. If you do supply a relying
party ID, the YubiKey would return an AuthToken, but that AuthToken would not work when
trying to get the credential metadata.

## Expiry

The standard lists a number of ways the YubiKey can expire an AuthToken. See section 6.5.

The most common way to expire a PinUvAuthToken is by "user presence" (touch). Once a
command that requires user presence has been completed (including the touch), the
AuthToken is expired (or partially expired, see below).

For example, get a PinUvAuthToken with multiple permissions: Credential Management and
Get Assertion. Now use that AuthToken to enumerate the credentials on the YubiKey (one of
the Credential Management oeprations). That does not require user presence so the
AuthToken is not expired. Now call the Get Assertion command. That requires user presence.
It works, but once it is complete, the AuthToken is expired. Try to use it to do
Credential Management and the YubiKey returns an error.

Side note: It is not possible to make a credential so that user presence is not required
to get an assertion. In other words, if you make a credential, in order to get an
assertion from that credential, user presence will be required. Hence, "by definition", on
a FIDO2 version 2.1 YubiKey, an AuthToken can be used for one get assertion or one make
credential.

Another example: get an AuthToken with permissions for Credential Management, Get
Assertion, and Large Blobs. Perform the Get Assertion and the AuthToken is only partially
expired. The AuthToken can no longer be used for Credential Management nor Get Assertion,
but it can still be used for Large Blobs. Think of it as a quirk in the expiry rules.

In general, there is no way to know in advance whether an AuthToken has expired or not.
Of course, a client could be written so that it keeps track of the operations and updates
a local variable that specifies an AuthToken's state based on the rules it knows the
YubiKey follows. But the only real way to definitively know whether an AuthToken will work
or not is to try to use it. If an operation succeeds, it was valid. If it fails with an
error of CTAP2_ERR_PIN_AUTH_INVALID, then the AuthToken did not work. That error could
happen if an AuthToken has expired, but it can also happen for other reasons. Whatever the
reason, it's necessary to collect the PIN again (or perform UV again) and retrieve a new
AuthToken.

## SDK automatic verification

You can, if you want, let the SDK take care of all the AuthToken work. You simply supply a
KeyCollector. This only works if you perform all your FIDO2 work inside a
[Fido2Session](xref:Yubico.YubiKey.Fido2.Fido2Session).

In this case, if the SDK is performing an operation that needs an AuthToken, it will try
to use whatever it has (see the
[AuthToken](xref:Yubico.YubiKey.Fido2.Fido2Session.AuthToken) property). If that works,
the SDK completes the operation. If not, it will determine what it needs and make the
appropriate call to get a working AuthToken. Then it tries the original operation again.

For example, if your application calls the `GetAssertions` method, the SDK will know it
needs an AuthToken with the GetAssertion permission associated with the specified relying
party. It will retrieve an AuthToken and use it. The only way your application will know
it needed an AuthToken was that the SDK called on the KeyCollector, either to request UV
or a PIN.

The upside of this is that your application never needs to worry about AuthTokens at all.
The downside is that it is possible the SDK will require unnecessary PIN collection.

For example, suppose you know your application will do two things: enumerate the
credentials for a relying party, then get an assertion for one of the credentials tied to
that relying party. Your code might look like this.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;
        IList<CredentialUserInfo> credentialList = fido2Session.EnumerateCredentialsForRelyingParty(relyingParty);
        CredentialUserInfo credentialToUse = ChooseCredential(credentialList);
        GetAssertionParameters params = GetParamsForCredential(relyingParty, credentialToUse);

        IList<GetAssertionData> assertions = fido2Session.GetAssertions(params);
    }
```

The `Fido2Session` begins with no AuthToken. During the call to
`EnumerateCredentialsForRelyingParty` the SDK recognizes that it needs an AuthToken with
the CredentialManagement permission (with or without the relying party ID). It obtains
such an AuthToken and completes the operation.

Then, during the `GetAssertions` call, the SDK tries using the existing AuthToken. It
doesn't work, so it needs a new AuthToken. It calls the KeyCollector again and the user
needs to enter the PIN again.

In this situation, it would have been possible to get an AuthToken with both permissions
and the user would have had to enter the PIN only once.

### AddPermissions

One option is to call the
[AddPermissions](xref:Yubico.YubiKey.Fido2.Fido2Session.AddPermissions%2a)
method early in the session.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        fido2Session.KeyCollector = SomeKeyCollectorDelegate;
        fido2Session.AddPermissions(
            PinUvAuthTokenPermissions.CredentialManagement | PinUvAuthTokenPermissions.GetAssertion,
            relyingPartyId);
    }
```

This is a method you can call to obtain an initial AuthToken, one that contains all the
permissions your application will need during the session. Thus, it can eliminate some PIN
collection or UV (fingerprint) operations.

This is not foolproof, however. You could specify all the permissions you want at the
beginning, but because of expiry, it is possible multiple AuthTokens will be required. Or
if you want to get credential metadata or enumerate relying parties and get assertions,
you will need multiple AuthTokens, there is no way around the standard in this case.

## Caller-managed AuthTokens

Finally, it is possible for you to write your code in such a way that no KeyCollector is
needed. Your code would be responsible for calling `TryVerifyPin` each time a new
AuthToken is required.

First, this only works with PIN-based AuthTokens. There is no way to collect UV-based
AuthTokens without a KeyCollector. You could, of course, build a simple KeyCollector that
does nothing, but then how would a user know the fingerprint is needed?

If a YubiKey has fingerprint capabilities and one is enrolled, then the standard says your
application should use the fingerprints (and only use PIN if the fingerprints fail).
However, the standard also specifies that a client can use the PIN as the primary or even
only method of user authentication.

You could write your application so that you call the PIN-provided `Verify` method
directly each time you need a new PinToken.

```csharp
    using (fido2Session = new Fido2Session(yubiKeyDevice))
    {
        // Assume there is a ReadOnlyMemory<byte> containing the PIN.
        if (!fido2Session.TryVerifyPin(
            pin, PinUvAuthTokenPermissions.CredentialManagement, null, out int retriesRemaining, out bool rebootRequired))
        {
            // handle wrong PIN case.
        }
        (int discoverableCredentialCount, int remainingSlots) = fido2Session.GetCredentialMetadata();

        // Assume you have some method that determines which relying party to use.
        // This call will probably perform other CredentialManagement operations such as
        // enumeration. But you know that the current AuthToken has the appropriate
        // permission.
        RelyingParty relyingParty = ChooseRelyingParty(fido2Session);

        // Now that you have the relying party, you can get an assertion.
        // But we'll need a new AuthToken.
        if (!fido2Session.TryVerifyPin(
            pin, PinUvAuthTokenPermissions.GetAssertion, relyingParty.Id, out retriesRemaining, out rebootRequired))
        {
            // handle wrong PIN case.
        }
        
        var gaParams = new GetAssertionParameters(relyingParty, clientDataHash);
        IList<GetAssertionData> assertions = fido2Session.GetAssertions(gaParams);
    }
```

You could write your code in such a way that you don't need to get a new AuthToken each
time you call on the SDK to do something. In that case, you must know the requirements of
the standard and know for sure when the existing AuthToken is still valid for the next
call.

Or you could write your code to call verify right before each SDK call that will perform
some FIDO2 operation that requires authentication.

Or you could simply build a KeyCollector and let the SDK perform automatic AuthToken
retrieval. Although it is not necessarily secure, your KeyCollector could collect the PIN
once, store it locally, and return it each time. In this way, the user does not need to
enter the PIN several times during a session.

Note that the SDK will try UV first, so if you don't want the user to use the fingerprint,
your KeyCollector will return `false` when the `KeyEntryData.Request` is
`KeyEntryRequest.VerifyFido2Uv`. With automatic AuthToken retrieval, when the caller
cancels the fingerprint, the SDK will move on to PIN.

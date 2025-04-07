---
uid: Fido2Credentials
---

<!-- Copyright 2022 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 Credentials

A credential is what the YubiKey builds and sends to the relying party. In turn, the
relying party uses the credential to verify an assertion the YubiKey will build during a
later authentication procedure.

In FIDO2, a credential is a public key. The private key partner is generated on the
YubiKey (in the secure element) and never leaves the device. When authenticating to the
relying party, the YubiKey will build an assertion by using the private key to sign some
data that includes a challenge from the relyng party. The relying party will verify the
signature, thus verifying the assertion, in order to authenticate the YubiKey.

There are two kinds of credentials:

* Discoverable (FIDO2 version 2.0: resident keys)
* Non-discoverable or server-side (FIDO2 version 2.0: non-resident credentials)

A discoverable credential is stored on the YubiKey. It can be seen or used if you have
only the relying party ID. For example, if you want to get information about a
discoverable credential, you can simply ask the YubiKey to enumerate all credentials
associated with a particular relying party. You need only supply the relying party ID.

A non-discoverable credential is not stored on the YubiKey (hence the FIDO2 version 2.0
term "non-resident"). The credential is not stored anywhere, rather, the YubiKey can
reconstruct a non-discoverable credential if it has enough information. That includes the
credential ID. If you build a non-discoverable credential, then you must manage the
credential ID yourself. Then, when you need an assertion for that credential, supply the
credential ID and the YubiKey will be able to get an assertion.

There are two main operations in FIDO2:

- Make a credential (registration)
- Get an assertion (authenticate)

## Make a credential (registration)

The process of making a credential is generally the following:

- Relying party information (name, ID), a "client data hash" (which includes a challenge
  from the relying party), as well as other system information is sent to the YubiKey.
- The YubiKey generates a key pair, signs the input information, and returns the public
  key and signature, along with an attestation statement and attestation certificate.
- The relying party verifies the signature using the public key and verifies the public
  key using the attestation statement and certificate.

At this point, the YubiKey contains an entry for this credential, and the relying party
can update its entry for the user. The YubiKey's entry contains the relying party
information and the private key. The relying party's user entry is updated with the
public key.

The SDK offers two ways to make a credential:

- [Fido2Session.MakeCredential](xref:Yubico.YubiKey.Fido2.Fido2Session.MakeCredential%2a)
- [MakeCredentialCommand](xref:Yubico.YubiKey.Fido2.Commands.MakeCredentialCommand)

Most developers will use `Fido2Session.MakeCredential` because it is easier and more
straightforward to use.

### Make credential parameters

When you make a credential, you need to specify for which relying party this credential is
being built. Therefore, of course, the relying party is one of the parameters for making a
credential. However, there are several more parameters to consider. The standard specifies
that some of them are required, and some are optional.

Some of the parameters the standard describes as optional are required by the YubiKey.
This is because the YubiKey requires a PIN. The standard allows an authenticator to be
used without a PIN. In such a situation, if the client can connect to the authenticator,
and user presence is proven (e.g. touch a sensor), then the operation will proceed.

The YubiKey, in contrast, will only work with a PIN. If you want to make a credential, the
YubiKey must have a PIN set, and the PIN must be entered. Two of the parameters,
`Protocol` and `PinUvAuthParam` are related to the PIN, and, therefore, are required. If
you make a credential using `MakeCredentialCommand`, you must supply them.

However, if you use the `Fido2Session.MakeCredential` method, the SDK will collect them
for you. In fact, even if you supply them, the SDK will ignore what you supply and collect
them anyway.

You collect all the parameters in the
[MakeCredentialParameters](xref:Yubico.YubiKey.Fido2.MakeCredentialParameters) class.
According to the FIDO2 standard, these are the required elements:

- ClientDataHash
- RelyingParty
- UserEntity
- Algorithms (pubKeyCredParams)

The following are optional in the FIDO2 standard, but required by the YubiKey. Remember,
if you use `Fido2Session.MakeCredential`, you should not supply these parameters. If you
do, they will be ignored.

- Protocol
- PinUvAuthParam

The following are optional for both the FIDO2 standard and the YubiKey:

- ExcludeList
- Extensions
- Options (only "rk" is allowed when making credentials on a YubiKey, see section 6.1 of
  the FIDO2 standard, for more information on Options and "rk")
- EnterpriseAttestation

> [!NOTE]
> The FIDO2 standard specifies that a `UserEntity` is a required element in order to make
> a credential. The `UserEntity` is made up of an `ID`, a `Name`, and a `DisplayName`. The
> standard also says the `Name` and `DisplayName` are optional. It should be possible to
> make a credential using a `UserEntity` that contains only an `ID`. However, YubiKeys
> prior to version 5.3.0 require a `Name` in order to make a credential.

### MakeCredential example

```csharp
    using var fido2Session = new Fido2Session(yubiKey)
    {
        // If you do not call a VerifyPin method directly, the SDK will call
        // it automatically. But for automatic PIN collection, you must supply
        // a KeyCollector.
        fido2Session.KeyCollector = SomeKeyCollector;

        // Although the YubiKey requires the Protocol and PinUvAuthParam,
        // don't supply them here because the SDK's Fido2Session.MakeCredential
        // method will set these values correctly.
        var makeCredentialParams = new MakeCredentialParameters(
            new RelyingParty("sample-rp"), new UserEntity("sample-user"))
        {
            ClientDataHash = sampleClientDataHash;
        };
        // To make the credential discoverable (stored on the YubiKey), you must
        // set the "rk" option to true.
        makeCredParams.AddOption(AuthenticatorOptions.rk, true);

        MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParams);
    }
```

## Get an assertion (authenticate)

When the user needs to authenticate to the relying party, the YubiKey will build an
assertion. If the relying party verifies the assertion, the user is authenticated.

The assertion is a signature. The authenticator signed data that included a challenge from
the relying party. The relying party tries to verify the signature using the public key
(credential) it has in its user data. If that key does not verify the signature, either
the challenge was not signed (maybe an attacker sent an old, intercepted signature in a
replay attack), or the wrong private key was used (e.g. a YubiKey that has a private key
associated with the relying party, but it is not the one associated with the account).

Before the YubiKey can build an assertion, it must know which private key to use. It does
so by finding the private key associated with the relying party. Remember that when the
credential was first registered, the relying party information was stored with the
appropriate private key. During authentication, the client (browser) will send a message
to the YubiKey, requesting it sign the challenge (and other data) using the private key
associated with the provided relying party. If the YubiKey cannot find an entry for the
given relying party, it will not sign anything.

This generally happens when the client is connected to an attacker and not the correct
target. That is, the user wants to connect with relying party A, but has somehow been
hijacked and is connected to relying party X. The client (browser) will send to the
YubiKey the relying party information of who it is actually connected to, not the target.

The SDK offers three ways to get an assertion:

- [Fido2Session.GetAssertions](xref:Yubico.YubiKey.Fido2.Fido2Session.GetAssertions%2a)
- [GetAssertionCommand](xref:Yubico.YubiKey.Fido2.Commands.GetAssertionCommand)
- [GetNextAssertionCommand](xref:Yubico.YubiKey.Fido2.Commands.GetNextAssertionCommand)
  if there are multiple credentials

Most developers will use `Fido2Session.GetAssertions` because it is easier and more
straightforward to use.

### Get assertion parameters

As is the case with making a credential, there are parameters needed to get an assertion.
The FIDO2 standard specifies some as required and others as optional. In addition, as with
making a credential, some of the standard's optional parameters (PIN-related) are required
by the YubiKey.

However, if you use the `Fido2Session.GetAssertions` method, the SDK will collect them
for you. In fact, even if you supply them, the SDK will ignore what you supply and collect
them anyway.

You collect all the parameters in the
[GetAssertionParameters](xref:Yubico.YubiKey.Fido2.GetAssertionParameters) class.
According to the standard, these are the required elements:

- ClientDataHash
- RelyingParty

The following are optional in the FIDO2 standard, but required by the YubiKey. Remember,
if you use `Fido2Session.GetAssertions`, you should not supply these parameters. If you
do, they will be ignored.

- Protocol
- PinUvAuthParam

The following are optional for both the FIDO2 standard and the YubiKey:

- AllowList
- Extensions
- Options (only "up" is allowed when getting an assertion on a YubiKey, see section 6.1 of
  the FIDO2 standard, for more information on Options and "up")

> [!NOTE]
> The `AllowList` is required if there are credentials created as non-discoverable.

## Multiple credentials

It is possible a YubiKey holds multiple credentials for any particular relying party. This
might happen because a single user has multiple roles, such as end user, administrator,
and so on.

This is why the return from `Fido2Session.GetAssertions` returns an `IList`. If there is
only one assertion, the list will contain only one element. But if there are more, you can
examine each of the assertions to see which one you want to send.

Each assertion is represented as a
[GetAssertionData](xref:Yubico.YubiKey.Fido2.GetAssertionData) object. That object
contains information such as user data, which you can use to determine which one to send
to the relying party.

### `GetNextAssertion`

If there are multiple assertions available, and you use the `GetAssertionCommand`, it will
return the first assertion found on the YubiKey, along with information about the
assertion (e.g. user name) and a count of the total number of assertions available.

It will then be necessary to call the
[GetNextAssertionCommand](xref:Yubico.YubiKey.Fido2.Commands.GetNextAssertionCommand).
Your code will look something like this:

```csharp
        var getAssertionCommand = new GetAssertionCommand(assertionParams);
        GetAssertionResponse getAssertionResponse = connection.SendCommand(getAssertionCommand);
        GetAssertionData getAssertionData = getAssertionResponse.GetData();

        int count = getAssertionData.NumberOfCredentials ?? 0;
        for (int index = 1; index < count; index++)
        {
            var getNextAssertionCommand = new GetNextAssertionCommand();
            getAssertionResponse = connection.SendCommand(getNextAssertionCommand);
            getAssertionData = getAssertionResponse.GetData();
        }
```

The data type returned by the `GetNextAssertionCommand` is the same as the type returned
by the `GetAssertionCommand`, namely the assertion and identifying information. The actual
data is different, of course, because they are returning the data for two assertions.

There is also another difference. The return from the first call to `GetAssertionCommand`
contained the total number of credentials. You need to capture that number because each
successive call to the `GetNextAsssertionCommand` will not return it. That is, for each
successive call to `GetNextAssertionCommand`, the `NumberOfCredentials` field will be
null. It is the FIDO2 standard that specifies that the `GetNextAssertionCommand` not
return the number of credentials, or number of credentials remaining.

It is also important to note that the first call returned the first (index of zero)
assertion, so you need to continue counting at the second (index of one) assertion. Hence,
the for loop is `int index = 1; index < count; index++`.

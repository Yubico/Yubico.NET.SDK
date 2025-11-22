---
uid: Fido2ThirdPartyPayment
---

<!-- Copyright 2025 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Third-party payment extension (thirdPartyPayment)

CTAP 2.2 and YubiKeys with firmware 5.8+ support the [thirdPartyPayment extension](https://fidoalliance.org/specs/fido-v2.2-ps-20250714/fido-client-to-authenticator-protocol-v2.2-ps-20250714.html#sctn-thirdPartyPayment-extension), which allows credentials to be used for payment authentication scenarios where the transaction initiator is not the relying party.

For example, suppose a user creates a thirdPartyPayment-enabled credential with their bank. The user then purchases an item from an online merchant and pays using their bank account, and the transaction is validated with their bank credential on their YubiKey.

According to the CTAP 2.2 standard, implementation of the payment authentication flow is up to the platform. See the W3C's [Secure Payment Confirmation (SPC)](https://www.w3.org/TR/secure-payment-confirmation/) for an example of a possible implementation.

## Creating a thirdPartyPayment-enabled credential

YubiKey credentials (discoverable and non-discoverable) can only be used for a third-party payment transaction if the credential itself is thirdPartyPayment-enabled, and enablement must occur when the credential is first created. Enablement simply means that the credential's thirdPartyPayment bit flag has been set to ``true``.

Only YubiKeys with firmware version 5.8 and later support the thirdPartyPayment extension. To verify whether a particular YubiKey supports the feature, check the key's ``AuthenticatorInfo``:

```C#
using (Fido2Session fido2Session = new Fido2Session(yubiKey))
{
    if (fido2Session.AuthenticatorInfo.Extensions.Contains<string>("thirdPartyPayment"))
    {
        ...
    }
}
```

### MakeCredential example with thirdPartyPayment

To create a thirdPartyPayment-enabled credential, we must add the thirdPartyPayment extension to the parameters for ``MakeCredential()``:

```C#
using (Fido2Session fido2Session = new Fido2Session(yubiKey))
{
    // Your app's key collector, which will be used to check user presence and perform PIN/UV 
    // verification during MakeCredential().
    fido2Session.KeyCollector = SomeKeyCollectorDelegate;

    // Create the parameters for MakeCredential (relyingParty, userEntity, and clientDataHashValue
    // set elsewhere).
    var makeCredentialParameters = new MakeCredentialParameters(relyingParty, userEntity)
    {
        ClientDataHash = clientDataHashValue

    };

    // Add the thirdPartyPayment extension plus the "rk" option (to make the credential discoverable).
    makeCredentialParameters.AddThirdPartyPaymentExtension();
    makeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);

    // Create the third-party payment enabled credential using the parameters set above.
    MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
}
```

After calling ``MakeCredential()``, we can check the ``AuthenticatorData`` to verify extension enablement:

```C#
using (Fido2Session fido2Session = new Fido2Session(yubiKey))
{
    ...
    // Returns true if the extension was enabled.
    bool thirdPartyPaymentStatus = credentialData.AuthenticatorData.GetThirdPartyPaymentExtension();
}
```

## Authenticating a third-party payment transaction

To successfully authenticate a third-party payment transaction using a YubiKey, the following must occur:

- The YubiKey credential used for ``GetAssertion`` must be [third-party payment enabled](#creating-a-thirdpartypayment-enabled-credential).
- The thirdPartyPayment extension must be added to the parameters for ``GetAssertion``.

During ``GetAssertion``, the YubiKey will return a boolean value for the thirdPartyPayment extension. If both requirements have been met, the value returned will be ``true``.

### GetAssertion example with thirdPartyPayment

To get an assertion with thirdPartyPayment, do the following:

```C#
using (Fido2Session fido2Session = new Fido2Session(yubiKey))
{
    // Your app's key collector, which will be used to check user presence and perform PIN/UV 
    // verification during GetAssertion().
    fido2Session.KeyCollector = SomeKeyCollectorDelegate;

    // Create the parameters for GetAssertion (relyingParty and clientDataHashValue set elsewhere),
    // and add the request for the thirdPartyPayment return value to the parameters.
    var getAssertionParameters = new GetAssertionParameters(relyingParty, clientDataHashValue);
    getAssertionParameters.RequestThirdPartyPayment();

    // Get the assertion using the parameters set above.
    IReadOnlyList<GetAssertionData> assertionDataList = fido2Session.GetAssertions(getAssertionParameters);
}
```

After calling ``GetAssertions()``, we can check the state of the ThirdPartyPayment return value via the ``AuthenticatorData``:

```C#
using (Fido2Session fido2Session = new Fido2Session(yubiKey))
{
    ...
    // If the YubiKey contains multiple credentials for the relyingParty, this returns the value 
    // from the first credential's data. True = third-party payment enabled.
    bool thirdPartyPaymentValue = assertionDataList[0].AuthenticatorData.GetThirdPartyPaymentExtension();
}
```
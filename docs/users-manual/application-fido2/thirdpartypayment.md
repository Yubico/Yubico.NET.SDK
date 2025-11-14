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



## Creating a thirdPartyPayment-enabled credential

```C#
using (Fido2Session fido2Session = new Fido2Session(yubiKey))
{
    // Your app's key collector, which will be used to perform PIN/UV verification during MakeCredential()
    fido2Session.KeyCollector = SomeKeyCollectorDelegate;

    // Create the parameters for MakeCredential (relyingParty and userEntity set elsewhere)
    var makeCredentialParameters = new MakeCredentialParameters(relyingParty, userEntity);

    // Add the thirdPartyPayment extension
    makeCredentialParameters.AddThirdPartyPaymentExtension();

    // Add the "rk" option to make the credential discoverable
    makeCredentialParameters.AddOption(AuthenticatorOptions.rk, true);

    // Create the thirdPartyPayment-enabled credential using the parameters set above
    MakeCredentialData credentialData = fido2Session.MakeCredential(makeCredentialParameters);
}
```

## Authenticating a third-party payment transaction
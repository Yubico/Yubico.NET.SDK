---
uid: OathPassword
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

# Protecting OATH application with a password 

For greater security, you can protect the OATH application on the YubiKey with a password. If a password is set, the user will first need to verify the password to unlock the application and perform OATH operations. The exception is resetting the application. The password is not required for that.

## Setting the password

To set a password for the OATH application on a YubiKey, you will call the `SetPassword()` method from your instance of `OathSession`. The password you use can be any string of bytes. However, most applications will choose to encode a user-supplied string using UTF-8.

The `SetPassword()` method will use those bytes along with a device ID supplied by the YubiKey to apply many rounds of a key-derivation function called [PBKDF2](https://en.wikipedia.org/wiki/PBKDF2), ensuring an extra level of security against brute-force attacks. Then, the first 16 bytes (128-bits) of the output from that operation are used as the hash value for the password.

The secret then will be stored on the YubiKey.

Use this method in OathSession class to set the password:

```csharp
oathSession.SetPassword();
```

## Verifying the password

Mutual authentication is performed to unlock the OATH application protected with the password. 

The challenge for this comes from the YubiKey. The response is computed by performing the correct HMAC function of that challenge with the secret, which is a user-supplied password and deviceID put through 1,000 rounds of PBKDF2.

A new random challenge is then sent to the application, together with the calculated response.

The application will then respond with a similar calculation that the host software can verify.

Use this method in OathSession class to verify the password:

```csharp
oathSession.VerifyPassword();
```

##  Removing the authentication

The authentication can be removed after the current password is successfully verified.

Use this method in OathSession class to unset the password:

```csharp
oathSession.UnsetPassword();
```

Read more about [OathSession](./oath-session.md) methods.

##  Setting password in Yubico Authenticator App

Yubico Authenticator enables you to protect your YubiKey with a password.

If the OATH application on the Yubikey is protected with the password, you will be prompted to type this password each time you insert the YubiKey into a USB port or connect over NFC.

When using the Yubico Authenticator App with the OATH application on the YubiKey, you will configure it to use the password configured on the YubiKey. It will perform the same algorithmic processes on the password to set and verify the password. Once configured with the password, it can be configured to save the calculated secret on the device to negate the need to enter the password each time the OATH application is used.

If so configured, the calculated secret will be stored in the one of the following locations:

- Keychain (iOS devices)
- Android Keystore (on Android devices)
- In ~/.ykman/oath.json on Linux/macOS
- In %UserProfile%\.ykman\oath.json (Windows)

Note: The Yubico Authenticator App has the ability to protect the secret with FaceID or TouchID, which means you would be prompted to authenticate with one of those methods when using the credential.

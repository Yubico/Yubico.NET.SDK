---
uid: OathSession
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

# OATH session APIs

The high level OATH session APIs provide a simpler way to work with the OATH application on the YubiKey. The OATH session API is a layer built on the lower level command API. Session APIs will help perform OATH scenarios in a shorter amount of development time and without getting involved with each command's details.

## General Definitions

The OATH application is used to manage and use OATH credentials with the YubiKey NEO, YubiKey 4, or YubiKey 5.

It can be accessed over USB (when the CCID transport is enabled) or over NFC.

### IYubiKeyDevice

There is an IYubiKeyDevice interface that represents the YubiKey chosen.

```csharp
// use the first YubiKey found
var yubiKeyToUse = YubiKeyDevice.FindAll().First();
```

### Credential

The `Credential` class represents a single OATH credential. The credential can be a TOTP (Time-based One-time Password) or a HOTP (HMAC-based One-time Password). 

```csharp
var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 30,
  Algorithm = Sha1,
  Digits = 6,
  Secret = "test",
  RequireTouch = true 
};  

var credentialHotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Hotp,
  Period = 0,
  Algorithm = Sha256,
  Digits = 8,
  Counter = 10,
  Secret = "test",
  RequireTouch = true 
};   
```

### Code

The `Code` class represents the credential’s OTP code generated on the YubiKey. The YubiKey supports Open Authentication (OATH) standards for generating one-time password (OTP) codes. The YubiKey-generated passcode can be used as one of the authentication options in two-factor or multi-factor authentication. 

```csharp
var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 30 
};

Code otpCode = oathSession.CalculateCredential(credentialTotp);

// otpCode will look like:
Value = "799357",
ValidFrom = DateTimeOffset.Now;
ValidUntil = DateTimeOffset.MaxValue; // HOTP credential.
// or
ValidUntil = DateTimeOffset.Now + Period // TOTP credential period (15, 30, 60 seconds) 
```

Read more about [credentials](./oath-credentials.md).

## OathSession

The `OathSession` class contains methods to perform high-level operations and scenarios. 

Once you have chosen the YubiKey, you have an object: an instance of the IYubiKeyDevice interface representing the actual hardware. 

To perform OATH operations, create an instance of an OathSession class and pass the YubiKey object. This will connect to the OATH application on the chosen YubiKey: 

```csharp
var oathSession = new OathSession(yubiKeyToUse);
```

This class implements [IDisposable](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable) so that we can close out a session. If the OATH application on the chosen YubiKey is protected with a password, the user will need to verify the password first to unlock the application in order to perform any OATH operations except resetting the application.

Each method except ResetApplication() will call the KeyCollector delegate to manage authentication if the ResponseStatus returns AuthenticationRequired. So, the delegate will be called every time when a password is needed in order to unlock the OATH application.

## KeyCollector delegate

This delegate will be called every time when a password is needed in order to unlock the OATH application.

The delegate provided will read the KeyEntryData which contains the information needed to determine what to collect and methods to submit what was collected. The delegate will return true for success or false for "cancel." A cancel will usually happen when the user has clicked a "Cancel" button.
        
The SDK will call the KeyCollector with a Request of Release when the process completes.

## Methods

### Get credentials

The GetCredentials() method gets all configured credentials on the YubiKey.

```csharp
IList<Credential> credentials = oathSession.GetCredentials();

// Use LINQ to filter credentials by credential's type
List<Credential> filteredCredentials = oathSession.GetCredentials().Where(credential => credential.Type == CredentialType.Totp).ToList();
```

### Get OTPs

The CalculateAllCredentials() method calculates OTP (one-time passwords) values for all configured credentials on the YubiKey except HOTP credentials and TOTP credentials requiring touch.

The OTPs need to be calculated because the YubiKey doesn't have an internal clock. The system time is used and passed to the YubiKey.

```csharp
IDictionary<Credential, Code> credentialCodes = oathSession.CalculateAllCredentials(); 
```

Also, there is the CalculateCredential() method which gets a single OTP value for a specific credential on the YubiKey. This can be used for HOTP credentials and when the RequireTouch property is set for a credential, so you just need to request to recalculate one credential.

```csharp
var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 30 
};

// Pass Credential object.
Code otpCode = oathSession.CalculateCredential(Credential);

// Or 

// Pass Issuer, AccountName, Type and Period of the credential you want to calculate.
Code otpCode = oathSession.CalculateCredential(
    "Yubico",
    "test@yubico.com",
    CredentialType.Totp,
    CredentialPeriod.Period30);
```

### Add credential

The AddCredential() method adds a new credential or overwrites the existing one on the YubiKey.

The existing credential will be overwritten if the same Issuer and Account Name is used when adding a new credential. It applies to TOTP with a default period (30sec) and HOTP credentials. For example, suppose you have a HOTP credential stored on the YubiKey, and you try to add a TOTP credential with a default period and the same Issuer and Account name. In that case, the credential will be overwritten. The behavior would also be the same if the TOTP credential was added first, and the HOTP credential was second. However, this won't apply to TOTP credentials with non-default periods 15sec or 60sec; they will be added separately.

A YubiKey is an embedded device, and storage is a scarce resource. Due to this constraint, the maximum number of credentials that can be added to a YubiKey is 32. Also, the same reason applies to the 64 character restriction for the credential's name (issuer + account name).

Note that credentials on the YubiKeys with a firmware version 5.3.0 or older cannot be renamed once they have been added; they can only be viewed or deleted. If you want to change anything about the credential, including the name of the credential, you must delete the existing credential, and create a new credential with the settings and the name that you want. 

```csharp
var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 30,
  Secret = "test",
  Digits = 6
};

// Pass Credential object.
oathSession.AddCredential(credentialTotp);

// Or 

// Pass the string that you received from QR reader or retrieved from the server. This method will return the credential parsed from the URI string.
Credential credential = oathSession.AddCredential(
                "otpauth://totp/ACME%20Co:test@example.com?secret=HXDMVJECJJWSRB3HWIZR4IFUGFTMXBOZ&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30");
```

Read more about [credentials](./oath-credentials.md) and [URI strings](./uri-string-format.md).

## Remove credential

The RemoveCredential() method removes an existing credential from the YubiKey.

```csharp
var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 60,
  Secret = "test",
  Digits = 8
};

// Pass Credential object.
oathSession.RemoveCredential(credentialTotp);

// Or 

// Pass Issuer, AccountName, Type and Period of the credential you want to remove.
Credential credential = oathSession.RemoveCredential(
    "Yubico",
    "test@yubico.com",
    CredentialType.Totp,
    CredentialPeriod.Period60);

// Or, pass just the Issuer and AccountName if the credential is a TOTP type with a default period.
Credential credential = oathSession.RemoveCredential(
    "Yubico",
    "test@yubico.com");
```

## Rename credential

The RenameCredential() method renames an existing credential on the YubiKey by setting new issuer and account names.

This is only available on the YubiKeys with a firmware version 5.3.0 or later.

```csharp
var credentialTotp = new Credential 
{
  Issuer = "Yubico",
  AccountName = "test@yubico.com",
  Type = Totp,
  Period = 60,
  Secret = "test",
  Digits = 8
};

// Pass Credential object and the new Issuer and AccountName.
oathSession.RenameCredential(credentialTotp, "Test", "example@test.com");

// Or 

// Pass Issuer, AccountName, Type and Period of the credential you want to rename, as well as the new Issuer and AccountName.
Credential credential = RemoveCredential(
    "Yubico",
    "test@yubico.com",
    "Test",
    "example@test.com",
    CredentialType.Totp,
    CredentialPeriod.Period60);

// Pass just the current and new Issuer and AccountName if the credential has TOTP type and default period.
Credential credential = RemoveCredential(
    "Yubico",
    "test@yubico.com",
    "Test",
    "example@test.com");
```

## Reset OATH

The ResetApplication() method resets the YubiKey's OATH application back to a factory default state.

This will remove the password if one set and delete all OATH credentials stored on the YubiKey.

```csharp
oathSession.ResetApplication();
```

## Set password

The SetPassword() method sets or changes the password for the OATH application.

Suppose the password was previously configured on the YubiKey. In that case, this method will prompt for the current password to verify, as well as a new password to change to using the KeyCollector callback.

If a password is not configured, this method will collect only a new password to set.
        
The password can be any string of bytes. However, most applications will choose to encode a user supplied string using UTF-8. 

The password is passed through 1,000 rounds of PBKDF2 with a salt value supplied by the YubiKey, ensuring an extra level of security against brute force attacks.

```csharp
oathSession.SetPassword();
```

## Verify password

The VerifyPassword() method attempts to proactively verify the current password. Note that the the SDK will automatically call the KeyCollector delegate when the password is required. Sometimes an application may want to choose when the password is gathered. This may help with implementing a specific user experience that may otherwise be impossible if you relied on the default KeyCollector behavior.

The method performs mutual authentication with the YubiKey using the password collected by the key collector.

```csharp
oathSession.VerifyPassword();
```

## Unset password

The UnsetPassword() method attempts to remove the current password. This method prompts for the current password to verify first and then removes the authentication.

```csharp
oathSession.UnsetPassword();
```

Read more about [OathPassword](./oath-password.md) implementation on the YubiKey.

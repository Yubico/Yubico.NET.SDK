---
uid: UsersManualPivCommands
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

# PIV commands

For each possible PIV command, there will be a class that knows how to build the
command [APDU](xref:UsersManualApdu) and parse the data in the response APDU. Each class will know
what information is needed from the caller for that command.

According to the PIV standard, there is "Off-Card" and "On-Card". The off-card application
is the one calling the YubiKey. The keys, data, and firmware running on the YubiKey is
"on-card"

#### List of PIV commands

* [Get the serial number](#get-the-serial-number)
* [Get the firmware version number](#get-the-firmware-version-number)
* [Get metadata](#get-metadata)
* [Get Bio metadata](#get-bio-metadata)
* [Verify the PIN](#verify)
* [Biometric verification](#biometric-verification)
* [Authenticate: management key](#authenticate-management-key)
* [Set PIN retries](#set-pin-retries)
* [Change reference data](#change-reference-data) (change the PIN or PUK)
* [Set the management key](#set-management-key) (change the management key)
* [Reset retry](#reset-retry-recover-the-pin) (recover the PIN)
* [Generate asymmetric key pair](#generate-asymmetric)
* [Import asymmetric](#import-asymmetric) (import a private key)
* [Authenticate: sign](#authenticate-sign)
* [Authenticate: decrypt](#authenticate-decrypt)
* [Authenticate: key agreement](#authenticate-key-agreement)
* [Create attestation statement](#create-attestation-statement)
* [Get data](#get-data)
* [Put data](#put-data)
* [Reset](#reset-the-piv-application)

___

## Get the serial number

This gets the YubiKey's serial number.

### Available

YubiKey 5 and later.

### SDK Classes

[GetSerialNumberCommand](xref:Yubico.YubiKey.Piv.Commands.GetSerialNumberCommand)

[GetSerialNumberResponse](xref:Yubico.YubiKey.Piv.Commands.GetSerialNumberResponse)

### Input

None.

### Output

`int`

To see the serial number as a decimal string, use `ToString()`. For example,

```C#
  int serialNumber = serialResponse.GetData();
  string decimalSerial = serialNumber.GetString();
  string hexSerial = serialNumber.GetString("X");

  // Print out the decimalSerial to get something like "11409355"
  // Print out the hexSerial to get something like "00AE17CB"
```

### APDU

[Technical APDU Details](apdu/serial.md)
___

## Get the firmware version number

This gets the YubiKey firmware version number.

### Available

All YubiKeys with the PIV application.

### SDK Classes

[VersionCommand](xref:Yubico.YubiKey.Piv.Commands.VersionCommand)

[VersionResponse](xref:Yubico.YubiKey.Piv.Commands.VersionResponse)

### Input

None.

### Output

[FirmwareVersion](xref:Yubico.YubiKey.FirmwareVersion)

### APDU

[Technical APDU Details](apdu/version.md)
___

## Get metadata

This gets information about the key in a particular slot.

It is possible to get metadata about public/private key pairs in those slots that hold
private keys. It is also possible to get metadata about the symmetric key that is the
management key in slot 9B. Finally, it is possible to get metadata about the PIN and
PUK, accessed by slots 80 and 81.

There are six possible information elements (described below), but not all keys will
report all six elements. Furthermore, if there is no key in a particular slot, there
will be no metadata to get. That is, if you execute the Get Metadata command for a slot
that has no key, the data retrieved will be "None" or "NoKey".

### Available

YubiKey 5.3 and later.

### SDK Classes

[GetMetadataCommand](xref:Yubico.YubiKey.Piv.Commands.GetMetadataCommand)

[GetMetadataResponse](xref:Yubico.YubiKey.Piv.Commands.GetMetadataResponse)

### Input

Slot number. See the User's Manual [entry on PIV slots](slots.md) and the enum
[PivSlot](xref:Yubico.YubiKey.Piv.PivSlot) for information on the valid
PIV slots.

### Output

#### Table 1: List of Metadata Elements

|   Name    |                  Meaning                   |                                              Data                                               |              Slots              |
|:---------:|:------------------------------------------:|:-----------------------------------------------------------------------------------------------:|:-------------------------------:|
| Algorithm |            Algorithm of the key            | PIN, PUK, Triple DES, AES-128, AES-192, AES-256, <br/>RSA-1024, RSA-2048, ECC-P256, or ECC-P384 |            all slots            |
|  Policy   |            PIN and touch policy            |           PIN: Default, Never, Once, Always<br/>Touch: Default, Never, Always, Cached           | 9A, 9B, 9C, 9D, 9E, F9, 82 - 95 |
|  Origin   |           Imported or generated            |                                       imported/generated                                        |   9A, 9C, 9D, 9E, F9, 82 - 95   |
|  Public   |       Pub key partner to the pri key       |                                   DER encoding of public key                                    |   9A, 9C, 9D, 9E, F9, 82 - 95   |
|  Default  | Whether PIN/PUK/Mgmt Key has default value |                                     Default or Not Default                                      |           80, 81, 9B            |
|  Retries  |     Retry count and retries remaining      |                                           two numbers                                           |             80, 81              |

Another way to look at what is returned is the following table that lists which data
elements are returned for each slot.

#### Table 2: List of PIV slots and the metadata elements returned

|     Slot Number (hex)      |         Key         |           Data returned           |
|:--------------------------:|:-------------------:|:---------------------------------:|
|             80             |         PIN         |    Algorithm, Default, Retries    |
|             81             |         PUK         |    Algorithm, Default, Retries    |
|             9B             |     Management      |    Algorithm, Policy, Default     |
| 82, 83, ..., 95 (20 slots) |    Retired Keys     | Algorithm, Policy, Origin, Public |
|             9A             |   Authentication    | Algorithm, Policy, Origin, Public |
|             9C             |       Signing       | Algorithm, Policy, Origin, Public |
|             9D             |   Key Management    | Algorithm, Policy, Origin, Public |
|             9E             | Card Authentication | Algorithm, Policy, Origin, Public |
|             F9             |     Attestation     | Algorithm, Policy, Origin, Public |

### APDU

[Technical APDU Details](apdu/metadata.md)
___

## Get Bio metadata

This gets YubiKey's biometric metadata.

### Available

YubiKey Bio Multi-protocol 5.6 and later.

### SDK Classes

[GetBioMetadataCommand](xref:Yubico.YubiKey.Piv.Commands.GetBioMetadataCommand)

[GetBioMetadataResponse](xref:Yubico.YubiKey.Piv.Commands.GetBioMetadataResponse)

### Output

#### Table 1: List of Metadata Elements

|       Name       |                           Meaning                           |                              Data                               |
|:----------------:|:-----------------------------------------------------------:|:---------------------------------------------------------------:|
|   IsConfigured   | Whether the device is configured for biometric verification |                              bool                               |
| RetriesRemaining |   Number of remaining retries for biometric verification    | integer; zero value means the biometric verification is blocked |
| HasTemporaryPin  |            Whether a temporary PIN is generated             |                              bool                               |

### APDU

[Technical APDU Details](apdu/bio-metadata.md)
___

## Verify

Verify a PIN.

This is generally used in conjunction with other commands that require PIN entry to work.
For example, to sign data using one of the PIV private keys, it is possible the PIN is
needed. Use this command to enter the PIN, and if the operation is a success, execute the
sign command.

A YubiKey will allow "retry count" incorrect PIN verification attempts in a row, before it
blocks the PIN. For example, suppose the retry count is three. That means if you call
Verify with an incorrect PIN three times in a row, the PIN will be blocked. Any attempt
thereafter to verify the PIN will fail, so any operation that requires the PIN will not
run.

There is a "retry count" and a "remaining count". The retry count is the number of tries
the YubiKey will allow before locking the PIV application. The remaining count is the
current number of tries still left.

Note that when a PIV PIN is blocked, it is only the PIV application that will be unusable.
This has no effect on the other applications.

For example, suppose the program sets the retry count to 5. At that point, both the retry
and remaining count are 5. Now suppose someone tries to verify the PIN with the wrong
value. The remaining count drops to 4. The retry count is still 5, but at the moment there
are only 4 tries left before the PIN will be blocked.

If you call Verify with the incorrect PIN, the result will include the remaining count.
Once you call Verify with the correct PIN, the remaining count is reset to the retry
count.

Note that if the remaining count is more than 15, and the wrong PIN is given, the response
to the Verify command will indicate 15 retries remaining. This is because the YubiKey has
only 4 bits in its response to return the remaining retry count.

The default retry count is three, but you can change that using the
[Set PIN retries](#set-pin-retries) command. The retry count can be a value from 1 to 255.
You can also get the PIN retry numbers (retry count and remaining count) using the
[Get metadata](#get-metadata) command (valid on YubiKeys 5.3 and later).

It is possible that the remaining count is 0 before calling the verify PIN command. This
means the PIN has been blocked. If you call verify PIN again, even with the correct PIN,
the result will be `false` and the remaining count will remain 0.

If a PIN is blocked, it is possible to unblock it using the
[Reset Retry](#reset-retry-recover-the-pin) command. If the PUK is blocked as well, the
only recovery is to [Reset the PIV application](#reset-the-piv-application).

### Available

All YubiKeys with the PIV application.

### SDK Classes

[VerifyPinCommand](xref:Yubico.YubiKey.Piv.Commands.VerifyPinCommand)

[VerifyPinResponse](xref:Yubico.YubiKey.Piv.Commands.VerifyPinResponse)

### Input

The PIN to verify. It is six, seven, or eight characters (bytes) long.

### Output

An `int?`. If the PIN verifies, this will be NULL, if it did not verify, this will be the
remaining count (number of retries left before the PIN is blocked).

If the PIN is correct, there will be no remaining count (it will be null), and the
`Status` property will be `Success`.

If the PIN is not correct, the `int` returned will be the remaining count and the `Status`
property will be `AuthenticationRequired`. An incorrect PIN is not an (exception-throwing)
error. This Command determines if a PIN is correct or not, and if a PIN is incorrect, the
command performed its task.

Each time an incorrect PIN is entered, the remaining count is decremented. Once the
correct PIN has been entered, the remaining count is restored to the full retry count. See
also [Set PIN retries](#set-pin-retries).

Note that it is possible to have an error (such as malformed input data), and the PIN
could not be verified. In this situation the `Status` will be set to the appropriate
value and `GetData` will throw an exception.

### APDU

[Technical APDU Details](apdu/verify.md)
___

## Biometric verification

With biometric verification, users can authenticate the PIV session with a successful match of a fingerprint. To execute
biometric verification, the YubiKey must have biometrics configured and enabled. Clients can verify these conditions by
reading the properties of biometric metadata (see [Get Bio metadata](#get-bio-metadata)).

The YubiKey keeps track of failed biometric matches and will block biometric authentication if there are more than three
such failures. In that case, the client should use the [PIV PIN verification](#verify) as soon as possible. The number
of remaining biometric verification attempts is returned in the command response's `AttemptsRemaining` property. The
value is present only after a failed match.

Clients can also request to generate a temporary PIN, which can be used with the `VerifyTemporaryPinCommand` for
authentication without the need of a biometric match. The temporary PIN is stored in YubiKey's RAM and is invalidated
after the PIV session is closed or an invalid temporary PIN is used. For `PIN_OR_MATCH_ALWAYS` slot policy, the
temporary PIN can be used only once.

### Available

YubiKey Bio Multi-Protocol keys.

### SDK Classes

[VerifyUvCommand](xref:Yubico.YubiKey.Piv.Commands.VerifyUvCommand)

[VerifyUvResponse](xref:Yubico.YubiKey.Piv.Commands.VerifyUvResponse)

[VerifyTemporaryPinCommand](xref:Yubico.YubiKey.Piv.Commands.VerifyTemporaryPinCommand)

[VerifyTemporaryPinResponse](xref:Yubico.YubiKey.Piv.Commands.VerifyTemporaryPinResponse)

### Input

#### VerifyUvCommand

Two boolean values:

- **request temporary PIN** - if true, the YubiKey will wait for the user to perform biometric verification (match an
  enrolled fingerprint) and, if verification is successful, generate a temporary PIN.
- **check only** - when true, the YubiKey verifies internally that the biometric state is valid. No biometric
  verification is performed on the YubiKey.

A client application would typically call the command with `false`, `false` parameters - this will make the YubiKey
request the biometric verification from the users.

#### VerifyTemporaryPinCommand

The temporary PIN is the only parameter.

### Output

#### VerifyUvResponse

If a temporary PIN was requested and the status is Success, the returned value is the temporary PIN.
In case of failure (for example, the fingerprint did not match), the clients should read the `AttemptsRemaining`
property, which contains the number of remaining biometric attempts.

#### VerifyTemporaryPinResponse

No output. The Status will be Success if the temporary PIN was verified.

### APDU

[Technical APDU Details for VerifyUvCommand](apdu/verify-uv.md)

[Technical APDU Details for VerifyTemporaryPinCommand](apdu/verify-temporary-pin.md)
___

## Authenticate: management key

The Authenticate command can be used to perform several cryptographic operations:

* Authenticate the Management Key to the YubiKey
* Sign data using a private key
* Decrypt data using a private key
* Perform key agreement using a private key

This section discusses authenticating the management key. See
[Authenticate: sign](#authenticate-sign),
[Authenticate: decrypt](#authenticate-decrypt), and
[Authenticate: key agreement](#authenticate-key-agreement)
for information on signing, decrypting, and key agreement.

The primary purpose of this command is to authenticate the client application (off-card
application) to the YubiKey. That is, before the YubiKey is able to perform some
operations, it must know that the caller has access to the management key. This section
will refer to this action as <b>Client Authentication</b>.

It is also possible to authenticate the YubiKey to the client application, so that the
app knows it is communicating with the appropriate YubiKey. Maybe the app wants to be sure
it will not call on an attacker's YubiKey to perform a sensitive operation. This section
will refer to this action as <b>YubiKey Authentication</b>.

Hence, the authenticate management key command can actually perform two different
operations: "single authentication" (Client Authentication only), or "mutual authentication"
(Client Authentication and YubiKey Authentication). How you call the API
determines which operation will be performed.

The authentication is done using "challenge-response". Note that the word "response" is
used in "Response APDU" and "Response Class". So to avoid ambiguity, these are the terms
we will use in this section.

* Response APDU
* Response Class or Response Object (e.g. InitializeAuthenticateManagementKeyResponse is a
  Response Class and an instantiation of that class is a Response Object)
* Client Authentication Challenge/Response (the Challenge-Response pair associated with Client Authentication)
* YubiKey Authentication Challenge/Response (the Challenge-Response pair associated with YubiKey Authenticaiton)

The process is the following:

|                                Single Authentication                                |                                                                                              |
|:-----------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------:|
|                          **Client (Off-Card) Application**                          |                                         **YubiKey**                                          |
|                                      *Step 1*                                       |                                                                                              |
|                         Initiate the process (single auth)                          |                                                                                              |
|                                                                                     |                       Generate random Client Authentication Challenge                        |
|                                      *Step 2*                                       |                                                                                              |
| Compute the Client Authentication Response based on Client Authentication Challenge |                                                                                              |
|                                                                                     |                          Verify the Client Authentication Response                           |
|                                                                                     |                         Return `Success` or `AuthenticationRequired`                         |
|                                                                                     |                                                                                              |
|                              **Mutual Authentication**                              |                                                                                              |
|                          **Client (Off-Card) Application**                          |                                         **YubiKey**                                          |
|                                      *Step 1*                                       |                                                                                              |
|                         Initiate the process (mutual auth)                          |                                                                                              |
|                                                                                     |                       Generate random Client Authentication Challenge                        |
|                                      *Step 2*                                       |                                                                                              |
| Compute the Client Authentication Response based on Client Authentication Challenge |                                                                                              |
|                  Generate random YubiKey Authentication Challenge                   |                                                                                              |
|                                                                                     |                          Verify the Client Authentication Response                           |
|                                                                                     |    Compute the YubiKey Authentication Response based on YubiKey Authentication Challenge     |
|                                                                                     | Return `Success` or `AuthenticationRequired`<br />along with YubiKey Authentication Response |
|                                      *Step 3*                                       |                                                                                              |
|                     Verify the YubiKey Authentication Response                      |                                                                                              |

### Available

All YubiKeys with the PIV application.

Beginning with YubiKey 5.4.2, the management key can be an AES key.

### SDK Classes

[InitializeAuthenticateManagementKeyCommand](xref:Yubico.YubiKey.Piv.Commands.InitializeAuthenticateManagementKeyCommand)

[InitializeAuthenticateManagementKeyResponse](xref:Yubico.YubiKey.Piv.Commands.InitializeAuthenticateManagementKeyResponse)

[CompleteAuthenticateManagementKeyCommand](xref:Yubico.YubiKey.Piv.Commands.CompleteAuthenticateManagementKeyCommand)

[CompleteAuthenticateManagementKeyResponse](xref:Yubico.YubiKey.Piv.Commands.CompleteAuthenticateManagementKeyResponse)

### Input

Authenticating with the management key requires two calls (send two APDUs). It will be a
challenge-response process. The first call will take the algorithm, the second will take
in the response APDU from the first call, the management key, and the algorithm.

### Output

The output of the YubiKey depends on whether the process is single or mutual
authentication, and step 1 or step 2.

* Single Auth, Step 1: output is Client Authentication Challenge
* Single Auth, Step 2: output is the result of verifying the Client Authentication Response
* Mutual Auth, Step 1: output is Client Authentication Challenge
* Mutual Auth, Step 2: output is YubiKey Authentication Response (to be verified by the client
  (off-card) application) and the result of Client Authentication

The output of the Response classes is the following.

* Single Auth, Step 1: output is Client Authentication Challenge
* Single Auth, Step 2: output is the result of verifying the Client Authentication Response
* Mutual Auth, Step 1: output is Client Authentication Challenge
* Mutual Auth, Step 2: output is the result of Client Authentication,
  and, if Client Authentication was successful, YubiKey Authentication Response

The process is explained in the documentation for
[CompleteAuthenticateManagementKeyCommand](xref:Yubico.YubiKey.Piv.Commands.CompleteAuthenticateManagementKeyCommand)

### APDU

[Technical APDU Details](apdu/auth-mgmt.md)
___

## Set PIN retries

Set the number of PIN retries allowed before the PIN and PUK are blocked. The default is
three. Note that this command will set the retry count for both the PIN and PUK. If you
want to set the retry count for only one entity, you must still set the retry count for
the other.

Note also that this will reset the PIN and PUK to the default values. For example,

```
  current PIN: 7777777    retry count: 3
  current PUK: 88888888   retry count: 3

Call this command to set the PIN retry count to 5 and the PUK retry count to 2.
After successful completion of the command, the PIN and PUK are the following.

  current PIN: 123456     retry count: 5
  current PUK: 12345678   retry count: 2
```

If you don't want to leave the PIN and PUK as the default values, follow this command with
the [Change reference data](#change-reference-data) command.

Before the YubiKey can set the PIN retries, the caller must have authenticated the
management key and verified the PIN. See the User's Manual
[entry on PIV commands access control](access-control.md) for information on how to
authenticate with the management key and/or PIN for commands. See also the sections in
this page on [Authenticate: management key](#authenticate-management-key) and
[Verify the PIN](#verify).

### Available

All YubiKeys with the PIV application.

### SDK Classes

[SetPinRetriesCommand](xref:Yubico.YubiKey.Piv.Commands.SetPinRetriesCommand)

[SetPinRetriesResponse](xref:Yubico.YubiKey.Piv.Commands.SetPinRetriesResponse)

### Input

The management key, and the number of retries for the PIN and the number of retries for
the PUK.

### Output

There is no data output, only the status.

If the command was not successful, it will almost certainly be because the management key
or PIN supplied was not correct. This command will return an error indicating
authentication is required. But it will not report which element (management key or PIN)
was incorrect. Generally you will not call this command until you have successfully
authenticated the management key and verified the PIN.

### APDU

[Technical APDU Details](apdu/set-pin-retries.md)
___

## Change Reference Data

Change a PIN or PUK (PIN Unblocking Key). The term "reference data" in this case refers
to the "authentication data".

According to NIST 800-73, there are three possible reference data elements that can be
changed using this command:

* Global PIN
* PIV PIN
* PUK

However, the YubiKey does not have a global PIN. Hence, the SDK will only support
changing the PIV PIN and PUK using this command.

To change reference data, supply the current value and the new value. For example, to
change the PIN, provide the current PIN and the new PIN. If the current PIN provided is
not correct, the PIN will not be changed.

A YubiKey will allow "retry count" incorrect PIN verification attempts in a row, before it
blocks the PIN. For example, suppose the retry count is three. That means if you call
change reference data with an incorrect PIN three times in a row, the PIN will be blocked.
Any attempt thereafter to verify the PIN will fail, so the change reference data command
will fail.

If you call change reference data with the incorrect PIN or PUK, the result will include
the remaining count (the number of retries left before it is blocked). Once you call
change reference data with the correct PIN or PUK, the remaining count is reset to the
full retry count.

For example, suppose the retry count is 5, and no invalid PINs have been tried. Now if you
call change reference data with the wrong PIN, the remaining count is 4 (the retry count is
still 5). Call change reference data with the correct PIN and the remaining count is 5
again.

Note that if the remaining count is more than 15, and the wrong PIN or PUK is given, the
response to the change reference data command will indicate 15 retries remaining. This is
because the YubiKey has only 4 bits in its response to return the remaining count.

The default retry count is three, but you can change that using the
[Set PIN retries](#set-pin-retries) command. The retry count can be a value from 1 to 255.
You can also get the PIN retry numbers (total count and current remaining) using the
[Get metadata](#get-metadata) command (valid on YubiKeys 5.3 and later).

It is possible that the remaining count is 0 before calling the change reference data
command. This means the PIN or PUK has been blocked. If you call change reference data
again, even with the correct PIN or PUK, the result will be `false` and the remaining
count will remain 0.

If a PIN is blocked, it is possible to unblock it using the
[Reset Retry](#reset-retry-recover-the-pin) command. If the PUK is blocked as well, the
only recovery is to [Reset the PIV application](#reset-the-piv-application).

### Available

All YubiKeys with the PIV application.

### SDK Classes

[ChangeReferenceDataCommand](xref:Yubico.YubiKey.Piv.Commands.ChangeReferenceDataCommand)

[ChangeReferenceDataResponse](xref:Yubico.YubiKey.Piv.Commands.ChangeReferenceDataResponse)

### Input

Which reference data element to change (PIN or PUK), the current reference value, and
the new value.

Both the PIN and PUK are allowed to be 6 to 8 characters.

### Output

An `int?`. If the PIN or PUK was successfully changed, this will be NULL, if the correct,
current PIN or PUK was not supplied, this will be the remaining count (the number of
retries before the PIN or PUK is blocked).

If the current reference data supplied is correct, there will be no remaining count (the
return will be null), and the `Status` property will be `Success`,

If the current reference data supplied is not correct, the `int` returned will be the
remaining count and the `Status` property will be `AuthenticationRequired`. An incorrect
PIN/PUK is not an (exception-throwing) error. This Command determines if a PIN/PUK is
correct or not, and if it is correct, changes to the new value. If the PIN/PUK is
incorrect, the command will return that information, so it has performed its task.

Note that it is possible to have an error (such as malformed input data), and the
operation could not be performed. The `Status` property in the Response object will be
`Failed`.

### APDU

[Technical APDU Details](apdu/change-ref.md)
___

## Set management key

Set the management key to a new value.

The YubiKey is manufactured with a default PIV management key:
`hex 010203040506070801020304050607080102030405060708` (`0102030405060708` three times). If
you want to change to a different key, use this command.

You can also set the management key to a newer value after changing it from the default.
You can use this to change the PIN and touch policies as well. If you supply the same key
as before, just new PIN and/or touch policies, this will leave the key the same and change
only the PIN and touch policies.

Before the YubiKey can set the management key, the caller must have authenticated the
current management key. See the User's Manual entry on
[PIV commands access control](access-control.md) for information on how to
authenticate with the management key for commands. See also the section in this page on
[Authenticate: management key](#authenticate-management-key).

### Available

All YubiKeys with the PIV application, although require touch is available on only 4 and 5.

Beginning with YubiKey 5.4.2, the management key can be an AES key.

### SDK Classes

[SetManagementKeyCommand](xref:Yubico.YubiKey.Piv.Commands.SetManagementKeyCommand)

[SetManagementKeyResponse](xref:Yubico.YubiKey.Piv.Commands.SetManagementKeyResponse)

### Input

The new key, a touch policy, and the algorithm. This command must be used in conjunction
with the Authenticate command. See
["Authenticate: Management Key"](#authenticate-management-key).

For YubiKeys prior to 5.4.2, the new key must be a Triple-DES key. Beginning with 5.4.2,
the managment key can be AES.

If the key is Triple-DES, the new key must be 24 bytes, no more, no less. If the key is
AES-128, the new key must be 16 bytes, if AES-192, 24 bytes, and if AES-256, 32 bytes. The
touch policy is one of three values: always, cached, or never.

### Output

There is no data output, only the status.

If the command was not successful, it will almost certainly be because the current
management key was not authenticated. This command will return an error indicating
authentication is required. Generally you will not call this command until you have
successfully authenticated the management key.

### APDU

[Technical APDU Details](apdu/set-mgmt-key.md)
___

## Reset Retry (Recover the PIN)

Reset the PIN.

This is the command to recover the PIN, using the PUK (PIN Unblocking Key), if the PIN
has been lost (the user forgot the PIN).

This is similar to the [Change Reference Data](#change-reference-data) command. That
command can change the PIN if the current PIN is known. This command can change the PIN
(or reset it) if the current PIN is unknown, but the PUK is known.

This can be run no matter what the remaining count of the PIN is.

If you call reset retry with the incorrect PUK, the result will include the PUK's
remaining count (the number of retries left before it is blocked). Once you call reset
retry with the correct PUK, the remaining count is reset to its full retry count.

For example, suppose the retry count is 5, and no invalid PUKs have been tried. Now if you
call reset retry with the wrong PUK, the retry count is still 5, but the remaining count
is 4. Call reset retry with the correct PUK and the retry count is still 5, and the
remaining count is 5 again.

Note that if the remaining count is more than 15, and the wrong PUK is given, the response
to the reset retry command will indicate 15 retries remaining. This is because the YubiKey
has only 4 bits in its response to return the remaining count.

The default retry count is three, but you can change that using the
[Set PIN retries](#set-pin-retries) command. The retry count can be a value from 1 to 255.
You can also get the PUK retry numbers (total count and current remaining) using the
[Get metadata](#get-metadata) command (valid on YubiKeys 5.3 and later).

It is possible that the remaining count is 0 before calling the reset retry command. This
means the PUK has been blocked. If you call reset retry again, even with the correct PUK,
the result will be `false` and the remaining count will remain 0.

If the PUK is blocked, the only recovery is to
[Reset the PIV application](#reset-the-piv-application).

### Available

All YubiKeys with the PIV application.

### SDK Classes

[ResetRetryCommand](xref:Yubico.YubiKey.Piv.Commands.ResetRetryCommand)

[ResetRetryResponse](xref:Yubico.YubiKey.Piv.Commands.ResetRetryResponse)

### Input

The PUK and the new PIN.

### Output

An `int?`. If the PUK is correct, the PIN will be changed and this will be NULL. If the
PIN was not changed, it will be the remaining count.

If the current reference data supplied is correct, there will be no remaining count (the
return will be null), and the `Status` property will be `Success`,

If the PUK supplied is not correct, the `int` returned will be the remaining count and the
`Status` property will be `AuthenticationRequired`. An incorrect PUK is not an
(exception-throwing) error. This Command determines if a PUK is correct or not, and if it
is correct, changes the PIN to the new value. If the PUK is incorrect, the command
performed its task.

Each time an incorrect PUK is entered, the PUK's remaining count is decremented. Once the
correct PUK has been entered, the remaining count is restored to the full retry count. See
also [Set PIN retries](#set-pin-retries).

Note that it is possible to have an error (such as malformed input data), and the
operation could not be performed. The `Status` property in the Response object will be
`Failed`.

### APDU

[Technical APDU Details](apdu/reset-retry.md)
___

## Generate Asymmetric

Generate a new asymmetric key pair and store it in one of the asymmetric key slots.

If a slot is empty, the new generated key pair goes into that slot. If the slot already
contains a key, the new key pair replaces the old one. That old key will be gone and there
will be nothing you can do to recover it. There is no way to save the old key pair (e.g.
move it to a retired slot) before generating the new key pair, so it will be lost. Hence,
use this command with caution.

You can generate a new key pair in any slot that holds asymmetric keys, including the
slots described as holding retired keys.

Note that you can generate a new key in slot `F9`, which holds the attestation key. If you
do so, however, you could lose the ability to create an attestation statement, unless you
obtain, for the new key in `F9`, a proper attestation certificate that chains to a
supported root.

### Available

All YubiKeys with the PIV application.

### SDK Classes

[GenerateKeyPairCommand](xref:Yubico.YubiKey.Piv.Commands.GenerateKeyPairCommand)

[GenerateKeyPairResponse](xref:Yubico.YubiKey.Piv.Commands.GenerateKeyPairResponse)

### Input

The management key, slot number, algorithm, key size, PIN policy, and touch policy.

The YubiKey supports RSA 1024 and 2048, along with ECC P-256 and P-384.

### Output

The public key partner to the private key now residing in the given slot.

### APDU

[Technical APDU Details](apdu/generate-pair.md)
___

## Import Asymmetric

Import an asymmetric key, that was generated outside the YubiKey, into one of the
asymmetric key slots.

To load the associated public key and/or cert, use the PUT DATA command.

If a slot is empty, the imported key goes into that slot. If the slot already contains a
key, the new key replaces the old one. That old key will be gone and there will be nothing
you can do to recover it. There is no way to save the old key (e.g. move it to a retired
slot) before importing the new key, so it will be lost. Hence, use this command with
caution.

Note that you can import a new key in slot `F9`, which holds the attestation key. If you
do so, however, you could lose the ability to create an attestation statement, unless you
obtain, for the key in `F9`, a proper attestation certificate that chains to a supported
root.

### Available

All YubiKeys with the PIV application.

### SDK Classes

[ImportAsymmetricKeyCommand](xref:Yubico.YubiKey.Piv.Commands.ImportAsymmetricKeyCommand)

[ImportAsymmetricKeyResponse](xref:Yubico.YubiKey.Piv.Commands.ImportAsymmetricKeyResponse)

### Input

The management key, the slot number, PIN policy, touch policy, and the new key.

### Output

`bool`: was the private key successfully imported?

### APDU

[Technical APDU Details](apdu/import-asym.md)
___

## Authenticate: sign

The Authenticate command can be used to perform several cryptographic operations:

* Authenticate the Management Key to the YubiKey
* Sign data using a private key
* Decrypt data using a private key
* Perform key agreement using a private key

This section discusses signing using a private key. See
[Authenticate: management key](#authenticate-management-key),
[Authenticate: decrypt](#authenticate-decrypt), and
[Authenticate: key agreement](#authenticate-key-agreement)
for information on authenticating the management key, decrypting, and key agreement.

This command signs arbitrary data. The signature process generally involves digesting the
data to sign and then computing a signature based on that digest and the private key. This
command does not perform the digest, only the private key operations.

### Available

All YubiKeys with the PIV application.

### SDK Classes

[AuthenticateSignCommand](xref:Yubico.YubiKey.Piv.Commands.AuthenticateSignCommand)

[AuthenticateSignResponse](xref:Yubico.YubiKey.Piv.Commands.AuthenticateSignResponse)

### Input

The PIN (maybe), the slot number, and the digest of the data to sign, possibly formatted.
The data to sign is digested outside the YubiKey.

Whether the PIN or touch is required before the YubiKey will sign is dependent on the PIN
and touch policies specified when the key pair was generated or imported. See the sections
on [generating key pairs](#generate-asymmetric) and [importing keys](#import-asymmetric),
along with [PIV PIN and touch policies](pin-touch-policies.md). For example, if a key
pair was generated with the PIN policy of "Never", then no PIN will be required to sign.
However, most applications will likely set the policy to "Always" or "Once".

Slot `9C` is the digital signature slot, although the YubiKey will sign using any slot
holding a private key, other than `F9` (F9 holds the attestation key, which can sign a
a certificate it creates, so it can sign, however, it cannot sign arbitrary data, only).

For RSA signatures, the digest must be formatted into a block. The block will be the same
size as the key. The two block formats allowed are PKCS 1 v 1.5 or PKCS 1 PSS.

For example, if using PKCS 1 v 1.5, before calling, build the following block.

```
  formatted digest = 00 01 FF FF ... FF 00 \<DER of DigestInfo\>

  For a 2048-bit key, the block is 256 bytes long (the leading 00 byte is one of the 256).

  If the digest algorithm is SHA-256, the DER of the DigestInfo will be 49 bytes long:

  30 2f
     30 0b
        06 09
           60 86 48 01 65 03 04 02 01
     04 20
        <32-byte digest>

  The block to pass to the YubiKey will be

  00 01 FF FF ... FF 00 \<49-byte DER of DigestInfo\>
        ^          ^
        |          |
        -------------- 204 bytes of 0xFF
```

PSS (Probabilistic Signature Scheme) is much more complicated. If you want to learn how to
build a PSS block, see RFC 8017. The formatted digest will appear to be simply random
bytes.

For ECC signatures, simply provide the digest. No DER encoding, just the digest.

If the key is EccP256, the digest must be 256 bits (32 bytes) or shorter. You will
generally use SHA-256.

If the key is EccP384, the digest must be 384 bits (48 bytes) or shorter. You will
generally use SHA-384.

The actual APDU will format the data you provide even further:

```
  7C len1 82 00 81 len2 \<digest block\>
```

However, you will not need to worry about this.

### Output

The signature.

### APDU

[Technical APDU Details](apdu/auth-sign.md)
___

## Authenticate: decrypt

The Authenticate command can be used to perform several cryptographic operations:

* Authenticate the Management Key to the YubiKey
* Sign data using a private key
* Decrypt data using a private key
* Perform key agreement using a private key

This section discusses decrypting arbitrary data. See
[Authenticate: management key](#authenticate-management-key),
[Authenticate: sign](#authenticate-sign), and
[Authenticate: key agreement](#authenticate-key-agreement)
for information on authenticating the management key, signing, and key agreement.

Decryption with a private key is possible only if the key in the slot is RSA.

If the key in the slot is ECC, calling this command will produce an exception.

### Available

All YubiKeys with the PIV application.

### SDK Classes

[AuthenticateDecryptCommand](xref:Yubico.YubiKey.Piv.Commands.AuthenticateDecryptCommand)

[AuthenticateDecryptResponse](xref:Yubico.YubiKey.Piv.Commands.AuthenticateDecryptResponse)

### Input

The PIN (maybe), the slot number, and the corresponding party's public key.

Whether the PIN or touch is required before the YubiKey will perform key agreement is
dependent on the PIN and touch policies specified when the key pair was generated or
imported. See the sections on [generating key pairs](#generate-asymmetric) and
[importing keys](#import-asymmetric), along with
[PIV PIN and touch policies](pin-touch-policies.md). For example, if a key pair was
generated with the PIN policy of "Never", then no PIN will be required to sign. However,
most applications will likely set the policy to "Always" or "Once".

Slot `9D` is the Key Management slot, although the YubiKey will perform key agreement
using any slot holding a private key, other than `F9` (the attestation key, which can only
sign a certificate it creates).

The input data must be the same size as the key. For example, if the key is RSA-2048, the
input data (the data to decrypt) must be 256 bytes. If the data is shorter than the key,
prepend as many 00 bytes as needed to make it the correct size.

### Output

The decrypted data. The result will likely be formatted following PKCS 1 v. 1.5 or OAEP.
It is the responsibility of the caller to extract the actual plaintext from the formatted
result.

### APDU

[Technical APDU Details](apdu/auth-decrypt.md)
___

## Authenticate: key agreement

The Authenticate command can be used to perform several cryptographic operations:

* Authenticate the Management Key to the YubiKey
* Sign data using a private key
* Decrypt data using a private key
* Perform key agreement using a private key

This section discusses performing key agreement using a private key. See
[Authenticate: management key](#authenticate-management-key),
[Authenticate: sign](#authenticate-sign), and
[Authenticate: decrypt](#authenticate-decrypt),
for information on authenticating the management key, signing, and decrypting.

Key agreement is possible only if the key in the slot is ECC. If so, this command will
perform the EC Diffie-Hellman Key Agreement protocol, phase 2.

If the key in the slot is RSA, calling this command will produce an exception.

### Available

All YubiKeys with the PIV application.

### SDK Classes

[AuthenticateKeyAgreeCommand](xref:Yubico.YubiKey.Piv.Commands.AuthenticateKeyAgreeCommand)

[AuthenticateKeyAgreeResponse](xref:Yubico.YubiKey.Piv.Commands.AuthenticateKeyAgreeResponse)

### Input

The PIN (maybe), the slot number, and the corresponding party's public key.

Whether the PIN or touch is required before the YubiKey will perform key agreement is
dependent on the PIN and touch policies specified when the key pair was generated or
imported. See the sections on [generating key pairs](#generate-asymmetric) and
[importing keys](#import-asymmetric), along with
[PIV PIN and touch policies](pin-touch-policies.md). For example, if a key pair was
generated with the PIN policy of "Never", then no PIN will be required to sign. However,
most applications will likely set the policy to "Always" or "Once".

Slot `9D` is the Key Management slot, although the YubiKey will perform key agreement
using any slot holding a private key, other than `F9` (the attestation key, which can only
sign a certificate it creates).

The input data must be the other party's public point, and must include both the x- and
y-coordinates. It will be in the form

```
  04 || x-coordinate || y-coordinate

  where each coordinate is the same size as the key. For example, if the key
  in the slot is ECC-P256, then each coordinate must be 32 bytes. Prepend 00
  bytes if necessary. The total length will be 65 bytes.
```

### Output

The result of ECDH phase 2, which is the shared secret.

The result will be the same size as the key. For example, if the key is ECC-P384, the
result will be 48 bytes, possibly with leading 00 bytes. It happens to be the x-coordinate
of the point result of ECC scalar multiplication, which is phase 2 of ECDH. It is the
shared secret itself, no tag or length octets.

### APDU

[Technical APDU Details](apdu/auth-key-agree.md)
___

## Create attestation statement

Create an attestation statement for a key that had been generated by the YubiKey. See the
article on [attestation](attestation.md).

This command will instruct the YubiKey to build and return an attestation statement. An
attestation statement is an X.509 certificate. This certificate is signed by the
attestation key.

The cert returned will affirm that a private key was generated on the YubiKey, and not
imported. The private keys that can be attested are those in slots `9A`, `9C`, `9D`, `9E`
and `82` - `95`.

> [!NOTE]
> In version 1.0.0 of the SDK, it was not possible to create an attestation statement for
> keys in slots 82 - 95 (retired key slots). Beginning with version 1.0.1 of the SDK it is
> possible to create an attestation statement for the keys in those slots.

The private key that will sign this newly-created certificate (the attestation statement)
is the attestation key in slot `F9`. This slot also contains the attestation certificate.

The attestation key is generated at the time of manufacture. The same attestation key is
loaded onto many YubiKeys. At the same time it is generated and loaded onto YubiKeys, a
certificate for it is built, signed by the YubiKey PIV Certificate Authority. The CA cert
is signed by the YubiKey root.

To obtain the YubiKey CA and root certs, visit the
[Yubico Developer's PIV Attestation website](https://developers.yubico.com/PIV/Introduction/PIV_attestation.html).

So to verify that a private key is indeed attested, extract the public key from the
attestation statement and verify that it is the appropriate public key (this is generally
done by verifying a signature), extract the serial number (if part of the attestation
statement) and verify it is the serial number of the YubiKey in question, and finally,
verify the certificate. To verify the certificate, use the attestation cert (acquired by
using the GET DATA command), the YubiKey PIV CA cert, and the YubiKey root cert.

```txt
      Yubico Root Cert
             |
             |
     Yubico PIV CA Cert
             |
             |
      Attestation Cert
         (Slot F9)
   (from Get Data command)
             |
             |
   Attestation Statement
   (an X.509 certificate)
```

Note that each time this command is executed, a new cert will be created.

### Available

YubiKey 4.3 and later.

### SDK Classes

[CreateAttestationStatementCommand](xref:Yubico.YubiKey.Piv.Commands.CreateAttestationStatementCommand)

[CreateAttestationStatementResponse](xref:Yubico.YubiKey.Piv.Commands.CreateAttestationStatementResponse)

### Input

Slot number.

### Output

The DER encoding of an X.509 certificate signed by the attestation key, asserting that the
key in the given slot was generated by a YubiKey.

The public key in the certificate is the public key partner to the private key in the
specified slot, and an extension in the certificate is the serial number of the YubiKey
itself. Therefore, it is possible to attest that the specific private key was generated by
the specific YubiKey.

### APDU

[Technical APDU Details](apdu/attest.md)
___

## Get Data

Get a data element from the YubiKey.

There are a number of data elements that are retrievable using a command specifically
for that element (e.g. serial number). However, for other elements, it is necessary to
use the GET DATA command.

GET DATA is general purpose (i.e. it is not unique to the PIV application). In fact, you
will likely find GET DATA commands in other areas (FIDO commands, Inter-Industry
commands, etc.). However, for the PIV command namespace, there is a set of classes that
represent a GET DATA command that gets specific PIV info. That is, this class will be able
to construct a specific subset of GET DATA APDUs related to PIV, but not an "arbitrary"
GET DATA command.

See also the User's Manual entry on the
[GET and PUT DATA commands](get-and-put-data.md).

### Available

All YubiKeys with the PIV application.

### SDK Classes

[GetDataCommand](xref:Yubico.YubiKey.Piv.Commands.GetDataCommand)

[GetDataResponse](xref:Yubico.YubiKey.Piv.Commands.GetDataResponse)

### Input

A "tag" specifying which data element to get. Tables 4x list which data objects will be
supported in the PIV GET DATA command class. It also contains links to descriptions of the
data returned.

Some tags require PIN authentication as well. The tags that do are listed in tables 4x.

### Output

Data elements based on the input tag. The SDK returns the data as a byte array. It is the
responsibility of the caller to further parse that result.

<a name="getdatatable"></a>
Table 4A lists PIV standard elements that the YubiKey will possess upon manufacture (as
long as the PIV application is initialized).

Table 4B lists PIV standard elements that the YubiKey will not possess upon manufacture.
Requesting these elements using GET DATA will return "NoData". If you want these elements
to contain data, you will have to load them using PUT DATA.

#### Table 4A: PIV GET DATA elements available upon manufacture

|   Name    | Tag |              Meaning              |  Authentication<br/>Required   |              Data Returned              |
|:---------:|:---:|:---------------------------------:|:------------------------------:|:---------------------------------------:|
| DISCOVERY | 7E  | PIV AID plus<br/>PIN usage policy | PUT: not allowed<br/>GET: none | [Encoded discovery](#encoded-discovery) |

#### Table 4B: PIV GET DATA elements empty upon manufacture

|           Name            |                Tag                |                 Meaning                  |   Authentication<br/>Required    |                   Data Returned                   |
|:-------------------------:|:---------------------------------:|:----------------------------------------:|:--------------------------------:|:-------------------------------------------------:|
|      AUTHENTICATION       |             5F C1 05              |         Cert for key in slot 9A          |   PUT: mgmt key<br/>GET: none    |    [Encoded certificate](#encoded-certificate)    |
|         SIGNATURE         |             5F C1 0A              |         Cert for key in slot 9C          |   PUT: mgmt key<br/>GET: none    |    [Encoded certificate](#encoded-certificate)    |
|      KEY MANAGEMENT       |             5F C1 0B              |         Cert for key in slot 9D          |   PUT: mgmt key<br/>GET: none    |    [Encoded certificate](#encoded-certificate)    |
|         CARD AUTH         |             5F C1 01              |         Cert for key in slot 9E          |   PUT: mgmt key<br/>GET: none    |    [Encoded certificate](#encoded-certificate)    |
| RETIRED1 to<br/>RETIRED20 | 5F C1 0D<br/>through<br/>5F C1 20 |              Retired certs               |   PUT: mgmt key<br/>GET: none    |    [Encoded certificate](#encoded-certificate)    |
|           CHUID           |             5F C1 02              |     Cardholder Unique<br/>Identifier     |   PUT: mgmt key<br/>GET: none    |          [Encoded CHUID](#encoded-chuid)          |
|        CAPABILITY         |             5F C1 07              |   Card Capability<br/>Container (CCC)    |   PUT: mgmt key<br/>GET: none    |            [Encoded CCC](#encoded-ccc)            |
|          PRINTED          |             5F C1 09              |   Information printed<br/>on the card    |    PUT: mgmt key<br/>GET: PIN    |        [Encoded printed](#encoded-printed)        |
|         SECURITY          |             5F C1 06              |             Security object              |   PUT: mgmt key<br/>GET: none    |   [Encoded security](#encoded-security-object)    |
|        KEY HISTORY        |             5F C1 0C              |         Info about retired keys          |   PUT: mgmt key<br/>GET: none    |    [Encoded key history](#encoded-key-history)    |
|           IRIS            |             5F C1 21              |          Cardholder iris images          |    PUT: mgmt key<br/>GET: PIN    |    [Encoded iris images](#encoded-iris-images)    |
|       FACIAL IMAGE        |             5F C1 08              |         Cardholder facial image          |    PUT: mgmt key<br/>GET: PIN    |   [Encoded facial image](#encoded-facial-image)   |
|       FINGERPRINTS        |             5F C1 03              |         Cardholder fingerprints          |    PUT: mgmt key<br/>GET: PIN    |   [Encoded fingerprints](#encoded-fingerprints)   |
|           BITGT           |               7F 61               | Biometric Information<br/>Group Template | PUT: not supported<br/>GET: none |          [Encoded BITGT](#encoded-bitgt)          |
|         SM SIGNER         |             5F C1 22              | Secure Messaging<br/>Certificate Signer  |   PUT: mgmt key<br/>GET: none    | [Encoded SM cert signer](#encoded-sm-cert-signer) |
|        PC REF DATA        |             5F C1 23              |     Pairing Code<br/>Reference Data      |   PUT: mgmt key<br/>GET: none    |         [Encoded PC Ref](#encoded-pc-ref)         |

All the tags supported are one, two, or three bytes long. The APDU data contains the tag,
each constructed as a TLV itself. That is, there is a DER TLV with a T of `5C`, and a V of
the GET DATA tag. Unfortunately, that is the terminology used in the standard. There is a
tag for TLV and a value that is itself called a "tag".

```
 5C 01 7E
 5C 02 7F 61
 5C 03 5F C1 xx
```

#### Encoded certificate

If the certificate retrieved is the attestation statement, it is returned encoded as follows.

```
  53 L1
     70 L2
        --X.509 certificate--

The X.509 certificate is the DER encoding of the ASN.1 definition "Certificate"
from the X.509 standard (see RFC 5280).
```

All other certificates are returned as specified in the PIV standard:

```
  53 L1
     70 L2
        --X.509 certificate--
     71 01
        00 (compression)
     FE 00 (LRC)

The X.509 certificate is the DER encoding of the ASN.1 definition "Certificate"
from the X.509 standard (see RFC 5280).

The 71 01 00 means the certificate itself is uncompressed. If it were 71 01 01, it would
mean the certificate were gzipped.

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.
```

#### Encoded CHUID

As specified in the PIV standard:

```
53 3B
   30 19
      --FASC-N, fixed at 25 bytes--
   34 10
      --GUID, fixed at 16 bytes--
   35 08
      --expiration data, ASCII YYYYMMDD, fixed at 8 bytes--
   3E 00 (Issuer Asymmetric Signature, max 2816 bytes, unused in YubiKey)
   FE 00 (LRC, unused in PIV)

The FASC-N is a value generated following the TIG SCEPACS standard (a smart card
standard).

The GUID is a value generated following RFC 4122.

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.
```

#### Encoded CCC

As specified in the PIV standard:

```
53 33
   F0 15 (card identifier, fixed at 21 bytes)
      A0 00 00 01 16 FF 02
      --14 random bytes--
   F1 01
      21 (container version number)
   F2 01
      21 (grammar version number)
   F3 00 (unused by YubiKey)
   F4 01
      00 (PKCS 15 support, YubiKey does not support)
   F5 01
      10 (Data model number)
   F6 00 (unused by YubiKey)
   F7 00 (unused by YubiKey)
   FA 00 (unused by YubiKey)
   FB 00 (unused by YubiKey)
   FC 00 (unused by YubiKey)
   FD 00 (unused by YubiKey)
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

#### Encoded discovery

As specified in the PIV standard:

```
7E 12
   4F 0B
      A0 00 00 03 08 00 00 10 00 01 00 (Application AID, fixed)
   5F 2F 02
      40 00 (PIN Usage Policy, the only policy YubiKey supports)
```

#### Encoded printed

As specified in the PIV standard:

```
53 L1
   01 len
      --Name, ASCII text, up to 125 bytes--
   02 len
      --Employee afiliation, ASCII text, up to 20 bytes--
   04 len
      --Expiration date, ASCII numbers YYYYMMMDD, fixed at 9 bytes--
   05 len
      --Agency Card Serial Number, ASCII text, up to 20 bytes--
   06 len
      --Issuer Id, ASCII text, up to 15 bytes--
   07 len
      --Org affiliation, line 1, ASCII text, up to 20 bytes--
   08 len
      --Org affiliation, line 2, ASCII text, up to 20 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

Note that the SDK does not allow putting data into the Printed tag. However, Yubico uses
this tag to store information. If you GET DATA with this tag, you will likely see either
no data or data that does not follow this format.

You should never overwrite the information in the Printed tag. If you do, it could make
your YubiKey unusable.

#### Encoded security object

As specified in the PIV standard:

```
53 L1
   BA len
      --Mapping of DG (Data Group, see PIV standard) to ContainerID, up to 30 bytes--
   BB len
      --Security object (See MRTD standard), up to 1298 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

#### Encoded key history

As specified in the PIV standard:

```
53 L1
   C1 01
      --number of keys with on card certs--
   C2 01
      --number of keys with off card certs--
   F3 len
      --off card cert URL, only if C2 value is > 0, up to 118 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

#### Encoded iris images

As specified in the PIV standard:

```
53 L1
   BC len
      --image for verification, up to 7,100 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

#### Encoded facial image

As specified in the PIV standard:

```
53 L1
   BC len
      --image for verification, up to 12,704 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

#### Encoded fingerprints

As specified in the PIV standard:

```
53 L1
   BC len
      --Fingerprint I and II, up to 4,000 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

#### Encoded BITGT

As specified in the PIV standard:

```
7F 61 L1
   02 01
      --number of fingers--
   7F 60 len
      --BIT for first finger, up to 28 bytes--
   7F 60 len (Optional)
      --BIT for second finger, up to 28 bytes--
```

#### Encoded SM cert signer

As specified in the PIV standard:

```
53 L1
   70 len
      --X.509 cert, up to 3,048 bytes--
   71 01
      --compression--
   7F 21 len (Optional)
      --intermediate CVC (see PIV standard), uncompressed, up to 3,048 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

The YubiKey will allow for up to a combined 3,048 bytes for the cert and CVC. That is,
even though the maximum length of each element is 3,048 bytes, the length of the two
combined must be 3,048 bytes or fewer. So a cert of 3,048 bytes with no CVC (length 0) is
acceptable, but a cert of length 2,500 bytes combined with a CVC of 600 bytes would not be
allowed.

#### Encoded PC Ref

As specified in the PIV standard:

```
53 0C
   99 08
      --Pairing code, Ascii text, fixed at 8 bytes--
   FE 00 (LRC, unused in PIV)
```

The "LRC" is an error detection code. While it is mandatory according to
one smart card standard, the PIV standard does not use it and therefore its
length is zero.

### APDU

[Technical APDU Details](apdu/get-data.md)
___

## Put data

Put the given data into a data element on the card.

See also the User's Manual entry on the
[GET and PUT DATA commands](get-and-put-data.md).

### Available

All YubiKeys with the PIV application.

### SDK Classes

[PutDataCommand](xref:Yubico.YubiKey.Piv.Commands.PutDataCommand)

[PutDataResponse](xref:Yubico.YubiKey.Piv.Commands.PutDataResponse)

### Input

In order to put data, the management key must be authenticated. See the User's Manual
[entry on PIV commands access control](access-control.md) for information on how to
authenticate with the management key for commands. See also the section in this page on
[Authenticate: management key](#authenticate-management-key).

The input data will be a "tag" specifying where the data is to go, along with the data to
PUT. [Tables 4x](#getdatatable) in the Get Data command section above lists all possible
tags. The "Data Returned" in that table is the data to put. Note that the SDK requires the
data be formatted as defined in the PIV standard, described above.

Note that the YubiKey will not allow putting data for the following tags.

* Printed
* Discovery
* Biometric Information Group Template

You should never overwrite the information in the Printed tag. If you do, it could make
your YubiKey unusable.

### Output

`bool`: was the data element successfully put?

### APDU

[Technical APDU Details](apdu/put-data.md)
___

## Get and Put Vendor Data

The SDK also contains the ability to get and put data into tags not defined by the PIV
standard. These are vendor-defined tags. The tag must be of the form 0x5FFFxx. Using this
feature, a caller can also store arbitrary data into a tag, data that is not necessarily
encoded following the PIV specification.

This feature, however, is not publicly available. It is only callable from inside the SDK.

<a name="getvendordatatable"></a>
Table 5A lists Yubico-defined elements that the YubiKey will possess upon manufacture (as
long as the PIV application is initialized).

Table 5B lists Yubico-defined elements that the YubiKey will not possess upon manufacture.
Requesting these elements using GET DATA will return "NoData". If you want these elements
to contain data, you will have to load them using PUT DATA.

#### Table 5A: Yubico-defined GET DATA elements available upon manufacture

|    Name     |   Tag    |     Meaning      | Authentication<br/>Required |                Data Returned                |
|:-----------:|:--------:|:----------------:|:---------------------------:|:-------------------------------------------:|
| ATTESTATION | 5F FF 01 | Attestation cert | PUT: mgmt key<br/>GET: none | [Encoded certificate](#encoded-certificate) |

#### Table 5B: Yubico-defined GET DATA elements empty upon manufacture

|           Name           |                Tag                |                     Meaning                     | Authentication<br/>Required |               Data Returned               |
|:------------------------:|:---------------------------------:|:-----------------------------------------------:|:---------------------------:|:-----------------------------------------:|
|        ADMIN DATA        |             5F FF 00              | PIV manager application<br/>administrative data | PUT: mgmt key</br>GET: none | [Encoded admin data](#encoded-admin-data) |
|          MSCMAP          |             5F FF 10              |             Microsoft container map             | PUT: mgmt key</br>GET: none |             [MSCMAP](#mscmap)             |
| MSROOTS1 to<br/>MSROOTS5 | 5F FF 11<br/>through<br/>5F FF 15 |              Microsoft root certs               | PUT: mgmt key</br>GET: none |            [MSROOTS](#msroots)            |

#### Encoded Admin Data

The GET DATA element ADMIN DATA is used by the PIV manager application to store data about
the PIV application and the data in the PIV portion of the YubiKey. The information is
generally not used by the YubiKey anymore and is retained only for backwards compatibility.
It is safe to ignore this element.

```C
80 L1
   81 01 (optional)
      --bit field, PUK blocked, Mgmt Key stored in protected data--
   82 L2 (optional)
      --salt, deprecated--
   83 L3 (optional)
      --time the PIN was last updated--
```

The bit field contains up to two bits: 1 is PUK blocked and 2 is PIN-protected.

It is permissible to have no salt (either no TLV for tag 82, or `82 00` as the TLV) or a
16-byte value.

The "PIN last updated" field is the UNIX time of seconds since 1970, in little endian
order. For example, some time in Jan. 14, 2022 is 0x61E1B870. It would be encoded as

```
   83 04
      70 B8 E1 61
```

It will generally be a 4-byte value until sometime in January, 2038, when it will be 5
bytes.

#### MSROOTS

These tags were created so that Yubico libraries (minidriver, SDK) can better interface
with the Microsoft Smart Card Base Crypto Service Provider (CSP). There will likely never
be a scenario where an application will need to use the data the SDK will PUT into and GET
from these objects. If your application uses the Base CSP, and you use a YubiKey, any
necessary operations with the MSROOTS will be handled by the SDK.

#### MSCMAP

This tag was created so that Yubico libraries (minidriver, SDK) can better interface with
the Microsoft Smart Card Base Crypto Service Provider (CSP). There will likely never be a
scenario where an application will need to use the data the SDK will PUT into and GET from
these objects. If your application uses the Base CSP, and you use a YubiKey, any necessary
operations with the MSCMAP will be handled by the SDK.
___

## Reset the PIV Application

Delete all the credentials and keys, and set the PIN, PUK, and management key to the
default values:

```
PIN:       123456
PUK:       12345678
Mgmt Key:  hex 010203040506070801020304050607080102030405060708
               0102030405060708 three times
```

This command will be accepted only if the PIN and PUK are both blocked.

### Available

All YubiKeys with the PIV application.

### SDK Classes

[ResetPivCommand](xref:Yubico.YubiKey.Piv.Commands.ResetPivCommand)

[ResetPivResponse](xref:Yubico.YubiKey.Piv.Commands.ResetPivResponse)

### Input

None.

### Output

There is no data output, only the status.

### APDU

[Technical APDU Details](apdu/reset-piv.md)
___

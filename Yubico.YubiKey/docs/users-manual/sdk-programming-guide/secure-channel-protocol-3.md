---
uid: UsersManualScp03
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

# Secure Channel Protocol 3 ("SCP03")

Commands sent to the YubiKey, or responses from the YubiKey, may contain
senstive data that should not leak to or be tampered with by other applications
on the host machine. The operating system of the host machine may provide at
least one layer of protection by isolating applications from each other using
separation of memory spaces, permissioned access to system resources like
devices, and other techniques.

The YubiKey also supports an additional layer of protection that aims to provide
confidentiality and intergrity of communication to and from the YubiKey, using a
standardized protocol called "Secure Channel Protocol 3" (commonly referred to
as "SCP03"). This protocol is produced by GlobalPlatform, an industry consortium
of hardware security vendors that produce standards. While there are serveral
versions of the Secure Channel Protocol, the only one supported by the YubiKey
is version 3, and only YubiKey 5 series devices with firmware version 5.3 or
greater support it.

This added layer of protection makes the most sense when the communication
channel between the host machine and the device could feasibly be compromised.
For example, if you tunnel YubiKey commands and resonses over the Internet, in
addition to standard web security protocols like TLS, it could makes sense to
leverage SCP03 as an added layer of defense. Additionally, several 'card
management systems' use SCP03 to securely remotely manage devices.

> [!NOTE]
> SCP03 works only with SmartCard applications, namely PIV, OATH, and OpenPgp.
> However, SCP03 is supported only on series 5 YubiKeys with firmware version 5.3
> and later, and only the PIV application.

## Static Keys

SCP03 relies on a set of shared, symmetric secret cryptographic keys, called the
'static keys' ([StaticKeys](xref:Yubico.YubiKey.Scp03.StaticKeys)), which are
known to the application and the YubiKey. A well-known static key is set by
default on YubiKeys; it is important to emphasize that using the default SCP03
keys to connect to a device offers *no additional protection* over cleartext
communication.

The three keys that comprise the `StaticKeys` are 16 byte, AES-128 cryptographic
keys, referred to in the GlobalPlatform SCP03 Specification (v1.1.2) as the
`Key-MAC`, `Key-ENC`, and `Key-DEK`. These can be used to construct an instance
of `StaticKeys` using the 
`StaticKeys(ReadOnlyMemory<byte> channelMacKey, ReadOnlyMemory<byte> channelEncryptionKey, ReadOnlyMemory<byte> dataEncryptionKey)`
constructor, where `channelMacKey` is the `Key-MAC`, `channelEncryptionKey` is
the `Key-ENC`, and `dataEncryptionKey` is the `Key-DEC`.

Through custom orders, Yubico can preprogram devices with a non-default set of
SCP03 static keys. These can be fixed, or derived from a "batch master key" on a
per-device basis. For more details on how key derivation works, please see [this
project](https://github.com/YubicoLabs/yubikey-diversification-tool). The SDK
does not at this time support performing derivation of static keys.

## Prerequisites

To securely connect to a device using SCP03, you must have a YubiKey device with
firmware 5.3 or later, with non-default static keys set and known to you. The
YubiKey must support the PIV application.

## Connecting to a device

Given the above prerequisites, you can connect to a device using SCP03 by
following these steps:

1. Get an instance of the [IYubiKeyDevice](xref:Yubico.YubiKey.IYubiKeyDevice)
   class that you want to connect to. (See the user's manual entry on
   [making a connection](xref:UsersManualMakingAConnection).)
2. Construct the static keys for this device as an instance of the
   [StaticKeys](xref:Yubico.YubiKey.Scp03.StaticKeys) class. See the above
   section for details on how to construct an instance of `StaticKeys`.
3. Specify the key set in the connection.  
    A. When creating a `PivSession`
     ```c#
     using (var pivSession = new PivSession(yubiKeyDevice, scp03Keys))
     {
     }
     ```
    B. When creating an `IYubiKeyConnection`.
     ```c#
     yubiKeyDevice.ConnectScp03(YubiKeyApplication.Piv, scp03Keys);
     ```

## Security

Previous versions of the Secure Channel Protocol have had vulnerabilities that
compromised data confidentiality or integrity. Yubico did not design or vet the
correctness of the SCP03 protocol itself; we simply implemented it. SCP03 is not
at this time known to contain protocol-level flaws.

The specific aims of the SCP03 protocol can be summarized as follows (see the
[APDU](xref:UsersManualApdu) page for more details):

1. The *data* field of command APDUs should not be readable to any party without
access to the static keys. It's important to note that the `INS`, `P1`, and `P2`
fields are **not** encrypted.
2. The *entire* command APDU should maintain integrity under malicious
modification. Specifically, any party without access to the static keys should
not be able to modify **any** part of command APDUs without triggering an error
in the YubiKey.
3. The *data* field of response APDUs should not be readable to any party
without access to the static keys. The status word is **not** encrypted.
4. The *data* field of response APDUs should maintain integrity under malicious
modification. Note that any non-success (`90 00`) status word is **not**
authenticated.

In summary:

| APDU Type     | Field             | SCP03 Aim                                  |
|---------------|-------------------|--------------------------------------------|
| Command APDU  | Data              | Confidentiality and integrity              |
| Command APDU  | `INS`, `P1`, `P2` | Integrity **only**                         |
| Response APDU | Data              | Confidentiality and integrity              |
| Response APDU | Status Word       | Integrity when `90 00`, otherwise **none** |

The SDK makes conservative choices in SCP03 support. When a command sent over
SCP03 generates a status word indicating an error (anything other than `90
00`), the SDK will throw a `SecureChannelException`, since this response is
unauthenticated. When using SCP03, it is therefore important to ensure that the
expected response to all issued commands is a success, and to treat any
unexpected non-success status word as possibly forged.

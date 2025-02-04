---
uid: YubiKeyTransportOverview
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

# Physical interfaces

Physical interfaces are the ways that a computer, phone, or other device can connect
with the YubiKey in order to communicate with it.

## USB Transports

All of the models in the YubiKey 5 Series provide a USB 2.0 interface, regardless of
the form factor of the USB connector. The YubiKey is a composite USB device. When plugged
into a computer with its default settings, the YubiKey will present three separate USB
transports:

- A Human Interface Device (HID) Keyboard
- A HID FIDO device
- A smart card reader with smart card attached

Each device serves one or more applications on the YubiKey, with the smart card interface
being the most versatile. Below is a list of applications available on the YubiKey 5 and
which USB transport they are available on:

| Application | HID Keyboard | Smart Card  |  HID FIDO   | 
|:------------|:------------:|:-----------:|:-----------:|
| Management  | Partial [1]  |     Yes     | Partial [1] |
| OTP         |     Yes      | Partial [2] |     No      |
| OATH        |      No      |     Yes     |     No      |
| PIV         |      No      |     Yes     |     No      |
| OpenPGP     |      No      |     Yes     |     No      |
| FIDO U2F    |      No      |   No [3]    |     Yes     |
| FIDO2       |      No      |     No      |     Yes     |

[1]: The GetDeviceInfo and SetDeviceInfo commands are available over all transports.

[2]: OTP was available over Smart Card in the YubiKey NEO. Most OTP operations are now
blocked over smart card.

[3]: The FIDO U2F was available over smart card in the YubiKey NEO.

The USB product ID (PID) and product string will change depending on which of the USB
interfaces are enabled as described in the table below. Yubico's vendor ID (VID)
is `0x1050`.

| USB interfaces  |  PID   |    Product string     |
|:---------------:|:------:|:---------------------:|
|       OTP       | 0x0401 |      YubiKey OTP      |
|      FIDO       | 0x0402 |     YubiKey FIDO      |
|      CCID       | 0x0404 |     YubiKey CCID      |
|    OTP, FIDO    | 0x0403 |   YubiKey OTP+FIDO    |
|    OTP, CCID    | 0x0405 |   YubiKey OTP+CCID    |
|   FIDO, CCID    | 0x0406 |   YubiKey FIDO+CCID   |
| OTP, FIDO, CCID | 0x0407 | YubiKey OTP+FIDO+CCID |

An interface is enabled so long as there is a single application enabled which uses that
interface. For example, the OTP (HID Keyboard) is enabled when the OTP application is enabled
over USB. The HID FIDO interface is enabled when either the U2F or FIDO2 applications are
enabled over USB. The CCID (smart card) interface is enabled when the PIV, OATH, or OpenPGP
applications are enabled over USB.

OTP interface output is sent as a series of keystrokes from a virtual HID keyboard. This allows
for OTP to be used in any environment which can accept standard keyboard input. For more information 
on the HID Keyboard transport, see the [OTP application documentation](xref:OtpHID).

The FIDO interface has a HID usage page set to `0xF1D0`.

### Linux support

#### HID keyboard

The SDK's HID operations on Linux make use of "libudev", "libc", and "hidraw". Make sure
they are available on the device.

The shared libraries "libudev" and "libc" must in one of the paths the SDK will search
(see the .NET documentation for the `DllImportSearchPath` enum; the SDK uses the value
`SafeDirectories`).

One directory the SDK searches is `/usr/lib`. If the SDK cannot find some needed library,
it will likely be easiest to simply create a symbolic link. For example:

```
$ cd /usr/lib
$ sudo ln -s /usr/lib/x86_64-linux-gnu/libudev.so libudev.so
```

**udev**:

The udev library is part of Linux and will probably already be installed on the device. It
is commonly found in a directory such as

```
/usr/lib/x86_64-linux-gnu/libudev.so
```

If so, there is likely nothing you will need to do. If the SDK cannot find `libudev.so`,
make sure it is on the device (e.g. `$ find /usr -name libudev.so`). If it is, maybe it is
not in a standard location and you need to make a symbolic link.

**libc**:

The SDK expects a libc library named `libc.so.6` to be in the shared library search path.
If it is not, you will likely make a symbolic link in `/usr/lib`.

```
$ cd /usr/lib
$ sudo ln -s /usr/lib/x86_64-linux-gnu/libc.so.6 libc.so.6
```

**hidraw**:

The hidraw library is a driver that provides an interface to USB devices. This driver
should be part of the Linux kernel and there should be nothing you need to do.

#### Smart card

In order to use the SDK to contact a YubiKey on a Linux device, you need to install the
"pcsclite" library. This is an Open Source implementation of PC/SC (personal computers/
smart card), a specification for integrating smart cards into computer environments. If it
is not already installed on your Linux device, you will likely run a command such as:

```
$ apt-get install libpcsclite1
```

**Arch Linux**: 

If on Arch Linux, you may also need to install `pcsc-tools`:

```
sudo pacman -S pcsc-tools 
```

Once installed, start the pcsc daemon:

```
sudo systemctl enable --now pcscd.socket
sudo systemctl start --now pcscd.socket
```


## NFC

In addition to USB, the YubiKey 5 NFC keys also provide an NFC wireless interface for
additional convenience. Unlike the YubiKey NEO, the YubiKey 5 NFC does not support RFID
tags, such as MIFARE Classic and MIFARE DESFire.

The URI used by the NDEF tag has been updated to a new format; an example of the new format
is provided below. The `<OTP>` value will be replaced with the OTP generated by the YubiKey.

```txt
https://my.yubico.com/yk/#<OTP>
```

For operations that require a touch, all touch requests within the first 15 seconds of the
operation will succeed. After a period of inactivity, a YubiKey placed on a desktop NFC reader
may power down to help prevent unintended access to the device. To regain connectivity with
an NFC reader, remove the YubiKey from the reader and reposition it on the reader. Some NFC
readers may power cycle the YubiKey and, in doing so, prevent the device from powering down.

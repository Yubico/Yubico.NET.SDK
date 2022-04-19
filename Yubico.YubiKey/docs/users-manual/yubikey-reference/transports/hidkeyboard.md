---
uid: YubiKeyTransportHIDKeyboard
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

# HID Keyboard (OTP)

The OTP interface presents itself to the operating system as a USB keyboard.
The OTP application is accessible over this interface. None of the other
applications are available through the OTP interface.

Output is sent as a series of keystrokes from a virtual keyboard. This allows
for OTP to be used in any environment which can accept standard keyboard input.

## The keyboard interface

### Reading status

### Sending a request

## Mapping commands to keyboard frames

## Linux HID support

The SDK's HID operations on Linux make use of libudev", and "libc", and "hidraw. Make sure
they are available on the device.

The shared libraries "libudev" and "libc" must in one of the paths the SDK will search
(see the .NET documentation for the `DllImportSearchPath` enum, the SDK uses the value
`SafeDirectories`).

One directory the SDK searches is `/usr/lib`. If the SDK cannot find some needed library,
it will likely be easiest to simply create a symbolic link. For example,

```
$ cd /usr/lib
$ sudo ln -s /usr/lib/x86_64-linux-gnu/libudev.so libudev.so
```

### udev

The udev library is part of Linux and will probably already be installed on the device. It
is commonly found in a directory such as

```
/usr/lib/x86_64-linux-gnu/libudev.so
```

If so, there is likely nothing you will need to do. If the SDK cannot find `libudev.so`,
make sure it is on the device (e.g. `$ find /usr -name libudev.so`). If it is, maybe it is
not in a standard location and you need to make a symbolic link.

### libc

The SDK expects a libc library named `libc.so.6` to be in the shared library search path.
If it is not, you will likely make a symbolic link in `/usr/lib`.

```
$ cd /usr/lib
$ sudo ln -s /usr/lib/x86_64-linux-gnu/libc.so.6 libc.so.6
```

### hidraw

The hidraw library is a driver that provides an interface to USB devices. This driver
should be part of the Linux kernel and there should be nothing you need to do.

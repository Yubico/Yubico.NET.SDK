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

# Running .NET SDK applications on Linux

Some special steps may be required to run applications that depend on the .NET
SDK on Linux.

## Distributions

### Officially supported

A distribution is officially supported after our team has run through our
acceptance tests on that distro. This is not the exhaustive suite of tests
for the SDK, but enough that it gives us confidence that the platform-specific
device discovery and communication logic are working properly. Any abnormalities
observed should be reported to our [GitHub repo](https://github.com/Yubico/Yubico.NET.SDK/issues).

At this time, the SDK has been tested to work on the following Linux distributions:

- Debian
- Ubuntu
- CentOS
- RedHat Enterprise Linux (RHEL)

### Other distros

There are plenty of popular distros that are *not* on this list. Though not official supported,
there is a high likelihood that the SDK will still work on these distros. The instructions
below will likely be identical, or very similar to what you will need to do to get started
with your distribution of choice.

## Dependencies

The .NET SDK depends on two main components for device communication: udev and pcsc-lite.

### PCSC-lite

PCSC-lite (PCSC for short from now on) is a library used to communicate with smart card
readers and smart cards. Much of the YubiKey functionality is exposed to an operating
system as a smart card, so PCSC is likely critical for your user to have installed. If
you are planning to develop against the OATH, PIV, or OpenPGP YubiKey applications, or
you want to communicate with the YubiKey over NFC, you will certainly need to have PCSC
installed on the computer running your application.

PCSC can be install in the following way:

```shell
# For APT based distros (e.g. Debian, Ubuntu):
sudo apt install pcscd

# For YUM based distros (e.g. RedHat, CentOS):
sudo yum install pcsc-lite
```

### UDev

UDev is a common Linux device manager available on most distributions. It is used to
discover PnP devices, from displays and sound cards, to mice and keyboards. In the
case of the .NET SDK, UDev is used to discover the two HID devices exposed by the
YubiKey.

UDev should typically already be installed on your system. If it is not, that may
mean that your Linux distro uses an alternate device management system. Swapping
your current device manager with UDev is likely not a viable option, and is not
recommended. If you are running a distro that is not running UDev and you are interested
in using this SDK, please open an issue on our [GitHub repo](https://github.com/Yubico/Yubico.NET.SDK/issues).
If there is broad interest, we will evaluate adding support to our roadmap.

#### Making sure the SDK can find UDev

As of SDK 1.4.0, the SDK P/Invokes UDev's shared library `libudev.so`. This library can have
multiple names (usually including a version number, e.g. `libudev.so.1`), so we target
the lowest common denominator name: `libudev`. .NET's library resolver is currently
not capable of automatically resolving the full path / name of this dependency.

As such, it will likely be required that you create a symbolic link from your shared
library directory, typically `/usr/lib`, to the real location of libudev.

For example:
```shell
# On Debian and Ubuntu, libraries are usually stored in an x86_64 subdirectory:
sudo ln -s /usr/lib/linux-x86_64/libudev.so.1 /usr/lib/libudev.so
```

Note that the exact file name and location may change based on your distribution and
the version of libudev installed.

### .NET and OpenSSL / libcrypto

Although the SDK supports any .NET implementation of .NET Standard 2.0, it is
recommended that you target the latest LTS or STS release of .NET if you are
planning support for Linux. .NET is making continual cross-platform improvements.

Some distributions, like Ubuntu, have started to phase out older versions of
OpenSSL, making OpenSSL 3.x its default. Older .NET versions such as .NET Core 3.1
do not support this and require older versions of OpenSSL. Be aware that .NET
itself may have dependencies like this, and that the choice of framework version
may have further impact for your application's stated dependencies.

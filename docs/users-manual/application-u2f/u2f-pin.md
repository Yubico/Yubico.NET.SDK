---
uid: FidoU2fPin
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

# The FIDO U2F PIN

Both the FIDO U2F and FIDO2 standards specify that a device have a "test of user presence"
in order to perform registration and authentication operations. During the test,
"the user touches a button (or sensor of some kind) to 'activate' the U2F
device".

With the YubiKey, there is the option to also require a PIN in order to use the U2F
application. However, this option is available only for YubiKey 4 FIPS
Series keys. If you try to set a PIN for the U2F application on a non-FIPS YubiKey 4 Series key,
a YubiKey 5 Series key (FIPS or non-FIPS), or a YubiKey Security Key Series key, it will not
work.

However, with YubiKey 5 Series (FIPS and non-FIPS) and YubiKey Security Key Series keys, it 
is possible to set a PIN on the *FIDO2*
application. YubiKey 4 Series keys do not have a FIDO2 application.

| YubiKey      | U2F Available | FIDO2 Available | U2F PIN | FIDO2 PIN |
|:------------:|:-------------:|:---------------:|:-------:|:---------:|
|   4          |      yes      |       no        |   no    |     -     |
|   4 FIPS     |      yes      |       no        |   yes   |     -     |
|   5          |      yes      |       yes       |   no    |    yes    |
|   5 FIPS     |      yes      |       yes       |   no    |    yes    |
| Security Key |      yes      |       yes       |   no    |    yes    |

To learn more about how the U2F PIN is used with YubiKey 4 FIPS
Series keys, see [FIDO U2F and FIPS](fips-mode.md).

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
in order to perform registration and authentication operations. This test is described as
follows. "The user touches a button (or sensor of some kind) to 'activate' the U2F
device".

With the YubiKey, there is the option to specify a PIN in order to use the U2F
application as well as touch. However, this option is available only for YubiKey 4 FIPS
series. If you try to set a PIN for the U2F application on a non-FIPS version 4 YubiKey,
or a YubiKey 5 (FIPS or non-FIPS) series, or a YubiKey Security Key series, it will not
work.

Note that with a version 5 FIPS series YubiKey, it is possible to set a PIN on the FIDO2
application. Version 4 series YubiKeys do not have the FIDO2 application, and it is not
possible to set a FIDO2 PIN on a non-FIPS version 5 series YubiKey.

| YubiKey  | U2F Available | FIDO2 Available | U2F PIN | FIDO2 PIN |
|:--------:|:-------------:|:---------------:|:-------:|:---------:|
|   v 4    |      yes      |       no        |   no    |     -     |
| v 4 FIPS |      yes      |       no        |   yes   |     -     |
|   v 5    |      yes      |       yes       |   no    |    no     |
| v 5 FIPS |      yes      |       yes       |   no    |    yes    |
|   SKY    |      yes      |       yes       |   no    |    no     |

To learn more about the U2F PIN, see the article on [FIDO U2F and FIPS](fips-mode.md).

---
uid: YubiKeyTransportSmartCard
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

# Smart card transport

Content coming soon.

## Linux smart card support

In order to use the SDK to contact a YubiKey on a Linux device, you need to install the
"pcsclite" library. This is an Open Source implementation of PC/SC (personal computers/
smart card), a specification for integrating smart cards into computer environments. If it
is not already installed on your Linux device, you will likely run a command such as

```
$ apt-get install libpcsclite1
```
---
uid: UsersManualSupportOverview
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

# Supporting routines overview

There are some classes in the SDK that are not directly related to the YubiKey. Each of
these classes performs some sort of support operation.

For example, the TLV classes (see [TLV support](support-tlv.md)) are used to build and
parse "tag-length-value" constructions. These classes do not call on the YubiKey to do any
work, but rather help format and parse data as required by the YubiKey.

Many developers will never need to use these support classes. However, it is possible that
an application will need to perform some of the operations these classes do. It will
likely be easier to take advantage of the SDK's implementations, rather than write them
all over again.

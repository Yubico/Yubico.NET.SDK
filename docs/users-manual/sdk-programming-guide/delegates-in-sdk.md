---
uid: UsersManualDelegatesInSdk
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

# Delegates (callbacks) in the SDK

There are a number of places in the SDK where the developer must supply a `delegate`,
the C# version of a `callback`. This page discusses what they are, and how to write your
own.

## A callback

A callback is simply a function you provide the SDK. In turn, the SDK will call that
function when it needs to do some work that only your application can do. The easiest way
to illustrate this is with an example: the PIN collector.

Suppose you are building a PIV application, and want to sign some data. That operation
requires the PIN to be verified (it is possible to turn off the PIN requirement, but for
now, let's just look at the case where the PIN is required). In order to verify the PIN,
the SDK, of course, needs the PIN. How should that be provided?

While it is possible to simply require the caller enter the PIN as a byte array, the SDK
requests a PIN-collecting function. In this way, when the SDK needs the PIN, it will call
this function, use the PIN it collects, then release it. Following this pattern, the PIN
appears in memory the least amount of time (see the User's Manual entry on
[sensitive data](sensitive-data.md)).

The SDK does not have a PIN-collecting class or method. That's because how your
application collects the PIN is up to you. Maybe you collect it at the command line, maybe
you launch a new Window, or have a "sub-window" of the main window. Maybe you collect it
once at the beginning and simply keep it in a buffer somewhere (not recommended).

When the sign operation needs the PIN, it will call a PIN-collecting function. But because
there is no such method in the SDK, it will be your responsibility to provide one. Read
[this article about the `KeyCollector`](key-collector.md) for details on this delegate.

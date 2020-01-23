---
uid: UsersManualThreads
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

# Threads

The YubiKey is effectively single-threaded. Although there really is no concept of a
thread on a YubiKey, it is best to think of all operations on the YubiKey happening in the
same thread.

Suppose you have a multithreaded application. Now suppose you make a connection to a
YubiKey in one of those threads, and then make another connection in a separate thread.
You will not get two connections operating independently. Any call to the YubiKey in one
thread will affect the internal state, which will affect any call from the other thread.

Because of this, the SDK is built to be run in a single thread. You should write your code
accordingly. Either make sure all interactions with the YubiKey are handled by one thread
only, or use locks to guarantee only one thread at a time calls to the YubiKey.

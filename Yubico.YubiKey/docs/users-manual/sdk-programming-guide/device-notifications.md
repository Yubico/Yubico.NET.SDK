---
uid: DeviceNotifications
summary: *content
---

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

# Device notifications

A YubiKey is a device that can be added or removed to a computer at any time. To give the best
experience to end users, applications should be aware of when a change like this occurs, and
have this reflected in their UI as soon as possible.

One possible way to achieve this would be to continually call [YubiKeyDevice.FindAll](xref:Yubico.YubiKey.YubiKeyDevice.FindAll)
in a loop, and look for changes from one iteration to the next. This is called "polling". While
this can certainly work, it is not ideal for several reasons. First, it is resulting in calls
to the SDK and to the computer that may not be necessary. Since you don't know when a change will
occur, you must continually ask the computer for its state. Second, as part of enumerating keys,
the SDK must talk to the key to gather basic information such as serial number and firmware version.
Since a YubiKey is single threaded and potentially stateful, this could be a disruptive action
to an existing key that is in the middle of performing an action.

A better approach would be to use the [YubiKeyDeviceListener](xref:Yubico.YubiKey.YubiKeyDeviceListener)
class that was added to the SDK in version 1.2.0. This class exposes two events: Arrived and Removed.
As the names suggest, these events will trigger when a YubiKey is added or removed from the computer,
respectively. For more general information about the C# event mechanism and how to use them, please
refer to the official documentation for
[C# Events](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/events/).

## YubiKeyDeviceListener

### Usage

YubiKeyDeviceListener is a singleton class. This means that you do not need to worry about
constructing it. You can get at the single instance through the [Instance](xref:Yubico.YubiKey.YubiKeyDeviceListener.Instance)
static property.

Once you have obtained the instance to the listener class, you can subscribe (or unsubscribe)
to its events using the `+=` (or `-=`) operator. These events expect a delegate or method
that follow the standard event handler signature, that is, a method that takes two parameters:
an `object` that represents the sender of the event, and [YubiKeyDeviceEventArgs](xref:Yubico.YubiKey.YubiKeyDeviceEventArgs)
that represents the event payload.

`YubiKeyDeviceEventArgs` exposes a property called `Device`. This is the `YubiKeyDevice` that
caused the event to be raised. The device property will be populated for both arrival and removal
events, even if the actual device is not physically present.

The following code snippet shows how you can subscribe to events in your application:

```c#
var listener = YubiKeyDeviceListener.Instance;

listener.Arrived += (s, e) => { Console.WriteLine($"YubiKey arrived!: {e.Device}"); };
listener.Removed += (s, e) => { Console.WriteLine($"YubiKey removed!: {e.Device}"); };
```

### Implementation

The device listener was implemented with two primary design goals in mind:

1. **One YubiKey = One Event.** Since a YubiKey can expose up to three child devices while plugged
   in via USB, a naive implementation of events would raise up to three events per key. This was
   considered unacceptable, as the SDK already does its best to mask this detail from application
   developers. Since enumeration exposes a single logical YubiKey, so should the events.
2. **Minimize disruption to YubiKeys that are already present.** In order to represent the YubiKey 
   as an SDK object, the SDK must first ask the YubiKey to describe itself. This involves sending
   a command over each of the available interfaces to the key. If sent to a YubiKey that was
   already present and engaged in a stateful operation, this command could disrupt that key and
   cause the other active thread to start failing unexpectedly. The implementation of events
   in the SDK avoid this by only communicating with keys that the SDK has never seen before.

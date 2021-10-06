---
uid: OtpSwapSlot
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

# How to swap slot configurations

Because swapping [slot](xref:OtpSlots) settings requires zero configuration, this operation is not designed as a Fluent Builder operation. It’s as simple as calling the ```SwapSlots``` method:

```
using (OtpSession otp = new OtpSession(yKey))
{
  otp.SwapSlots();
}
```

> [!NOTE]
> This method will fail if at least one of the slots is not currently configured.

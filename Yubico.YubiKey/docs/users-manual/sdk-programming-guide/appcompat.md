---
uid: AppCompat
---

<!-- Copyright 2024 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Maintaining compatibility

This article describes the various decisions that the SDK makes in order to maintain application and source
compatibility across our versions.

## App-compat strategy for this SDK

The .NET SDK strives to maintain both source and behavioral compatibility across its releases.

**Source compatibility** is the promise that your source code should continue to compile as-is when you update your
application to the latest version of the SDK. This promise extends to "minor" (feature) and "patch" (bug-fix) releases
of the SDK. Additionally, we only make this guarantee for the `Yubico.YubiKey` and the `Yubico.Core` assemblies. Since
`Yubico.NativeShims` is meant to be purely for internal use, we *do not* make any
guarantees here.

**Behavioral compatibility** is the promise that your application will behave exactly the same after you have upgraded
the YubiKey SDK version. Maintaining this guarantee is far more difficult and is sometimes simply not possible. However,
we will continue to do our best to maintain behavioral stability across releases. If a behavioral change is
necessitated, such as fixing a bug, we may choose to simply fix the issue. This is far more likely to occur if the bug
prevented the feature from ever working in the first place and no workaround was present. If the change is more nuanced
than that, or it is changing behavior for some other reason, we have a separate mechanism that we've started using so
that these behavior changes may be managed through an opt-in or opt-out decision.

There are two exceptions to these promises:

1. Sometimes a breaking change is unavoidable. Perhaps a new YubiKey feature was released that is simply impossible to
   express with the existing shape of the API. Or a bug was discovered, and it simply must be addressed. In these cases,
   we will do everything in our power to first mark the affected types or members with the `ObsoleteAttribute` so that
   you are alerted the fact that there's an issue with the old usage. The attribute will contain text that will result
   in a usage warning when you recompile. This text will be included in the warning message and will point you to the
   new API that should be used instead. The old API will remain for several minor releases before we consider it safe to
   remove entirely. A major release would remove all obsolete APIs in one go.

2. You will note that the promise is only made for minor and patch releases. For example, upgrading feature releases
   (i.e. `1.9.1` to `1.10.0`) or upgrading patch releases (i.e. `1.9.0` to `1.9.1`) have this guarantee. What has been
   omitted here is "major" releases (i.e. `1.10.0` to `2.0.0`). Major version releases are our chance to make broader
   changes that address design-level issues. It should be expected that there will be source level breaking changes when
   a major version is released.

Our SDK does *not* make any promises around **Application Binary Interface (ABI)** stability. This expectation is
generally far less common in the .NET ecosystem to begin with, however there are two very important implications here:

1. You *must* recompile your code against a new version of our SDK. Simply replacing our assemblies with a newer version
   is **not** supported and could result in undefined behavior and bugs in your application's behavior.

2. If an enumeration does not have an explicitly defined value, you should assume that the underlying value may change.
   While these changes should not result in any changes to behavior (assuming you've recompiled) it does mean that these
   values should not be serialized and stored across versions. If you need to persist these values for whatever reason,
   it is strongly recommended you create your own stable values to map to, or use another mechanism that does not depend
   on the specific compiler-generated enumeration value.

## Managing behavior changes through app-compat switches

Sometimes it's unavoidable that the SDK must make a behavior breaking change. For example: a bug has been addressed that
causes subtle behavior changes that have existed for many releases. Or perhaps an optimization has been made that may
result in different timings that could have an effect on UI applications.

In these cases, we've introduced a new mechanism for adjusting these behaviors through the use of app-compat switches.
These switches use the
[`AppContext.SetSwitch`](https://learn.microsoft.com/en-us/dotnet/api/system.appcontext.setswitch) mechanism exposed by
the .NET Base Class Library.

Whether a behavior change is opt-in or opt-out will be decided on a case-by-case basis. Generally, if we view the change
to be a net positive and have a low risk of observable changes to an application, we will make the change opt-out. That
means, you will get the new behavior by default. Only if the change causes your application problems should you consider
setting the switch to disable that behavior.

For more observable or impactful changes, or changes that would benefit a smaller subset of consumers, we will make the
change opt-in. That is, the existing behaviors will be maintained, and your application must explicitly call `SetSwitch`
with a value of `true`.

This decision is clearly very subjective. Any time a behavior change is made, there is a high likelihood that at least
one consumer will be adversely affected no matter which behavior we choose. That's why we've introduced these switches
in the first place. There will always be a case where someone will need to override out decision. This is your mechanism
to do so.

All of our compatibility switch names are defined in two central classes:

- [YubiKeyCompatSwitches](xref:Yubico.YubiKey.YubiKeyCompatSwitches) - This class holds all the compatibility switches
  that affect the behaviors of the `Yubico.YubiKey` assembly.
- [CoreCompatSwitches](xref:Yubico.Core.CoreCompatSwitches) - This class holds all the compatibility switches that
  affect the `Yubico.Core` assembly. `Yubico.Core` serves as our platform abstraction layer, so switches here may only
  impact a certain operating system or a certain downstream dependency. While not YubiKey specific, it may affect things
  like enumeration and eventing of YubiKeys.

Each flag will have a clear explanation of what behavior it affects, what the default is, and what the impact of
overriding the default should be. Use these constants as the value for the `switchName` parameter of
`AppContext.SetSwitch`.

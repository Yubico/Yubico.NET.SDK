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

# Polyfills

A polyfill is a coding shim for an API, allowing developers to access newer APIs on older framework
versions. Typically, polyfills check if the platform supports an API, and use it if available, otherwise
using their own implementation.

Adding a new polyfill requires prior approval from the Yubico dev team.

## Yubico.DotNetPolyfills

Yubico.DotNetPolyfills is a package that allows all projects targeting .NET implementations supporting
at least `.NET Standard 2.0` (e.g. `.NET Core 2.0+`, `Xamarin.iOS 10.14+`, etc) to access a selection
of future C# and .NET API features. These reimplemented features are based on the open source .NET Core
framework implementations. Polyfills are only loaded when needed - if the consuming project targets a
framework that already has the feature, Yubico.DotNetPolyfills allows calls to pass through to the
framework's base library.

Because Yubico.DotNetPolyfills is a separate, publicly available package, polyfilled types can be
used at the SDK's public interface. Projects consuming the SDK will simply reference this Nuget package,
which will either fill the missing implementations or forward them on to the base library.

## Yubico.DotNetPolyfills Design

### API Implementation

Implementations are generally based on the .NET Core LTS versions. Changes are kept to a minimum,
focusing on preserving functionality, which means that most warnings in the ported code are suppressed.

### Conditional Compilation and Type Forwarding

Preprocessor directives (`#if`/`#else`) are used to conditionally compile sections of code based on
target framework version. If the target framework version already contains the API,
`TypeForwardedToAttribute` is used to pass calls through to the base library. Otherwise the ported
implementation is used.

### Project Framework Multi-Targeting

Yubico.DotNetPolyfills targets multiple framework versions. The need for `netstandard2.0` is clear
since that's what the SDK is targeting. The other framework version targets are where the polyfill
APIs originate from. This enables appropriate type forwarding.

### Nuget Package

When NuGet installs a package that has multiple assembly versions, it tries to match the framework
name of the assembly with the target framework of the project.

If a match is not found, NuGet copies the assembly for the highest version that is less than or
equal to the project's target framework, if available. If no compatible assembly is found, NuGet
returns an appropriate error message.

This means the Yubico.DotNetPolyfills can be used by any project targeting at or above
`netstandard2.0`, and Nuget will automatically select the best version to use.

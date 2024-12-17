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

> ![build-action-status](https://github.com/Yubico/Yubico.NET.SDK/actions/workflows/build.yml/badge.svg)
> ![tests-action-status](https://github.com/Yubico/Yubico.NET.SDK/actions/workflows/test.yml/badge.svg)
> ![tests-windows-action-status](https://github.com/Yubico/Yubico.NET.SDK/actions/workflows/test-windows.yml/badge.svg)
> ![tests-ubuntu-action-status](https://github.com/Yubico/Yubico.NET.SDK/actions/workflows/test-ubuntu.yml/badge.svg)
> ![tests-macos-action-status](https://github.com/Yubico/Yubico.NET.SDK/actions/workflows/test-macos.yml/badge.svg)
> ![codeql-analysis-action-status](https://github.com/Yubico/Yubico.NET.SDK/actions/workflows/codeql-analysis.yml/badge.svg)

# .NET YubiKey SDK

This is a cross-platform, all encompassing SDK for the YubiKey aimed at large to mid-sized enterprise
customers. This version is written against .NET Core, and will eventually include bindings to languages
outside the direct .NET ecosystem.

## Quick Start
```csharp
// Installation
dotnet add package Yubico.YubiKey
```

## Documentation

The public documentation for this project is located
at [https://docs.yubico.com/yesdk/](https://docs.yubico.com/yesdk/).  
Here you can find both API reference and a user's manual that describes the concepts that this SDK exposes.

## SDK Support
The SDK is targeting net47, netstandard2.0 and netstandard2.1.  
This means the SDK can be loaded in NET Framework, NET6 and upwards.

## SDK Packages

The SDK consists of the following assemblies:

### Public Assemblies

#### Yubico.YubiKey
Primary assembly containing all classes and types needed for YubiKey interaction.

#### Yubico.Core
Platform abstraction layer (PAL) providing:
- OS-specific functionality abstraction
- Device enumeration
- Utility classes for various encoding/decoding operations:
    - Base32
    - Tag-Length-Value (BER TLV)
    - ModHex

### Internal Assemblies

#### Yubico.DotNetPolyfills
> ⚠️ **Not for public use**  
> Backports BCL features needed by the SDK. Target newer .NET versions directly if you need modern features.

#### Yubico.NativeShims
> ⚠️ **Not for public use**  
> 🔧 **Unmanaged Library** 
> Provides stable ABI for P/Invoke operations in Yubico.Core.

## Project structure

The root of this repository contains the various projects that make up the SDK. Inside each project
folder, you will find:

- docs - Supplementary documentation content for the SDK's API documentation.
- examples - Example code demonstrating various capabilities of the SDK.
- src - All source code that makes up the project.
- tests - Unit and integration tests for the project.

## Contributing

Please read the [Contributor's Guide](./CONTRIBUTING.md) and [Getting started](./contributordocs/getting-started.md)
pages before opening a pull request on this project.

### Building

Read the [Getting started](./contributordocs/getting-started.md) page to understand the prerequisites needed
to build. Once those have been installed, you should be able to load the Yubico.NET.SDK.sln file and build.

Note that it is also possible to build the DocFX output at the same time as building the libraries. However,
that is not done by default.

If you want to build the DocFX output when you build the libraries using Visual Studio, open the Visual
Studio solution file, and open `Build:Configuration Manager...`. In the resulting window, under
`Active solution configuration:` is a drop-down menu. Select `ReleaseWithDocs`.

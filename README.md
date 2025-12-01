<!-- Copyright 2025 Yubico AB

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
> [![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/Yubico/Yubico.NET.SDK/badge)](https://scorecard.dev/viewer/?uri=github.com/Yubico/Yubico.NET.SDK)

# .NET YubiKey SDK

Enterprise-grade cross-platform SDK for YubiKey integration, built on .NET.

## Table of Contents
- [.NET YubiKey SDK](#net-yubikey-sdk)
  - [Table of Contents](#table-of-contents)
  - [Quick Start](#quick-start)
    - [Installation](#installation)
    - [Basic Usage](#basic-usage)
  - [Documentation](#documentation)
  - [SDK Support](#sdk-support)
  - [SDK Packages](#sdk-packages)
    - [Public Assemblies](#public-assemblies)
      - [Yubico.YubiKey](#yubicoyubikey)
      - [Yubico.Core](#yubicocore)
    - [Internal Assemblies](#internal-assemblies)
      - [Yubico.NativeShims](#yubiconativeshims)
  - [Build asset attestation](#build-asset-attestation)
  - [Project Structure](#project-structure)
  - [Contributing](#contributing)
    - [Building the Project](#building-the-project)
  - [Connect with us](#connect-with-us)

## Quick Start

### Installation
```bash
dotnet add package Yubico.YubiKey
```

### Basic Usage
```csharp
using Yubico.YubiKey;

// Chooses the first YubiKey found on the computer.
IYubiKeyDevice? GetFirstYubiKey()
{
    IEnumerable<IYubiKeyDevice> list = YubiKeyDevice.FindAll();
    return list.First();
}
```

## Documentation

📚 Official documentation: [docs.yubico.com/yesdk](https://docs.yubico.com/yesdk/)
- User Manual
- API Reference

## SDK Support

Supported Target Frameworks:
- .NET Framework 4.7.2
- .NET Standard 2.1
- .NET 6 and above

Linux Compatibility:
- Native libraries built with glibc 2.28+ support

## SDK Packages

### Public Assemblies

#### Yubico.YubiKey
Primary assembly containing all classes and types needed for YubiKey interaction.

#### Yubico.Core
Platform abstraction layer (PAL) providing:
- OS-specific functionality abstraction
- Device enumeration
- Utility classes for various encoding/decoding operations:
  - Base16
  - Base32
  - Tag-Length-Value (BER Encoded TLV)
  - ModHex

### Internal Assemblies

#### Yubico.NativeShims
> ⚠️ **Not for public use**  
> 🔧 **Unmanaged Library**  
> Provides stable ABI for P/Invoke operations in Yubico.Core.

## Build asset attestation
Using [GitHub Attestations](https://docs.github.com/en/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds), you are able to verify that the NuGet package has been generated from our repository, right down to the specific GitHub Action that built it like this:  
`> gh attestation verify .\Yubico.Core.1.12.0.nupkg --repo Yubico/Yubico.NET.SDK`

Note that you need to have the [`gh`](https://cli.github.com/) CLI installed.

## Project Structure

Repository organization:
- 📁 `docs/` - API documentation and supplementary content
- 📁 `examples/` - Sample code and demonstrations
- 📁 `src/` - Source code for all projects
- 📁 `tests/` - Unit and integration tests

## Contributing

1. Read the [Contributor's Guide](./CONTRIBUTING.md)
2. Review [Getting Started](./contributordocs/getting-started.md)
3. Submit your Pull Request

### Building the Project

Prerequisites:
1. Install required tools (see [Getting Started](./contributordocs/getting-started.md))
2. Load `Yubico.NET.SDK.sln` into your IDE.
3. Build solution

---

## Connect with us

📫 Need help? [Create an issue](https://github.com/Yubico/Yubico.NET.SDK/issues/new/choose)  
📖 Read our blog for the latest Yubico updates [here](https://www.yubico.com/blog/)

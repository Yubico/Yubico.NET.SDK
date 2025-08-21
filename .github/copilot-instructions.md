# Copilot Instructions for Yubico.NET.SDK

This document provides instructions for the Copilot coding agent to work efficiently with the Yubico.NET.SDK repository.

## High-Level Details

- **Repository Summary**: The Yubico.NET.SDK is an enterprise-grade, cross-platform SDK for integrating YubiKey functionality into .NET applications. It provides APIs for device communication, cryptography, and management of YubiKey features. The protocols used include FIDO2, CTAPP, OATH, PIV, SCP3, SCP11 and OTP.
- **Project Type**: This is a .NET library project, consisting of multiple sub-projects.
- **Languages**: The primary language is C#.
- **Frameworks and Runtimes**:
    - .NET Framework 4.7
    - .NET Standard 2.1
    - .NET 6 and later
- **Key Dependencies**: The solution has a dependency on the `Yubico.NativeShims` project, which provides a stable ABI for P/Invoke operations.
- The project makes use of an .editorconfig file to maintain consistent coding styles across the codebase.

## Build Instructions

To build, test, and validate changes, follow these steps.

### Building the Solution

You can build the entire solution from the root of the repository using the following command:

```bash
dotnet build Yubico.NET.SDK.sln
```

Alternatively, you can use the pre-configured build tasks in Visual Studio Code:

- `build project: Yubico.Yubikey`: Builds the `Yubico.YubiKey` project.
- `dotnet: build`: A general-purpose build task.

### Running Tests

The repository contains both unit and integration tests.

- **Unit Tests**:
    - `run unit tests: Yubico.YubiKey`: Runs unit tests for the `Yubico.YubiKey` project.
    - `run unit tests: Yubico.Core`: Runs unit tests for the `Yubico.Core` project.

- **Integration Tests**:
    - Integration tests require a YubiKey plugged in and are categorized. You can run them using the provided tasks:
        - `run tests: integration (Simple)`
        - `run tests: integration (RequiresTouch)`
        - `run tests: integration (Elevated)`
        - `run tests: integration (RequiresBio)`
        - `run tests: integration (RequiresSetup)`
        - `run tests: integration (RequiresStepDebug)`
        - `run tests: integration (RequiresFips)`

## Project Layout

- **`Yubico.Core/`**: Contains the platform abstraction layer (PAL) for OS-specific functionality, device enumeration, and utility classes. It connects to devices such as HID and Smart Cards.
- **`Yubico.YubiKey/`**: The primary assembly with all the classes and types for YubiKey interaction.
- **`Yubico.NativeShims/`**: An internal project that provides a stable ABI for P/Invoke operations. It is not intended for public use. This is a C project that wraps native code for easier use in .NET.
- **`contributordocs/`**: Contains detailed documentation for contributors, including coding guidelines, testing procedures, and code flow information.
- **`.github/workflows/`**: Contains GitHub Actions workflows for continuous integration, including builds, tests, and code analysis.

## Validation and CI

The repository uses GitHub Actions for continuous integration. The following workflows are run on pull requests:

- `build.yml`: Builds the solution.
- `build-nativeshims.yml`: Builds the native shims C project.
- `test.yml`: Runs tests on macOS, Windows, and Ubuntu.
- `codeql-analysis.yml`: Performs static code analysis using CodeQL.

Before submitting a pull request, ensure that your changes build successfully and that all relevant tests pass. Adhering to the coding style is also crucial.

## Documentation

The repository includes XML documentation comments for public APIs. To generate the documentation, use the following command:

```bash
dotnet docfx docfx.json
```

This will create a `docs` folder with the generated documentation.
The published documentation can be found at `https://docs.yubico.com/yesdk/`. You may search this website for specific topics or API references.

## Exceptions
The exceptions strings are stored in the ./Yubico.Core/src/Resources/ExceptionMessages.resx and ./Yubico.YubiKey/src/Resources/ExceptionMessages.cs files.

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

# How to install the SDK

Installing the .NET YubiKey SDK can be done using the NuGet package manager.

## Supported versions of .NET

The YubiKey SDK targets .NET Standard 2.0 for wide compatibility. .NET Standard is not an implementation
of .NET, but instead describes the minimum set of requirements for an implementation. You can read more
about .NET Standard and what it means [here](https://docs.microsoft.com/en-us/dotnet/standard/net-standard).

Targeting 2.0
means that this SDK can be used by:

- **.NET 5.0** or newer
- **.NET Core 2.0** or newer
- **.NET Framework 4.6.1** or newer
- **Mono 5.4** or newer
- **UWP 10.0.16299** or newer
- **Xamarin.Mac 8.0**

While the Xamarin.iOS and Xamarin.Android frameworks are technically supported by .NET Standard 2.0,
this SDK does not currently support the iOS or Android platforms.

## Adding the NuGet package reference

The official SDK releases can be found on the NuGet package manager under the
[Yubico organization](https://www.nuget.org/profiles/Yubico). The package to install is called
[Yubico.YubiKey](https://www.nuget.org/packages/Yubico.YubiKey/).

### Using Visual Studio

Adding the SDK to your project using Visual Studio can be done in a few steps:

1. With your solution open, right click on the project you wish to add the dependency to in the solution
   explorer tool window.
2. Make sure the package source, located in the top right of the NuGet Package Manager window, is set
   to `nuget.org`.
3. Click on the `Browse` tab and search for `Yubico.YubiKey`.
4. Click on the `Install` button. NuGet will display a list of the SDK's dependencies. Click `Accept`.
   NuGet will then display the license information for the project and dependencies. Read and
   accept the license agreements to continue.
   
Now your project is ready to use the YubiKey SDK! Start by adding

```c#
using Yubico.YubiKey;
```

to the top of your source file to get started.

> [!NOTE]
> In order to install a pre-release version of the SDK, you need to make sure the "Include Prerelease"
> checkbox is checked in the NuGet Package Manager window.

### Using the dotnet CLI

You can add the SDK to your project using the `dotnet` command line tool. Use the [`add package`
command](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package). The package name
for the SDK is `Yubico.YubiKey`.

Use the following example invocation as reference:

```txt
dotnet add <path/to/your/project.csproj> package Yubico.YubiKey
```

> [!NOTE]
> In order to install a pre-release version of the SDK, include the `--prerelease` parameter.


### Editing your project manually

This section assumes the project file format is the "SDK-style" seen in .NET Core and .NET 5+ projects.
Open the `*.csproj` file for your project in your favorite text editor. You should see something like
the following:

```xml
<Project Sdk="Microsoft.NET.Sdk">

   <PropertyGroup>
      <OutputType>Exe</OutputType>
      <TargetFramework>net5.0</TargetFramework>
   </PropertyGroup>

</Project>
```

Insert a new `ItemGroup` tag pair after the property group, or use an existing group that contains
other package references. Add a `PackageReference` tag with an `Include` attribute set to the name
of the SDK package: `Yubico.YubiKey`. Add a `Version` attribute and enter the latest version number
of the SDK found on NuGet.

Your project file should now look something like:

```xml
<Project Sdk="Microsoft.NET.Sdk">

   <PropertyGroup>
      <OutputType>Exe</OutputType>
      <TargetFramework>net5.0</TargetFramework>
   </PropertyGroup>

   <ItemGroup>
      <PackageReference Include="Yubico.YubiKey" Version="1.0.0-Beta.20210618.1" />
   </ItemGroup>

</Project>
```

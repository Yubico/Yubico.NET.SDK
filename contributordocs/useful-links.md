<!-- [Copyright 2021 Yubico AB]()

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Useful links
These are some of the external resources that were useful while developing the SDK.

## .NET / C#
- [Introducing .NET 5](https://devblogs.microsoft.com/dotnet/introducing-net-5/)
- [.NET team's blog](https://devblogs.microsoft.com/dotnet/)
- [.NET Core Runtime source repo](https://github.com/dotnet/runtime/)

## P/Invoke and Native Interop
### General Guidance
- [.NET Core / CoreFX guidance on platform invoke in a cross platform environment](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/interop-guidelines.md)
- [Additional .NET Core guidance on platform invoke](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/interop-pinvokes.md)
- [Mono Project documentation on P/Invoke](https://www.mono-project.com/docs/advanced/pinvoke/)

### Interop on macOS
- [Let's bind an IOKit method by hand](https://medium.com/@donblas/lets-bind-an-iokit-method-by-hand-fba939b54222)
- [Example P/Invoking IOKit with Xamarin](https://gist.github.com/chamons/82ab06f5e83d2cb10193)
- [MonoMac-IOKit-USBDevice project](https://github.com/Lunatix89/MonoMac-IOKit-USBDevice/blob/master/MonoMac.IOKit/MonoMac.IOKit/IOKitInterop.cs)

### Calling .NET from Native Code
- [Calling C# natively from Rust](https://medium.com/@chyyran/calling-c-natively-from-rust-1f92c506289d)
- [CoreRT - A .NET Runtime for AOT (Ahead-of-Time compilation)](https://mattwarren.org/2018/06/07/CoreRT-.NET-Runtime-for-AOT/)
- [.NET Embedding](https://docs.microsoft.com/en-us/xamarin/tools/dotnet-embedding/)

## Build
### 'SDK Style' projects
- [Target frameworks in SDK-style projects](https://docs.microsoft.com/en-us/dotnet/standard/frameworks)
- [.NET Core Runtime Identifier (RID) catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)
- [Full list of RIDs](https://github.com/dotnet/corefx/blob/master/src/pkg/Microsoft.NETCore.Platforms/runtime.json)
- [Additions to the csproj format for .NET Core](https://docs.microsoft.com/en-us/dotnet/core/tools/csproj)
- [Renovate your .NET solution](https://cezarypiatek.github.io/post/renovate-your-dot-net-solution/)
- [Moving to SDK Style projects and package references](http://hermit.no/moving-to-sdk-style-projects-and-package-references-in-visual-studio-part-1/)
- [Demystifying the SDK Project](https://dansiegel.net/post/2018/08/21/demystifying-the-sdk-project)
- [Where is the full documentation about the csproj format for .NET Core](https://stackoverflow.com/questions/45096549/where-is-full-documentation-about-the-csproj-format-for-net-core)
- [Converting Xamarin Libraries to SDK style multi-targeted projects](https://montemagno.com/converting-xamarin-libraries-to-sdk-style-multi-targeted-projects/)
- [.NET Core 3.0 SDK Projects: Controlling Output Folders and Content](https://weblog.west-wind.com/posts/2019/Apr/30/NET-Core-30-SDK-Projects-Controlling-Output-Folders-and-Content)

### General MSBuild
- [MSBuild concepts](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-concepts?view=vs-2019)

### Reference Assemblies
- [Main reference assembly documentation on docs.microsoft.com](https://docs.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies)
- [Create and pack reference assemblies](https://oren.codes/2018/07/03/create-and-pack-reference-assemblies/)
- [Create and pack reference assemblies made easy](https://oren.codes/2018/07/09/create-and-pack-reference-assemblies-made-easy/)

## Logging and Tracing
- [Reporting metrics using .NET (Core) EventSource and EventCounter](https://dev.to/expecho/reporting-metrics-using-net-core-eventsource-and-eventcounter-23dn)
- [Logging in .NET Core and ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1)
- [.NET Core logging and tracing](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/logging-tracing)
- [System.Diagnostics.Tracing namespace](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.tracing?view=netstandard-2.1)
- [What's the future of log abstractions like ASP.NET 5 ILogger, LibLog, EventSource, etc...](https://github.com/aspnet/Logging/issues/332)

## Polyfills
- [Wikipedia: Polyfill (programming)](https://en.wikipedia.org/wiki/Polyfill_(programming))
- [Type forwarding in the CLR](https://docs.microsoft.com/en-us/dotnet/standard/assembly/type-forwarding)
- [How .NET Standard uses type forwarding](https://www.youtube.com/watch?v=vg6nR7hS2lI&feature=youtu.be)
- [Preprocessor #if and target framework symbols](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-if)
- [Nuget package - matching assembly versions and the target framework in a project](https://docs.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks#matching-assembly-versions-and-the-target-framework-in-a-project)

## Design Inspiration
### .NET design patterns
- [Builder design pattern](https://code-maze.com/builder-design-pattern/)
- [Fluent builder design pattern](https://code-maze.com/fluent-builder-recursive-generics/)

### Other code
- [DeviceInformation WinRT class](https://docs.microsoft.com/en-us/uwp/api/windows.devices.enumeration.deviceinformation)
- [Device information properties](https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/device-information-properties)
- [SmartCardReader.FromIdAsync](https://docs.microsoft.com/en-us/uwp/api/windows.devices.smartcards.smartcardreader.fromidasync)
- [Windows-universal-samples/NFC/PcscSdk](https://github.com/microsoft/Windows-universal-samples/blob/master/Samples/Nfc/PcscSdk/PcscUtils.cs)

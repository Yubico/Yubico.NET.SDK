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

# Software and tools

This project is built for multiple platforms using various .NET technologies.

## Recommended IDEs

### Installing Visual Studio for Windows (VSfW)

Download Visual Studio [here](https://visualstudio.microsoft.com/vs/).

1. Install the following workflows:

    - .NET desktop development
    - Universal Windows Platform development
    - .NET Core cross-platform development

2. Once that is installed, you should have everything you need to launch the top level solution (sln)
   files and start contributing!

### Installing Visual Studio for Mac (VSfM)

Download Visual Studio for Mac [here](https://visualstudio.microsoft.com/vs/mac/).

1. Install the following workflows:

    - .NET desktop development
    - Universal Windows Platform development
    - .NET Core cross-platform development

2. Once that is installed, you should have everything you need to launch the top level solution (sln)
   files and start contributing!

### Installing Visual Studio Code

VSCode can be useful for quickly navigating and reading code, or editing build files, however that is
roughly the extent to which it can be used right now. Unit tests that do not depend on Yubico.Core
also run successfully. To get set up with VSCode:

1. Download and install .NET Core 3.1 for your system [here](https://dotnet.microsoft.com/download).
2. Open the project root in VSCode.
3. Install the 'C#' Extension from Microsoft, and the '.NET Core Test Explorer' from Jun Han.
4. In the '.NET Core Test Explorer' Extension settings, set the 'Test Project Path' to
   `*/tests/*UnitTests.csproj`. You can easily access the setting by typing
   `@ext:formulahendry.dotnet-test-explorer dotnet-test-explorer.testProjectPath` in the settings
   search bar.
5. Perform a build (`Cmd + shift + B` on macOS, `Ctrl + shift + B` on Windows). You should have a
   silent and successful build (you can see progress in the bottom status bar). If this fails, run
   `dotnet restore` in the Yubico.YubiKey and Yubico.Core directories.
6. Once it's successfully built, run the tests by opening the .NET Core Test Explorer pane (the
   'beaker' icon on the left). Tests that use Yubico.Core will fail on non-Windows platforms.

Other plugins of interest include AsciiDoc, EditorConfig, Markdown All in One (Yu Zhang) and IntelliCode.

## Loading the projects

Visual Studio uses "Solution Files" (\*.sln) as their top level project file. A solution can contain
one or more 'Project File' (\*.csproj, \*.cxproj, etc.) that represents a single binary (\*.exe,
\*.dll, \*.so, etc.) output. In .NET, an output binary is often called an 'assembly'.

The .NET YubiKey SDK project has a single solution file located in the root of the repository called
Yubico.NET.SDK.sln.

Within the solution there are three groupings of projects: Yubico.YubiKey, Yubico.Core, and
Yubico.DotnetPolyfills.

Each project folder may have one or more subfolders:

- src: The production source-code for this project
- docs: Extra documentation pages and collateral
- tests: Unit and integration tests for the project

CSharp projects are defined in *.csproj files. These files can be hand edited, though in most
circumstances editing these files should not be necessary.

Avoid loading the csproj projects directly, and instead favor using the Solution file. The Solution
contains all of the projects required to build and test that assembly, and have the correct build
order and dependencies in place.

## A crash course in .NET

The best place to start is with Microsoft's .NET documentation. The
[.NET Guide](https://docs.microsoft.com/en-us/dotnet/standard/) is a high level page which directs
you to various important topics to learn.

Specifically, please refer to:

- [.NET Architectural Components](https://docs.microsoft.com/en-us/dotnet/standard/components)
- [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
- [.NET Core overview](https://docs.microsoft.com/en-us/dotnet/core/about)

These documents will help to disambiguate ".NET Standard", ".NET Framework", and ".NET Core".

### Resources for developers coming from other platforms

#### Java

C# is similar in many respects to Java, however there are plenty of differences. Microsoft has put
together an excellent set of material, including side-by-side examples of Java / C# in this
[Moving to C# and the .NET Framework, for Java Developers](https://www.microsoft.com/en-us/download/details.aspx?id=6073)
package.

#### Python

Since Python and C# are not from the same language family, there are unfortunately less illustrative
guides, however this GitHub user has put together a decent cheat-sheet comparing major functionality:
[C# For Python Programmers](https://gist.github.com/mrkline/8302959).

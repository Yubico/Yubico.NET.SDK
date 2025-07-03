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

# Setup

For most common developer scenarios, you should not have to do anything to set up your system. We
have updated the Visual Studio project files to take care of this automatically.

To begin contributing to the documentation, simply move on to [Comments in the code](./comments-in-code.md).

But if you want to learn about the system, especially if you are interested in the command line
version, read on.

## Getting DocFX

There are two ways to use DocFX:

Integrate into the project's build system using NuGet (their documentation refers to this as
"[Integrating with Visual Studio](https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html#3-use-docfx-integrated-with-visual-studio)"),
though that’s not entirely accurate as Visual Studio is not required.

Download and use it manually at the command line.

## Integrate with the build system (Visual Studio / MSBuild)

Start by adding the DocFX package to your Visual Studio project. This can be done through the UI by
right clicking on the solution or project file within Solution Explorer and selecting "Manage NuGet
Packages...". Click on "Browse", make sure package source is "[nuget.org](https://www.nuget.org/)"
and search for "docfx.console" (no quotes).

> ℹ️ **Note** that this does not load DocFX into Visual Studio, but loads it into the project. If you
> want to use DocFX in another Visual Studio project, you must load it again.

Alternatively, you can edit the `.csproj` file and add this in the `ItemGroup`

```xml
    <PackageReference Include="docfx.console" Version="2.52.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
```

If you have taken these steps, you won't have to install DocFX. It will be done automatically during
the “restore” phase of building the project.

This has already been done for most of the .NET SDK projects. If we add additional projects that
require documentation, the steps outlined on this page are what should be followed.

For more information, read [DocFX Getting Started](https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

## Command Line

The command line version makes it easier to put elements into specified directories. It also makes
it easier to use options (log file or not e.g.), as well as alter the templates.

To get the command line version, the easiest method is to use Chocolatey on Windows or Homebrew on Mac.

## Create the Project

There is DocFX documentation
at [https://dotnet.github.io/docfx/](https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).
However, this section provides a brief description of what you need to know to set up a project. It
includes information not in or not easily found in the official documentation.

For the command line version, call `docfx init` to build the structure. Run this in the directory
where you want the docs subdirectory to be.

For example, suppose we want the documentation for `Yubico.YubiKey` to be in a directory called `docs`:

```txt
  - Yubico.NET.SDK
    - Yubico.Core
    - Yubico.YubiKey
      - src
      - tests
      - docs
```

Create the `docs` directory (don't cd into that directory). Run the `init` command. Use the `-o`
option to specify the directory, and the `-q` (quiet) option so there is less information written
to the screen.

```shell
$ cd Yubico.NET.SDK/Yubico.YubiKey
$ md docs
$ docfx init -q -o docs
```

### Point to the Source Code

Now look in the `docs` directory. There you will find a `docfx.json` file (among other things). Edit
this file to point to the directory with the source code. There is a section that looks like this.

```json
  "metadata": [
    {
      "src": [
        {
          "files": [
            "**.csproj"
          ],
        }
      ],
      "dest": "api",
      "disableGitFeatures": false,
      "disableDefaultFilter": false
    }
  ],
```

Edit it to indicate where the `src` directory is and point it to the project file.

```json
  "metadata": [
    {
      "src": [
        {
          "files": [
            "src/**.csproj"
          ],
          "src": "../"
        }
      ],
      "dest": "api",
      "disableGitFeatures": false,
      "disableDefaultFilter": false
    }
  ],
```

The `"src": "../"` says to look up one directory from the current one (the `docfx.json` file is in
`docs`) to find where to look for project files. The project file is in `..\src"`). Changing the
value for the JSON key `"files"` said to look in the `src` directory and all sub directories for any
file with the file extension `csproj`. In that file is where you will find the list of files to use
in building the documentation.

It's somewhat confusing, there is a key (of key/value pair) called `"src"` that simply tells DocFX
where to look relative to where the JSON file is. Then there is the `src` directory itself. It would
have been better if DocFX used a different key (such as `"dir"`) but it uses `"src"`, so we just
have to live with it.

### Run

Now run DocFX. See [Building the docs and running](./building-docs-and-running.md).

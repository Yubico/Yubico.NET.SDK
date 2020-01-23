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

This directory contains code common to all the .NET YubiKey SDK sample code. It
currently contains the Menu code and the List and Choose YubiKey methods.

The project in this directory will build a class library. It will also use nuget to obtain
the SDK itself.

Each sample program (e.g. PIV or OATH) will "link" to this library. In that way, each
sample program will have access to common code and the SDK.

To build a new sample program using Visual Studio, create a new project and add this
project to the new one.

For example, suppose we want to build a new sample program for PIV operations.

  Launch Visual Studio
  Choose "Create a new project"
  Choose C# Console Application (click Next)
  Give the project a name (e.g. PivSampleCode) and location (e.g.
    Yubico.NET.SDK/Yubico.YubiKey/examples and click Next

You now have a project in which you can build your sample code. To add the shared code,
add this SharedSampleProgram to your new project.

  In the "Solution Explorer" pane, right click on the Solution line (for our example of a
    project named "PivSampleCode, it will be the line "Solution 'PivSampleCode'"). Choose
  Add:Existing Project.
  Navigate to this directory and choose SharedSampleCode.csproj.

Once the project is added to the sample program's Solution, you will see two projects
under the Solution line (in our example, one line is "PivSampleCode" and the other is
"SharedSampleCode"). Expand your new project (in our example, click on the triangle to the
left of "PivSampleCode"). The resulting list of components includes an element called
"Dependencies". Right click on the word "Dependencies".

Choose "Add Project Reference...". In the resulting window, make sure the left pane has
"Projects:Solutions" selected. In the right pane should be one option, "SharedSampleCode".
Make sure the box next to that is checked. Click OK.

Now write your sample code and build. You will have access to the shared code (a menu and
methods to list and choose YubiKeys), along with the SDK.

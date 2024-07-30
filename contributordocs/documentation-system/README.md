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

# Documentation system

This section contains information on how the .NET SDK is to be documented.

Currently, we are only using DocFX to build documentation. In the future there might be more resources
and tools.

## Types of documentation

Many products come with multiple forms of documentation, often falling into one of two buckets: a
"User's Manual" and a "Reference Manual". The User's Manual gives a high-level view of the product
and how to use it. Very often it gives examples of what functions to call, in what order, to accomplish
something. The Reference Manual has the details of each call. For example, a User's Manual will say
something like, "The first thing you need to do for any application is call the `InitLibrary` function."
The Reference Manual will give details on the `InitLibrary` function itself, what arguments are needed,
what they mean, what the return value is.

For the .NET SDK project, we will strive to have four types of documents:

- User's manual:
    - **Conceptual documentation**: What is this thing? Why would I use it?
    - **Tutorials**: How do I use the SDK? Show me, from the ground up, how to do common tasks in the SDK.
    - **How-To articles**: How do I accomplish this very specific task, with example code?
- Reference manual:
    - **API reference**: What are all of the types, methods, properties, and parameters, and what
      specifically do they do?

## DocFX: API reference generator

C# has a system of documenting source code with specially formatted comments that can be converted
to "professional-looking" html pages. These pages will make our reference manual.

In order to start contributing, you need to perform three steps. One, setup your system to use DocFX;
two, format the comments following a particular pattern; and three, use the DocFX tool to convert
those comments into html.

It is also possible to build user manual documents with DocFX (not just the source code comments),
so we will use DocFX here as well, unless a better tool / system presents itself to us.

### Source Code Comments and DocFX

There are specific documents describing each step:

1. [Setup](./setup.md)
2. [Comments in the code](./comments-in-code.md)
3. [Building the docs and running](./building-docs-and-running.md)

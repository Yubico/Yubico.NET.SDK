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

# Coding standards

## Framework design guidelines

The .NET framework team has published their best practices for .NET-style framework (API) design. A subset of the
guidelines are [posted online](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/). The best
resource, however, is
the [“Framework Design Guidelines” book by Cwalina, et. al.](https://www.amazon.com/Framework-Design-Guidelines-Conventions-Addison-Wesley/dp/0135896460/ref=sr_1_1?crid=B6MTRR8KZCD1&dchild=1&keywords=framework+design+guidelines&qid=1599875133&sprefix=framework+design+%2Caps%2C210&sr=8-1)
It is currently in its third edition. This book contains the complete set of rules, along with commentary from the
.NET designers themselves.

We apply these guidelines strictly, and a cursory understanding of the rules - and how to quickly look them up -
is essential for any code contributor looking to add or modify the public API surface of this SDK.

✅ **DO** read the first three chapters of the Framework Design Guidelines book: Introduction, Framework Design
Fundamentals, and Naming Guidelines.

These chapters set the foundation for the design principles our team strives to follow.

✅ **DO** refer to the rest of the book as a reference guide. If you know you will be designing a certain type,
refresh your knowledge of the guidelines by reading the relevant sections.

It’s impossible to commit the entire rule set to memory. Some rules can be enforced by code analyzers, but not
all. View the book (and website) as a reference to refer to on-demand.

## Style guidelines

Appendix A of the Framework Design Guidelines book covers generally the style preference for this project.

The repository also defines an [.editorconfig](https://editorconfig.org/) file which codifies these rules and
can be checked by editors and IDEs that understand .editorconfigs.

✅ **DO** follow the style guidelines. If in doubt - ask the team, or at least defer to the style found throughout
the rest of the file or directory.

✅ **CONSIDER** using an editor that supports the .editorconfig standard and has auto-formatting features. This
way you can focus more on writing code rather than getting the style right.

The Visual Studio IDE or JetBrains Rider is recommended, but Visual Studio Code, Sublime Text, and others will work
just as well.

## Code analyzers

This project makes heavy usage of static code analyzers. The project is essentially compiling at `-Werror -Wall
-Wpedantic -Wextra` and then some...

Code analyzers are run at build time. No extra steps are needed to run them.

The analyzers have been tuned to strike a good balance between pedantry and expressiveness. However, there are
still some cases where an error needs to be suppressed. If an error is suppressed, justification MUST be provided
in the form of a comment (in the case of `#pragma`) or inline (in the case of `[SuppressAttribute]`).

✅ **DO** Always provide justification for suppressing a warning. This leaves notes for future maintainers as to
why it was deemed necessary at the time.

❌ **DO NOT** change global suppression rules or suppress a warning at a scope larger than a single line. Rules
may change, but they should be discussed with the entire team prior to making the change.

## Arrays, collections, and buffers

### General guidance for collections that are not \<byte>

When choosing a data structure to use in this project, be sure to reference the Framework Design Guidelines'
sections on arrays and collections. A few notable guidelines on arrays:

> ✅ **DO** prefer collections over arrays.
>
> ✅ **CONSIDER** using arrays in low-level APIs to minimize memory consumption and maximize performance.
>
> ✅ **DO** use byte arrays instead of collections of bytes.
>
> ❌ **DO NOT** use arrays for properties if the property would have to return a new array.

There are some additional considerations for choosing collection types:

❌ **DO NOT** use concrete collection types (i.e. List, Dictionary, etc.) in public interfaces.

✅ **DO** use the generic collection interface that best suits the need (i.e. IList, IDictionary, ICollection),
keeping the following two guidelines in mind as well:

✅ **DO** use the most flexible and generic interface as *input* to a method. For example, favor IEnumerable over
ICollection, over IList.

✅ **CONSIDER** using a more specific interface as *output* from a method.

### Byte buffer guidance

✅ **CONSIDER**
using [Memory- and Span-related types](https://docs.microsoft.com/en-us/dotnet/standard/memory-and-spans/)
on the public interface for shared references to an array.

Due to the low level nature of this project, it is common for data to be represented as a sequence of bytes. This
kind of data works very well with Memory and Span since it is the array as a whole which has meaning, not the
individual elements.

```C#
/// Unlikely candidate for Memory
/// <summary>List of version strings of CTAP supported by the authenticator.</summary>
public string[] Versions { get; set; }

/// Good candidate for Memory
/// <summary> ...a 128-bit identifier indicating the type of authenticator.</summary>
public byte[] AAGuid { get; set; }
```

Memory and Span can represent references to a contiguous region of managed or unmanaged memory, and are designed
to be used in pipelines. That is, they are designed so that some or all of the data can be efficiently passed to
components in the pipeline, which can process them and optionally modify the buffer.

```C#
/// This code should consider the alternative
public IReadOnlyList<byte>? Data { get; set; }

/// This would provide better flexibility in memory backing and
/// was specifically designed for efficient use in pipelines
public ReadOnlyMemory<byte> Data { get; set; } = ReadOnlyMemory<byte>.Empty;
```

✅ **DO** adhere to
the [Memory<T> and Span<T> usage guidelines](https://docs.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines).
This document contains information on ownership, lifetime management, consumption, and member design. They are
copied here, and augmented with additional findings, for easy reference:

When to use what:

- Use `ReadOnlySpan<T>` or `ReadOnlyMemory<T>` if the buffer should be read-only

- For a **synchronous API**, use `Span<t>` instead of `Memory<t>` as a **parameter** if possible.

- If you plan on making a “defensive-copy” of a parameter value, use `Span<t>` instead of `Memory<t>`

- If a class is wrapping a buffer and storing a reference to it, the constructor of the object should use
  `Memory<t>` instead of `Span<t>`.

Lifetime and ownership assumptions:

- If your **constructor** accepts `Memory<t>` as a **parameter**, instance methods on the constructed object
  are assumed to be consumers of the `Memory<t>` instance.

- If you have a **settable** `Memory<t>`-typed **property** (or an equivalent instance method) on your type,
  instance methods on that object are assumed to be consumers of the `Memory<t>` instance.

- If your **method** accepts `Memory<t>` and **returns void**, you must not use the `Memory<t>` instance
  after your method returns.

- If your **method** accepts a `Memory<t>` and **returns a Task**, you must not use the `Memory<t>` instance
  after the Task transitions to a terminal state.

Advanced use cases:

- If you’re wrapping a synchronous P/Invoke method, your API should accept `Span<t>` as a parameter.

- If you’re wrapping an asynchronous P/Invoke method, your API should accept `Memory<t>` as a parameter.

- If you have an `IMemoryOwner<T>` reference, you must at some point dispose of it or transfer its ownership
  (but not both).

- If you have an `IMemoryOwner<T>` parameter in your API surface, you are accepting ownership of that instance.

✅ **DO** use documentation to clearly communicate the ownership and consumption model. These Memory-related
types can be accessed by multiple components or by multiple threads, so clear expectations and contracts are
vital. For example:

```C#
/// <summary>
/// Creates an instance of <see cref="ReadOnlySequence{T}"/> from the <see cref="ReadOnlyMemory{T}"/>.
/// Consumer is expected to manage lifetime of memory until <see cref="ReadOnlySequence{T}"/> is not used anymore.
/// </summary>
public ReadOnlySequence(ReadOnlyMemory<T> memory)
```

✅ **CONSIDER** returning a byte array in cases where you want to return a copy of a method’s results.

## Designing constructors

In addition to the Framework Design Guidelines' ruleset, there are some additional heuristics that can be
applied to determining how many constructors to have and of what type.

There are three main ways an object can be constructed with input data:

1. Calling a constructor with arguments:

   `var obj = new Foo(param1, param2, param3);`

2. Calling a parameter-less default constructor followed by setting properties:

    ```csharp
    var obj = new Foo();
    obj.Param1 = param1;
    obj.Param2 = param2;
    obj.Param3 = param3;
    ```

3. Using object-initialization syntax. This leverages settable properties, but constructs the object in a
   thread-safe manner:

   `var obj = new Foo { Param1 = param1, Param2 = param2, Param3 = param3 };`

There is, of course, a fourth style of construction which is a combination of the above, but for the purposes
of this section we can set that aside.

✅ **CONSIDER** Supporting object-initialization for your object. Data transfer objects (such as the Command
objects in the SDK) are prime candidates. However, not all objects may be well suited for this model. The
following is a heuristic to help determine whether object-initialization should be supported:

- Does the property lack a reasonable default? Note that 0 and FooEnum.Unknown are reasonable defaults.
- Is the property an array or collection? Then consider creating a method called SetFoo() instead of a
  property. (Or use ReadOnlySpan / ReadOnlyMemory)
- Is the input a secret? Then we probably shouldn't use a property and should use a method (like above).
- Is the parameter mandatory? Have a constructor which takes this parameter.
- Is the parameter optional? Consider having a constructor without the parameter and fall back on construction
  style 2, 3, or 4.
- Is it optional but frequently used? Consider having an additional constructor overload with it as a parameter.
- Are there only 1-3 inputs total? Create a constructor and you can consider not having settable properties.
- You should still consider get-only properties so that the object is easier to visualize while debugging.

When determining how many non-default constructors to have, consider the following:

- Estimate how often each parameter will be given a non-default value and create concentric sets. *Consider*
  giving each set a constructor. Note that this would not apply to default constructors, as there, no
  parameters are available and all would be set through properties.
- If the difference between two constructors is only one or two parameters of basic or trivially constructable
  types, consider only choosing the constructor with more parameters.

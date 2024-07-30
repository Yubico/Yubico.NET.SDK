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

# Comments in the code

In order to automatically create the html file, we have to create the content. We do that by formatting the
comments in the C# source files.

For example, suppose you have a class to generate random numbers.

```C#
namespace Random
{
  public class DigestPrng
  {
    public DigestPrng (Digester digester) { }
    int AddSeed (byte[] seedData) { }
    int GenerateRandomBytes (byte[] randomBytes, int index, int randomLen) { }
  }
}
```

Now add the comments.

```C#
namespace Random
{
  /// <summary>
  /// This class uses a message digest to generate random bytes.
  /// </summary>
  /// <remarks>
  /// The caller creates a message digest object. This <c>DigestPrng</c> class
  /// will use that <c>Digester</c> object to process the seed data and to generate
  /// the random output.
  /// <p>
  /// For example,
  /// <code>
  ///  SHA256 sha256 = new SHA256 ();
  ///  Digester digester = new Digester (sha256);
  /// <b/>
  ///  DigestPrng = new DigestPrng (digester);
  /// </code>
  /// </p>
  /// <p>
  /// It uses an internal state and a counter buffer. The seed data updates
  /// the internal state. When random bytes are needed, the object will digest
  /// the internal state. When the output of one digest are exhausted, update
  /// the internal state with the counter buffer.
  /// </p>
  /// Although it is possible to share an object of this class among threads,
  /// it is recommended not to.
  /// </remarks>
  public class DigestPrng
  {
    /// <summary>
    /// Constructor, the caller must provide an instance of the Digester
    /// class.
    /// </summary>
    /// <remarks>
    /// The digester will be the foundation of the PRNG.
    /// <p>
    /// The constructor will clone the Digester object, so that the PRNG
    /// object will have its own digester object.
    /// </p>
    /// </remarks>
    /// <param name = "digester>
    /// An instance of Digester, which might perform SHA-1, SHA-256, or some
    /// other algorithm
    /// </param>
    public DigestPrng (Digester digester) { }

    /// <summary>
    /// Add new seed material.
    /// </summary>
    /// <remarks>
    /// The method will use all the bytes in the buffer. You can call AddSeed
    //  at any time. It can be called two or more times in a row, it can be
    /// called in between calls to Generate. If you call AddSeed after a call
    /// to Generate, the new seed material will update the internal state, not
    /// reset it.
    /// </remarks>
    /// <param name = "seedData">
    /// The buffer containing the seed material to use.
    /// </param>
    /// <returns>
    /// An int, 0 for no error, or a non-zero error code.
    /// </returns>
    int AddSeed (byte[] seedData) { }

    /// <summary>
    /// Generate <c>randomLen</c> bytes, placing them into the <c>randomBytes</c>
    /// buffer, beginning at <c>index</c>.
    /// </summary>
    /// <remarks>
    /// If you want to fill an entire buffer with random bytes, you would pass in
    /// <c>randomBytes,0,randomBytes.Length</c>.
    /// </remarks>
    /// <param name = "randomBytes">
    /// The buffer where the random output will be deposited.
    /// </param>
    /// <param name = "index">
    /// The index in <c>randomBytes</c> where the random output will begin.
    /// </param>
    /// <param name = "randomLen">
    /// The number of bytes to be placed into the buffer.
    /// </param>
    /// <returns>
    /// An int, 0 for no error, or a non-zero error code.
    /// </returns>
    int GenerateRandomBytes (byte[] randomBytes, int index, int randomLen) { }
  }
}
```

Notice that this is not that different from what you would normally do. Instead of using `/* */` or `//` you're using
`///`. Also there are the tags, such as `<summary>`,`<remarks>`, `<para>`, `<code>`, and `<c>`. Plus, there are extra
sections for detailed descriptions of the input arguments and return value.

## Which Tags

There are many possible documentation tags to use. Not all may be necessary. For a recommended subset, see
[Recommended Tags for Documentation Comments](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/recommended-tags-for-documentation-comments).

In the example earlier, notice in the `code` section, before the `DigestPrng = new` line, there is a line that
is `<br/>`.
This is because the DocFX software, at the time of this writing, does not handle blank lines in `code` sections
contained
within the remarks. It does handle it as expected if the `code` section is contained within an `examples` tag. If you
have a blank line, the resulting html will not be quite right. So you may need to replace blank lines with a `/// <b/>`.

Notice also that each of the opening and closing tags are on separate lines. That is not necessary. You can write

```C#
/// <summary>The summary of whatever</summary>
```

However, I recommend putting the tags on separate lines. It makes the comments easier to read.

## Documenting Namespaces

When you build the html, there will be pages for each namespace. On that page will be hyperlinks to each class that is
in the namespace.

Suppose you want to supply a description of a namespace in general. You could put some comments into a source file,
but DocFX will not capture comments about a namespace.

Go to the `docs` directory, it is a peer of the `src` directory where the `docfx.json` file is located. In that
directory
should be a subdirectory called `namespaces`.

Look in the `docfx.json` file, there should be a section with the key `"overwrite"`. Make sure under `"files"` there is
`"namespaces/**.md"`.

```json
    "overwrite": [
      {
        "files": [
          "apidoc/**.md",
          "namespaces/**.md"
        ],
        "exclude": [
          "obj/**",
          "_site/**"
        ]
      }
    ],
```

For each namespace to document, add an `md` file. Give it the name of the namespace itself. For example, if the
namespace
is `Random`, create the file `Random.md`. The file contains the following.

```markdown
---
uid: Random
summary: *content
---

The `Random` namespace is a set of classes that perform Random Number Generation.

## Detailed Description

and so on
```

The top of the file must include the metadata banner shown above (including the '---' lines). Additional metadata items
exist, however the only metadata required at this moment is what's listed in the above example.

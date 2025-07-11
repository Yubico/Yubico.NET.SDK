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

<!-- Copyright (c) .NET Foundation and Contributors
See LICENSE.txt in project root for full license.
source: https://github.com/dotnet/runtime/blob/3ce1168233140e01890568b605ce9b2d7767a50e/CONTRIBUTING.md -->

# Contributing to the .NET YubiKey SDK

👍🎉 First off, thank you for taking the time to contribute! 🎉👍

The .NET YubiKey SDK is principally developed and maintained by Yubico, and licensed under
[Apache License v2.0](./LICENSE.txt). A great way to contribute is by filing issues (see the Issues
tab) for any problems or ideas you have. PRs are welcome too, though be sure to follow the guidelines
defined in this document.

## Contribution Acceptance Criteria

Project maintainers will consider changes that improve the product significantly and broadly align
with the product roadmap.

Maintainers will not consider changes that have narrowly-defined benefits, due to compatibility risk
(several companies are building products on the SDK). Contributions must also satisfy the other
published guidelines defined in this document.

## DOs and DON'Ts

Please do:

* ✅ **DO** follow our [coding style](./contributordocs/coding-guidelines/README.md).
* ✅ **DO** give priority to the current style of the project or file you're changing even if it
  diverges from the general guidelines.
* ✅ **DO** start by adding a test when fixing a bug. It should highlight how the current behavior
  is not working as intended.
* ✅ **DO** keep the discussions focused. When a new or related topic comes up it's often better to
  create a new issue than to sidetrack the discussion.

Please do not:

* ❌ **DON'T** surprise us with big pull requests. Instead, file an issue so we can discuss whether
  it fits our current roadmap and the best way to handle it.
* ❌ **DON'T** add API additions without filing an issue and discussing with us first.
* ❌ **DON'T** make PRs for style changes.
* ❌ **DON'T** commit code that you didn't write. If you find code that you think is a good fit to
  add to the SDK, file an issue and start a discussion before proceeding.
* ❌ **DON'T** submit PRs that alter licensing related files or headers. If you believe there's a
  problem with them, file an issue and we'll be happy to discuss it.

## Breaking Changes

Contributions must maintain API signature and behavioral compatibility. If your idea or change may
include a breaking change, start by filing an issue so discussions can occur.

## Suggested Workflow

We use and recommend the following workflow:

1. Create an issue for your work.
    - You can skip this step for trivial changes.
    - Reuse an existing issue on the topic, if there is one.
    - Get agreement from the team that your proposed change is a good one.
    - Clearly state that you are going to take on implementing and testing it, if that's the case.
      You can request that the issue be assigned to you. Note: The issue filer and the implementer
      don't have to be the same person.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. In your fork, follow our [Gitflow process](./contributordocs/code-flow-and-pull-requests.md).
    - Create a branch off of `develop` (`git checkout -b mybranch develop`).
    - Name the branch so that it clearly communicates your intentions, such as `bugfix/issue-123` or
      `feature/githubhandle-issue`.
    - Branches are useful since they isolate your changes from incoming changes from upstream. They
      also enable you to create multiple PRs from the same fork.
4. [Add new tests](./contributordocs/testing.md) corresponding to your change, if applicable.
5. Make and commit your changes to your branch.
    - [Code Flow and PRs](./contributordocs/code-flow-and-pull-requests.md#getting-your-code-ready-for-review)
      explains how to build and test.
    - Your commits must
      be [signed](https://docs.github.com/en/github/authenticating-to-github/managing-commit-signature-verification/signing-commits).
    - Please follow our [Commit Messages](#commit-messages) guidance.
6. [Build the repository](./contributordocs/code-flow-and-pull-requests.md#getting-your-code-ready-for-review)
   with your changes.
    - Make sure that the builds are clean.
    - Make sure that the unit tests are all passing, including your new tests.
7. Create a pull request (PR) against the Yubico/Yubico.NET.SDK repository's **develop** branch.
    - State in the description what issue or improvement suggestion your change is addressing.
    - Check if all the Continuous Integration checks are passing.
8. Wait for feedback or approval of your changes from the necessary project maintainers.
    - Details about
      the [pull request procedure here](./contributordocs/code-flow-and-pull-requests.md#merging-into-develop).
9. When the necessary project maintainers have signed off, and all checks are green, your PR will be merged.
    - The schedule of official releases will be determined by project maintainers.
    - You can delete the branch you used for making the change.

## Commit Messages

Please format commit messages as follows (based
on [Go Contribution Guide](https://golang.org/doc/contribute#commit_messages)):

```
The first line is a short one-line summary of the change

The rest of the description elaborates and should provide context for
the change and explain what it does. Write in complete sentences with
correct punctuation, just like for your comments in Go. Don't use
HTML, Markdown, or any other markup language.

Add any relevant information, such as benchmark data if the change
affects performance. The benchstat tool is conventionally used to
format benchmark data for change descriptions.

As the last line in the message, you can include a reference to the
issue this commit addresses.

Fixes #42
```

Also do your best to factor commits appropriately, not too large with unrelated things in the same
commit, and not too small with the same small change applied N times in N different commits.

## Contributions Licensing

Contributions to this repository are subject to the GitHub Terms of Service. The licensing of
contributions is covered by
section [D.6 "Contributions Under Repository License"](https://docs.github.com/en/github/site-policy/github-terms-of-service#6-contributions-under-repository-license):

> Whenever you add Content to a repository containing notice of a license, you license that Content
> under the same terms, and you agree that you have the right to license that Content under those
> terms. If you have a separate agreement to license that Content under different terms, such as a
> contributor license agreement, that agreement will supersede.
>
> Isn't this just how it works already? Yep. This is widely accepted as the norm in the open-source
> community; it's commonly referred to by the shorthand "inbound=outbound". We're just making it
> explicit.

## File Headers

The following file header is used for the SDK. Please add it to all new files, using the appropriate
comment syntax.

```
Copyright 2025 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License").
You may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

- See [how-to-install.md](./docs/users-manual/getting-started/how-to-install.md) for
  an example of the header in a markdown file.
- See [YubiKeyDeviceInfo.cs](./Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyDeviceInfo.cs) for an example
  of the header in a C# file.

## PR - CI Process

The continuous integration (CI) system will automatically perform the required builds and run tests
(including the ones you are expected to run) for PRs. Builds and test runs must be clean.

If the CI build fails for any reason, the output of the CI test can be used to determine the cause
of the failure.

## PR Feedback

Yubico team and community members will provide feedback on your change. Community feedback is highly
valued, and one or more Yubico team members will review every PR prior to merge.

We encourage using [the guidelines listed here](./contributordocs/code-flow-and-pull-requests.md#doing-the-review)
when leaving or responding to feedback.

There are lots of thoughts and [approaches](https://github.com/antlr/antlr4-cpp/blob/master/CONTRIBUTING.md#emoji)
for how to efficiently discuss changes. It is best to be clear and explicit with your feedback.
Please be patient with people who might not understand the finer details about your approach to
feedback.

## Copying Files from Other Projects (Third-Party Works)

The SDK uses some files from other projects, typically where a NuGet distribution does not exist or
would be inconvenient. We use Apache Software Foundation projects as a reference for how to handle
these works:

- [ASF 3rd Party License Policy](https://www.apache.org/legal/resolved.html)
- [ASF Treatment of Third-Party Works](https://www.apache.org/legal/src-headers.html#3party)

The following rules must be followed for PRs that include files from another project:

- The license of the file is compatible with Apache License 2.0, such as
    - Apache License 2.0
    - Apache Software License 1.1
    - BSD (without advertising clause)
    - MIT/X11
    - [Full list here](https://www.apache.org/legal/resolved.html#category-a)
- The license of the file is left in-tact.
- The contribution is correctly attributed, as needed (generally in LICENSE.txt or NOTICE.txt in
  project root).

See [CryptographicOperations.cs](./Yubico.Core/src/System.Security.Cryptography/CryptographicOperations.cs) for an example of a
file copied from another project and attributed in the [LICENSE.txt](./LICENSE.txt) file.

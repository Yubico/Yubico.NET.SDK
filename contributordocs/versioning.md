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

# Versioning

Yubico.YubiKey and Yubico.Core versions are incremented following
[Semantic Versioning (SemVer) 2.0.0](https://semver.org/spec/v2.0.0.html). In contrast to 1.0.0, the
pre-release notation in 2.0.0 uses [dot separated identifiers](https://semver.org/spec/v2.0.0.html#spec-item-9)
and identifiers consisting of only digits are compared numerically for
[determining precedence](https://semver.org/spec/v2.0.0.html#spec-item-11). This allows for incrementing
pre-release versions indefinitely while maintaining intended precedence.

To edit the version numbers for the project, edit the file `\build\Versions.props`.

Prerelease versions should use the following nomenclature: Alpha (internal), Beta (public), RC (public).
We should also add the date to differentiate between these types of builds. Finally, a build number should
be appended to the end. We should likely only ever have a single build, so it will almost always be `1` -
but we include this just in case we need to spin a second build. For example:

`1.0.0-alpha.2021.03.01.1` - An alpha build that was snapshotted on March 1st, 2021. First build official build
for the day.

`1.1.0-beta.2021.11.11.2` - A beta build that was snapshotted on November 11th, 2021. It is the second build
for that day (meaning another release/tag exists called 1.1.0-beta.2021.11.11.1)

We only need to rev the version when we plan to snapshot a release. This should always happen prior to
releasing updated packages on NuGet.

While both projects have similar version numbers early on in the project - they may diverge
over time. We should avoid updating the revision for the sake of updating the revision (except maybe
during major releases). This avoids churning our customer's package feeds.

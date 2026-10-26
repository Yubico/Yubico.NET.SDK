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

# Building the combined V1 and V2 documentation

The existing site contains the V1 manual and API references, plus the V2 YubiKit API
reference. V1 URLs remain unchanged; V2 pages are under `v2-api/`.

## Prerequisites

Install Python 3.9 or newer, Git, and both .NET SDKs specified by the V1 and V2
`global.json` files (currently .NET 9 and .NET 10). The build helper installs
DocFX 2.81.0 into `.docbuild/tools`; a global DocFX install is unnecessary.

From the repository root, create the isolated V2 checkout:

```shell
git clone --depth 1 --branch yubikit https://github.com/Yubico/Yubico.NET.SDK.git .docbuild/yubikit
python3 build/build-docs.py
```

On Windows, use `python` instead of `python3`. To refresh an existing checkout,
run `git -C .docbuild/yubikit pull --ff-only` before building. CI checks out the
latest `yubikit` commit on every run. A local build uses the supplied checkout as-is.

For a checkout or SDK installed elsewhere:

```shell
python3 build/build-docs.py --v2-source /path/to/yubikit --v2-dotnet /path/to/dotnet
```

V2 restores using its own `nuget.config`. If the selected source requires private
Yubico packages, configure the corresponding feed credentials locally; never put
tokens in a tracked file. CI uses its existing package-read token.

## Generation and configuration

- `docfx.json` defines V1 metadata and the combined site.
- `docfx.v2.json` explicitly selects the ten V2 SDK library projects. Update this
  list and the V2 landing page when adding a module. CLI and test projects are excluded.
- `build/build-docs.py` restores the projects, generates each version's metadata
  from its own working directory/SDK, then renders one site with warnings as errors.
- Metadata is regenerated into `.docbuild/metadata/`; the helper cleans that staging
  directory and `docs/_site/` to remove obsolete pages. Generated YAML is then copied
  beside each authored API index so DocFX resolves navigation correctly; old generated
  YAML there is replaced. Authored Markdown stays in `docs/`.
- Logs are in `.docbuild/logs/`. `docs/_site/build-info.json` records the two source
  commit IDs and DocFX version. Uncommitted local changes are included in a local build.

Do not run the bare `docfx docfx.json` command for a fresh combined build: it generates
only V1 metadata. Use the helper to ensure both versions are present. After a successful
build, `.docbuild/tools/docfx build docfx.json` can render prose-only changes without
regenerating APIs.

## Preview locally

```shell
python3 -m http.server 8080 --bind 127.0.0.1 --directory docs/_site
```

Open [the V2 API reference](http://localhost:8080/v2-api/index.html), or
[the site home page](http://localhost:8080/). This serves the generated site locally;
production adds the existing `/yesdk/` prefix.

## CI and publishing

The main build uploads the combined site as the existing `Documentation` artifact.
Documentation-only pull requests get a separate preview build. Both workflows keep
logs on failure, and neither publishes an incomplete combined site. The existing
upload and deployment controls still determine when the site goes live.

Generated source checkouts, tools, metadata, and HTML must not be committed.

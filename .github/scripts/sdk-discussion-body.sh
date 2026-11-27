#!/usr/bin/env bash
# Copyright 2026 Yubico AB
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Prints the GitHub Discussions announcement for an SDK version: the release date and bullets
# of its whats-new.md section without PR links, followed by release and NuGet links.
# Assumes the section follows .claude/skills/Release/Templates/whats-new-section.md.
# Usage: sdk-discussion-body.sh <version>
set -euo pipefail

version=$1
whats_new=docs/users-manual/getting-started/whats-new.md
repository=${GITHUB_REPOSITORY:-Yubico/Yubico.NET.SDK}

awk -v heading="### $version" '
  $0 == heading { found = 1; next }
  found && /^(###|_+$)/ { exit }
  found && /^Release date: / { print; print ""; next }
  found && /^- / { sub(/ \(\[#[0-9]+\]\([^)]*\)(, \[#[0-9]+\]\([^)]*\))*\)$/, ""); print }
' "$whats_new"

echo
echo "[Release notes](https://github.com/$repository/releases/tag/$version) · [Yubico.YubiKey on NuGet](https://www.nuget.org/packages/Yubico.YubiKey/$version) · [Yubico.Core on NuGet](https://www.nuget.org/packages/Yubico.Core/$version)"

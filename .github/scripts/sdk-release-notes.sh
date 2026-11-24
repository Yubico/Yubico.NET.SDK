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

# Prints the GitHub release notes for an SDK version: its section of whats-new.md with bold
# category headings, followed by a full changelog link from the previous stable release.
# Usage: sdk-release-notes.sh <version>
# Requires GH_TOKEN and GITHUB_REPOSITORY.
set -euo pipefail

version=$1
whats_new=docs/users-manual/getting-started/whats-new.md

section=$(awk -v heading="### $version" '
  $0 == heading { found = 1; print; next }
  found && /^##/ { exit }
  found && !/^_+$/ { print }
' "$whats_new")

if [ -z "$section" ]; then
  echo "::error::$whats_new has no '### $version' section." >&2
  exit 1
fi

sed -E 's/^([A-Z][A-Za-z /]*):$/**\1**:/' <<<"$section"

previous=$(
  {
    gh api --paginate "repos/$GITHUB_REPOSITORY/tags" --jq '.[].name' | grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' || true
    echo "$version"
  } | sort -u -V | grep -x -B1 -F "$version" | head -n 1
)

if [ "$previous" != "$version" ]; then
  echo
  echo "**Full Changelog**: https://github.com/$GITHUB_REPOSITORY/compare/$previous...$version"
fi

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

# Fails a release run before anything is built if it could not be published.
# Usage: release-preflight.sh <version> <tag> <allowed-branches, comma-separated> <package-id>...
# Requires GH_TOKEN and the GitHub Actions environment.
set -euo pipefail

version=$1
tag=$2
allowed_branches=$3
shift 3

fail() {
  echo "::error::$*"
  exit 1
}

semver='^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(\.(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?$'
[[ "$version" =~ $semver ]] || fail "A release needs a three-component SemVer version, got '$version'."

branch=${GITHUB_REF#refs/heads/}
[[ "$GITHUB_REF" == refs/heads/* && ",$allowed_branches," == *",$branch,"* ]] ||
  fail "Releases run only from: $allowed_branches (this run is on $GITHUB_REF)."

if gh api "repos/$GITHUB_REPOSITORY/git/ref/tags/$tag" > /dev/null 2>&1; then
  fail "Tag $tag already exists."
fi

for id in "$@"; do
  lower_id=$(tr '[:upper:]' '[:lower:]' <<<"$id")
  lower_version=$(tr '[:upper:]' '[:lower:]' <<<"$version")
  url="https://api.nuget.org/v3-flatcontainer/$lower_id/$lower_version/$lower_id.nuspec"
  if curl --silent --fail --head --output /dev/null "$url"; then
    fail "$id $version is already on NuGet.org."
  fi
done

echo "Release preflight passed: $tag from $branch."

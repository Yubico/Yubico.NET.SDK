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

# Posts the SDK release announcement to GitHub Discussions (Announcements), titled with the
# version. Does nothing if an announcement with that title already exists, so re-runs are safe.
# Usage: sdk-announce-discussion.sh <version>
# Requires GH_TOKEN and GITHUB_REPOSITORY.
set -euo pipefail

version=$1
category=announcements
owner=${GITHUB_REPOSITORY%/*}
name=${GITHUB_REPOSITORY#*/}

ids=$(gh api graphql -f owner="$owner" -f name="$name" -f category="$category" -f query='
  query($owner: String!, $name: String!, $category: String!) {
    repository(owner: $owner, name: $name) {
      id
      discussionCategory(slug: $category) { id }
    }
  }' --jq '.data.repository | "\(.id) \(.discussionCategory.id)"')
read -r repository_id category_id <<<"$ids"

existing=$(gh api graphql -f owner="$owner" -f name="$name" -f category="$category_id" -f query='
  query($owner: String!, $name: String!, $category: ID!) {
    repository(owner: $owner, name: $name) {
      discussions(first: 100, categoryId: $category, orderBy: {field: CREATED_AT, direction: DESC}) {
        nodes { title url }
      }
    }
  }' --jq ".data.repository.discussions.nodes[] | select(.title == \"$version\") | .url")

if [ -n "$existing" ]; then
  echo "Announcement for $version already exists: $existing"
  exit 0
fi

body=$(bash "$(dirname "$0")/sdk-discussion-body.sh" "$version")

gh api graphql -f repository="$repository_id" -f category="$category_id" -f title="$version" -f body="$body" -f query='
  mutation($repository: ID!, $category: ID!, $title: String!, $body: String!) {
    createDiscussion(input: {repositoryId: $repository, categoryId: $category, title: $title, body: $body}) {
      discussion { url }
    }
  }' --jq '"Posted announcement: \(.data.createDiscussion.discussion.url)"'

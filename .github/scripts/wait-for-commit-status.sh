#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${GITHUB_TOKEN:-}" ]]; then
  echo "GITHUB_TOKEN must be set."
  exit 1
fi

if [[ -z "${REF:-}" ]]; then
  echo "REF must be set."
  exit 1
fi

ORG=${ORG:-Yubico}
REPO=${REPO:-docs-gitops}
KUSTOMIZATION_NAME=${KUSTOMIZATION_NAME:-docs}

timeout_minutes=15
end_time=$(date -ud "+$timeout_minutes minutes" +%s)

echo "Looking for status:"
echo "  Repo: $ORG/$REPO"
echo "  Ref: $REF"
echo "  Status context: kustomization/$KUSTOMIZATION_NAME"
echo

state=""
message_printed=false
while [[ -z "$state" ]]; do
  state=$(gh api "/repos/$ORG/$REPO/commits/$REF/status" | jq -r ".statuses[] | select(.context | startswith(\"kustomization/$KUSTOMIZATION_NAME/\")).state")

  if [[ -n "$state" ]]; then
    echo "Status: $state"
    break
  fi

  if [[ $(date -u +%s) -ge $end_time ]]; then
    echo "Deployment was not complete after $timeout_minutes minutes."
    exit 1
  fi

  if [[ "$message_printed" == "false" ]]; then
    echo "Waiting for deployment to complete (timeout: $timeout_minutes minutes)..."
    message_printed=true
  fi
  sleep 10
done

if [[ "$state" != "success" ]]; then
  exit 1
fi

exit 0

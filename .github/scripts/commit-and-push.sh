#!/usr/bin/env bash
set -euo pipefail

commit_msg=$(
cat <<EOF
Update $DOC_PATH

Using image tag $IMAGE_TAG

Triggered by workflow: https://github.com/${GITHUB_REPOSITORY}/actions/runs/${GITHUB_RUN_ID}
Triggered by actor: ${GITHUB_ACTOR}
EOF
)

if git diff --exit-code --quiet; then
  echo "Nothing to commit. Skipped git commit/push."
  exit 0
fi

git add --all
git commit --message "$commit_msg"

max_retries=10
retry_count=0
while [ $retry_count -lt $max_retries ]; do
  git fetch origin "$GITOPS_BRANCH"
  git rebase "origin/$GITOPS_BRANCH"

  if git push; then
    echo "Push successful"
    break
  else
    ((retry_count += 1))
    if [ $retry_count -ge $max_retries ]; then
      echo "Max retries reached. Exiting with failure."
      exit 1
    fi

    echo "Failed to push. Retrying..."
    sleep 5
  fi
done

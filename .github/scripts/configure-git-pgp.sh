#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${PGP_KEY:-}" ]]; then
  echo "PGP_KEY must be set."
  exit 1
fi

echo "$PGP_KEY" | gpg --import

key_id=$(gpg --list-secret-keys --with-colons | awk -F: '$1 == "sec" {print $5}')
git config --global commit.gpgsign "true"
git config --global user.signingKey "${key_id}"
echo "Using PGP key for git commit signing: ${key_id}"

pgp_user_id=$(gpg --list-keys --with-colons "$key_id" | awk -F: '$1 == "uid" {print $10}')
pgp_name=$(echo "$pgp_user_id" | cut --delimiter '<' --fields 1)
pgp_email=$(echo "$pgp_user_id" | cut --delimiter '<' --fields 2 | cut --delimiter '>' --fields 1)
git config --global user.name "${pgp_name}"
git config --global user.email "${pgp_email}"

echo "Git name: ${pgp_name}"
echo "Git email: ${pgp_email}"

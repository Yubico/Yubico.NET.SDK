# 19 Documentation workflow runs an unpinned installer with a write token (YESDK-1627)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction. The installer does check a SHA-256 digest. The gap is that the digest is not pinned in the repository, and that the generator jobs hold a write-scoped token. |
| Our severity | MED |
| Root cause | CI configuration. Not device or SDK code. |
| Fix group | A (permissions, persisted credentials, pinned digest). The question of who approves digest changes is an open decision (see below). |
| Evidence | static: workflow read, installer read (not run), manifest fetched, vendor and GitHub documentation. No workflow run, no unit or hardware test. |
| Since the audit | `.github/workflows/docs-update.yml` is unchanged at current yubikit. |

## What the audit says

The documentation workflow installs Claude Code by piping a mutable script into a shell, and it runs the installed binary with an OAuth token and a write-capable repository token in the same job. A compromise of the external distribution endpoint would reach a credentialed step.

> "It is conditional on compromise of an explicitly trusted external distribution endpoint -> not an existing contributor-controlled RCE -> but that endpoint currently receives code-execution authority immediately before a credentialed operation."

Abbreviations: CI is continuous integration. OAuth is open authorization, the credential the Claude step uses. PR is pull request. RCE is remote code execution. SHA-256 is the 256-bit secure hash algorithm. npm is the Node.js package manager. SDK is software development kit. The Claude agent is the large language model run by the Claude Code command-line tool. A write-scoped token can change repository contents and open pull requests.

## What is right

- **The installer is fetched and run without a pin.** `curl -fsSL https://claude.ai/install.sh | bash -s 2.1.199` runs at [docs-update.yml:81](../../../.github/workflows/docs-update.yml#L81) (migration) and [docs-update.yml:191](../../../.github/workflows/docs-update.yml#L191) (architecture). The script is fetched from a live URL each time. Nothing in the repository fixes its bytes.
- **Write permissions reach the generator jobs.** The workflow sets `contents: write` and `pull-requests: write` for every job ([docs-update.yml:17-19](../../../.github/workflows/docs-update.yml#L17-L19)). The migration and architecture jobs inherit them, but they only produce patch artifacts. Only `create-pr` needs write access, to open the pull request ([docs-update.yml:306-316](../../../.github/workflows/docs-update.yml#L306-L316)).
- **The checkouts keep the token on disk.** Neither generator checkout sets `persist-credentials: false` ([docs-update.yml:31-35](../../../.github/workflows/docs-update.yml#L31-L35), [docs-update.yml:152-156](../../../.github/workflows/docs-update.yml#L152-L156)). The `actions/checkout` documentation says the token "is persisted in the local git config" until post-job cleanup.
- **The OAuth token goes to the Claude steps.** It is set in the environment of the migration step ([docs-update.yml:85-86](../../../.github/workflows/docs-update.yml#L85-L86)) and the architecture step ([docs-update.yml:195-196](../../../.github/workflows/docs-update.yml#L195-L196)), and the binary runs at [docs-update.yml:92](../../../.github/workflows/docs-update.yml#L92) and [docs-update.yml:202](../../../.github/workflows/docs-update.yml#L202).
- **The architecture agent can run shell commands.** Its allowed tools include `Bash` ([docs-update.yml:202](../../../.github/workflows/docs-update.yml#L202)).
- **The trust boundary is trusted-branch events.** The workflow has no `pull_request` trigger. It runs on pushes to `yubikit` ([docs-update.yml:9-14](../../../.github/workflows/docs-update.yml#L9-L14)), on manual dispatch ([docs-update.yml:15](../../../.github/workflows/docs-update.yml#L15)), and on `workflow_call` ([docs-update.yml:4-8](../../../.github/workflows/docs-update.yml#L4-L8)). No caller of `workflow_call` was found among the nine workflows.
- **The artifact split already exists.** The generator jobs upload patches and `create-pr` applies them with path checks ([docs-update.yml:280-297](../../../.github/workflows/docs-update.yml#L280-L297)). The permission split does not.

## What is wrong or imprecise

- **"Unverified" overstates it.** The installer checks the binary's SHA-256 against the `checksum` field in a manifest it fetches from the same origin ([install.sh, `checksum_matches`](https://claude.ai/install.sh)). So the binary is checked against its manifest. The real gap is that no digest is pinned in this repository, so an origin that changes both the manifest and the binary passes the check.
- **The version argument does not pin the bootstrap.** The script first downloads whatever `/latest` names, then runs that binary's `install` subcommand with `2.1.199`. The argument selects what gets installed. The binary that runs is the current latest. The audit's statement that `2.1.199` "does not pin or validate the installer itself" is correct.
- **The install step is also a credentialed step.** The audit's source-to-sink path starts the credentialed work at the Claude step. But the install step runs the downloaded binary's `install` subcommand while the checkout's write-scoped token is still on disk, and before the OAuth token is set. The exposure starts one step earlier than the audit says.
- **Native updates are not disabled.** The Claude Code documentation says: "Native installations automatically update in the background." The workflow does not set `DISABLE_UPDATES`. The documentation says `DISABLE_UPDATES` "block[s] all update paths, including manual updates". So the binary can replace itself at run time, from the same origin, outside any pin.
- **The architecture agent can read both credentials.** The workflow runs the Claude CLI directly, not through `claude-code-action`. The reviewed material shows no secret scrubbing for that run. The agent's shell commands therefore inherit the OAuth token from the environment, and can read the write-scoped token from `.git/config`. Nothing in the reviewed material shows that the agent does this. No attempt was made to exploit it.
- **Line references.** The audit's range for the PR job (lines 250-312) is loose. The job starts at line 258, and the PR action is at lines 306-316.

## Why it matters

- **Who can trigger it.** Anyone with write access to `yubikit` (a push that changes source, package or project files, since the installer runs only when the impact check finds them) or who can start the workflow manually. A fork pull request cannot activate it.
- **What the endpoint compromise gains.** Code execution on the runner in the install step, with the write-scoped token on disk. Then the OAuth token in the Claude step's environment. The compromised step can steal credentials and use the repository token for direct writes within its permissions, independently of the patch pipeline. Protected-branch restrictions and review requirements were not checked. The normal generated-patch path creates a pull request, which a person must still review and merge.
- **Preconditions.** A changed or compromised external endpoint, and a run of the workflow. Runs happen on pushes to `yubikit` that touch source paths, so the window is not rare.
- **Severity.** The likelihood depends on the external endpoint, which the repository does not control. The impact is credential theft and direct repository writes within the token's permissions, not only an unwanted pull request. The rating stays MED, as in the verification report. The branch-protection check is the open item that could change it.

## Specification

No device or protocol specification applies. The governing references are the following.

> "It's good security practice to set the default permission for the `GITHUB_TOKEN` to read access only for repository contents. The permissions can then be increased, as required, for individual jobs within the workflow file." (GitHub, Secure use reference, "Use secrets for sensitive information")

> "Pinning an action to a full-length commit SHA is currently the only way to use an action as an immutable release." (GitHub, Secure use reference, "Using third-party actions")

Source: [GitHub Secure use reference](https://docs.github.com/en/actions/reference/security/secure-use).

> "This check determines whether the project's automated workflows tokens follow the principle of least privilege." and "Set any required write permissions at the job-level. Only set the permissions required for that job; do not set `permissions: write-all` at the job level." (OpenSSF Scorecard, Token-Permissions)

> "A "pinned dependency" is a dependency that is explicitly set to a specific hash instead of allowing a mutable version or range of versions." (OpenSSF Scorecard, Pinned-Dependencies)

Source: [OpenSSF Scorecard checks](https://github.com/ossf/scorecard/blob/main/docs/checks.md). These are check criteria and guidance, not a score for this repository.

> "The auth token is persisted in the local git config. This enables your scripts to run authenticated git commands. The token is removed during post-job cleanup. Set `persist-credentials: false` to opt-out." (actions/checkout, README at the commit pinned in the workflow)

Source: [actions/checkout README at 93cb6efe](https://github.com/actions/checkout/blob/93cb6efe18208431cddfb8368fd83d5badbf9bfd/README.md).

> "To block all update paths, including manual updates, set DISABLE_UPDATES instead." (Claude Code documentation, "Disable auto-updates")

Source: [Claude Code advanced setup](https://code.claude.com/docs/en/setup).

The 2.1.199 manifest, fetched on the review date, lists the `linux-x64` checksum as `b31dfd5e3dee23b51c42e0d8ddb405148978237d3aabc8cbbf77c5cf83367e27`. The manifest has no signature field. It is the same value as in the sketch.

## Canonical Python reference

Not applicable. This is a workflow, not a device protocol.

## Sibling SDKs (context only)

Not applicable.

## Reproduction

- Unit: none. The finding is in CI configuration.
- Hardware: none.
- Static checks run for this document:
  - `actionlint` on the current workflow: no findings.
  - The sketch in [yesdk1627-docs-update.patch](../evidence/scripts/yesdk1627-docs-update.patch) cannot be applied with `git apply`. The error is "patch with only garbage". The hunk headers have no line ranges, and line 1 is a note. The file is a readable diff, not a patch.
  - The sketch's edits were applied by hand to a scratch copy of the workflow. The result passes `actionlint` and parses as YAML. The top-level permissions are read-only, and only `create-pr` has write permissions. The create-pr checkout keeps persisted credentials, because the sketch does not change it.
  - A scratch repository check showed that `git apply` creates a symbolic link (mode 120000) from a patch, and that `git status` then lists it under the allowed path. Path checks based on names do not see this.

## Proposed fix

- **Recommendation** (the sketch implements items 1-3):
  1. Replace the pipe-to-shell install with a direct download of the fixed `linux-x64` release, verified against a SHA-256 digest committed in the repository. Use `DISABLE_UPDATES: '1'` on each Claude run. This is option (b) below.
  2. Set workflow-level permissions to `contents: read`, and grant `contents: write` and `pull-requests: write` only to `create-pr`.
  3. Set `persist-credentials: false` on the migration and architecture checkouts.
  4. Also set `persist-credentials: false` on the `create-pr` checkout, after confirming that `peter-evans/create-pull-request` does not need the persisted credential. Not tested. The `git fetch origin` steps then rely on anonymous access, which works for a public repository. A private repository needs a read-only token.
  5. In `create-pr`, reject patches that contain symbolic links or mode changes before `git apply`. A check on `git apply --summary` output, or on the patch text (`new file mode 120000`), is enough. The existing path check does not detect this.
  6. Keep the human review of the generated pull request before merge.
- **Options considered for the install** (from the CI review in [agent-reports/H.md](../evidence/agent-reports/H.md)):
  - (a) An exact npm dependency with a lockfile. Changes the distribution path, and still needs the permission split.
  - (b) A pinned release binary with a committed SHA-256. Smallest auditable change. Recommended.
  - (c) Pinning `anthropics/claude-code-action` to a commit SHA. Pins the action's code, not the Claude CLI it runs. Not sufficient alone.
  - (d) Splitting the generator from the writer. The jobs are already separate; only the job-level permissions are missing.
  - (e) `persist-credentials: false`. Removes the token from disk, but does not reduce the token's scope.
  Recommended combination: (b), (d) and (e), plus `DISABLE_UPDATES`.
- **Open decision: who owns the pinned binary digest.** The repository has no CODEOWNERS file, and Dependabot tracks only NuGet and GitHub Actions. No automation updates a curl-downloaded digest. Options:
  - A named maintainer group, with a CODEOWNERS entry for `.github/workflows/docs-update.yml` and the digest location.
  - A two-person review rule for any digest change, with the manifest URL and build date recorded in the commit message.
  - Automation that proposes new digests. It would fetch from the same origin, so it adds no independent check.
  Recommendation: a human approves each digest change and each pull request merge. The question for the maintainer is who that is, and how often the digest is updated. The sketch's digest was taken from the same origin's manifest, so it must be confirmed against an independent source before it is adopted. No independent, signed checksum was found in the sources reviewed.
- **API impact.** None. CI only.
- **Proving checks** after the change: `actionlint` passes; only `create-pr` has write permissions; the install step has no OAuth token; a deliberately wrong digest on a branch makes the install step fail; a `workflow_dispatch` run on a branch with migration impact produces a patch artifact and a pull request limited to the allowed paths.
- **Depends on / interacts with.** N4 (the monthly synthesis workflows have the same credential layout and are not covered by this sketch). The digest-owner decision.

## Check it yourself

From the worktree root:

```bash
actionlint .github/workflows/docs-update.yml
curl -fsSL https://downloads.claude.ai/claude-code-releases/2.1.199/manifest.json | jq -r '.platforms["linux-x64"].checksum'
```

The second command should print `b31dfd5e3dee23b51c42e0d8ddb405148978237d3aabc8cbbf77c5cf83367e27`, the digest in the sketch. It confirms only what the origin says now.

To read the installer, download it and read it. Do not pipe it to a shell:

```bash
curl -fsSL https://claude.ai/install.sh -o install.sh && less install.sh
```

Read in this order:

- [docs-update.yml:17-19](../../../.github/workflows/docs-update.yml#L17-L19) (permissions), [docs-update.yml:78-92](../../../.github/workflows/docs-update.yml#L78-L92) (migration install and run), [docs-update.yml:188-202](../../../.github/workflows/docs-update.yml#L188-L202) (architecture install and run).
- [docs-update.yml:258-316](../../../.github/workflows/docs-update.yml#L258-L316) (create-pr).
- [yesdk1627-docs-update.patch](../evidence/scripts/yesdk1627-docs-update.patch) (the sketch, which is a readable diff and not an applicable patch).

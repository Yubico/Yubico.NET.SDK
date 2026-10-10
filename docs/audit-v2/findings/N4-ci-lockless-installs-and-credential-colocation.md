# N4 CI lockless installs and credential co-location (no Jira ticket)

| | |
| --- | --- |
| Audit severity | None. This is a new finding from the verification. |
| Our verdict | Confirmed (static observation) |
| Our severity | LOW-MED |
| Root cause | CI configuration. Not device or SDK code. |
| Fix group | A for the lockfile and the job split. B for the design of the monthly workflows, which is a maintainer decision. |
| Evidence | static: workflow files, the shared version file, npm registry metadata, Dependabot configuration, and the action's documentation. Nothing was run. |
| Since the audit | `docs-update.yml`, both monthly synthesis workflows, `dependabot.yml`, `scripts/architecture/architecture-images.env` and `render-architecture.sh` are unchanged at current yubikit. `publish-alpha-feed.yml` changed in two lines above its `sleet` install, which is unchanged (line 73 at the branch base). |

## What this is about

Two CI problems, found while reviewing [#19](19-docs-workflow-unpinned-installer.md):

1. The Mermaid command-line tool is installed with `npm install -g` and no lockfile in two workflows. Its version pin covers only the top-level package.
2. The monthly synthesis workflows put the Claude OAuth token and a write-scoped repository token in the same job as a persisted checkout. The architecture workflow also gives the agent the `Bash` tool.

Abbreviations: CI is continuous integration. OAuth is open authorization, the credential used for Claude. PR is pull request. SDK is software development kit. npm is the Node.js package manager. A lockfile records the exact versions of every package in an install.

## What is right

- **The top-level version is pinned in one place.** `MERMAID_CLI_VERSION="11.16.0"` is set in [architecture-images.env:6](../../../scripts/architecture/architecture-images.env#L6), and both workflows read it.
- **The render script refuses a mismatched version.** It probes the installed `mmdc` and fails unless the version equals the pin ([render-architecture.sh:36-60](../../../scripts/architecture/render-architecture.sh#L36-L60)).
- **The Claude action is pinned to a full commit SHA.** Both monthly workflows use `anthropics/claude-code-action@769e3bdff9bb7ecfb51bad4c9cba23d6f5fc5ea5` ([architecture-docs-monthly-synthesis.yml:52](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L52), [migration-docs-monthly-synthesis.yml:44](../../../.github/workflows/migration-docs-monthly-synthesis.yml#L44)).
- **The monthly workflows are not triggered by pull requests.** The architecture workflow is `workflow_dispatch` only ([architecture-docs-monthly-synthesis.yml:3-6](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L3-L6)). The migration workflow also has a monthly schedule ([migration-docs-monthly-synthesis.yml:7-8](../../../.github/workflows/migration-docs-monthly-synthesis.yml#L7-L8)). Its own comment says schedules run only from the default branch.

## What is wrong or imprecise

- **No lockfile exists.** The repository has no `package.json`, `package-lock.json`, `yarn.lock` or `pnpm-lock` file. The install lines are [docs-update.yml:165](../../../.github/workflows/docs-update.yml#L165) and [architecture-docs-monthly-synthesis.yml:35](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L35): `npm install -g "@mermaid-js/mermaid-cli@$version"`.
- **The pin covers the top-level package only.** The dependencies of `@mermaid-js/mermaid-cli@11.16.0` are caret or range specifiers, from the registry metadata queried for this document: `mermaid ^11.14.0`, `chalk ^5.0.1`, `commander ^13.1.0`, `katex ^0.16.25`, `p-limit ^6.2.0`, `import-meta-resolve ^4.1.0`, `@mermaid-js/layout-elk` `^0.1.5 || ^0.2.0`, `@mermaid-js/mermaid-zenuml ^0.2.0` and `@fortawesome/fontawesome-free` `^6.0.0 || ^7.0.1`. Each run resolves that tree again. The result can change with no change in the repository.
- **Nothing tracks the tree.** [dependabot.yml](../../../.github/dependabot.yml) lists two ecosystems, `nuget` and `github-actions`. No npm entry exists, so no pull request proposes an update.
- **The write token is on disk when the install runs.** In the architecture job of `docs-update.yml`, the checkout persists the write-scoped token ([docs-update.yml:152-156](../../../.github/workflows/docs-update.yml#L152-L156)). The npm install runs in that job ([docs-update.yml:165](../../../.github/workflows/docs-update.yml#L165)), before the Claude steps. Any install-time script in the resolved tree would run with that token readable. This review did not list which dependencies have install scripts.
- **The monthly architecture job combines everything.** The OAuth secret ([architecture-docs-monthly-synthesis.yml:54](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L54)) and the repository token ([architecture-docs-monthly-synthesis.yml:55](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L55)) are both given to the action. The checkout persists the token ([architecture-docs-monthly-synthesis.yml:20-24](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L20-L24)). The npm install runs in the same job ([architecture-docs-monthly-synthesis.yml:31-36](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L31-L36)). The agent may use `Bash` ([architecture-docs-monthly-synthesis.yml:62](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L62)).
- **The monthly migration job combines the two credentials.** The OAuth secret ([migration-docs-monthly-synthesis.yml:46](../../../.github/workflows/migration-docs-monthly-synthesis.yml#L46)) and the repository token ([migration-docs-monthly-synthesis.yml:47](../../../.github/workflows/migration-docs-monthly-synthesis.yml#L47)) are in one job with a persisted checkout ([migration-docs-monthly-synthesis.yml:22-26](../../../.github/workflows/migration-docs-monthly-synthesis.yml#L22-L26)). Its agent has no `Bash` tool ([migration-docs-monthly-synthesis.yml:54](../../../.github/workflows/migration-docs-monthly-synthesis.yml#L54)).
- **The action's scrub does not apply here.** The action's input documentation says: "When this input is set, Claude does a best-effort scrub of Anthropic, cloud, and GitHub Actions secrets from subprocess environments." The input is `allowed_non_write_users`, which these workflows do not set ([claude-code-action action.yml at the pinned SHA](https://github.com/anthropics/claude-code-action/blob/769e3bdff9bb7ecfb51bad4c9cba23d6f5fc5ea5/action.yml)). The action's run step also passes `CLAUDE_CODE_OAUTH_TOKEN` into its environment.
- **Adjacent: an unpinned tool in a privileged job.** [publish-alpha-feed.yml:73](../../../.github/workflows/publish-alpha-feed.yml#L73) runs `dotnet tool install -g sleet` with no version. The workflow grants `id-token: write`, `attestations: write` and `pages: write` ([publish-alpha-feed.yml:28-32](../../../.github/workflows/publish-alpha-feed.yml#L28-L32)). This is outside the documentation workflows.

## Why it matters

- **Lockless install.** A dependency version inside the caret ranges could change between two runs, with no change in the repository. If the changed version runs install-time code in the architecture job, that code can read the write-scoped token. The preconditions are a bad release of a package in the tree and a run of the workflow. The likelihood is low, because the workflows run on trusted events. The impact is a write-scoped token.
- **Co-location in the monthly jobs.** An agent with `Bash`, an OAuth token in its environment and a write-scoped token in `.git/config` can do more than the task needs. The risk is in what the agent is told to do, not in the install. Nothing here shows that the agent can be steered this way. The risk is that a compromised step or a malicious instruction in content the agent reads has a credential in reach.
- **Severity.** LOW-MED. Triggers are manual or on push to `yubikit`, and the impact is credential theft or an unwanted pull request.

## Specification

> "A "pinned dependency" is a dependency that is explicitly set to a specific hash instead of allowing a mutable version or range of versions." (OpenSSF Scorecard, Pinned-Dependencies)

> "If your project is producing an application and the package manager supports lock files (e.g. `package-lock.json` for npm), make sure to check these in the source code as well." (OpenSSF Scorecard, Pinned-Dependencies, remediation)

> "Any user with write access to your repository has read access to all secrets configured in your repository. Therefore, you should ensure that the credentials being used within workflows have the least privileges required." (GitHub, Secure use reference)

Sources: [OpenSSF Scorecard checks](https://github.com/ossf/scorecard/blob/main/docs/checks.md) and [GitHub Secure use reference](https://docs.github.com/en/actions/reference/security/secure-use). The Pinned-Dependencies check looks for unpinned dependencies in GitHub workflows and shell scripts, so this kind of install falls within what the check describes. This is not a Scorecard result for this repository.

## Canonical Python reference

Not applicable. This is CI configuration.

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

Static only. Nothing was run.

- `git ls-files | grep -i -E 'package(-lock)?\.json|yarn\.lock|pnpm-lock'` returns nothing. The only "lock" matches are unrelated source files.
- `npm view @mermaid-js/mermaid-cli@11.16.0 dependencies --json` returns the caret and range specifiers listed above.
- The file reads listed under "Check it yourself".

## Proposed fix

- **Recommendation.**
  1. Commit a `package.json` and `package-lock.json` for the Mermaid CLI, with the top-level version exact. Install with `npm ci` in both workflows ([docs-update.yml:161-166](../../../.github/workflows/docs-update.yml#L161-L166), [architecture-docs-monthly-synthesis.yml:31-36](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L31-L36)). Add an npm entry to Dependabot, so that updates arrive as pull requests.
  2. Split both monthly synthesis workflows the way `docs-update.yml` is split: a generator job with `contents: read`, the OAuth secret and `persist-credentials: false` that uploads a patch; and a writer job with write permissions and no OAuth secret that applies the patch and opens the pull request.
  3. Remove `Bash` from the architecture agent's tools, unless the maintainers can show the agent needs it.
  4. Pin `sleet` to a version at [publish-alpha-feed.yml:73](../../../.github/workflows/publish-alpha-feed.yml#L73).
- **Options considered for the install.**
  - (a) A lockfile with `npm ci`. Smallest change, and Dependabot can maintain it. Recommended.
  - (b) A fully vendored dependency closure with reviewed checksums. It can remove package-network access during installation, but the whole tree must be vendored and maintained. Vendoring only the Mermaid CLI tarball does not achieve that, because its dependencies would still be fetched from the registry.
  - (c) Keep the install lockless and add `--ignore-scripts`. Not tested. It could break rendering, so it is not recommended without a test.
- **API impact.** None. CI only.
- **Proving checks.** `npm ci` succeeds with the lockfile and fails when the lockfile is altered. `actionlint` passes. A manual run of the split workflows produces a pull request, and the writer job has no OAuth secret.
- **Depends on / interacts with.** [#19](19-docs-workflow-unpinned-installer.md). The same workflow file and the same permission split. The digest-owner decision in #19 applies here too.
- **Open questions for the maintainer.** Should the monthly workflows run on a schedule from the default branch, as the migration comment suggests? Does the architecture agent need `Bash`? Who maintains the Mermaid lockfile?

## Check it yourself

From the worktree root:

```bash
git ls-files | grep -i -E 'package(-lock)?\.json|yarn\.lock|pnpm-lock'
npm view @mermaid-js/mermaid-cli@11.16.0 dependencies --json
```

Read in this order:

- [docs-update.yml:152-202](../../../.github/workflows/docs-update.yml#L152-L202) (the architecture job, from checkout to Claude run).
- [architecture-docs-monthly-synthesis.yml:8-62](../../../.github/workflows/architecture-docs-monthly-synthesis.yml#L8-L62) and [migration-docs-monthly-synthesis.yml:10-54](../../../.github/workflows/migration-docs-monthly-synthesis.yml#L10-L54).
- [architecture-images.env:6](../../../scripts/architecture/architecture-images.env#L6) and [render-architecture.sh:36-60](../../../scripts/architecture/render-architecture.sh#L36-L60).
- [dependabot.yml](../../../.github/dependabot.yml) (two ecosystems only).

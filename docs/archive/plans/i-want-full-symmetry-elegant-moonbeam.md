# Plan: Rename toolchain.cs → toolchain.cs + Add publish-remote Target

## Context

`toolchain.cs` covers the full software delivery pipeline — build, test, pack, and (after this change) remote publish. The name `toolchain.cs` better reflects that scope. Simultaneously, the CI workflow's raw `dotnet nuget push` step is replaced with a `publish-remote` target in `toolchain.cs` so all pipeline operations go through one entry point.

---

## Part 1: Rename toolchain.cs → toolchain.cs

### Step 1 — Rename the files

```bash
git mv toolchain.cs toolchain.cs
git mv BUILD.md TOOLCHAIN.md
```

### Step 2 — Bulk replace all references (excluding .gitignore)

`.gitignore` contains `*.toolchain.csdef` (Azure Cloud Service pattern — unrelated). Exclude it explicitly.

```bash
# All files except .gitignore
grep -rl "build\.cs" . --exclude=".gitignore" --exclude-dir=".git" \
  | xargs sed -i '' 's/build\.cs/toolchain.cs/g'
```

### Step 3 — Update .sln (two entries)

`Yubico.YubiKit.sln` line 29-30 contains:
```
toolchain.cs = toolchain.cs
BUILD.md = BUILD.md
```
After bulk replace, verify both become:
```
toolchain.cs = toolchain.cs
TOOLCHAIN.md = TOOLCHAIN.md
```

### Files requiring changes (active tooling — not historical Plans/)

| File | Change |
|------|--------|
| `toolchain.cs` | Rename to `toolchain.cs` |
| `BUILD.md` | Rename to `TOOLCHAIN.md` |
| `Yubico.YubiKit.sln` | `toolchain.cs = toolchain.cs` → `toolchain.cs = toolchain.cs`; `BUILD.md = BUILD.md` → `TOOLCHAIN.md = TOOLCHAIN.md` |
| `.github/workflows/build.yml` | `dotnet toolchain.cs ...` → `dotnet toolchain.cs ...` |
| `CLAUDE.md` | All `dotnet toolchain.cs` references |
| `docs/TESTING.md` | All references |
| `docs/DEV-GUIDE.md` | All references |
| `docs/AI-DOCS-GUIDE.md` | All references |
| `README.md` | All references |
| `.github/copilot-instructions.md` | All references |
| `.github/agents/ralph-loop.agent.md` | All references |
| `.github/agents/yubikit-porter.agent.md` | All references |
| `.claude/agents/ralph-loop.md` | All references |
| `.claude/skills/domain-build/SKILL.md` | All references |
| `.claude/skills/domain-test/SKILL.md` | All references |
| `.claude/skills/agent-ralph-loop/SKILL.md` + `.ts` files | All references |
| `.claude/skills/agent-ralph-prompt/SKILL.md` + `WORKFLOW.md` | All references |
| All `src/**/CLAUDE.md` test files | All references |
| `src/Tests.Shared/Infrastructure/TestCategories.cs` | Comments referencing `dotnet toolchain.cs test` |

**Do NOT touch:** `.gitignore` (`*.toolchain.csdef` is an unrelated Azure pattern)

---

## Part 2: Add publish-remote Target to toolchain.cs

### New argument variables (add near top config block, ~line 108)

```csharp
var nugetFeedUrl = GetArgument("--nuget-feed-url");
var nugetApiKey  = GetArgument("--nuget-api-key");
```

### New target (add after existing `publish` target)

```csharp
Target("publish-remote", DependsOn("pack"), () =>
{
    PrintHeader(dryRun ? "Dry run - remote packages to publish" : "Publishing packages to remote feed");

    if (string.IsNullOrEmpty(nugetFeedUrl))
        throw new InvalidOperationException("--nuget-feed-url is required for publish-remote");
    if (string.IsNullOrEmpty(nugetApiKey))
        throw new InvalidOperationException("--nuget-api-key is required for publish-remote");

    var packages = Directory.GetFiles(packagesDir, "*.nupkg");

    if (packages.Length == 0)
    {
        Console.WriteLine("No packages found to publish");
        return;
    }

    foreach (var package in packages)
    {
        var packageName = Path.GetFileName(package);

        if (dryRun)
        {
            Console.WriteLine($"  Would publish to {nugetFeedUrl}: {packageName}");
        }
        else
        {
            Console.WriteLine($"\nPublishing: {packageName}");
            Run("dotnet", $"nuget push {package} -s {nugetFeedUrl} --api-key {nugetApiKey} --skip-duplicate");
            PrintInfo($"Published {packageName}");
        }
    }
});
```

### Register new args in FilterBullseyeArgs (~line 356)

```csharp
var bullseyeArgs = FilterBullseyeArgs(args,
    optionsWithValues: ["--project", "--filter", "--package-version", "--nuget-feed-name", "--nuget-feed-path",
                        "--nuget-feed-url", "--nuget-api-key"],   // ← add these
    flags: ["--integration", "--include-docs", "--dry-run", "--clean", "--smoke"]);
```

### Update PrintHelp() — TARGETS section

```
  publish-remote - Push packages to a remote NuGet feed (e.g. GitHub Packages)
```

### Update PrintHelp() — OPTIONS section

```
  --nuget-feed-url <url>         Remote NuGet feed URL (required for publish-remote)
  --nuget-api-key <key>          API key for remote NuGet feed (required for publish-remote)
```

### Update PrintHelp() — EXAMPLES section

```
  dotnet toolchain.cs publish-remote --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json --nuget-api-key $TOKEN
  dotnet toolchain.cs -- publish-remote --dry-run --nuget-feed-url https://... --nuget-api-key fake
```

---

## Part 3: Update CI Workflow

**File:** `.github/workflows/build.yml`

Replace the `Publish to GitHub Packages` step:

```yaml
# Before
- name: Publish to GitHub Packages
  if: github.event_name == 'push'
  run: |
    dotnet nuget push "artifacts/packages/*.nupkg" \
      --source https://nuget.pkg.github.com/Yubico/index.json \
      --api-key ${{ secrets.GITHUB_TOKEN }} \
      --skip-duplicate

# After
- name: Publish to GitHub Packages
  if: github.event_name == 'push'
  run: dotnet toolchain.cs publish-remote --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json --nuget-api-key ${{ secrets.GITHUB_TOKEN }}
```

Note: `publish-remote` depends on `pack` but the pack step already ran and artifacts persist within the job, so the pack step inside the target will be a no-op (packages already exist, `--no-build` + `-o packagesDir` → skip-duplicate logic doesn't apply, but `Directory.GetFiles` will find the existing packages).

Actually: `pack` target calls `dotnet pack --no-build`. If the build step already ran, this won't rebuild. Running pack again would re-create the packages — to avoid that, the `publish-remote` target should check if packages exist and skip re-packing. Simplest fix: remove `DependsOn("pack")` and instead validate packages exist with a clear error:

```csharp
Target("publish-remote", () =>
{
    // ...
    var packages = Directory.GetFiles(packagesDir, "*.nupkg");
    if (packages.Length == 0)
        throw new InvalidOperationException($"No packages in {packagesDir}. Run 'pack' first.");
    // ...
});
```

This keeps CI correct: `pack` step runs first, then `publish-remote` finds the packages.

---

## Verification

```bash
# 1. Confirm rename worked and script runs
dotnet toolchain.cs build

# 2. Dry-run the new target
dotnet toolchain.cs -- publish-remote --dry-run \
  --nuget-feed-url https://nuget.pkg.github.com/Yubico/index.json \
  --nuget-api-key fake-key

# 3. Confirm no remaining toolchain.cs references (excluding .gitignore and Plans/ history)
grep -r "build\.cs" . --exclude=".gitignore" --exclude-dir=".git" --exclude-dir="Plans" \
  --exclude-dir="docs/plans" | grep -v "toolchain"

# 4. Run tests to confirm nothing broken
dotnet toolchain.cs test
```

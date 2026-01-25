---
name: vslsp
description: Use when needing C# diagnostics (errors, warnings) without full rebuild - queries persistent OmniSharp daemon for instant feedback
---

# vslsp - C# LSP Diagnostics Tool

## Overview

CLI tool that connects to a persistent OmniSharp LSP daemon to fetch C# diagnostics (errors, warnings, compile status) for .NET solutions. Provides instant feedback after initial startup.

**Core principle:** Always use daemon mode - one-shot mode has ~20s overhead per invocation.

## Use when

**Use this skill when:**
- Checking if code changes compile before running full build
- Getting precise error locations (file, line, column)
- Needing machine-readable diagnostics JSON for processing
- Working on large solutions where full builds are slow
- Validating syntax/semantic errors incrementally

**Don't use when:**
- Need to run tests (use `test-project` skill)
- Need actual build artifacts (use `build-project` skill)
- Working with non-.NET code

## Installation

```bash
# One-line install (Linux/macOS)
curl -fsSL https://raw.githubusercontent.com/dyallo/vslsp/main/install.sh | bash

# Ensure PATH includes ~/.local/bin
export PATH="$HOME/.local/bin:$PATH"
```

### Installed Paths

| Path | Description |
|------|-------------|
| `~/.local/share/vslsp/vslsp` | Main binary |
| `~/.local/share/vslsp/omnisharp/` | OmniSharp LSP server |
| `~/.local/bin/vslsp` | Symlink for PATH access |

## Daemon Mode (ALWAYS USE THIS)

**CRITICAL:** Always use daemon mode. One-shot mode has ~20s startup overhead per invocation.

### Start Daemon (Once Per Session)

```bash
# Start daemon in background - do this ONCE at session start
vslsp serve --solution ./Yubico.YubiKit.sln --port 7850 &

# Wait for daemon to initialize (~10-20s first time)
sleep 15
vslsp status --port 7850
```

### Query Diagnostics (Instant)

```bash
# All diagnostics
vslsp query --port 7850

# Filter by specific file
vslsp query --file Yubico.YubiKit.Core/src/SomeFile.cs --port 7850

# Just counts (summary only)
vslsp query --summary --port 7850

# Pretty-printed output
vslsp query --format pretty --port 7850
```

### Notify File Changes

After editing a file, notify the daemon to trigger re-analysis:

```bash
vslsp notify --file src/Program.cs --port 7850
```

### Check Daemon Status

```bash
vslsp status --port 7850
```

### Daemon Options

| Option | Description | Default |
|--------|-------------|---------|
| `--port, -p` | HTTP port | 7850 |
| `--file` | Filter by file (query) or file to notify | - |
| `--summary` | Return only counts (query) | false |
| `--format, -f` | Output: `compact` or `pretty` | compact |

## Output Format

JSON output to stdout:

```json
{
  "solution": "/path/to/solution.sln",
  "timestamp": "2026-01-25T01:10:00.000Z",
  "summary": {
    "errors": 2,
    "warnings": 5,
    "info": 0,
    "hints": 0
  },
  "clean": false,
  "files": [
    {
      "uri": "file:///path/to/File.cs",
      "path": "/path/to/File.cs",
      "diagnostics": [
        {
          "severity": "error",
          "line": 10,
          "column": 5,
          "endLine": 10,
          "endColumn": 15,
          "message": "; expected",
          "code": "CS1002",
          "source": "csharp"
        }
      ]
    }
  ]
}
```

### Exit Codes

| Code | Meaning |
|------|---------|
| `0` | No errors (clean build) |
| `1` | Errors found or execution failure |

## Typical Workflow

### 1. Start Session

```bash
# Start daemon once at beginning of work session
vslsp serve --solution ./Yubico.YubiKit.sln --port 7850 &
sleep 15  # Wait for initialization
```

### 2. Edit Code

Make your code changes...

### 3. Check Diagnostics (Instant)

```bash
# Quick check - instant response
vslsp query --port 7850

# Check specific file you edited
vslsp query --file Yubico.YubiKit.Core/src/MyFile.cs --port 7850
```

### 4. Notify After Save (Optional)

```bash
# If diagnostics seem stale, notify the daemon
vslsp notify --file Yubico.YubiKit.Core/src/MyFile.cs --port 7850
vslsp query --port 7850
```

### 5. Process Results

```bash
# Get errors only, process with jq
vslsp query --port 7850 | jq '.files[] | select(.diagnostics | length > 0)'

# Check if clean
if vslsp query --port 7850 | jq -e '.clean' > /dev/null; then
  echo "No errors"
else
  echo "Errors found"
fi
```

## Common Mistakes

**❌ NEVER use one-shot mode:**
```bash
vslsp --solution ./MySolution.sln  # ~20s overhead EVERY time!
```
**✅ ALWAYS use daemon mode:**
```bash
vslsp serve --solution ./MySolution.sln --port 7850 &
vslsp query --port 7850  # Instant after daemon started
```

**❌ Starting multiple daemons on same port:**
```bash
vslsp serve --solution ./A.sln --port 7850 &
vslsp serve --solution ./B.sln --port 7850 &  # Port conflict!
```
**✅ Use different ports or kill existing daemon:**
```bash
vslsp status --port 7850  # Check if already running
# If different solution needed, kill existing and restart
```

**❌ Querying before daemon ready:**
```bash
vslsp serve --solution ./MySolution.sln --port 7850 &
vslsp query --port 7850  # May fail - daemon not ready
```
**✅ Wait for daemon initialization:**
```bash
vslsp serve --solution ./MySolution.sln --port 7850 &
sleep 15
vslsp status --port 7850  # Verify ready
vslsp query --port 7850
```

## Requirements

- .NET 6.0+ runtime (for OmniSharp)
- Linux (x64/arm64) or macOS (x64/arm64)

## Verification

Skill used successfully when:
- [ ] Daemon started with `vslsp serve` (NOT one-shot mode)
- [ ] `vslsp status` shows daemon running
- [ ] `vslsp query` returns JSON with `summary` and `files` fields
- [ ] Queries after initial startup are instant (<1s)

## Related Skills

- `build-project` - When you need actual build artifacts or test execution
- `test-project` - When you need to run tests, not just check compilation
- `debug` - When investigating test failures or unexpected behavior

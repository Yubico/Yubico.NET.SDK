---
name: perplexity-search
description: Use when needing up-to-date web information - queries Perplexity AI via CLI (requires PERPLEXITY_API_KEY in .env)
---

# Perplexity AI Search Skill

## Overview

Query Perplexity AI for real-time web search results directly from the command line using Bun.

**Core principle:** Use Perplexity when you need current information that may not be in your training data or when web citations are valuable.

## Use when

**Use this skill when:**
- Researching current APIs, libraries, or framework versions
- Looking up recent security advisories or CVEs
- Finding up-to-date documentation or best practices
- Answering questions about recent events or releases
- Need web citations to support answers

**Don't use when:**
- Information is already in the codebase
- Question is about static concepts (algorithms, math, language syntax)
- Working offline or API key unavailable
- Simple code generation (use your training instead)

## Prerequisites

- **Bun runtime**: https://bun.sh
- **PERPLEXITY_API_KEY** in `.env` at repository root

The script fails immediately if the API key is missing.

## Core Command

```bash
bun scripts/perplexity-search.ts "your question here"
```

## Options Reference

| Flag | Short | Description | Default |
|------|-------|-------------|---------|
| `--query` | `-q` | The question to ask | (positional arg) |
| `--model` | `-m` | Model ID | `sonar-pro` |
| `--system` | `-s` | System prompt | Helpful assistant |
| `--tokens` | `-t` | Max response tokens | Auto |
| `--verbose` | `-v` | Print debug info | `false` |
| `--help` | `-h` | Show help menu | - |

## Available Models

| Model | Use Case |
|-------|----------|
| `sonar` | Fast, basic queries |
| `sonar-pro` | Balanced (default, recommended) |
| `sonar-reasoning` | Deep analysis, complex questions |

## Common Workflows

### Basic Query

```bash
bun scripts/perplexity-search.ts "What are the latest C# 14 features?"
```

### Technical Research

```bash
bun scripts/perplexity-search.ts "FIDO2 WebAuthn browser support 2024" -m sonar-reasoning
```

### Code-Focused Query

```bash
bun scripts/perplexity-search.ts "SCP03 secure channel implementation patterns" \
  --system "You are a cryptography expert. Provide code examples in C#."
```

### API Documentation

```bash
bun scripts/perplexity-search.ts "Perplexity API rate limits and pricing"
```

### Debug Mode

```bash
bun scripts/perplexity-search.ts "test query" --verbose
```

## Output Format

```
🔍 Querying Perplexity: "your query"...

=== 💡 Perplexity Answer ===

[Response content with citations]

============================
```

## Error Handling

The script exits with code 1 on:
- Missing `PERPLEXITY_API_KEY` environment variable
- No query provided
- API errors (network, auth, rate limit)
- Invalid token count

## Example: Research Before Implementation

**Scenario:** Need to implement CTAP2 authenticator selection

```bash
# Research current spec
bun scripts/perplexity-search.ts "CTAP2 authenticatorGetInfo response fields 2024" \
  -m sonar-reasoning \
  --system "Focus on the data structure and required vs optional fields"
```

**Expected output:** Current CTAP2 spec details with citations to FIDO Alliance docs.

## Verification

Skill completed successfully when:
- Query returns a response (not an error)
- Response includes relevant information
- Citations are present (for factual queries)

## Related Skills

- `build` - For compiling/testing after implementing researched features
- `workflow-brainstorm` - When research should feed into design discussions

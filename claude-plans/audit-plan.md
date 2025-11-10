# Security Audit Remediation Plan

**Project**: Yubico.NET.SDK
**Date**: 2025-11-10
**Branch**: claude/code-audit-plan-011CUzVbhTU5UGYdLq4uKJmS

## Executive Summary

This document outlines the remediation plan for security findings and recommendations from the code audit. The primary focus is on GitHub Actions security hardening, with additional improvements to documentation and code quality.

## Priority Classification

- **Critical**: Immediate security risks requiring urgent attention
- **High**: Security improvements that should be addressed soon
- **Medium**: Important improvements that reduce attack surface
- **Low**: Best practices and code quality improvements

---

## Security Findings

### 1. Pin GitHub Actions by SHA Hash (MEDIUM)

**Risk**: Compromised builds, credential exfiltration, tampering of release artifacts, or hidden backdoors

**Current State**:
- All workflow files use semantic versioning (e.g., `@v4`, `@v5`)
- 12 workflow files affected
- Multiple action types: checkout, setup-dotnet, upload-artifact, download-artifact, attest-build-provenance, create-github-app-token

**Affected Files**:
```
.github/workflows/build.yml
.github/workflows/build-pull-requests.yml
.github/workflows/build-nativeshims.yml
.github/workflows/codeql-analysis.yml
.github/workflows/claude.yml
.github/workflows/deploy-docs.yml
.github/workflows/test.yml
.github/workflows/test-macos.yml
.github/workflows/test-ubuntu.yml
.github/workflows/test-windows.yml
.github/workflows/upload-docs.yml
.github/workflows/verify-code-style.yml
```

**Implementation Steps**:
1. Identify all action references across workflow files
2. Resolve each action to its current SHA hash for the specified version
3. Replace version tags with SHA hashes and version comments
4. Format: `uses: actions/checkout@<sha> # v5.0.0`
5. Test workflows in a feature branch before merging

**Actions to Pin** (preliminary list):
- `actions/checkout@v4` → find SHA for v4.x latest
- `actions/checkout@v5` → find SHA for v5.x latest
- `actions/setup-dotnet@v4` → find SHA for v4.x latest
- `actions/upload-artifact@v4` → find SHA for v4.x latest
- `actions/download-artifact@v4` → find SHA for v4.x latest
- `actions/attest-build-provenance@v2` → find SHA for v2.x latest
- `actions/create-github-app-token@v1` → find SHA for v1.x latest
- `github/codeql-action/init@v3` → find SHA for v3.x latest
- `github/codeql-action/analyze@v3` → find SHA for v3.x latest

**Verification**:
- Run workflows after changes to ensure they execute successfully
- Verify no breaking changes in functionality

---

### 2. Remove Stale CodeQL Configuration (INFO)

**Risk**: Repository hygiene issue; CodeQL complains about config issues

**Current State**:
- `codeql-analysis.yml:64` specifies `languages: csharp`
- CodeQL may be complaining about deprecated configuration

**Implementation Steps**:
1. Review current CodeQL documentation for recommended configuration
2. Remove or update the language specification as needed
3. Test CodeQL analysis after changes

**Files to Modify**:
- `.github/workflows/codeql-analysis.yml`

---

### 3. Add GitHub Actions to Dependabot Configuration (MEDIUM)

**Risk**: Outdated actions may contain security vulnerabilities

**Current State**:
- `dependabot.yml` only monitors NuGet packages
- No monitoring for GitHub Actions updates

**Implementation Steps**:
1. Add `github-actions` ecosystem to dependabot.yml
2. Configure monthly update schedule (align with existing cadence)
3. Group all GitHub Actions updates together

**Changes to `.github/dependabot.yml`**:
```yaml
- package-ecosystem: "github-actions"
  directory: "/"
  schedule:
    interval: "monthly"
    day: "wednesday"
    time: "09:00"
    timezone: "Europe/Stockholm"
  groups:
    github-actions:
      patterns:
        - "*"
```

---

### 4. Enhance CodeQL Analysis for GitHub Actions Scanning (MEDIUM)

**Risk**: Missing security issues in GitHub Actions workflows

**Current State**:
- CodeQL only scans C# and C code
- GitHub Actions YAML files are not analyzed

**Implementation Steps**:
1. Research CodeQL configuration for GitHub Actions scanning
2. Add workflow file scanning to CodeQL analysis
3. Follow GitHub's documentation: https://docs.github.com/en/code-security/code-scanning/creating-an-advanced-setup-for-code-scanning/codeql-code-scanning-for-compiled-languages

**Files to Modify**:
- `.github/workflows/codeql-analysis.yml`

**Expected Changes**:
- Add YAML language support if available
- Include workflow files in scanning paths
- Configure action-specific security checks

---

### 5. Remove Clear-Text Password Storage Flag (MEDIUM)

**Risk**: GitHub token written to disk in clear text; potential credential exposure

**Current State**:
- Multiple workflow files use `--store-password-in-clear-text` flag
- Affects build.yml, build-pull-requests.yml, and codeql-analysis.yml

**Affected Lines**:
- `build.yml:87` - Add local NuGet repository for non-release versions
- `build.yml:194` - Publish to internal NuGet
- `build-pull-requests.yml:57` - Add local NuGet repository
- `codeql-analysis.yml:68` - Add local NuGet repository

**Implementation Steps**:
1. Replace manual `dotnet nuget add source` commands with `actions/setup-dotnet@v4` configuration
2. Use the `source-url` parameter with `NUGET_AUTH_TOKEN` environment variable
3. Remove all instances of `--store-password-in-clear-text`
4. Test package restoration and publishing after changes

**Recommended Pattern**:
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
    source-url: https://nuget.pkg.github.com/Yubico/index.json
  env:
    NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Note**: The global.json file specifies SDK version, so we need to align dotnet-version with existing configuration or use `global-json-file` parameter.

---

### 6. Disable Credential Persistence in Checkout Actions (MEDIUM)

**Risk**: Repository tokens persist on disk; potential credential exposure

**Current State**:
- All `actions/checkout` calls use default `persist-credentials: true`
- Credentials remain accessible to subsequent steps

**Implementation Steps**:
1. Add `persist-credentials: false` to all checkout action calls
2. Verify no workflows depend on persisted credentials for git operations
3. Test all workflows after changes

**Pattern**:
```yaml
- name: Checkout repository
  uses: actions/checkout@<sha> # v5.0.0
  with:
    persist-credentials: false
```

**Files to Modify**: All workflow files with `actions/checkout` calls

**Exception Handling**:
- If any workflow needs git operations (push, PR creation), evaluate if credentials are truly needed
- Consider using GitHub App tokens or explicit authentication for those specific operations

---

## Code Quality Improvements

### 7. Update SCP11 Documentation (LOW)

**Risk**: Incomplete documentation may lead to incorrect implementation

**Current State**:
- `docs/users-manual/sdk-programming-guide/secure-channel-protocol.md:82-99`
- Example code is incomplete; missing intermediate steps for SCP11b setup

**Implementation Steps**:
1. Review the secure-channel-protocol.md documentation
2. Add complete code snippet showing:
   - KeyReference creation
   - Certificate retrieval and verification
   - Scp11KeyParameters instantiation
   - PivSession creation with SCP11 parameters
3. Ensure example is compilable and follows SDK best practices

**Suggested Complete Example**:
```csharp
// Using SCP11b
using var scp03Params = Scp03KeyParameters.DefaultKey;
using var sdSession = new SecurityDomainSession(yubiKeyDevice, scp03Params);

var keyVersionNumber = 0x1;
var keyId = ScpKeyIds.Scp11B;
var keyReference = KeyReference.Create(keyId, keyVersionNumber);

var certificates = sdSession.GetCertificates(keyReference);

// Verify certificate chain (implementation required)
CertificateChainVerifier.Verify(certificates);

// Extract public key from certificate
var publicKey = ExtractPublicKeyFromCertificate(certificates[0]);

// Create SCP11 parameters
var scp11Params = new Scp11KeyParameters(keyReference, new ECPublicKeyParameters(publicKey));

// Use SCP11 for PIV session
using (var pivSession = new PivSession(yubiKeyDevice, scp11Params))
{
    // All PivSession commands are now automatically protected by SCP11
}
```

---

### 8. Simplify Nested If Statements (LOW)

**Risk**: Reduced code readability and increased cognitive load

**Current State**:
- `Yubico.YubiKey/examples/PivSampleCode/Converters/KeyConverter.cs`
- Contains nested if statements checking OID values for P256 and P384

**Implementation Steps**:
1. Locate nested if statements in KeyConverter.cs
2. Combine conditions using logical operators
3. Ensure behavior is unchanged
4. Add unit test if not already covered

**Example Transformation**:
```csharp
// Before:
if (!string.Equals(eccParams.Curve.Oid.Value, OidP256, StringComparison.Ordinal))
{
    if (!string.Equals(eccParams.Curve.Oid.Value, OidP384, StringComparison.Ordinal))
    {
        return false;
    }
}

// After:
if (!string.Equals(eccParams.Curve.Oid.Value, OidP256, StringComparison.Ordinal) &&
    !string.Equals(eccParams.Curve.Oid.Value, OidP384, StringComparison.Ordinal))
{
    return false;
}
```

**Additional Scope**:
- Search for other nested if patterns (2-3 levels deep) across the codebase
- Prioritize code in main SDK assemblies over examples

---

## Implementation Timeline

### Phase 1: Critical Security Hardening (Priority)
1. Pin GitHub Actions by SHA hash (Finding #1)
2. Remove clear-text password storage (Finding #5)
3. Disable credential persistence (Finding #6)

**Estimated Effort**: 4-6 hours
**Testing Required**: All workflows must run successfully

### Phase 2: Security Configuration & Monitoring
1. Add GitHub Actions to Dependabot (Finding #3)
2. Enhance CodeQL for Actions scanning (Finding #4)
3. Remove stale CodeQL config (Finding #2)

**Estimated Effort**: 2-3 hours
**Testing Required**: CodeQL analysis runs successfully

### Phase 3: Code Quality & Documentation
1. Update SCP11 documentation (Finding #7)
2. Simplify nested if statements (Finding #8)

**Estimated Effort**: 2-3 hours
**Testing Required**: Documentation builds without errors; unit tests pass

---

## Testing Strategy

### Workflow Testing
1. Create feature branch for workflow changes
2. Test each modified workflow individually
3. Monitor workflow runs for failures
4. Check artifact uploads/downloads still function
5. Verify NuGet package operations succeed
6. Ensure CodeQL analysis completes

### Code Testing
1. Run full unit test suite after code changes
2. Run integration tests if applicable
3. Build documentation to verify no errors
4. Run code style verification

### Validation Checklist
- [ ] All workflows execute without errors
- [ ] NuGet restore and publish operations work
- [ ] CodeQL analysis completes successfully
- [ ] Dependabot can detect action updates
- [ ] Documentation builds successfully
- [ ] Unit tests pass
- [ ] No new compiler warnings introduced

---

## Risk Assessment

### High Risk Changes
- **NuGet authentication changes**: Could break builds if not configured correctly
- **Action SHA pinning**: Could break workflows if wrong SHA is used

### Mitigation Strategies
1. Test in feature branch before merging
2. Keep semantic version in comments for reference
3. Document rollback procedure
4. Monitor first few runs after deployment

### Rollback Plan
1. Revert commit if critical workflow fails
2. Re-pin actions if issues with specific versions
3. Restore clear-text password flag temporarily if auth fails (not recommended)

---

## Success Criteria

1. All GitHub Actions pinned to SHA hashes with version comments
2. No `--store-password-in-clear-text` flags in any workflow
3. All checkout actions use `persist-credentials: false`
4. Dependabot monitors GitHub Actions
5. CodeQL configuration is up-to-date
6. Documentation is complete and accurate
7. Code quality improvements implemented
8. All workflows execute successfully
9. All tests pass

---

## References

- [GitHub Actions Security Hardening](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
- [Dependabot Configuration Options](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file)
- [CodeQL for Compiled Languages](https://docs.github.com/en/code-security/code-scanning/creating-an-advanced-setup-for-code-scanning/codeql-code-scanning-for-compiled-languages)
- [Securing GitHub Actions with Checkout](https://julienrenaux.fr/2019/12/20/github-actions-security-risk/)

---

## Notes

- This plan prioritizes security improvements over code quality enhancements
- All changes should be made on the designated feature branch
- Each phase can be implemented and tested independently
- Review and approval should be obtained before starting implementation
- Consider running security scanning tools after implementation to verify improvements

"""Prepare this run's source-pinned inherited registry; performs no SDK execution.

Run with python3 -B. Inputs are read-only; outputs stay beside this file and in the
explicitly owned ../inherited-findings.md. This is a one-run record preparer.
"""
import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[8]
OUT = Path(__file__).resolve().parent
BASE = "fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef"
AUDIT = "docs/research/documentation-audit/"
C = "src/Core/src/"
U = "src/Core/tests/Yubico.YubiKit.Core.UnitTests/"


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def module_run(module):
    return (AUDIT + "records/runs/webauthn-001/" if module == "webauthn" else
            AUDIT + f"modules/{module}/pass-1/{module}-001/")


MODULES = {
    "oath": "Oath", "management": "Management", "ctap2": "Ctap2", "webauthn": "WebAuthn",
    "security-domain": "SecurityDomain", "yubiotp": "YubiOtp", "yubihsm": "YubiHsm",
    "openpgp": "OpenPgp", "piv": "Piv",
}
AUTHORITIES = {}
for m in MODULES:
    prefix = module_run(m)
    if m == "oath":
        ledger = AUDIT + "records/implementation-followups.md"
        review = AUDIT + "records/runs/fixes-wave-002/documentation-impact-review.md"
    else:
        ledger = prefix + ("triage.md" if m in ("management", "ctap2", "webauthn") else "findings.md")
        review = prefix + ("review-closeout-followup.md" if m == "ctap2" else
                           "review-a-followup.md" if m == "webauthn" else "review-closeout.md")
    AUTHORITIES[m] = {
        "handoff": AUDIT + m + ".md", "ledger": ledger, "review": review,
        "source_run": "oath-pilot-and-fixes-wave-002" if m == "oath" else m + "-001",
    }

ROWS = []


def add(m, original, group, state, conclusion, core, *, related=(), old=(), tests=(), actor=None,
        evidence=None, commits=(), reopen=None, qualification=None):
    authority = AUTHORITIES[m] if m in AUTHORITIES else {
        "source_run": "audit-v2-20261022-20261031",
        "handoff": "docs/research/audit-v2-close-out.md",
        "ledger": "docs/research/audit-v2-closeout-review.md",
        "review": "docs/research/audit-v2-execution.md",
    }
    if evidence:
        authority = dict(authority, ledger=evidence)
    if m == "oath" and original == "CORE-001":
        authority = dict(authority, source_run="fixes-wave-001")
    paths = [p if p.startswith(("src/", "docs/", "scripts/", ".github/")) else C + p for p in core]
    historical = extract_record(authority["ledger"], original)
    default_reopen = [p for p in paths if p.startswith(C) and p.endswith(".cs")]
    default_reopen += ["src/Core/README.md", "src/Core/CLAUDE.md", "docs/architecture/raw-access-tiers.md",
                      "docs/migration/v1-to-v2.md"]
    if m in MODULES:
        default_reopen += [f"src/{MODULES[m]}/README.md", f"src/{MODULES[m]}/CLAUDE.md"]
    ROWS.append({
        "key": authority["source_run"] + ":" + original,
        "source_run": authority["source_run"], "original_id": original,
        "module": m, "aliases_or_related_ids": list(related),
        "original_identity_kind": "historical-unkeyed-observation-label" if original in (
            "PCSC-stall-finalizer-flake", "multi-key-exclusions", "deviceinfo-wide-app-bits",
            "deviceinfo-FIPS-residual", "audit-host-selector-collision") else "original-numbered-identity",
        "dedup_group": group, "counting": "provenance/dependency occurrence; never an additive bug count",
        "authority": authority, "historical_definition": historical,
        "historical_implementation_paths": list(old) + re.findall(r"`([^`\n]*\.cs(?::[^`\n]*)?)`", historical["text"]),
        "current_state": state, "current_conclusion": conclusion,
        "current_implementation_paths": paths,
        "current_test_sources_to_read_or_reverify": [p if p.startswith("src/") else U + p for p in tests],
        "actor_boundary": actor or "Core supplies shared mechanics; the applet owns operation/profile policy; the OS owns native completion.",
        "relevant_historical_commits": list(commits),
        "documentation": {"state": "initial rebaseline; family/boundary review pending",
                          "reopen_paths": reopen or default_reopen,
                          "precise_trigger": historical["text"] if historical else conclusion},
        "qualification": qualification or "Source mapping only in this preparation. No fresh SDK, test, hardware or platform execution. Preserve the source run's exact scope and missing dimensions.",
    })


def extract_record(path, original):
    lines = (ROOT / path).read_text().splitlines()
    # Prefer stable table definitions or headings, not incidental cross-references.
    patterns = [rf"^\|\s*(?:\*\*)?{re.escape(original)}(?:\b|\s|/)",
                rf"^##.*{re.escape(original)}(?:\b|\s)", rf"{re.escape(original)}"]
    for pattern in patterns:
        hits = [i for i, line in enumerate(lines) if re.search(pattern, line)]
        if hits:
            i = hits[0]
            end = i + 1
            if lines[i].startswith("#"):
                while end < len(lines) and not lines[end].startswith("##"):
                    end += 1
            return {"path": path, "start_line": i + 1, "end_line": end,
                    "text": "\n".join(lines[i:end])}
    return {"path": path, "start_line": None, "end_line": None,
            "text": "Grouped source definition: consult this ledger and the module investigation; no exact-ID heading found."}


# OATH: keep its decision namespace distinct from CTAP/WebAuthn.
add("oath", "CORE-001", "discovery-abandonment", "changed-unresolved-physical",
    "Fresh scoped Find calls retire per-call evidence; discovery now separates bounded wait, shared owned read and borrowed protocol read. Linux owners retain admission/claim through checked release. These changes address mechanisms around the old failure, while serial 9681620's two-key abandoned HID read still lacks matching current capture/regression conditions.",
    ["Devices/FindYubiKeys.cs", "Devices/YubiKeyManager.cs", "Devices/DiscoveryIdentityReader.cs", "Devices/DiscoveryDeviceInfoReader.cs", "Devices/SharedDeviceInfoRead.cs", "Devices/ProtocolDeviceInfo.cs", "Devices/DeviceConnectionRegistry.cs"],
    related=["audit-v2:multi-key-exclusions", "OTP-DEP-001"],
    old=["src/Core/src/Devices/DiscoveryIdentityReader.cs", "ProtocolDeviceInfo.DiscoveryReadLeaseTracker", "DeviceConnectionRegistry.InterfaceOwnership"],
    tests=["Devices/FindYubiKeysScopeTests.cs", "Devices/DiscoveryReadSchedulingTests.cs", "Devices/LinuxHidRoutingTests.cs"],
    evidence=AUDIT + "records/runs/fixes-wave-001/discovery-blocker.md", commits=["6d1e8dc04", "85b41e579", "d4f61110a", "f32848f62"],
    reopen=["src/Core/src/Devices/YubiKeyManager.cs", "src/Core/src/Abstractions/IYubiKey.cs", "src/Core/src/Devices/README.md", "docs/usage/device-discovery.md", "docs/architecture/device-discovery-guarantees.md", "docs/architecture/connection-ownership-and-contention.md", "src/Oath/README.md", "src/Tests.Shared/CLAUDE.md"],
    qualification="Old 9681620 works alone; multi-key release/root cause remained unknown. Integration 4442 unit and 197 resilience executions are separate scoped evidence; zero physical modes. Preserve old missing raw logs if unavailable; no physical closure.")
add("oath", "IMP-004", "base32-failure-cleanup", "resolved-source-for-exact-scope",
    "Shared Base32 span decode rejects overlap before writing and clears disjoint destination on failure; private abandoned URI arrays remain delegation/finally evidence rather than directly observed storage.",
    ["Utilities/Base32.cs", "src/Oath/src/CredentialData.cs"], related=["OATH-002-F03"],
    tests=["Utilities/Base32Tests.cs"], commits=["8503fe2d", "eba72a81"])
add("oath", "IMP-005", "base32-shape", "resolved-source-for-exact-scope",
    "Core's common decoder enforces terminal symbol counts and padding shape for SDK and CLI callers. Permissive unused pad bits remain the accepted scoped policy.",
    ["Utilities/Base32.cs", "src/Oath/src/CredentialData.cs", "src/Cli.Commands/src/Oath/OathHelpers.cs"],
    tests=["Utilities/Base32Tests.cs"], commits=["8503fe2d", "eba72a81"])
add("oath", "IMP-010", "oath-terminal-reset", "resolved-source-physical-gap",
    "OATH successful reset disposes its session rather than reselecting/reusing old SCP state. A direct session still borrows its connection. Plain-channel historical reset passed; SCP attempts ended at INITIALIZE UPDATE, before RESET.",
    ["src/Oath/src/OathSession.cs", "Sessions/ApplicationSession.cs"], commits=["d74cdd44", "57e0fa4d"],
    qualification="Live plain reset is historical serial 125 evidence. Both FF and 00 INITIALIZE UPDATE attempts returned 6A88; EXTERNAL AUTHENTICATE and RESET were unreached. Reestablish key inventory and repeat on distinct SCP-capable devices before broader conclusions.")
add("oath", "DEC-001", "oath-auth-failure-state", "needs-decision",
    "Error provenance was corrected: host device-HMAC mismatch differs from device-reported rejection. The broader post-verification-failure session-usability policy remains open; Core SCP recovery latches have their own scope.",
    ["src/Oath/src/OathSession.cs", "Protocols/SmartCard/Apdu/PcscProtocol.cs", "Protocols/SmartCard/Scp/ScpProcessor.cs"], commits=["d74cdd44", "76a8ae52"])
add("oath", "DEC-002", "oath-raw-access-key", "needs-decision",
    "OATH accepts nonempty raw access keys while the normal password path derives 16 bytes. Decide the applet's supported raw-key contract with device/profile evidence; Core key types do not settle it.", ["src/Oath/src/OathSession.cs"], actor="OATH owns access-key validation; Core owns the carrying APDU and its lifetime.")
add("oath", "DEC-007", "scp-effective-version", "policy-known-rationale-gap",
    "OATH's effective 5.6.3 policy has pinned Python corroboration for 5.0.0 <= version < 5.6.3. Core's SCP03 5.3.0 and SCP11 5.7.2 gates are separate. Earliest physical support/workaround rationale remains bounded evidence debt.",
    ["src/Oath/src/OathSession.cs", "Protocols/SmartCard/Scp/ScpInitializer.cs", "Devices/Feature.cs", "Devices/FirmwareVersion.cs"])

# Management metadata defects are still directly visible in source.
add("management", "MGMT-XREF-001", "metadata-fips-bits", "reproducible-from-source",
    "DeviceInfo passes tags 14/15 through ReadCapabilities -> FromApp, although the same file defines distinct FipsMapping. FromFips reads Int16 after its narrow zero special case, so a one-byte nonzero input still lacks that width. Exact example: FIPS 01 becomes application OTP rather than FIDO2. Field-length hardening never corrected this mapping.",
    ["Devices/DeviceInfo.cs", "src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs"], tests=["Devices/CapabilityMapperTests.cs", "Devices/DeviceInfoFieldLengthTests.cs"], related=["audit-v2:deviceinfo-FIPS-residual"],
    reopen=["src/Core/src/Devices/DeviceInfo.cs", "src/Core/src/Devices/DeviceCapabilities.cs", "src/Management/README.md", "src/Management/CLAUDE.md", "src/Tests.Shared/CLAUDE.md", "docs/migration/v1-to-v2.md"])
add("management", "MGMT-XREF-002", "metadata-form-factor", "reproducible-from-source",
    "DeviceInfo computes IsFips/IsSky from high bits and casts the entire byte into non-flags FormFactor. Raw 81 therefore remains enum value 129 rather than UsbAKeychain(1). Decide normalized plus raw preservation and recheck consumers.",
    ["Devices/DeviceInfo.cs", "Devices/FormFactor.cs"], tests=["Devices/DeviceInfoTests.cs"])
add("management", "MGMT-XREF-003", "fixture-cache-docs", "changed-reopen-docs",
    "The earlier fixture-row/cached-binding prose was documentation-resolved. Current discovery introduces CachedDeviceInfo and owned monitors; fixture metadata and row binding explanations require a new boundary check. This is a reopening dependency rather than a new runtime defect.",
    ["Abstractions/IYubiKey.cs", "Devices/YubiKeyDevice.cs", "src/Tests.Shared/Infrastructure/YubiKeyTestInfrastructure.cs"], commits=["867005429", "5ff8d25af", "e0e15af5e"])
for original, group, note in [
    ("MGMT-IMP-008", "metadata-firmware-read", "Legacy read/factory feature enforcement remains an applet decision; Core Feature treats major-zero development versions as supported."),
    ("MGMT-IMP-009", "fixture-cache-docs", "Malformed two-component minimum-firmware filter and strict shared-filter policy need a current Tests.Shared check; preserve original ID."),
    ("MGMT-IMP-010", "management-reset-lifecycle", "Bio MPE family preflight/reset-success invalidation and configuration/lock effects remain Management policy and qualification, separate from OATH terminal reset."),
    ("MGMT-IMP-004", "scp-parameter-ownership", "Two historical undisposed owned-default SCP test inputs and cleanup ordering require exact current-call-site rebaseline; broad source ownership does not certify these tests."),
]:
    add("management", original, group, "still-candidate-boundary-review", note,
        ["Sessions/ApplicationSession.cs", "Devices/Feature.cs", "src/Management/src/ManagementSession.cs"])

# CTAP2: security/profile and response owners remain at their original applet layer.
add("ctap2", "CTAP-XREF-003", "credential-length-units", "reproducible-from-source",
    "ConsoleCredentialReader applies MinLength/MaxLength to UTF-16 char counts before Encoding.GetByteCount/GetBytes. Ctap2's 4..63 preset therefore differs from UTF-8 bytes/code-point/NFC/device-minimum policy. Allocation of enforcement is still a decision; preserve CTAP-IMP-006.",
    ["Credentials/ConsoleCredentialReader.cs", "Credentials/CredentialReaderOptions.cs", "src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs"],
    related=["B-04", "C-012", "F-10", "CTAP-IMP-006", "PGP-CORE-001"], evidence=module_run("ctap2") + "triage-addendum.md",
    reopen=["src/Core/src/Credentials/CredentialReaderOptions.cs", "src/Core/src/Credentials/ConsoleCredentialReader.cs", "src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs", "src/Ctap2/README.md", "src/WebAuthn/README.md", "docs/usage/user-interaction.md", "docs/migration/v1-to-v2.md"])
for original, group, state, note, paths in [
    ("CTAP-IMP-004", "ctap-response-ownership", "still-candidate-boundary-review", "Request-builder secret clones and raw request/response lifetime need applet-to-Core tracing; read-only views convey access rather than cleanup ownership.", ["src/Ctap2/src/Ctap2Session.cs", "Protocols/Fido/Hid/FidoHidProtocol.cs"]),
    ("CTAP-IMP-005", "ctap-crypto-cleanup", "still-candidate-boundary-review", "PIN protocol decryption/MAC/ECDH exceptional cleanup remains CTAP-owned; provider and Core helper copies require narrow lifetime tracing.", ["src/Ctap2/src/Pin/PinUvAuthProtocolV1.cs", "Cryptography/ECPrivateKey.cs"]),
    ("CTAP-IMP-006", "credential-length-units", "needs-decision", "CTAP PIN byte checks and Unicode/code-point/final-NUL rules remain an input-layer allocation decision; a Core reader preset alone cannot enforce the full authenticator policy.", ["src/Ctap2/src/Credentials/Ctap2CredentialOptions.cs", "Credentials/ConsoleCredentialReader.cs"]),
    ("CTAP-IMP-007", "ctap-peer-key-profile", "still-candidate-boundary-review", "Core catalog kty/crv metadata stays distinct from the strict CTAP PIN peer-key kty/crv/alg validation boundary.", ["src/Ctap2/src/Cose/CoseKey.cs", "Cryptography/CryptographicDefinitions.cs"]),
    ("CTAP-IMP-025", "ctap-aaguid-portability", "still-candidate-boundary-review", "Historical mixed-endian Guid source/probe settled its inference only on the recorded little-endian host. Keep code-inferred big-endian issue and missing execution separate.", ["src/Ctap2/src/Cbor/AaguidConverter.cs"]),
    ("CTAP-IMP-027", "ctap-response-ownership", "still-candidate-boundary-review", "CTAP response/extension DTOs materialize independent owned arrays and borrow other slices; Core ApduResponse.Clear clears only its own copy. Full response ownership is an applet design question.", ["src/Ctap2/src/Credentials/MakeCredentialResponse.cs", "src/Ctap2/src/Credentials/GetAssertionResponse.cs", "Protocols/SmartCard/Apdu/ApduResponse.cs"]),
    ("CTAP-IMP-028", "ctap-decoder-envelope", "still-candidate-boundary-review", "Structural/complete-envelope checks remain CTAP-owned; nested COSE readers can share a parent and need context-specific consumption rules.", ["src/Ctap2/src/Credentials/AuthenticatorData.cs", "src/Ctap2/src/Credentials/AttestedCredentialData.cs"]),
    ("CTAP-IMP-029", "ctap-arkg-profile", "needs-decision", "Expired ARKG draft-10 agrees on seed marker -65700 after the reviewer erratum. Outer kty/wrapped/direct maps/dkalg/curve/profile questions remain. Core mathematical reconstruction is separate from selecting the applet profile.", ["src/Ctap2/src/Cose/CoseArkgP256SeedKey.cs", "Cryptography"]),
    ("CTAP-IMP-030", "ctap-unsigned-outputs", "still-candidate-boundary-review", "Unsigned getAssertion key8/raw-envelope support remains a CTAP response API decision. Reuse old audit #4 identity rather than count its reobservation again.", ["src/Ctap2/src/Credentials/GetAssertionResponse.cs", "src/Ctap2/src/Credentials/MakeCredentialResponse.cs"]),
    ("CTAP-IMP-035", "ctap-crypto-cleanup", "still-candidate-boundary-review", "Storage/probe/plaintext/salt/failure buffers retain their CTAP-owned cleanup questions; Core transport copies have independent scopes.", ["src/Ctap2/src/LargeBlobs/LargeBlobStorage.cs", "src/Ctap2/src/Extensions/Shared/ExtensionBuilder.cs"]),
    ("CTAP-IMP-037", "ctap-blob-format", "still-candidate-boundary-review", "Signed origSize/integer-key ranges and read/write cap are distinct from preserved unknown encoded value slices, which borrow input. Preserve the accepted review correction.", ["src/Ctap2/src/LargeBlobs/LargeBlobData.cs"]),
    ("CTAP-IMP-039", "ctap-arkg-profile", "still-candidate-boundary-review", "Seed versus derived algorithm propagation remains a CTAP previewSign result issue, separate from Core catalog identity.", ["src/Ctap2/src/Extensions/PreviewSign/PreviewSignRegistrationOutput.cs"]),
    ("CTAP-IMP-041", "ctap-preview-der", "still-candidate-boundary-review", "Preview DER root/sequence exhaustion, integer bounds and provider behavior need selected validation; retain experimental scope without claiming exploitability.", ["src/Ctap2/src/Extensions/PreviewSign/PreviewSignDerivedKey.cs"]),
    ("CTAP-IMP-043", "ctap-metadata-wrapper", "needs-decision", "Protocol-specific encrypted metadata IV/token/length rules need a wrapper-versus-generic-helper contract; CBC output alone supplies no authentication claim.", ["src/Ctap2/src/Crypto/EncryptedMetadataDecryptor.cs"]),
]:
    add("ctap2", original, group, state, note, paths,
        evidence=module_run("ctap2") + ("triage-addendum.md" if int(original[-3:]) >= 26 else "triage.md"))
add("ctap2", "CTAP-XREF-001", "assertion-user-docs", "resolved-docs-recheck-boundary",
    "The final CTAP/WebAuthn review accepted the User id/display-field and version-specific descriptor correction. Native output representation/privacy and descriptor recovery retain separate open IDs.",
    ["src/Ctap2/src/Credentials/GetAssertionResponse.cs", "src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs"], related=["WA-XREF-001"],
    evidence=module_run("ctap2") + "boundary-final.md")
add("ctap2", "CTAP-DEC-002", "native-selected-user-actor", "needs-decision",
    "Decide whether native selected AuthenticationResponse/User is a browser-equivalent result or an RP-serialization boundary. The original decision authorizes neither automatic display-field stripping nor a new vulnerability assertion.",
    ["src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs", "src/Ctap2/src/Credentials/GetAssertionResponse.cs"],
    related=["G-011", "CTAP-IMP-026"], actor="WebAuthn/native application/RP serializer own disclosure policy; authenticator and raw CTAP response preservation are separate actors.")
add("ctap2", "CTAP-DEC-003", "ctap-profile-evidence", "missing-evidence",
    "Keep product/preview firmware origins, reset 5.5.2/5.5.3 interval, vendor ARKG/bio profile and unsigned-output introduction history bounded. The BE/BS citation gap was resolved by the pinned L3 Recommendation; other gaps retain their own scope.", ["src/Ctap2/src/Ctap2Session.cs", "Devices/FirmwareVersion.cs"])

# Preserve all WebAuthn engineering identities. They are dependencies, not Core bug counts.
wa_groups = {
    1: "wa-attestation-policy", 2: "wa-error-provenance", 3: "wa-timeout-outcome", 4: "wa-uv-test-applicability",
    5: "wa-clientdata-serialization", 6: "ctap-response-ownership", 7: "ctap-aaguid-portability",
    8: "wa-zeroing-test", 9: "wa-pin-negotiation", 10: "wa-descriptor-recovery", 11: "wa-prf-uv",
    12: "wa-credprotect", 13: "ctap-response-ownership", 14: "wa-blob-required", 15: "wa-blob-cardinality",
    16: "wa-blob-absence", 17: "ctap-unsigned-outputs", 18: "wa-minpin-input", 19: "ctap-arkg-profile",
    20: "wa-preview-fixture", 21: "wa-preview-membership",
}
wa_paths = {
    1: ["Preferences/AttestationPreference.cs", "Client/WebAuthnClient.Registration.cs", "Client/WebAuthnBackend.cs"],
    2: ["Client/WebAuthnClient.Ceremony.cs", "Client/WebAuthnClient.PinUvAuth.cs", "Extensions/Processing/AuthenticationExtensionSession.cs"],
    3: ["Client/CeremonyDeadline.cs", "Client/WebAuthnClient.Authentication.cs", "Client/WebAuthnClient.Registration.cs"],
    4: ["Client/UserVerification/UvDecision.cs", "Client/WebAuthnBackend.cs"],
    5: ["Client/WebAuthnClientData.cs", "Client/WebAuthnClient.Validation.cs"],
    6: ["Attestation/WebAuthnAttestationObject.cs", "Extensions/PreviewSign/GeneratedSigningKey.cs"],
    7: ["Cose/Aaguid.cs"],
    8: ["Client/PinUvAuthTokenSession.cs", "Client/PinUvSharedSecret.cs"],
    9: ["Client/WebAuthnBackend.cs", "Client/UserVerification/UvDecision.cs"],
    10: ["Client/Authentication/CredentialMatcher.cs", "Client/Authentication/AuthenticationResponse.cs", "Client/WebAuthnClient.Authentication.cs", "Client/CredentialFilter.cs"],
    11: ["Extensions/Processing/Processors/PrfProcessor.cs", "Client/UserVerification/UvDecision.cs", "Client/WebAuthnClient.Ceremony.cs"],
    12: ["Extensions/Inputs/CredProtectInput.cs", "Extensions/Processing/Processors/CredProtectProcessor.cs"],
    13: ["Extensions/Codec/PrfCodec.cs", "Extensions/Outputs/PrfOutput.cs", "Extensions/Processing/Processors/LargeBlobProcessor.cs"],
    14: ["Extensions/Inputs/LargeBlobInput.cs", "Extensions/Inputs/LargeBlobSupport.cs", "Extensions/Processing/Processors/LargeBlobProcessor.cs"],
    15: ["Client/WebAuthnClient.Authentication.cs", "Extensions/Processing/Processors/LargeBlobProcessor.cs"],
    16: ["Extensions/Outputs/LargeBlobOutput.cs", "Extensions/Processing/Processors/LargeBlobProcessor.cs"],
    17: ["Extensions/Processing/Processors/LargeBlobProcessor.cs", "Client/UserVerification/UvDecision.cs"],
    18: ["Extensions/Processing/Processors/MinPinLengthProcessor.cs", "Extensions/Codec/MinPinLengthCodec.cs"],
    19: ["Extensions/PreviewSign/PreviewSignSigningParams.cs", "Extensions/Processing/Processors/PreviewSignProcessor.cs"],
    20: ["Extensions/Codec/PreviewSignCodec.cs", "Extensions/PreviewSign/PreviewSignRegistrationOutput.cs"],
    21: ["Extensions/Processing/Processors/PreviewSignProcessor.cs", "Client/Authentication/AuthenticationResponse.cs"],
}
for i, group in wa_groups.items():
    add("webauthn", f"WA-IMP-{i:03}", group, "retained-applet-finding-current-rebaseline-pending",
        "Preserve the authoritative WebAuthn finding and its CTAP boundary relationship. This registry reads historical definition and maps current source locations; its family/boundary owner still needs the exact current applet behavior and regression assertions before accepting a current defect or resolution.",
        ["src/WebAuthn/src/" + p for p in wa_paths[i]] + ["src/Ctap2/src/Ctap2Session.cs", "Sessions/ApplicationSession.cs"],
        reopen=["src/WebAuthn/README.md", "src/WebAuthn/CLAUDE.md", "docs/architecture/webauthn-extension-processing.md", "docs/usage/user-interaction.md", "docs/migration/v1-to-v2.md"])
add("webauthn", "WA-XREF-001", "assertion-user-docs", "resolved-docs-recheck-boundary",
    "The active handoff and CTAP final independent boundary review resolve this documentation dependency only. WA-IMP-010 recovery and CTAP-DEC-002 privacy actor remain open.",
    ["src/Ctap2/src/Credentials/GetAssertionResponse.cs"], related=["CTAP-XREF-001"],
    reopen=["src/Ctap2/src/Credentials/GetAssertionResponse.cs", "src/WebAuthn/src/Client/Authentication/AuthenticationResponse.cs", "src/WebAuthn/README.md", "src/Ctap2/README.md"])

# Nine boundary packets will complete these dependency traces; preliminary facts are deliberately local.
dependencies = [
    ("security-domain", "SD-DEP-001", "scp-auth-readiness", "source-mapped-review-pending", "ApplicationSession prepares/binds before I/O, publishes IsInitialized after the full callback, and sets IsAuthenticated after successful SCP. SCP11b card authentication differs from host management authority; SD owns exception translation.", ["Sessions/ApplicationSession.cs", "src/SecurityDomain/src/SecurityDomainSession.cs"]),
    ("security-domain", "SD-DEP-002", "scp-parameter-ownership", "source-mapped-review-pending", "StaticKeys clones source arrays. Scp03KeyParameters retains Keys and disposes it; Scp11KeyParameters retains key/certificate object references, copies only the collection, and disposes its private key/certificates. SD retains parameters for reset; per-call/session borrowing and alias scopes need a complete example review.", ["Protocols/SmartCard/Scp/StaticKeys.cs", "Protocols/SmartCard/Scp/Scp03KeyParameters.cs", "Protocols/SmartCard/Scp/Scp11KeyParameters.cs", "src/SecurityDomain/src/SecurityDomainSession.cs"]),
    ("security-domain", "SD-DEP-003", "scp-effective-version", "changed-source-review-pending", "The current readiness completion boundary is centralized. SD's omitted reset configuration/explicit version was a local SD-IMP-005 question; compare reset callback arguments separately before calling options preserved.", ["Sessions/ApplicationSession.cs", "Devices/Feature.cs", "Devices/FirmwareVersion.cs", "src/SecurityDomain/src/SecurityDomainSession.cs"]),
    ("security-domain", "SD-DEP-004", "shared-exchange-ownership", "source-mapped-review-pending", "A direct factory borrows its connection; protocol exchange admission/drain is distinct from a whole multi-command workflow. APDU response copies and Le formatting need the SD GetData call-site trace.", ["Sessions/ApplicationSession.cs", "Protocols/SmartCard/Apdu/ApduResponse.cs", "Protocols/SmartCard/Apdu/PcscProtocol.cs"]),
    ("security-domain", "SD-DEP-005", "logging-payload-policy", "source-mapped-review-pending", "Core uses global YubiKitLogging configuration; formatter traces can expose APDU data. Check actual formatter log calls and SD diagnostics before broad sensitive-payload assurances.", ["YubiKitLogging.cs", "Protocols/SmartCard/Apdu/ApduFormatterShort.cs", "Protocols/SmartCard/Apdu/ApduFormatterExtended.cs"]),
    ("yubiotp", "OTP-DEP-001", "shared-exchange-ownership", "changed-source-review-pending", "Owned Linux typed HID opens now transfer grouped claims before native acquisition. Current ApplicationSession owns protocol disposal and only convenience factories own hidden connections. Recheck YubiOTP selection/failure cleanup with these routes.", ["Sessions/ApplicationSession.cs", "Devices/YubiKeyDevice.cs", "Devices/Slots/HidConnectionSlot.cs", "src/YubiOtp/src/IYubiKeyExtensions.cs"]),
    ("yubiotp", "OTP-DEP-002", "otp-hid-framing", "source-mapped-review-pending", "OTP HID builds outbound CRC, polls programming sequence and resets after abandonment; inbound CRC belongs to the consuming command. Report length assumptions and failed-reset/reuse behavior need custom as well as typed Linux route checks.", ["Protocols/Otp/Hid/OtpHidProtocol.cs", "src/YubiOtp/src/Backend/HidBackend.cs"],),
    ("yubiotp", "OTP-DEP-003", "shared-exchange-ownership", "source-mapped-review-pending", "SmartCard admitted exchange uses uncancellable constituent work. UserPresence fallback cannot reclaim a sensitive result after transfer if resolution throws; applet producer owns resolution-before-transfer and cleanup.", ["Sessions/ApplicationSession.cs", "Protocols/SmartCard/Apdu/PcscProtocol.cs", "Credentials/UserPresenceNotification.cs"]),
    ("yubiotp", "OTP-DEP-004", "scp-effective-version", "source-mapped-review-pending", "Feature admits major-zero development firmware; applet raw status, metadata and effective override remain separate. Initial OTP protocol indexes report bytes before returning status; exact route invariants require review.", ["Devices/FirmwareVersion.cs", "Devices/Feature.cs", "Protocols/Otp/Hid/OtpHidProtocol.cs", "src/YubiOtp/src/YubiOtpSession.cs"]),
    ("yubiotp", "OTP-DEP-005", "otp-relocation-evidence", "missing-evidence", "Legacy/FIPS configuration relocation was unverified, not an established missing Core feature. Search actual current public surfaces in the boundary packet and retain point-in-time migration distinctions.", ["src/YubiOtp/src/IYubiOtpSession.cs", "src/Management/src/IManagementSession.cs"],),
    ("yubihsm", "HSM-DEP-001", "scp-effective-version", "source-mapped-review-pending", "Core Feature major-zero acceptance explains a compatibility query, not raw applet/metadata identity. Recheck HSM SELECT fallback, override and reset version retention locally.", ["Devices/Feature.cs", "Devices/FirmwareVersion.cs", "src/YubiHsm/src/HsmAuthSession.cs"]),
    ("yubihsm", "HSM-DEP-002", "shared-exchange-ownership", "source-mapped-review-pending", "Connection/prompt borrowing and admitted native drain need the CALCULATE path. Historical 6982/Failed plus fresh-session recovery is measured outcome, not proof of native cancellation or timeout causality.", ["Sessions/ApplicationSession.cs", "Credentials/UserPresenceNotification.cs", "src/YubiHsm/src/HsmAuthSession.cs"]),
    ("yubihsm", "HSM-DEP-003", "apdu-tlv-ownership", "source-mapped-review-pending", "ApduResponse is a sealed copied owner with Clear invalidating its views. Tlv and serialized intermediates have independent storage/lifetimes; HSM ZeroApduResponse and SessionKeys copies need full reachable-path tracing.", ["Protocols/SmartCard/Apdu/ApduResponse.cs", "Utilities/Tlv.cs", "Utilities/TlvHelper.cs", "src/YubiHsm/src/HsmAuthSession.cs"]),
    ("openpgp", "PGP-CORE-001", "credential-length-units", "source-mapped-review-pending", "Core text lengths count UTF-16 chars before encoding. OpenPGP credential-option factories and each card-profile byte limit need the boundary check; no blanket CTAP PIN policy transfers to PW1/PW3/reset code.", ["Credentials/CredentialReaderOptions.cs", "Credentials/ConsoleCredentialReader.cs", "src/OpenPgp/src/OpenPgpSession.Pin.cs"]),
    ("openpgp", "PGP-CORE-002", "shared-exchange-ownership", "source-mapped-review-pending", "Core async session disposal dispatches DisposeAsyncCore then derived Dispose(bool) in finally; connection ownership remains borrowed unless transferred. OpenPGP KDF cache alias/disposal and SCP applicability need separate applet verification.", ["Sessions/ApplicationSession.cs", "src/OpenPgp/src/OpenPgpSession.cs", "src/OpenPgp/src/OpenPgpSession.Config.cs"]),
    ("openpgp", "PGP-CORE-003", "scp-effective-version", "source-mapped-review-pending", "Keep raw 0.0.1, device metadata 5.8 beta and effective override distinct; Feature major-zero bypass is a host compatibility policy, not physical-firmware proof.", ["Devices/Feature.cs", "Devices/FirmwareVersion.cs", "src/OpenPgp/src/OpenPgpSession.cs"]),
    ("openpgp", "PGP-CORE-004", "shared-exchange-ownership", "source-mapped-review-pending", "Exchange serialization and session disposal do not alone prove whole PIN/key/reset workflow atomicity. Preserve 6600/Failed and fresh-session recovery without native-interruption claims.", ["Sessions/ApplicationSession.cs", "Protocols/ExchangeGuard.cs", "src/OpenPgp/src/OpenPgpSession.Crypto.cs"]),
    ("piv", "PIVCORE-001", "pcsc-pin-isolation", "changed-scoped-policy-physical-gap", "Current default is strict Exclusive, with shared/fallback explicit opt-ins and UNPOWER_CARD release. Source and policy assertions settle mode selection, not universal physical PIN/cache isolation across OSes/handles.", ["Transports/SmartCard/SmartCardSharing.cs", "Transports/SmartCard/SmartCardConnectionFactory.cs", "Transports/SmartCard/PcscNativeResources.cs", "src/Piv/src/PivSession.cs"]),
    ("piv", "PIVCORE-002", "shared-exchange-ownership", "source-mapped-review-pending", "Check multi-APDU authentication/recovery/reset overlap beyond individual protocol exchange guards. The session has no in-flight operation admission lease; disposal may race a larger workflow between exchanges.", ["Sessions/ApplicationSession.cs", "Protocols/ExchangeGuard.cs", "src/Piv/src/PivSession.cs"]),
    ("piv", "PIVCORE-003", "apdu-tlv-ownership", "source-mapped-review-pending", "Owned response Clear and Tlv disposal remain separate from borrowed connection and caller-transferred crypto result lifetimes. Check PIV copy/clear/transfer paths before expanding cleanup claims.", ["Protocols/SmartCard/Apdu/ApduResponse.cs", "Utilities/Tlv.cs", "Sessions/ApplicationSession.cs", "src/Piv/src/Cryptography/PrivateKeyOperations.cs"]),
]
for m, original, group, state, note, paths in dependencies:
    add(m, original, group, state, note, paths)
add("yubihsm", "HSM-IMP-008", "hsm-supplied-cryptogram-shape", "still-candidate-applet-profile",
    "Historical valid supplied cryptogram yielded 56 bytes while SessionKeys.Parse expected 48. Core AES/CMAC/derivation helpers may inform the oracle, while response-region layout/version and key/receipt API remain HSM applet/profile questions. Omission-only three-key equality leaves this mismatch open.",
    ["src/YubiHsm/src/SessionKeys.cs", "src/YubiHsm/src/HsmAuthSession.cs", "Cryptography"],
    qualification="Serial 103 USB SmartCard/Linux historical failure, applet raw 0.0.1 versus metadata 5.8 beta. Preserve missing raw 56-byte region interpretation and real HSM/connector interoperability. No duplicate Core crypto defect is minted.")

# Old code-audit Core seeds retain original Jira aliases and distinct uncertainty.
legacy = [
    ("#17", "runtime-scalar-copy", "missing-evidence", "SDK-owned scalar temporaries have nested cleanup; the macOS runtime internal scalar-copy warning survives. Current runtime revision/upstream issue disposition remains unverified and is external to SDK cleanup.", ["Cryptography/ECPrivateKey.cs"], ["YESDK-1625", "65e36386", "6ff76c34"]),
    ("#4", "ctap-unsigned-outputs", "still-candidate-applet-profile", "The old key8 unsigned assertion output deferral maps to CTAP-IMP-030 and WA-IMP-017. Removing the old direct-extension API and deleted repro did not establish response-key8 handling. Supported-producer/profile clarification remains separate from Core framing.", ["src/Ctap2/src/Credentials/GetAssertionResponse.cs"], ["YESDK-1612", "CTAP-IMP-030", "WA-IMP-017"]),
    ("#18", "pcsc-reader-name-cleanup", "resolved-source-for-exact-scope", "SCARD_READER_STATE.CreateMany frees partial allocations; FindPcscDevices finally frees the owning array on status success/failure before context disposal. Read the fake-allocation regression assertions; physical leak measurement is a separate qualification.", ["Native/Desktop/SCard/SCardReaderStates.cs", "Transports/SmartCard/FindPcscDevices.cs"], ["YESDK-1626", "013f3587", "8b7427112"]),
    ("#20", "ber-length-safety", "resolved-source-for-exact-scope", "Current Tlv.ParseData validates truncated/overflowing long-form lengths before slicing and maps malformed lengths to ArgumentException. Same mechanism as 25e; preserve both original IDs with one dedup group.", ["Utilities/Tlv.cs"], ["YESDK-1628", "ed9cd7356"]),
    ("#21", "metadata-field-length", "resolved-source-for-exact-scope", "DeviceInfo fixed-field helpers reject present wrong widths; qualifier nested fields are checked; field-length regression sources cover zero/short/long and optional absence. This exact scope excludes still-open FIPS/application-bit and flagged form-factor interpretation.", ["Devices/DeviceInfo.cs"], ["YESDK-1629", "331d3dc1", "6bd7c3386"]),
    ("#22", "pcsc-pin-isolation", "changed-scoped-policy-physical-gap", "Strict Exclusive is the constructor/factory default and sharing violation fails without shared retry; explicit fallback/shared modes remain. Unit assertions cover selection/release policy. Historical physical PIN-state uncertainty stays OS/share-mode/device qualified.", ["Transports/SmartCard/PcscNativeResources.cs", "Transports/SmartCard/SmartCardConnectionFactory.cs", "Transports/SmartCard/SmartCardSharing.cs"], ["YESDK-1630", "26b77ece8", "eef399698"]),
    ("#23", "scp-short-response", "resolved-source-for-exact-scope", "ScpState rejects response data shorter than R-MAC before stack allocation; SCP03/11 setup validates lengths/ordered tags. Inspect exact malformed-response repro assertions, separately from bare-success N2 and live interoperability.", ["Protocols/SmartCard/Scp/ScpState.cs", "Protocols/SmartCard/Scp/ScpState.Scp03.cs", "Protocols/SmartCard/Scp/ScpState.Scp11.cs"], ["YESDK-1631", "169d3694", "668f8217", "77bee74a"]),
    ("#27", "private-key-consistency", "changed-source-review-pending", "Current Core contains RSA/EC consistency enforcement, while the old combined run had an input-dependent unsupported RSA length before the expected exception. Constructor sizing/test input and current regression oracle need bounded family rebaseline; passing rerun alone never closed it.", ["Cryptography/PrivateKeyConsistency.cs", "Cryptography/RSAPrivateKey.cs"], ["YESDK-1635", "39a519e6"]),
    ("25b", "metadata-pagination", "resolved-source-for-exact-scope", "DeviceInfoReader bounds the byte page index before continuation can wrap. Keep this distinct from field-value lengths and mapping correctness.", ["Devices/DeviceInfoReader.cs", "Devices/ProtocolDeviceInfo.cs"], ["YESDK-1639", "3645b20f"]),
    ("25c", "runtime-rng-substitution", "changed-source-review-pending", "Historical custom RNG helper was removed in favor of runtime generation. Family owner must verify the exact old reachable caller/range and replacement; source absence alone is not a general randomness guarantee.", ["Cryptography"], ["YESDK-1642", "78c44513"]),
    ("25d", "runtime-hkdf-substitution", "resolved-source-for-exact-scope", "The custom HKDF counter loop is removed; current Core ARKG and CTAP PIN V2 call runtime HKDF.DeriveKey. The original custom maximum-length wrap mechanism is absent. SCP11 uses a separate X9.63 derivation; runtime/profile correctness and consumer ownership remain separate.", ["Cryptography/ArkgPrimitivesOpenSsl.cs", "src/Ctap2/src/Pin/PinUvAuthProtocolV2.cs"], ["YESDK-1643", "9ef6588d8"]),
    ("25e", "ber-length-safety", "resolved-source-for-exact-scope", "Same long-form length guards as original #20; preserve truncation and five-plus-octet overflow scope, including leading-zero valid encodings.", ["Utilities/Tlv.cs"], ["YESDK-1644", "ed9cd7356"]),
    ("25f", "tlv-encoder-sizing", "resolved-source-for-exact-scope", "EncodeDictionary materializes owned Tlv entries and routes through encoding sized by actual Length; the old guessed underallocation is replaced. Encodings and input cleanup remain distinct lifetime obligations.", ["Utilities/TlvHelper.cs"], ["YESDK-1645", "a8066061a"]),
    ("26g", "credential-redirect-policy", "resolved-source-for-exact-scope", "ReadLineEntry applies the same text policy before conversion; redirected line input retains immutable-string/noninterruptible-read limits. UTF-16-versus-UTF-8 policy remains CTAP-XREF-003, not regression of this fix.", ["Credentials/ConsoleCredentialReader.cs"], ["YESDK-1646", "958b63ef"]),
    ("26h", "credential-confirm-cleanup", "resolved-source-for-exact-scope", "Confirmation try/finally owns the first and confirmation entries through cancellation/mismatch/failure, transferring only the successful first result. Exact retained-view tests matter independently of disposal-guard tests.", ["Credentials/ConsoleCredentialReader.cs"], ["YESDK-1647", "d184fc192"]),
    ("N2", "scp-response-authentication", "resolved-source-for-exact-scope", "ScpProcessor rejects empty unauthenticated 9000/62xx/63xx except EXTERNAL AUTHENTICATE; after state advancement failure reports recovery-required. Keep forged bare success, malformed initialization and short-response process death as distinct observations.", ["Protocols/SmartCard/Scp/ScpProcessor.cs", "Protocols/SmartCard/Scp/ScpState.Scp03.cs", "Protocols/SmartCard/Apdu/PcscProtocol.cs"], ["db301466"]),
    ("N3", "credential-redirect-policy", "changed-source-review-pending", "Redirected credential confirmation now uses line input and the common policy. The old CLI keyboard-encoding correction is a separate applet/tool scope; YubiOTP's later OTP-IMP-009 records UTF8-as-scancode in its current sample. Preserve both histories rather than universalizing an old fixed label.", ["Credentials/ConsoleCredentialReader.cs", "src/YubiOtp/examples/OtpTool"], ["7db073e0a", "OTP-IMP-009"]),
    ("consideration-3", "fido-config-initialization", "resolved-source-for-exact-scope", "FidoHidProtocol.Configure logs local setup; InitializeAsync owns awaited channel exchange and ApplicationSession prepares it. Raw lazy initialization retains its separate contract. OTP Configure still synchronously initializes and needs its own review.", ["Protocols/Fido/Hid/FidoHidProtocol.cs", "Protocols/Otp/Hid/OtpHidProtocol.cs", "Sessions/ApplicationSession.cs"], []),
    ("consideration-5", "certificate-bundle-partial-cleanup", "resolved-source-for-exact-scope", "Current SecurityDomainSession.GetCertificatesAsync reserves capacity, accumulates loaded certificates and disposes that list on partial failure; success transfers ownership. Scp11 parameter disposal aliases remain a separate concern.", ["src/SecurityDomain/src/SecurityDomainSession.cs", "Protocols/SmartCard/Scp/Scp11KeyParameters.cs"], ["d1559f7d"]),
    ("PCSC-stall-finalizer-flake", "native-stall-evidence", "changed-unresolved-physical", "Native PC/SC, macOS and Linux ownership code has changed substantially, but old PC/SC stall cause and finalizer/listener watchdog flake lacked diagnosis. Current deterministic worker/drain tests cannot retroactively identify those causes.", ["Transports/SmartCard", "Transports/Hid/MacOS", "Transports/Hid/Linux"], []),
    ("multi-key-exclusions", "discovery-abandonment", "changed-unresolved-physical", "The October combined candidate excluded 125 and 9681620 before applet execution; later OATH CORE-001 supplies related abandoned-read evidence. Fresh scopes and owned Linux routes change the mechanism; matching serial/firmware/topology/release proof is still required.", ["Devices/SharedDeviceInfoRead.cs", "Devices/DiscoveryDeviceInfoReader.cs", "Devices/DeviceConnectionRegistry.cs"], []),
    ("deviceinfo-wide-app-bits", "metadata-wide-app-bits", "reproducible-from-source", "CapabilityMapper.FromApp still reads the first Int16 for lengths >1. For bytes 00 00 10, the trailing PIV bit is lost although variable widths are admitted. This is distinct from fixed field-length guards and FIPS mapping.", ["Devices/DeviceInfo.cs"], []),
    ("deviceinfo-FIPS-residual", "metadata-fips-bits", "reproducible-from-source", "The old execution residual is the same current tag14/15 -> FromApp mismatch recorded as MGMT-XREF-001; one group preserves both report identities.", ["Devices/DeviceInfo.cs"], []),
]
for original, group, state, note, paths, aliases in legacy:
    add("legacy-code-audit", original, group, state, note, paths, related=aliases,
        commits=[a for a in aliases if re.fullmatch(r"[a-f0-9]{8,10}", a)],
        evidence="docs/research/audit-v2-execution.md" if original in ("multi-key-exclusions", "deviceinfo-wide-app-bits", "deviceinfo-FIPS-residual") else None)

# Cross-campaign tooling dependencies retain their original scope; no repair here.
add("oath", "DOC-002", "migration-yaml", "reproducible-record-check",
    "Read-only PyYAML safe_load at the pinned working source raises ScannerError at line274 column488: unquoted ': ' in the PIV constructor mapping scalar. Preserve this pre-existing map defect; docs-qa success is a different check and this pass adds no YAML runtime fix.",
    ["docs/migration/v1-to-v2-map.yml"],
    evidence=AUDIT + "oath.md", reopen=["docs/migration/v1-to-v2-map.yml", "docs/migration/v1-to-v2.md"],
    qualification="A records-only Python parse was executed in preparation; no SDK/build/test/hardware execution. Python parser version is captured in sources.md.")
add("legacy-code-audit", "audit-host-selector-collision", "toolchain-host-selection", "changed-source-review-pending",
    "ProjectDiscovery still recursively discovers project paths and MatchProjects matches filename substring. At the pinned tracked baseline, Core matches only the two canonical Core test projects: this source-equivalent check finds no present tracked Core-host collision. Historical other-module collisions retain their original scope; future or untracked matching hosts can reintroduce ambiguity. Use a collision-free new host name and full unambiguous selector, recording actual selected projects/counts before a Core command.",
    ["scripts/toolchain/Discovery.cs"],
    evidence=module_run("ctap2") + "validation.md", reopen=["TOOLCHAIN.md", "docs/TESTING.md", "src/Core/README.md"],
    qualification="Only source selector evaluation is performed by this preparation. No toolchain invocation or host compilation.")


# Actual statements read in this bounded preparation. Other current-path entries
# are mapping inputs for family reviewers, never implied whole-file acceptance.
ANCHORS = {
    "base32-failure-cleanup": [(C + "Utilities/Base32.cs", 47, 60, "Overlap rejection precedes try; catch zeroes admitted destination."),
        (U + "Utilities/Base32Tests.cs", 56, 91, "Invalid/short destinations assert all-zero actual output; overlap asserts original text and bytes unchanged.")],
    "base32-shape": [(C + "Utilities/Base32.cs", 63, 119, "Terminal symbol count/padding switch; ASCII symbols and literal-space handling."),
        (U + "Utilities/Base32Tests.cs", 23, 71, "RFC result bytes, untouched success tail and zeroed invalid destination assertions.")],
    "oath-terminal-reset": [("src/Oath/src/OathSession.cs", 670, 682, "Send RESET then await DisposeAsync without SELECT/rehandshake."),
        (C + "Sessions/ApplicationSession.cs", 541, 568, "Detach always; dispose connection only when _ownsConnection.")],
    "metadata-fips-bits": [(C + "Devices/DeviceInfo.cs", 148, 154, "FIPS tags use ReadCapabilities."),
        (C + "Devices/DeviceInfo.cs", 232, 240, "ReadCapabilities dispatches FromApp."),
        (C + "Devices/DeviceInfo.cs", 327, 360, "Different FIPS map and Int16 width; FromApp uses app enum/first Int16.")],
    "metadata-form-factor": [(C + "Devices/DeviceInfo.cs", 142, 146, "High flags are extracted then the whole byte cast."),
        (C + "Devices/FormFactor.cs", 17, 58, "Non-flags enum defines physical values 0..7.")],
    "metadata-wide-app-bits": [(C + "Devices/DeviceInfo.cs", 353, 359, "More than one byte reads only Int16 at the front of the supplied span."),
        (U + "Devices/DeviceInfoFieldLengthTests.cs", 254, 265, "Wide acceptance fixture is all zero, which does not assert trailing nonzero-bit interpretation.")],
    "credential-length-units": [(C + "Credentials/ConsoleCredentialReader.cs", 184, 207, "Text policy checks input.Length and char values."),
        (C + "Credentials/ConsoleCredentialReader.cs", 210, 290, "Interactive storage/admission counts chars."),
        (C + "Credentials/ConsoleCredentialReader.cs", 294, 313, "UTF-8 byte allocation/conversion follows text length validation.")],
    "scp-auth-readiness": [(C + "Sessions/ApplicationSession.cs", 307, 338, "Ready after full callback; prepared protocol retained for cleanup; FIDO initialized asynchronously."),
        (C + "Sessions/ApplicationSession.cs", 359, 397, "SCP completion sets IsAuthenticated and effective firmware after success.")],
    "scp-parameter-ownership": [(C + "Protocols/SmartCard/Scp/StaticKeys.cs", 59, 61, "Arrays copied."),
        (C + "Protocols/SmartCard/Scp/Scp03KeyParameters.cs", 32, 58, "Reference retained and Keys disposed."),
        (C + "Protocols/SmartCard/Scp/Scp11KeyParameters.cs", 43, 58, "Object refs retained; collection copied."),
        (C + "Protocols/SmartCard/Scp/Scp11KeyParameters.cs", 97, 112, "Private key and certificate objects disposed.")],
    "shared-exchange-ownership": [(C + "Sessions/ApplicationSession.cs", 42, 44, "Session has no full in-flight-operation lifetime lease."),
        (C + "Sessions/ApplicationSession.cs", 169, 199, "Terminal resolution receives primary failure; successful produced result cannot be reclaimed by fallback."),
        (C + "Sessions/ApplicationSession.cs", 470, 568, "Async core cleanup precedes derived Dispose(bool) in finally; borrowed connection detached separately.")],
    "apdu-tlv-ownership": [(C + "Protocols/SmartCard/Apdu/ApduResponse.cs", 23, 36, "Sealed copied owner."),
        (C + "Protocols/SmartCard/Apdu/ApduResponse.cs", 94, 99, "Clear zeroes the actual owned raw buffer, including SW.")],
    "pcsc-reader-name-cleanup": [(C + "Native/Desktop/SCard/SCardReaderStates.cs", 48, 83, "Partial creation cleanup and matching owning-array release."),
        (C + "Transports/SmartCard/FindPcscDevices.cs", 136, 162, "Status call within finally-owned name array."),
        (U + "Transports/SmartCard/PcscReaderNameOwnershipTests.cs", 27, 99, "Success/failure/partial-allocation assertions check allocation/free counts, no duplicate frees, managed copies, no status call after allocation failure.")],
    "metadata-field-length": [(C + "Devices/DeviceInfo.cs", 204, 239, "Present fixed widths checked; empty capability rejected."),
        (U + "Devices/DeviceInfoFieldLengthTests.cs", 8, 118, "Explicit wrong lengths, nested qualifier and malformed-firmware-with-fallback assertions."),
        (U + "Devices/DeviceInfoFieldLengthTests.cs", 120, 170, "Absent optional fields/fallback/missing mandatory firmware controls.")],
    "pcsc-pin-isolation": [(C + "Transports/SmartCard/PcscNativeResources.cs", 41, 86, "Exclusive default; shared retry only for explicit fallback."),
        (C + "Transports/SmartCard/PcscNativeResources.cs", 116, 120, "Release requests UNPOWER_CARD."),
        (U + "Transports/SmartCard/SmartCardSharingTests.cs", 43, 75, "Default exclusive, no shared retry after contention, released claim and fresh reopen asserted."),
        (U + "Transports/SmartCard/SmartCardSharingTests.cs", 176, 288, "Explicit policies and unpower disposition assertions.")],
    "scp-short-response": [(U + "AuditV2/CoreScpAuditReproTests.cs", 38, 118, "Child-process successful-exit and typed rejection for 1..7 bytes; recovery test asserts same failure and one transmitted command."),
        (U + "AuditV2/CoreScpAuditReproTests.cs", 126, 158, "Short SCP03 and malformed SCP11 TLV setup assert BadResponseException.")],
    "scp-response-authentication": [(C + "Protocols/SmartCard/Scp/ScpProcessor.cs", 156, 173, "Reject bare success/warnings except external authenticate; notify failure after state advancement.")],
    "ber-length-safety": [(C + "Utilities/Tlv.cs", 197, 258, "Bounded long-form tag/length reads and overflow check before slicing." )],
    "tlv-encoder-sizing": [(C + "Utilities/TlvHelper.cs", 234, 268, "Build Tlv instances, sum actual TotalLength, dispose all in finally.")],
    "credential-redirect-policy": [(C + "Credentials/ConsoleCredentialReader.cs", 81, 144, "Mode-correct confirmation with disposal finally."),
        (C + "Credentials/ConsoleCredentialReader.cs", 157, 181, "Redirected text applies SatisfiesPolicy before conversion; string remains immutable.")],
    "credential-confirm-cleanup": [(C + "Credentials/ConsoleCredentialReader.cs", 85, 130, "Both owners under finally, only successful first transferred.")],
    "fido-config-initialization": [(C + "Protocols/Fido/Hid/FidoHidProtocol.cs", 49, 71, "Configure logs only; Initialize awaits guard-protected handshake.")],
    "certificate-bundle-partial-cleanup": [("src/SecurityDomain/src/SecurityDomainSession.cs", 303, 339, "Capacity reserved; append loaded certificate before observer; partial list disposed on failure; success transfers list.")],
    "toolchain-host-selection": [("scripts/toolchain/Discovery.cs", 10, 18, "Recursive project discovery includes research hosts under tests paths."),
        ("scripts/toolchain/Discovery.cs", 98, 109, "Filename substring selector, case-insensitive, fails only when zero matches.")],
}


def finish():
    # Resolve exact current file names where older handoffs used approximate names.
    alternatives = {
        C + "Devices/Slots/HidConnectionSlot.cs": C + "Devices/HidConnectionSlot.cs",
        C + "Protocols/ExchangeGuard.cs": C + "Utilities/ExchangeGuard.cs",
    }
    all_files = git("ls-tree", "-r", "--name-only", BASE).splitlines()
    tracked = set(all_files)
    evidence_paths = set()
    for row in ROWS:
        resolved = []
        for path in row["current_implementation_paths"]:
            if path not in tracked and path in alternatives and alternatives[path] in tracked:
                path = alternatives[path]
            if path not in tracked and not any(p.startswith(path.rstrip("/") + "/") for p in tracked):
                matches = [p for p in all_files if p.startswith("src/") and Path(p).name == Path(path).name]
                row.setdefault("path_mapping_gaps", []).append({"historical_or_proposed": path, "candidate_matches": matches})
                if len(matches) == 1:
                    path = matches[0]
            resolved.append(path)
        row["current_implementation_paths"] = resolved
        row["bounded_current_statement_evidence"] = [
            {"path": p, "start_line": a, "end_line": b, "observation": text,
             "read_scope": "bounded source/assertion read; no execution"}
            for p, a, b, text in ANCHORS.get(row["dedup_group"], [])]
        row["documentation"]["reopen_paths"] = [alternatives.get(p, p) for p in row["documentation"]["reopen_paths"]]
        row["documentation"]["reopen_paths"] = list(dict.fromkeys(
            [p for p in resolved if p.startswith(C) and p.endswith(".cs")]
            + row["documentation"]["reopen_paths"]))
        if row["module"] in MODULES:
            module = MODULES[row["module"]]
            prefix = "src/" + module + "/"
            extras = [p for p in all_files if p.startswith(prefix + "examples/") and
                      (p.endswith("/README.md") or p.endswith("/Program.cs"))]
            extras += [p for p in all_files if p.startswith(prefix + "tests/") and p.endswith("/CLAUDE.md")]
            if row["module"] == "webauthn":
                extras += [prefix + "tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/WebAuthnClientCeremonyFrameTests.cs",
                           prefix + "tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/UserPresenceIntentTests.cs"]
                if row["original_id"] in ("WA-IMP-020", "WA-IMP-021"):
                    extras += [prefix + "tests/Yubico.YubiKit.WebAuthn.IntegrationTests/PreviewSignTests.cs"]
            row["documentation"]["sample_test_reopening_paths"] = extras
            row["documentation"]["module_xml_reopening_paths"] = [p for p in all_files if p.startswith(prefix + "src/") and
                (p.endswith("Session.cs") or p.endswith("IYubiKeyExtensions.cs"))]
        else:
            row["documentation"]["sample_test_reopening_paths"] = ["src/Core/tests/CLAUDE.md"]
        if row["module"] == "piv":
            row["qualification"] += " PIV physical prerequisite/touch/hotplug and touch-dependent cancellation/recovery remain user-deferred. Windows concurrency work remains pending a user-provided handoff."
        row["implementation"] = {
            "state": "no runtime closure implied" if row["current_state"] in (
                "resolved-docs-recheck-boundary", "changed-reopen-docs") else row["current_state"],
            "review": "initial source rebaseline; assigned full family/boundary review pending"}
        row["documentation"]["historical_resolution"] = (
            "resolved at prior reviewed checkpoint; reopen changed binding/cache assumptions" if row["original_id"] == "MGMT-XREF-003" else
            "resolved documentation dependency at prior CTAP/WebAuthn review" if row["current_state"] == "resolved-docs-recheck-boundary" else
            "retain source-run handoff and original trigger; no new content acceptance in this preparation")
        row["verification"] = {"fresh_sdk_build": False, "fresh_sdk_test": False,
            "fresh_device_operation": False, "fresh_platform_qualification": False,
            "historical_scope_and_missing_evidence": row["qualification"],
            "next_owner": "assigned Core family and named applet boundary owner; Astra adjudicates consequential decisions"}
        row["current_path_evidence"] = []
        for path in list(dict.fromkeys(resolved + [e["path"] for e in row["bounded_current_statement_evidence"]])):
            if path in tracked:
                body = subprocess.check_output(["git", "show", BASE + ":" + path], cwd=ROOT)
                commits = git("log", "-3", "--format=%H %s", BASE, "--", path).splitlines()
                row["current_path_evidence"].append({"path": path, "git_blob": git("rev-parse", BASE + ":" + path),
                    "sha256": hashlib.sha256(body).hexdigest(), "recent_path_history_at_baseline": commits})
                evidence_paths.add(path)
            else:
                row["current_path_evidence"].append({"path": path, "kind": "directory scope or unresolved path; exact call-site review remains assigned"})
        row["historical_commit_resolution"] = []
        for commit in row["relevant_historical_commits"]:
            try:
                full = git("rev-parse", "--verify", commit + "^{commit}")
                present = subprocess.run(["git", "merge-base", "--is-ancestor", full, BASE], cwd=ROOT).returncode == 0
                row["historical_commit_resolution"].append({"requested": commit, "commit": full, "ancestor_of_source_baseline": present})
            except subprocess.CalledProcessError:
                row["historical_commit_resolution"].append({"requested": commit, "state": "missing git object"})
    groups = collections.defaultdict(list)
    for row in ROWS:
        groups[row["dedup_group"]].append(row["key"])
    counts = dict(collections.Counter(r["current_state"] for r in ROWS))
    registry = {
        "source_baseline": BASE, "observed_record_head": git("rev-parse", "HEAD"),
        "phase": "initial inherited rebaseline; family review and nine boundary packets pending",
        "counts": {"run_qualified_provenance_rows": len(ROWS),
                   "original_numbered_identity_rows": sum(r["original_identity_kind"] == "original-numbered-identity" for r in ROWS),
                   "historical_unkeyed_observation_rows": sum(r["original_identity_kind"] == "historical-unkeyed-observation-label" for r in ROWS),
                   "separately_preserved_pgp_documentation_labels": 21,
                   "nonadditive_dependency_groups": len(groups), "states": counts},
        "counting_policy": "Rows are provenance occurrences. Groups collect shared mechanisms/questions and can contain distinct defects. Neither total is a confirmed-bug count. Proposed new CORE-DOC/CORE-IMP identifiers require Astra triage after family review; never reuse historical CORE-001.",
        "groups": dict(groups), "rows": ROWS,
    }
    registry["preserved_applet_authorities"] = AUTHORITIES
    former = "f0ba9687e9532e0a08b3ea17cacd9c52a6ba6ffa"
    registry["historical_architecture_path_map"] = []
    for snapshot, old_path, replacements, transition, disposition in [
        (former, "YubiKeyDeviceMonitorService.cs", ["YubiKeyMonitor.cs", "HotplugSignals.cs", "RescanScheduler.cs"], "7bd381b33", "old service deleted; owned-lifetime worker was subsequently removed"),
        ("8754f075b^", "YubiKeyMonitorWorker.cs", ["YubiKeyMonitor.cs", "RescanScheduler.cs"], "8754f075b", "worker deleted in single-producer rewrite"),
        (former, "YubiKeyDeviceRepository.cs", ["DeviceChangeFeed.cs", "SnapshotDiff.cs"], "e8828151d", "repository deleted; publication/retention now use feed and diff"),
        (former, "DeviceEventHub.cs", ["DeviceChangeFeed.cs", "DeviceChange.cs"], "e8828151d", "event hub deleted; per-subscription replay and bounded delivery live in feed"),
        (former, "DiscoveryIdentityReader.cs", ["DiscoveryIdentityReader.cs", "DiscoveryDeviceInfoReader.cs", "SharedDeviceInfoRead.cs"], "d4f61110a", "identity reader remains and delegates bounded work; only the responsibility split changed"),
        (former, "ProtocolDeviceInfo.cs", ["ProtocolDeviceInfo.cs", "SharedDeviceInfoRead.cs", "DiscoveryDeviceInfoReader.cs"], "d4f61110a", "protocol reader remains borrowed; discovery scheduling/native ownership moved to dedicated owners"),
    ]:
        path = C + "Devices/" + old_path
        registry["historical_architecture_path_map"].append({
            "old_snapshot": git("rev-parse", snapshot), "old_path": path,
            "old_blob": git("rev-parse", snapshot + ":" + path),
            "current_paths": [C + "Devices/" + p for p in replacements],
            "transition_commit": git("rev-parse", transition), "disposition": disposition,
            "review_trigger": "Recheck old lifecycle/publication/timeout/replay/freshness claims against complete current producer and native-owner contracts; deletion alone closes no physical failure."})
    pgp = module_run("openpgp") + "investigation.md"
    pgp_lines = (ROOT / pgp).read_text().splitlines()
    registry["openpgp_documentation_dependencies"] = []
    for i in range(1, 22):
        label = f"PGP-DEP-{i:03}"
        hit = next(j for j, line in enumerate(pgp_lines) if label in line)
        start = hit
        while start > 0 and not pgp_lines[start].startswith("### PGP-C"):
            start -= 1
        end = hit + 1
        while end < len(pgp_lines) and not pgp_lines[end].startswith("### "):
            end += 1
        registry["openpgp_documentation_dependencies"].append({
            "key": "openpgp-001:" + label, "kind": "documentation impact label; zero additional Core bugs",
            "source": pgp, "start_line": start + 1, "end_line": end,
            "original_definition_and_reopening_map": "\n".join(pgp_lines[start:end])})
    (OUT / "registry.json").write_text(json.dumps(registry, indent=2) + "\n")
    source_lines = ["# Inherited source and authority register", "", f"Source baseline: `{BASE}`.", "",
        "This is a bounded source rebaseline, not a complete Core semantic audit. Registry current-path hashes and history are taken from the pinned Git object, independently of metadata-only HEAD advancement.", "",
        "## Authority order", "",
        "Active external module handoff → latest final review/correction and final boundary record → authoritative triage/findings → original investigations and retained raw evidence. Original phase-pending language stays historical. Source existence, a rename, a PR title and a green suite each answer different questions.", "",
        "The nine handoffs and their stable ledgers were read. Final evidence reviews were read for Management, CTAP2, Security Domain, YubiOTP, YubiHSM, OpenPGP and PIV; WebAuthn uses its correction chain plus CTAP's final independent boundary acceptance. OATH uses the final wave reset/provenance/documentation-impact records. Their source excerpts are incorporated by exact registry paths, without rewriting the archives.", "",
        "## Historical source authority", "",
        "GlobalPlatform final SCP11 source remained unavailable in the Security Domain cycle. The accessible source is Card Specification v2.3 Amendment F, Secure Channel Protocol 11, Version 1.3.0.13, Public Review, August 2023, GPC_SPE_093. It targets later final-1.4 work and is distinct from final 1.3 (November 2021) and final 1.4 (March 2024). The reviewed chain-serial passage is draft section 7.5.1, superseding the investigator's 7.5.4 pointer; SCP11c command restrictions are draft section 4.6. Final Card 2.3.1 section 11/11.11, Amendment F 1.3 sections 4.3/7.9 and Amendment D exact final KDF clauses remain unavailable/unverified in that bounded research. Preserve vendor actor/profile statements separately. Family reviewers reverify original passages before accepting normative conclusions.", "",
        "OATH's Python revision 4ca60f706af930459138d8dc0f0f953480e1c7a4 corroborates policy/reset flow, not earliest physical support or fresh native proof. OpenPGP Card source at URL 3.4 is the retained 3.4.1 (2020-03-18). CTAP's seed-marker erratum and WebAuthn native-result actor decision remain authoritative. The source family owners receive the retained exact source/review paths rather than a new web-research corpus.", "",
        "Historical original and alternate implementation hashes are resolved individually in registry.json. The alternate reader-cleanup 8b7427112, field-length 6bd7c3386 and strict-exclusive 26b77ece8 objects are not ancestors of this baseline; their corresponding original implementation hashes 013f3587, 331d3dc1 and eef399698 are ancestral. Current source/assertions, not those nonancestral variant titles, support the scoped rebaseline.", "",
        "The old code-audit closeout/traceability/execution records are read-only context. Three are original untracked user files; their presence here authorizes no edit, staging or publication. Missing old tmpfs logs remain missing evidence rather than reconstructed successes.", "",
        "## Execution qualification", "",
        "No SDK compilation, unit/integration test, formatter, device operation, web retrieval, branch/worktree mutation, commit, push or subagent occurred in this preparation. The existing integration reports 4,442 unit and 197 resilience executions (overlapping populations), Linux JIT/AOT native checks and zero physical modes. That evidence does not close the old two-key 9681620 native-ownership failure, serial125 SCP initialization, or the user's PIV physical deferral. Windows CCID/HID concurrency handoff is pending user readiness; no unfinished checkout was inspected.", "",
        "## Read-only tooling observations", ""]
    import yaml
    source_lines += [f"PyYAML `{yaml.__version__}` safe_load of current `docs/migration/v1-to-v2-map.yml` failed with ScannerError at line 274 column 488. The unchanged unquoted `: ` in the PIV constructor mapping is the observed scope; no map edit was made.", ""]
    # Capture actual candidate project membership using the current source algorithm, without running toolchain.
    candidates = [p for p in all_files if p.endswith(".csproj") and "/tests/" in p and ".claude/" not in p and "Core" in Path(p).stem]
    source_lines += ["Source-equivalent case-sensitive discovery plus case-insensitive filename `Core` selector yields these tracked test-host candidates (untracked generated hosts can add more):", ""]
    source_lines += ["- `" + p + "`" for p in candidates]
    source_lines += ["", "Use `Yubico.YubiKit.Core.UnitTests` / `Yubico.YubiKit.Core.IntegrationTests` only after checking actual filename uniqueness, rather than a partial Core filter. New hosts should avoid embedding those names and all archived host selectors.", "", "## Current source anchors", "",
        "These focused anchors support initial mapping. Every full semantic unit and regression assertion still receives its assigned family/boundary read. A listed test path means source evidence to inspect; it is never a fresh test pass.", ""]
    for path in sorted(evidence_paths):
        source_lines += [f"- `{path}` — blob `{git('rev-parse', BASE + ':' + path)}`; latest pinned-path commit `{git('log', '-1', '--format=%h %s', BASE, '--', path)}`."]
    source_lines += ["", "## Retired architecture mapping", "",
        "| Earlier symbol/path | Current replacement | Rebaseline consequence |", "| --- | --- | --- |",
        "| `ProtocolDeviceInfo.DiscoveryReadLeaseTracker`; earlier identity-read plumbing | `DiscoveryIdentityReader` remains and delegates to `DiscoveryDeviceInfoReader`; `SharedDeviceInfoRead` owns the native/read lifetime; `ProtocolDeviceInfo` borrows an open connection | Bounded caller wait and actual native teardown remain separate. `d4f61110a` splits responsibilities; `f32848f62` owns Linux FIDO/OTP native lifetimes. |",
        "| Device repository / event hub / global manager service | `DeviceChangeFeed`, `SnapshotDiff`, `YubiKeyMonitor`, `HotplugSignals`, `RescanScheduler` | `e8828151d`, `8754f075b`, `5ff8d25af`: recheck replay/publication/retention/disposal claims against single owned lifetime. Deleted generation/start-stop paths are history, not current defects. |",
        "| Shared discovery/filter-only Find calls | fresh `FindYubiKeys.CreateForInterfaces`, manager `FindAllAsync(interfaces: ...)` | `6d1e8dc04`: scope is pre-enumeration selection rather than post-merge filtering. Disjoint view member sets still lack global serial aliasing. |",
        "| `RawFidoHidSession`, `ApplicationIds.Fido2` | `RawCtapHidSession`, `ApplicationIds.Fido` plus same-AID `FidoU2f` | Keep CTAPHID framing separate from CTAP2 operation/UV/profile policy and FIDO2 capability identity. |",
        "| Deleted per-tool `FidoDeviceSelector` / `OpenPgpDeviceSelector` | surviving shared CLI selector and its consumers | Carry selection/cancellation/result documentation triggers into active shared call sites; avoid resurrecting deleted files. |", ""]
    (OUT / "sources.md").write_text("\n".join(source_lines))
    print(json.dumps(registry["counts"], indent=2))
    gaps = [(r["key"], g) for r in ROWS for g in r.get("path_mapping_gaps", [])]
    print("Path mapping candidates requiring disposition:", json.dumps(gaps, indent=2))


if __name__ == "__main__":
    finish()

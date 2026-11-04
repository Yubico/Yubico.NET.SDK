"""Generate the baseline CTAP2 file ownership/evidence inventory; no source edits or builds."""
import collections
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[7]
RUN = Path(__file__).resolve().parent
BASELINE = "5b09a9b6942ded3913b5c9340c3e126617e61a82"

def owner(path):
    relative = path.removeprefix("src/Ctap2/")
    if not relative.endswith(".cs") or relative.startswith("examples/"):
        return "F"
    if relative.startswith("src/"):
        unit = relative.removeprefix("src/")
        if unit.startswith("Pin/"):
            return "B"
        if unit.startswith(("Credentials/", "Cose/")) or unit in {"Cbor/CoseKeyWriter.cs", "Cbor/Ctap2SessionRequestEncoding.cs"}:
            return "C"
        if unit.startswith(("CredentialManagement/", "BioEnrollment/", "Config/")):
            return "D"
        if unit.startswith(("Extensions/", "LargeBlobs/", "Arkg/", "Crypto/")):
            return "E"
        if unit.startswith(("Backend/", "Ctap/", "Cbor/")) or unit in {"Ctap2Session.cs", "ICtap2Session.cs", "IYubiKeyExtensions.cs", "AuthenticatorInfo.cs"}:
            return "A"
        raise ValueError(f"Unassigned source: {path}")
    name = Path(path).stem
    if "/Pin/" in path or name in {"Ctap2EnhancedPinTests", "Ctap2PinManagementTests"}:
        return "B"
    if any(f"/{part}/" in path for part in ["Credentials", "Cose"]) or name in {
        "Ctap2SessionRequestEncodingTests", "Ctap2RequestAuditReproTests", "MakeCredentialResponseTests",
        "Ctap2AlgorithmSupportTests", "Ctap2EnterpriseAttestationTests", "Ctap2FipsComplianceTests",
        "Ctap2GetAssertionTests", "Ctap2MakeCredentialTests",
    }:
        return "C"
    if any(f"/{part}/" in path for part in ["CredentialManagement", "BioEnrollment", "Config"]) or name in {
        "Ctap2AuthenticatorConfigTests", "Ctap2BioEnrollmentTests", "Ctap2CredentialManagementTests",
        "Ctap2CredentialManagementExtendedTests",
    }:
        return "D"
    if any(f"/{part}/" in path for part in ["Extensions", "LargeBlobs", "Crypto"]) or name in {
        "Ctap2ExtensionAuditReproTests", "Ctap2LargeBlobAuditReproTests", "Ctap2CredBlobTests",
        "Ctap2CredProtectTests", "Ctap2EncryptedMetadataTests", "Ctap2HmacSecretTests",
        "Ctap2LargeBlobTests", "Ctap2MinPinLengthTests", "Ctap2PreviewSignTests", "Ctap2PrfTests",
    }:
        return "E"
    if name in {
        "AuthenticatorInfoTests", "CtapResponseParserTests", "Ctap2BackendLifecycleTests",
        "Ctap2SessionHidCrossLayerTests", "Ctap2SessionTests", "CtapExceptionTests", "CtapRequestBuilderTests",
        "IYubiKeyExtensionsTransportTests", "Ctap2GetInfoTests", "Ctap2SessionSimpleTests", "Ctap2SmartCardTests",
        "Ctap2TransportTests", "Ctap2TestData", "Ctap2TestHelpers", "IntegrationTestBase",
        "Ctap2TestStateExtensions", "UserPresenceProbeTests",
    }:
        return "A"
    raise ValueError(f"Unassigned test: {path}")

paths = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", BASELINE, "--", "src/Ctap2"], cwd=ROOT, text=True).splitlines()
records = []
for path in paths:
    data = subprocess.check_output(["git", "show", f"{BASELINE}:{path}"], cwd=ROOT)
    records.append(dict(path=path, owner=owner(path), bytes=len(data), sha256=hashlib.sha256(data).hexdigest(), status="inventoried-not-yet-investigated"))
assert len(records) == 183
result = dict(baseline=BASELINE, file_count=len(records), csharp_count=sum(p.endswith(".cs") for p in paths), owner_counts=dict(sorted(collections.Counter(r["owner"] for r in records).items())), note="G owns cross-layer report/matrix, not overlapping module file edits; full semantic dispositions are still pending.", files=records)
(RUN / "inventory.json").write_text(json.dumps(result, indent=2) + "\n")
print(json.dumps({key: value for key, value in result.items() if key != "files"}, indent=2))

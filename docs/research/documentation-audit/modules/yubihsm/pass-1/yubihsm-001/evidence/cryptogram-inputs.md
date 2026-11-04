# Read-only input identities for cryptogram follow-up

Captured2026-11-04. Paths below are relative to RUN unless marked repository-relative. Current working
sources may have editorial changes from the initial investigation; these identities describe this
follow-up read, not a new binary-build provenance claim. No files below were modified by investigator.

| Input | Bytes | SHA256 |
|---|---:|---|
| validation/hardware-v1-kat-20261104-125119-840-test/01-test-AuditHsm001.Hardware.IntegrationTests.log |21304|5de41071c077e10db426ec7c9f533af13a4efcf660f832be95a77b6958d8319c|
| validation/hardware-probe/tests/Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests/src/HardwareProbeTests.cs |14548|4b8b9c36f5e2d9124cf2fa32e0dc9cc3922b4c3a249c378bb9a73b222ffab84a|
| validation/hardware-probe/tests/Yubico.YubiKit.AuditHsm001.Hardware.IntegrationTests/src/SymmetricOracle.cs |7073|0fa4f481ee971468449d2efa64ddf1812eb8e526523e497408287034e30ef2bd|
| validation/hardware-probe/sources/provenance.json |2563|8c4a6f42235dbe4e8a7bb3fe3f6a998ed13938bc44103d2aac65a6e4cc78c49f|
| hardware/initial-applet.log |157|ea331a0ef062585fa107696cbbc539579562a60fd2fb8d0a0d6d93b59ce90663|
| hardware/initial-info.log |518|ed8abf5c1d7a6b63fe48af572280b7893012d0c05236c7814875e8eeb2816104|
| repository: src/YubiHsm/src/SessionKeys.cs |4241|9f7e682b20ca6aa0f106342483b156ab3b23857a28314984805fad265976e0fb|
| repository: src/YubiHsm/src/HsmAuthSession.cs |39799|683e2b71f220bd783ab3bfea78ec57383e095f4275c298c84b541e08ca1b1bf4|

Also read retained focused original-oracle excerpts `sources/python-yubihsm-core-1.txt` and `-2.txt`;
their exact original revision/ranges/hashes are in the provenance.json above. Scope of local product
read: SessionKeys parser/lifecycle and symmetric CALCULATE encoding/response handoff only. Core paths
visible in the failure stack were treated as stack provenance, not opened as implementation sources.

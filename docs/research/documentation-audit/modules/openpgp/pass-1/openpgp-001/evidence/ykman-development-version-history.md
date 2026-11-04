# Development-version follow-up from coordinator steer

This focused read responds to new execution evidence, rather than reopening general firmware research.
No device operation was performed by the investigator.

Command run in the pinned reference repository:

```bash
git show --format=fuller c910e5d25ba675ac2bfc97d35f14a4cc0314864f -- yubikit/openpgp.py ykman/_cli/openpgp.py
```

Original commit:
<https://github.com/Yubico/yubikey-manager/commit/c910e5d25ba675ac2bfc97d35f14a4cc0314864f>

- Author: Dain Nilsson; authored 2025-04-22, committed 2025-05-12.
- Message: **Override version checks for 0.0.1**.
- The OpenPGP source imports `_override_version` from its Core package.
- `_read_version` changes its successful GET VERSION decoding from
  `Version(*(_bcd(x) for x in bcd))` to
  `_override_version.patch(Version(*(_bcd(x) for x in bcd)))`.
- The pinned present source retains that patch call at `yubikit/openpgp.py:1019–1027`.

The coordinator reports direct installed Python session version 0.0.1, CLI application version 5.8.0,
card version 3.4 and device metadata identifying 5.8 beta. Its certificate helper rejects ATT/DEC/SIG
before reads at the >=5.2 gate; AUT reads absent. The narrow public-certificate baseline probe belongs
to the coordinator's separate probe owner. Record installed revision and CLI setup separately from
this pinned repository. An override-dependent difference is plausible, but its exact installed cause
is pending that owner's evidence. A historical override hook is corroboration, not proof of either
current SDK failure or device incapability. Core effective feature/override handling is deferred to
PGP-DEP-021 and the final Core boundary review.

## Superseding baseline result

The coordinator supplied `hardware/vendor-baseline-v2.json`, read narrowly by the investigator for
certificate/version/UIF evidence. The corrected helper performs real GET DATA reads for all four UIF
DOs, each returning Off; its earlier direct `get_uif` result followed Python's low-version synthesized
Off branch and was not a hardware read. Explicit ATT GET DATA returns SW9000 and 793 certificate
octets, DER SHA-256 `94afb703099edb4aaaee443c6e197c379f7f909d23f717f0868ef73057e4a16a`.
Ordinary certificate selection/read commands succeed with empty certificate data. Framing is selected
from fresh serial-103 metadata `5.8.0.beta.0`, direct session version remains0.0.1, and the helper
records `private_version_override:false`. The coordinator subsequently started the 48 non-KDF tests,
including Slow cases. Results and any later mutation/final readback remain coordinator-owned evidence.
The installed-source identity in that baseline differs from this report's separately pinned repository;
retain both identities rather than treating historical source agreement as installed execution proof.

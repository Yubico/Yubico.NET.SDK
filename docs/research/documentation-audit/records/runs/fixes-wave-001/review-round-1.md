# Wave1 first implementation review

Both implementations reached green focused tests, then fresh independent review required corrections before integration. No finding is closed merely by the initial green counts.

## CLI

Author session: `ses_e61eb4c7fffe2aIFZMwz1BiU4Z`; reviewer: `ses_e61ddcc03ffejlYWhXv6K3x7me`.
Initial evidence: 64 focused OathTool tests and231 OATH unit tests passed, zero warnings. Root bootstrap now routes through the testable execution boundary before hardware monitoring.

Required corrections, verified by coordinator source inspection:

1. Rejected `--password=VALUE`/new-password forms echo unclassified input and can disclose secrets.
2. Case normalization copies arbitrary secret-bearing arguments into additional immutable strings.
3. Global help scan reinterprets consumed option values and post-`--` positionals, including legitimate `help` data.
4. Raw add path newly rejects7 digits by importing a6/8-only restriction; preserve numeric syntax without an unapproved URI-policy restriction.
5. Local retry/selection prompts remain synchronous/token-unaware and metadata fallback catches cancellation; the new Ctrl+C handler otherwise leaves those waits blocked.
6. TryParse exceeds the root complexity limit; split purposeful shallow parsing phases. The shipping-only complexity runner does not measure example methods.

Author is addressing these with additional red/green tests. Scope remains OathTool and its tests; no hardware access or SDK parser changes.

## Protocol

Author session: `ses_e61eb4c25ffePGHfPbnG7wRzDE`; reviewer: `ses_e61ddcc01ffeWOPOE7R1nS8zU4`.
Initial evidence:205 OATH unit tests passed; live verification executed only on125, with older-firmware rows skipped due discovery.

Required corrections, verified by coordinator:

1. Known malformed full/non-result aggregate tags still become null entries; distinguish them from genuine extensions. Do not require empty HOTP/touch marker values: the official legacy APDU example contains `77 01 06`.
2. Host ID omission runs before known truncated wire validation, hiding malformed values on invalid recognized period IDs.
3. Full-HMAC documentation must reflect header removal, required structure, returned-buffer ownership and cleanup; old tag-only validation guidance is stale.
4. Hardware teardown reused an expired operation token, failed to encompass the SetKey/reopen transition, and could mask primary failures.

Author reported a correction pass with221 OATH unit tests plus two offline teardown cases green, zero warnings. Exact live reruns and re-review are still pending. Scope was expanded narrowly to Code's reusable wire parser, named metadata tags, and affected XML/README/CLAUDE contracts; timestamps/authentication taxonomy/reset recovery remain separate.

## Hardware coordination

The protocol worker originally had exclusive device access. It released hardware after reporting the older-key blocker; coordinator then investigated logs and read Core discovery ownership code. The user unplugged125 on request, leaving9681620. An existing OATH lifecycle test passed on9681620 alone. Ownership was returned to the protocol worker to run corrected tests after unit GREEN. No guard was bypassed and no Core code was changed.

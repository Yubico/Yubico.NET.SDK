# Coordinator clean-unit sampling

Astra, 2026-11-04. Final population is **17 eligible units** after removing three edited nominees
(U01, U04, U07) from the investigator's 20. Exact baseline ranges, full nominated text, SHA-256 and
current eligibility are in sampling-final.json. Required sample is three; **U08, U14 and U18** passed.

| Unit | Independent semantic review | Result |
| --- | --- | --- |
| U08 API: one-shot LIST | Read complete IYubiKeyExtensions.cs, including preceding session-creation method. The one-shot method awaits session creation, owns the await-using scope and returns the completed LIST result. The commentary accurately describes this local lifecycle. Inherited physical/native ownership remains a Core question. | PASS |
| U14 example: applet RESET | Read complete ResetHsmAuth.cs and local ResetAsync. Confirmation precedes the applet reset call; success messages describe credential removal and default management key. Independent device cleanup additionally verified this state on serial 103. The example itself performs no readback, and claims no readback measurement. | PASS |
| U18 internal/test: retry extraction | Read complete RetryExtractionTests.cs and local extraction helper. Five retry-nibble examples and five nonretry status cases directly assert the helper result. Nibble 15 is a parser test, with no claim of 15 actual device retries. Core's delegated implementation was outside this bounded review. | PASS |

Selected semantic text remains unchanged after formatting. Material misses: zero; category expansion
was unnecessary. The current product/review/coverage reconciliation covers all 56 module paths and the
five edited shared sections; this sample contributes independent clean-unit evidence, not a second audit.

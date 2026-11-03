# Astra clean-unit sampling and consequential checks

## Method and current scope

Sample baseline explanations accepted as clean by investigators; read full units and their evidence,
not only grep hits. These are source/assertion/citation checks, not fresh hardware observations.
Final denominator and narrative/example sample will be reconciled after C's inventory arrives.

## Completed clean samples (2026-11-03)

| Sample | Investigator unit | Independent check | Outcome |
| --- | --- | --- | --- |
| S-01 | A-C02, SmartCardBackend class / SELECT / configuration write | Read full backend. CCID APDU forwarding, SELECT version and write INS 1C match official Config Reference independently fetched today. Dormant SetMode discrepancy remains separately deferred. | Clean within stated scope. |
| S-02 | A-C07, ManagementBackendLifecycleTests | Read complete tests and throwing prepared-channel fake. Tests assert non-IDisposable shape and C3 command without a redundant Initialize call. They do not prove native disposal or hardware lifecycle. | Clean. |
| S-03 | A-C08, ManagementSessionExchangeOverlapTests | Read full test/fake. First page held; second call refused; wire sequence has exactly two original pages; later read succeeds; finally releases hold. Prose explicitly distinguishes exchanges from multi-exchange atomicity. | Clean. |
| S-04 | A-C19, ReconfigurationDiscoveryInvariantTests.AssertIdentityInvariants | Read full helper and surrounding caller/restore flow. Assertions enforce pairwise distinct IDs and at most one exact serial-form identity per completed scan, not completeness of device grouping or observing every transitional state. | Clean. |
| S-05 | B type summary, DeviceConfig optional/null values | Read every conditional serialization branch: null properties omitted, explicit zero/false encoded. Optional/null summary matches source independently of unresolved hardware semantics for false. | Clean. |
| S-06 | B MaxConfigLength/prefix explanatory unit | Read serialization and size check: TLV body checked against 255, output allocated body+1, first byte body length, body copied after prefix. No transport maximum or hardware acceptance inferred. | Clean. |
| S-07 | C-CLEAN-01, PACKAGE_README narrative | Read entire package guide and module csproj; read root alpha passage. Descriptive tasks match module surface, transitive Core project reference exists, alpha wording agrees with root. Remote feed/package installation not exercised. | Clean. |
| S-08 | C-CLEAN-02, README capability-query fragment | Read full fragment and surrounding supported/enabled explanation: checks USB supported PIV and enabled NFC OATH separately; implicit device/import prerequisites identified. No claim that support means enabled or that metadata proves FIPS approval. | Clean. |
| S-09 | C-CLEAN-09, DeviceInfoResult and ResetResult XML | Read both complete result files: success/error factories initialize the described fields. Nullable data permits absence; XML does not assert an invariant across arbitrary manual object initializers or prove device erasure. | Clean. |

No material miss found in these nine samples. Independent [coverage reconciliation](coverage.md)
establishes 40 clean semantic units: A20 + B8 + C12. The sample is **9/40 (22.5%)**, exceeding the
four-unit numerical minimum and spanning public XML, narrative/examples and internal/test explanations.
S-05 maps to B-C01 and S-06 to B-C03 in the machine-readable manifest. Final cross-document
reconciliation remains separate.

## Consequential adjudication (not counted as clean samples)

- Reset: independently fetched official Bio MPE manual and DeviceResetCommand reference, confirming
  product applicability and retained F9 qualification; neither establishes Management lock/configuration
  erasure. Accepted narrower documentation and deferred exact lifecycle/erasure matrix (MGMT-IMP-010).
- Independently fetched Config Reference: full replacement bits, per-command unlock and sixteen-zero
  persistent lock removal confirmed. Its NFC write tag typo and obsolete capability inventory are real;
  do not use that isolated row to change code or claim exhaustive current feature coverage.

Sources (accessed 2026-11-03):

- https://developers.yubico.com/yubikey-manager/Config_Reference.html
- https://docs.yubico.com/yesdk/users-manual/sdk-programming-guide/bio-mpe.html
- https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Management.Commands.DeviceResetCommand.html

These are live manufacturer references without immutable document revision, used for their specific
product/protocol passages. Legacy SDK syntax is not evidence of v2 API syntax.

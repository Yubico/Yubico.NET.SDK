# Published Windows audit integration successor

GPT-6.1 Sol committed and published the approved integration on 2026-11-13. The runtime baseline is
[`d33f19f972c755c78b8d07edd3a47dd6e98ce4db`](https://github.com/Yubico/Yubico.NET.SDK/commit/d33f19f972c755c78b8d07edd3a47dd6e98ce4db),
a normal merge with exact parents:

1. Audited parent `d5d01ef99f1478e4d49911194c17667b2450d2fd`.
2. Incoming parent `e322712d609aaf03b9bd1cf2964a99326728866a`.

The committed tree equals approved final tree `110339e9db4be1b287a9256b107cf4f11ab736df`. Relative
to audited parent, it contains 135 product/incoming-tooling paths and 169 packet records, including all
67 receipt-listed evidence files. All three incoming complexity justifications are in the merge message.
Final approval adds only the rereview, approval and preserved manifest records to the bounded-reviewed
tree `3a789e598180e5f3011e2fd4d0bd05878224fc70`; no product or original evidence input changed.

The initial publication used exactly:

```text
git push origin HEAD:refs/heads/yubikit-windows-audit-integration-20261113
git ls-remote --heads origin refs/heads/yubikit-windows-audit-integration-20261113
d33f19f972c755c78b8d07edd3a47dd6e98ce4db refs/heads/yubikit-windows-audit-integration-20261113
```

Both commands succeeded. Sol verified the merge's two parents, exact tree and clean checkout before
the push. No force push, audit/upstream branch promotion, original-checkout change or PR occurred.
The additive docs-only publication handoff follows the merge; its final HEAD/remote equality is checked
after pushing, rather than predicting its future OID in this document. The merge remains the runtime baseline.

The [final bounded rereview](review/final-publication-rereview.md), SHA-256
`1edd3d9facd61fd3d2d2eeb8a28c8658817f8edcdf2c2f935f8b725e8f449d40`, passed with no Critical or
Important blocker. It closes the predecessor's 40-log omission; the original failed review, correction
and both predecessor manifests remain byte-exact. Sol's final verification independently checked all
67 staged receipt entries, unchanged 1,363-file source fingerprint
`c05c4010789edd3424ec62dc143c68bb3fa3cdc36cacc2eef4069c27d6e82b02`, 13,666 frozen files,
four original user files and the original checkout/HEAD. The remaining 29 ignored packet paths are
generated outputs, not missing evidence.

The [software summary](validation/summary.md) retains 4,620 ordinary unit executions across 13 projects,
zero failures, five existing skips and zero warnings; three separate lifecycle executions; portable
guards; scoped formatter membership; and ten local XML/readme package pairs. No software rerun was
needed for log retention or this publication-only metadata. Registry links and historical/native cases
are not added to unit totals. Windows first-open/mixed 17-pass/1-fail outcomes, the two independent
unsealed JIT 10-scenario/65-check bundles, sealed/AOT/HID-driver limits, unavailable Mac raw evidence,
OTP permission/expert-topology and PIV physical/F9 qualifications remain explicit in the handoff.

The authorized integration is complete. Broader deferred fixes can use this successor as their
baseline under a separate scoped task; no deferred implementation or native campaign occurred here.

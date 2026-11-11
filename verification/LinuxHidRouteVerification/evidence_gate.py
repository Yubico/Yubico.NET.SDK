"""Shared result contract for local capture and Linux CI. No device I/O."""

REQUIRED_NATIVE_CHECKS = frozenset("""
libc-failed-open-negative-fd-and-errno-ENOENT
libc-real-eventfd-write
libc-real-eventfd-read
libc-eventfd-checked-close
libc-fd-zero-fixture-child-stdin-closed
libc-real-fd-zero-valid-and-checked-close
reactor-real-registration-pending-readiness
reactor-explicit-eventfd-signal-not-data
reactor-real-poll-readiness-generation
reactor-removal-eventfd-wake
reactor-quiescence-owner-release-before-checked-close
reactor-idle-registration-activated
reactor-shutdown-eventfd-wake-actual-quiescence
reactor-shutdown-reference-release-checked-close
reactor-real-eventfd-control-syscalls-observed
reactor-held-internal-read-callback-entered
reactor-no-false-quiescence-while-callback-held
reactor-early-reference-release-rejected
reactor-held-callback-real-exit-quiescence-release-close
fd-reuse-predecessor-close-proven
fd-reuse-real-same-integer-descriptor
fd-reuse-stale-token-generation-isolation
fd-reuse-successor-and-sentinel-drained-close
owner-output-entered-report-seam
owner-report-entry-off-caller-and-returned-task-still-pending
owner-one-pending-output-no-replacement-worker
owner-real-native-input-await-is-pending
owner-held-output-cannot-stall-other-input-progress
owner-owned-output-copy-production-L1-zero-ID-framing-no-drop
owner-40-owned-input-buffers-no-drop-reorder-or-reuse
owner-pending-reader-cancel-detaches
owner-cancel-recovery-real-reactor
owner-dispose-read-was-pending
owner-dispose-terminalizes-real-pending-read
owner-actual-quiescence-worker-release-checked-close
owner-original-output-worker-performs-checked-close
owner-after-pending-dispose-reopen-real-reactor-read
owner-reopened-checked-close
""".split())


def validate_result(result, kind, mode, exit_code, source_hash):
    if kind not in ("jit", "aot") or result.get("DynamicCode") is not (kind == "jit"):
        raise ValueError(f"DynamicCode mismatch for {kind}")
    if result.get("Mode") != mode or result.get("SourceHash") != source_hash:
        raise ValueError("mode/source label mismatch (label is not build attestation)")
    checks = result.get("Checks")
    if not isinstance(checks, list) or not all(isinstance(item, str) and item for item in checks):
        raise ValueError("named checks must be nonempty strings")
    if len(set(checks)) != len(checks):
        raise ValueError("duplicate named checks")
    if mode.startswith("--sensitivity-"):
        blocking = mode == "--sensitivity-blocking"
        expected = "owner-report-entry-off-caller-and-returned-task-still-pending" if blocking else "reactor-no-false-quiescence-while-callback-held"
        cleanup = [] if blocking else ["reactor-held-internal-read-callback-entered", "reactor-held-callback-real-exit-quiescence-release-close"]
        failure = result.get("Failure")
        if (exit_code != 1 or result.get("Outcome") != "FAILED" or checks != cleanup or
                not isinstance(failure, str) or not failure.startswith("System.InvalidOperationException: " + expected + "\n")):
            raise ValueError("exact intended negative assertion/cleanup contract")
        return 1
    if mode == "--list" and exit_code == 2 and result.get("Outcome") == "BLOCKED":
        if checks or result.get("Failure") is not None:
            raise ValueError("blocked enumeration contract")
        return 2
    if exit_code != 0 or result.get("Outcome") != "PASSED" or not checks or result.get("Failure") is not None:
        raise ValueError("positive result contract: PASSED and nonempty named checks required")
    if mode == "--native" and set(checks) != REQUIRED_NATIVE_CHECKS:
        raise ValueError("native required named checks mismatch")
    return 0

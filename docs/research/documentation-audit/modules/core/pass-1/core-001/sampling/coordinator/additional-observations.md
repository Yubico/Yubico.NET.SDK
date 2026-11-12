# Additional source observations during clean sampling

## SAMPLE-A004 — deterministic disposal of CLI attestation certificate

Selected unit `core-001:unit:10653b759a2209f8`,
`src/Cli.Commands/src/Piv/PivCommands.cs:545–566` at `fe342fb0`, receives `cert` from
`session.AttestKeyAsync(slot)`, reads/exports it, and returns without disposing that certificate.
The session is independently scoped with `await using`.

Source tracing confirms `PivSession.AttestKeyAsync` returns `Task<X509Certificate2>` and delegates to
`KeyProtocol.AttestKeyAsync`, which constructs a new certificate from the response bytes. Disposing
the session does not dispose this separately returned object. The observable source concern is missing
deterministic certificate disposal on output success/failure. This is **not a demonstrated permanent
leak**, native runtime failure, hardware result or newly implemented fix.

Current .NET documentation orientation was consulted through Context7 (`/dotnet/docs`):
`docs/standard/garbage-collection/unmanaged.md` recommends consumers use `using` for IDisposable
objects; `docs/fundamentals/syslib-diagnostics/syslib0026.md` explicitly disposes the previous
certificate instance when replacing it. These are library-use guidance, not a device standard.

Keep the runtime code unchanged in Pass 1. The deferred owner should resolve the returned certificate's
deterministic lifetime, preserving session ownership and output behavior, then reopen this caller's
resource guidance. This supplements the original candidate registry; it does not renumber or erase
the original 185 source candidates. Final handoff reconciliation must check existing aliases before
treating it as an additional unique engineering group. This sample receives no final clean credit.

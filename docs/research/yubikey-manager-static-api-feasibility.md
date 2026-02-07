     # Technical Feasibility Report: YubiKeyManager
    Static API

     **Document:** YubiKeyManager Static API Design
    Research
     **Auditor:** technical-validator
     **Date:** 2026-02-07
     **Verdict:** ✅ PASS (with recommendations)

     ---

     ## Summary

     | Severity | Count |
     |----------|-------|
     | CRITICAL | 0 |
     | WARN | 3 |
     | INFO | 3 |

     **Overall:** The design is technically
   feasible. The existing codebase contains
   foundational patterns. No P/Invoke changes or
   breaking changes required.

     ---

     ## Architecture Impact

     ### Affected Modules

     - `Yubico.YubiKit.Core/` — Primary changes to
   device discovery layer

     ### Files Changed (Estimated)

     | File | Impact |
     |------|--------|
     | `YubiKeyManager.cs` | Add static methods or
   new entry point |
     | `DeviceRepositoryCached.cs` | Add static
   factory method |
     | `DeviceMonitorService.cs` | Merge or
   simplify |
     | `DeviceListenerService.cs` | Remove after
   merge |
     | `DeviceChannel.cs` | Remove after merge |

     ---

     ## Findings

     ### WARN-001: DeviceRepositoryCached Requires
   DI Dependencies

     **Issue:** Constructor requires `ILogger<T>`
   and `IFindYubiKeys`.

     **Recommendation:** Add static factory:
     ```csharp
     public static DeviceRepositoryCached Create()
   =>

   new(NullLogger<DeviceRepositoryCached>.Instance,
    FindYubiKeys.Create());

   WARN-002: IObservable Requires System.Reactive

   Issue: Static API consumers must reference
   System.Reactive for DeviceChanges.

   Recommendation: Provide both IObservable (DI)
   and IAsyncEnumerable via MonitorAsync()
   (static).

   WARN-003: Static State Testing Challenge

   Issue: Static fields prevent parallel test
   execution.

   Recommendation: Internal swappable instance
   pattern for testability.

   -----------------------------------------------

   INFO-001: Channel Pattern Removal Safe

   IDeviceChannel, DeviceChannel,
   DeviceListenerService can be deleted after
   consolidation.

   INFO-002: Use PeriodicTimer

   Use PeriodicTimer (from
   DeviceMonitorService.cs:77) instead of
   Task.Delay.

   INFO-003: Open Question Recommendations

   ┌─────────────────────────────────┬─────────────
   ───────────────────┐
   │ Question                        │ 
   Recommendation                 │
   ├─────────────────────────────────┼─────────────
   ───────────────────┤
   │ Share cache?                    │ No —
   separate instances        │
   ├─────────────────────────────────┼─────────────
   ───────────────────┤
   │ FindAllAsync during monitoring? │ Allow —
   returns cache          │
   ├─────────────────────────────────┼─────────────
   ───────────────────┤
   │ Configuration?                  │
   YubiKey.Configure(opts => ...) │
   └─────────────────────────────────┴─────────────
   ───────────────────┘

   -----------------------------------------------

   Checklist Results

   ┌─────────────────────────┬────────┬────────────
   ─────────────────────────────┐
   │ Check                   │ Result │ Notes      
                                │
   ├─────────────────────────┼────────┼────────────
   ─────────────────────────────┤
   │ Existing infrastructure │ ✅     │
   FindYubiKeys.Create() validates pattern │
   ├─────────────────────────┼────────┼────────────
   ─────────────────────────────┤
   │ P/Invoke availability   │ ✅     │ No new
   native calls                     │
   ├─────────────────────────┼────────┼────────────
   ─────────────────────────────┤
   │ Dependency conflicts    │ ⚠️     │
   System.Reactive for IObservable         │
   ├─────────────────────────┼────────┼────────────
   ─────────────────────────────┤
   │ Breaking changes        │ ✅     │ Additive —
   IYubiKeyManager unchanged    │
   ├─────────────────────────┼────────┼────────────
   ─────────────────────────────┤
   │ Platform support        │ ✅     │ Same
   PCSC/HID code                      │
   └─────────────────────────┴────────┴────────────
   ─────────────────────────────┘

   -----------------------------------------------

   Verdict Justification

   PASS — Foundation exists (FindYubiKeys.Create()
   ), no breaking changes, platform neutral.

   Required before implementation:

     - Add DeviceRepositoryCached.Create() static
   factory
     - Decide IObservable vs IAsyncEnumerable
   (recommend both)
     - Add internal testing seam

   -----------------------------------------------

   Related Documents

     - Design Research — Original proposal
     - DX Audit — API conventions review


     ---
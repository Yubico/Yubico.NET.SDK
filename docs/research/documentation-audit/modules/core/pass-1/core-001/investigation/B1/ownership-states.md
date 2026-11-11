# Portable lifetime vocabulary for B2–B6

Source pin: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. This initial handoff describes source behavior; test-oracle and original-source qualification follows in the completed B1 report.

| Observation | Meaning and evidence boundary |
| --- | --- |
| Admitted | An owner accepted responsibility for work or a release token. Registry acquisition, worker admission and logical exchange admission are separate events. |
| Wait canceled | A caller stopped waiting. Discovery budgets and scope retirement can end a wait while already-open work retains its native resources, registry lease and transferred PID admission. |
| Native pending | An admitted worker is inside acquisition, transmit, transaction end, disconnect or context release. A cancellation request supplies no release evidence. |
| Terminal | A particular public task has completed, faulted or canceled. Name the task: a discovery wait can be terminal while its underlying read remains pending. Built-in PC/SC ordinary work publishes completion after the synchronous native borrow ends. |
| Drained | The relevant admitted work and native borrows have finished. Protocol drain, reactor quiescence and native release require their own observations. |
| Release proven | The owner has checked the required native release outcome. PC/SC requires successful disconnect of a valid card handle and successful context release; already-invalid/unacquired resources need no release call. Linux/macOS owners have platform-specific evidence rules for their investigators to verify. |
| Uncertain claim retained | Cleanup lacks release proof. Built-in PC/SC keeps the root and registration, marks it unrecovered and faults shutdown. A discovery abandonment can mark a still-active lease unrecovered temporarily; that discovery lease's eventual proven release clears the state. |

## Ownership boundaries

`YubiKeyDevice.ConnectAsync<T>` acquires sorted, deduplicated **known member interface IDs**. The registration covers the discovered grouping, rather than all interfaces that may physically exist. Disjoint scoped views remain uncoordinated. The registry is process-local; PC/SC share mode separately governs resource-manager contention, including other processes.

`PcscConnectionSlot` transfers the registration to its registered factory before opening. Built-in macOS/Linux HID slots transfer the same opaque token to their native owner. Windows/custom HID use registered wrappers, whose failure-release behavior requires separate qualification. External SmartCard creation failures release the managed token only when ordinary failure promises proven partial cleanup; `UnrecoveredConnectionException` retains it. External SmartCard disposal failures retain it regardless of exception type.

Built-in PC/SC has one lazy worker, one admitted ordinary call, and a separately tracked transaction. A transaction permits sequential transmits; it does not reserve a managed multi-command workflow against other callers on that connection. A protocol exchange guard covers one logical exchange. Applications serialize complete workflows when state spans several exchanges.

## Keep input lifetime and disposal dispatch separate

Raw SmartCard input remains caller-owned until the actual returned transmit task is terminal. Built-in PC/SC borrows that memory through native return. A token request after dispatch does not detach the task. Discovery's outer bounded wait has a different lifetime and must never be used to clear another owner's input or release its connection.

Async disposal requests teardown and awaits the shared outcome. Synchronous disposal blocks for that outcome. Finalization requests best-effort cleanup; it supplies neither a completion bound nor a promise that an abandoned public session or connection will release its physical claim. Callbacks that capture owners and pending awaits can keep objects reachable.

## Questions for platform returns

Record exact admission, cancellation, terminal, drain and release observations independently. Identify the actor retaining each handle/reference/root/claim after a failed wake or close. Distinguish borrowed buffers from owned staging. Retain the original token through checked release, including failed opens. A faulted disposal task alone supplies no generic release proof.

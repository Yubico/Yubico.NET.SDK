# Product: predictable single-key asynchronous operations

Status: revised single-key-first Gate 1 approved by the user on 2026-09-22.
The earlier broader product approval remains historical. Public access and
compatibility decisions remain settled. This is the product target, not a claim
of release completion: the [master checklist](../../../2026-09-21-yubikit-async-boundaries-ISA.md#criteria)
is currently 18/72 checked. The [status](00-status.md) separates verified
macOS typed/direct paths and listener removal, smart-card async acquisition,
and published `.8` packaging from remaining universal and other-platform work.
The selected-host macOS and smart-card milestone is closed at `21498d24`
under the local definition of done in the program design. The latest native
verification passed on one connected key, serial 31683481, including three
smart-card transaction/read/dispose/reopen cycles after replug resolved the earlier
sharing contention. Protected-chain cancellation/state proof remains managed evidence.
The Core inventory has 205 classified/outstanding sites; the test-link registry
has 27 required operation rows and 49 profile links, and the public synchronous
map has 16 entries. None closes global coverage. Matched final-source same-package/
runtime before/after measurements and a normal/idle profile exist; median
invocation return decreased while median lifecycle increased about 5% across
ten completed samples per variant. This descriptive local evidence approves no
global budget: native-only duration and pending worker counts are unavailable,
so ISC-62 remains open. Fresh-host Input Monitoring permission testing was
explicitly deferred by the user, not accepted as full production evidence.

## Problem

An application can ask a YubiKey to do something asynchronously and still freeze
while the device is working. Developers should be able to keep their application
responsive without understanding platform-specific waiting behavior. They also need
to know when cancellation has finished safely, when their input data is no longer
being used, and whether a connection can be used again.

This affects desktop applications, services, command-line applications, and advanced
users working through raw sessions or raw connection operations.

## Success metric

**For one selected physical YubiKey, 100% of the agreed asynchronous device-entry
scenarios return control to the caller while the deliberately delayed device operation
is still pending.**
Measure this by checking the order of caller return and device completion, including
opening, initialization, ordinary operations, and asynchronous disposal. A quick
successful result remains valid. Zero required scenarios may be omitted or counted
as passed because a fixture was unavailable.

Cancellation, data lifetime, connection ownership, safe reuse, and sensitive-data
handling remain release requirements alongside responsiveness. Cancellation must
not be presented as proof that the device did nothing, and a completed operation
must no longer use the caller's borrowed input. Where recovery cannot be established,
the failure must make the connection's usability clear.

Record before/after responsiveness, operation duration, resource use (including thread count), and idle
activity for the agreed scenarios. Numerical performance budgets remain a
global acceptance requirement; none is retroactively approved from local
descriptive data. Test-suite duration is not a performance baseline. Measure
one selected key at a time. This product gate does not invent a universal
millisecond target or a cross-key throughput/fairness target.

## Scope and product commitments

- Deliver predictable asynchronous behavior across the existing desktop product.
  The initial recurring verification targets are Windows on x64, Linux on x64, and
  macOS on arm64. This does not withdraw any other currently packaged platform.
- Iteration 1 proves the operation and lifetime model for one selected key at a time.
  Cover open, initialization, operation, cancellation, recovery, disposal and safe
  reopening. The previously discussed three-key simultaneous workload is deferred;
  other available keys may later be checked sequentially for compatibility.
- Preserve applet sessions, public raw sessions, and public raw connection operations,
  including advanced callers supplying their own connection implementations or
  reusing a connection sequentially across sessions.
- Breaking changes are permitted in v2. Equivalent public applet concepts must stay
  consistent; meaningful differences between applets remain explicit. Types without
  an intentional public use case may become internal. Future public expansion is
  possible, not promised.
- Preserve existing connection/session ownership and refusal of overlapping logical
  exchanges. Raw connection callers retain their documented responsibilities;
  public raw access does not imply applet-level safety guarantees.
- Retain same-key overlap refusal, discovery/connection ownership, cancellation versus
  completion, touch abandonment, unplug/replug, and disposal during an operation.
  These are required lifetime behaviors, even in a single-key iteration.
- Measure single-device operation and recovery. Exact operating-system versions,
  reader/firmware fixtures, and required platform cells must be agreed before measurement implementation and
  frozen before production changes. Unavailable required evidence blocks release
  acceptance, not independent design work.
- Defer new fairness and performance guarantees for simultaneous devices, scale limits,
  and keeping healthy keys progressing when another key stalls. Preserve the findings
  for a later iteration.
- This is a scope boundary, not a new product restriction to one attached device.
  Preserve current enumeration and existing multi-device behavior; introduce no artificial
  one-key restriction or other deliberate regression as a shortcut.
- Production delivery is accepted separately from later evaluation of alternative
  smart-card backends. Evaluation alone never changes the default shipping behavior.
  Single-key iteration completion is also distinct from full production-epic completion.

## Explicit limits

This work does not add new applets, mobile platforms, synchronous applet convenience
operations, or different prompting behavior. It does not promise instant cancellation,
undoing a device-side change, or a fixed completion deadline for an irrecoverably hung
device driver. It does not authorize unrelated cryptographic or device-identity rewrites.
The first iteration should solve this single-key use case directly, without prebuilding
features justified only by multi-key workloads. Its lifetime guarantees remain intact.

## Announcement

This first YubiKit v2 iteration establishes a predictable lifecycle for one selected
YubiKey. It keeps your application responsive while an operation waits on the
device. Applet sessions, raw sessions, and raw connection operations retain clear
completion, cancellation, and ownership contracts across desktop platforms.
Cancellation and disposal protect data lifetime and make uncertain recovery visible.
The public surface follows consistent conventions across applets, while advanced raw
access remains supported. Release claims are backed by repeatable platform and device
verification rather than asynchronous naming alone.
Shared capacity and cross-key isolation are later delivery steps, building on this model.

This is the intended release announcement, not a claim that the work is implemented.

## Screens

No user interface is added or changed by this work.

# SAMPLE-B2-003 — udev receive-result qualification

## Outcome and patch base

Verified the managed receive path, its rescan caller and the retained original libudev source. They
support null/no-event/error possibilities and conservative recovery after potential notification loss,
but do not support the claim that receive failure is “most likely during event storms”. Removed that
unsupported frequency wording; no evidence of an actual likelihood ranking was found in the cited scope.

Only ReceiveEvent's XML in `src/Core/src/Transports/Hid/Linux/LinuxHidEventSource.cs` changes.
The before-file matched **current primary bytes**, including the accepted Linux sampling patch `a994…`;
this additive patch is not based on the older revision-2 Linux source. The primary checkout remains
read-only and reserved for its owner.

**Focused patch:** `source.patch`, SHA-256
**`9405abcd2f52d438f281b809ef8d4b3f5fbd010cfb3e9d17a51ddc9b1140be86`**.

- Before/current-primary SHA-256:
  `676d31c9d87d67a9b481082caed6cb06a91576f3120aa3c336d2dd9ded1f2f29`.
- After SHA-256:
  `27276323b17745eaddd0b16d1030d01304ae6122d49d516795a945620d040445`.
- Actual selected unit: **`core-001:unit:82f001f6db22d507`**, original lines 79–86.
- Worktree: `/home/rupicola/Code/y/yubikit-core-docs-edit-transports`.

## Verified source and corrected meaning

The retained `investigation/B2/sources/libudev-monitor.c` is systemd v257 source, as identified by its
original register/manifest. Its API comment at lines 220–237 documents a newly allocated device or NULL
on error. The helper at lines 192–218 can exhaust available messages after filtering and return
`-EAGAIN`; the public wrapper at lines 239–249 maps a negative result to NULL with errno. These are
source-level outcome paths, not measurements of deployed error frequency or event-storm behavior.

The current managed ReceiveEvent returns null for a missing/invalid monitor or an invalid device
returned by the native receive. It does not inspect errno to identify a specific failure or collect
evidence about how often each cause occurs. The corrected XML names the built-in source's null paths
and explains that a native no-device result can mean no available event or a receive failure.

The recovery rationale remains: a receive failure can coincide with lost notifications, including a
removal. The listener's existing null-result branch logs its warning and emits an unknown-change rescan
hint so discovery can reconcile potentially missed changes. A null result does not itself prove that
a notification was lost. No event-storm, ENOBUFS, dropped-removal or native/device result is claimed here.

The accepted SignalShutdown attempted-wake/no-acknowledgment qualification is unchanged, as are the
loop-policy test limits, session/factory correction, native implementation, assertions and runtime
strings. No rescan behavior or native ownership policy is modified.

## Records, preservation and review

`inputs.json` preserves the current-primary before identity, before-file text, original selected-unit
record and all prior transport-record hashes. `file-disposition.json` records the one-unit XML edit;
`source-evidence.json` binds the retained native source and local control-flow anchors to exact hashes.
`static-preservation.json` records the fresh static checks and their limits.

All preceding full patches, sampling patches and editor/review records remain byte-exact, including:

- Original full transport patch `86b0c6ed78d5d1c3cf2cd68ec4b529ad8903e9b0ad3258dfa8b2bd7cb1656ef6`.
- Accepted revision-2 full patch `109bd7833b71d69f9b25b2a021ea27e460334535fe28e8fcdc8cd6bd160404de`.
- Accepted Linux sampling patch `a994f3081f0bc7439221d2a6073b5bc4f0dec9149d9dfa4c3e254799cbab1049`.
- Separate macOS seam patch `a097122a13ba002f9126de2e00bbf8d7bd2d73fc2ae4a9d9d890d873083b1206`.

Review the corrected complete ReceiveEvent XML against the managed null branches, retained libudev
contract/helper and listener rescan branch identified in `source-evidence.json`. Bind the focused patch
hash above. Independent content acceptance remains pending; static checks do not supply compilation,
native/deployment qualification or runtime proof.

No primary writes, SDK build/tests, formatter, native/device/SDK-crypto execution, Git mutation,
subagent, new framework or broader semantic audit was performed. Only this one source XML block and
new records under `editing/transports/sampling-udev-receive/` were written. Prior sampling accounting,
held implementation/qualification gaps and example identities remain unchanged. The sampler owns
edited-overlap exclusion, sampling credit and category expansion.

Finish Core Pass 1 publication and the deferred finding handoff, then stop; this correction authorizes
no implementation work or new native/device campaign.

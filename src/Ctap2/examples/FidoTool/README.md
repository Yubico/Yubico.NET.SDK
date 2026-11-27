# FidoTool - YubiKey CTAP2 CLI

A Spectre.Console example for a subset of the SDK's CTAP2 operations. Its familiar command grouping
resembles [ykman](https://docs.yubico.com/software/yubikey/tools/ykman/FIDO_Commands.html)'s `fido` group,
but it is not a compatible replacement or the full API. There are no large-blob, hmac-secret/PRF,
previewSign, raw-CBOR or authenticator-selection command flows.

The tool retains its FIDO CLI identity: the application is FIDO and its PIN is a FIDO2 PIN.
`Yubico.YubiKit.Ctap2` provides CTAP2 operations over the shared CTAPHID USB transport or the
FIDO SmartCard application. WebAuthn is the higher-level ceremony API, not the HID transport.

**Demo only.** FidoTool demonstrates CTAP exchanges; it does not register accounts or verify sign-ins.
Its credential demo uses a generated account handle and a synthetic client-data hash, and its assertion
demo displays the first result rather than running account selection. A relying party needs its own
challenge, exact client data, account binding, and independent verification of the returned data.

## Quick Start

PIN-changing and token-authorized flows select PIN/UV protocol 2 without negotiation. Before using
them, check that `AuthenticatorInfo.PinUvAuthProtocols` from a GetInfo query advertises 2. Legacy
token-command fallback retains protocol 2, not protocol-1 negotiation. This prerequisite does not
apply to plain info/reset or every PINless operation.

```bash
# From the repository root: build the SDK module (does not compile this sample).
dotnet toolchain.cs -- build --project Ctap2

# Compile/run the sample separately, from the repository root.
dotnet run --project src/Ctap2/examples/FidoTool/FidoTool.csproj

# CLI example; omit PIN arguments to use interactive entry.
dotnet run --project src/Ctap2/examples/FidoTool/FidoTool.csproj -- info
dotnet run --project src/Ctap2/examples/FidoTool/FidoTool.csproj -- access change-pin
```

## Commands

The commands below assume a built `FidoTool` executable on PATH. With `dotnet run`, use the explicit
project path above followed by `--` and the arguments. Select a dedicated evaluation key whose state
you know; PIN attempts can consume retries and administrative operations change persistent state.

### info

Display general status of the FIDO application.

```bash
FidoTool info
```

### reset

Reset the selected authenticator's FIDO application: invalidate generated credentials (including
non-discoverable/U2F credentials), erase discoverable credentials and clear the PIN. Storage/configuration
effects follow the applicable product/protocol, not a promise to reset every applet/setting on the key.
Some blocked Bio Multi-protocol states require the product's device-wide reset instead.

```bash
# Confirmation and power-cycle instructions; human-coordinated device presence is still required.
FidoTool reset

# Skips confirmation AND remove/reinsert instructions, not device timing/presence restrictions.
FidoTool reset -f
```

1. Check the physical target and authorize the irreversible reset on that dedicated key.
2. Power-cycle and send reset promptly. CTAP 2.1/2.3 specify receipt within ten seconds for displayless
   devices. [Yubico's reset guide](https://docs.yubico.com/yesdk/users-manual/application-fido2/fido2-reset.html)
   documents five seconds before firmware 5.5.2 and ten on 5.5.4+, but does not classify 5.5.2/5.5.3 there.
   Discovery, session initialization and preflight reads also consume this interval.
3. Meet the separate presence requirement: the guide describes a USB touch wait of thirty seconds,
   NFC remaining in reader contact, and product-dependent long touch. CTAP 2.3's enabled long-touch
   option requires at least five seconds, not a universal ten-second press.

The sample first tries the selected device view on the same transport after reinsertion. On macOS,
fresh HID discovery is attempted only when a valid registry iterator confirms no matching entry,
and only on the same transport with a confirmed serial that matches exactly one candidate. An
inconclusive lookup (including a null iterator) can require explicit reselection even when the entry
is gone. The sample checks the live serial on the same connection that will send Reset. Without a
serial it never retargets: if the original view opens, it compares GetInfo
AAGUID, firmware and supported versions on that connection. A matching fingerprint is **not unique device identity**.
Manually verify the physical target and avoid ambiguous multi-key setups. Failed checks prevent Reset;
missing optional metadata is not proof of unrestricted transports or a short touch. Existing executable prompts
still contain overgeneralized five/ten-second wording; follow the qualified product procedure, not those
messages as a timing authority. `-f` is not an unattended CI reset mode. A failed call does not prove no reset.

### access

Manage the FIDO2 PIN.

```bash
# Change PIN (prompts interactively if --pin/--new-pin omitted)
FidoTool access change-pin

# Verify current PIN
FidoTool access verify-pin

# Set a first PIN (implemented even though current help omits it)
FidoTool access set-pin
```

The current `verify-pin` helper requests GetAssertion permission without its required RP ID.
A permission-capable authenticator can reject that request even with the correct PIN; legacy fallback
drops the requested permission/RP scope. Failure is not proof of a wrong PIN. Do not blindly retry;
the request-scope correction remains pending.

### config

Configure advertised authenticator settings with appropriate PIN authorization. Firmware metadata
alone is insufficient. alwaysUv is a toggle affecting credential ceremonies, not every CTAP operation;
read current GetInfo state before acting and re-read after an uncertain outcome rather than repeating
the toggle blindly. The menu also sets minimum-PIN policy in code points; it does not expose the helper's
RP-list argument. This differs from the minPinLength reporting extension. Empty RP lists are omitted by the current helper,
not a request to clear recipients; raising the minimum can force a PIN change.

```bash
# Toggle always-UV setting
FidoTool config toggle-always-uv

# Enable enterprise attestation (irreversible without reset)
FidoTool config enable-ep-attestation
```

### credentials

Manage discoverable credentials using the standard credential-management command. A preview-only
advertisement or nominal 5.2 threshold does not guarantee that route. IDs are full hexadecimal values,
not ykman's credential-ID substring matching. Set `CREDENTIAL_ID_HEX` to the actual full ID to delete:

```bash
# List all stored credentials
FidoTool credentials list

# Delete a credential by ID (prompts for confirmation unless -f)
FidoTool credentials delete "$CREDENTIAL_ID_HEX"
FidoTool credentials delete "$CREDENTIAL_ID_HEX" -f
```

### fingerprints

Fingerprint flows need actual biometric hardware and advertised support; a firmware threshold is not
qualification. The standard request format is checked in software, not qualified on biometric hardware.
A decoded remaining-samples zero is not independently verified enrollment persistence. Next replies need not repeat the begin
template ID; the sample correctly retains it separately. A thrown cancellation can skip the explicit
enrollment-cancel path. Do not rely on these flows as working standardized enrollment or cancellation.
Set `FINGERPRINT_ID_HEX` to the actual template ID:

```bash
# List enrolled fingerprints
FidoTool fingerprints list

# Enroll new fingerprint with a name
FidoTool fingerprints add "Right index"

# Delete a fingerprint enrollment
FidoTool fingerprints delete "$FINGERPRINT_ID_HEX" -f

# Rename a fingerprint enrollment
FidoTool fingerprints rename "$FINGERPRINT_ID_HEX" "Left thumb"
```

## Global Options

| Option | Description |
|--------|-------------|
| `--pin PIN` | Provide PIN on the command line. If omitted, the tool prompts interactively. |
| `--new-pin PIN` | New PIN for set/change; omitted values use interactive entry. |
| `-f`, `--force` | Skip confirmations where implemented; reset also skips remove/reinsert instructions. |

These are per-command options, not a complete parser contract. Unknown/incomplete subcommands can show
usage with exit code zero, and subcommand help can select a device first. The simple parser can treat
another flag as an option value. Exit status/help are not a complete validation or operation-provenance API.

## Interactive Mode

Run without arguments to launch the interactive menu:

```bash
FidoTool
```

The menu exposes additional sample operations, not all SDK APIs:

- Set PIN (first time; also available as `access set-pin`)
- PIN/UV retry counts
- Create credential (MakeCredential)
- Get assertion (GetAssertion)
- Update user info on stored credentials
- Sensor info for biometric hardware
- Min PIN length configuration

Create/get helpers use random hash inputs and generated user handles, return the first discoverable
assertion only, and do not complete RP account registration/sign-in. The assertion hash is not returned.
The PIN branches currently also send mutually exclusive uv and token-auth fields; executable correction
is pending. The [module README](../../README.md) shows the correct token form and fresh exact-client-data/
challenge/account/RP verification responsibilities. Do not turn the sample's success display into login.

## Feature Checks

| Feature | SDK threshold metadata | Commands |
|---------|-------------|----------|
| CTAP2 base | 5.0 | info, reset, access |
| Credential Management | 5.2 | credentials list/delete |
| Bio Enrollment | 5.2 | fingerprints list/add/delete/rename |
| Authenticator Config | 5.4 | config toggle-always-uv/enable-ep-attestation |

Check actual GetInfo versions/extensions/options, enabled state, transport and permissions. Several
sample support probes catch every exception, including cancellation, and report false; "not supported"
can therefore mean a connection/read/cancellation failure rather than absent capability.

## Transport Support

- **FIDO HID** (USB) - Primary transport
- **SmartCard/CCID** - NFC and USB APDU transport when the connected authenticator exposes the FIDO AID

Prefer HID FIDO for ordinary USB CTAP2 coverage. USB SmartCard CTAP2 is supported on firmware 5.8.0+ when the FIDO AID is exposed; older USB-connected YubiKeys must use HID FIDO. CTAP2 is not available over OTP HID.

## Device Selection

The active `DeviceSelector` delegates to `Cli.Shared` and admits exposed HID FIDO **or SmartCard**
interfaces. Session creation subsequently checks the FIDO AID and transport/firmware requirements;
SmartCard-only non-CTAP keys can appear in selection. One candidate auto-selects in an interactive
terminal; multiple candidates prompt. Non-interactive commands require `--serial SERIAL` and never
fall back to another key if the requested serial is absent or ambiguous.
The shared selector identifies choices by index, uses cancellable selection/retry prompts, and returns
without prompting when a noninteractive scan finds no candidates. It reuses last-known discovery
metadata or performs one best-effort live fallback per candidate during that selection invocation.

Ctrl+C requests a cancellation token; the console handler detaches when its source is disposed.
Selection and the biometric support probe propagate cancellation and support-probe failures;
synchronous menu/PIN/ReadLine prompts remain blocked until input returns. Operation helpers still catch
general exceptions and return failure results, losing cancellation and error provenance. Support probes
other than the biometric probe may still conflate failure and absence. Inspect the operation outcome and device state before
retrying; cancellation provides no rollback of a sent command.

## Security Notes

- PIN byte owners are disposed after awaited operations; token arrays are cleared in finally. Borrowed
  PIN input remains the caller's responsibility. This is not end-to-end SDK request/response cleanup.
- CLI PIN arguments remain immutable strings in args/process memory and can appear in process listings
  or shell history. Clearing encoded bytes cannot erase these strings; prefer interactive entry.
- Do not log PINs, tokens, secret-derived outputs or application-sensitive blob data. Credential/template
  identifiers and display fields can correlate accounts even when they are not cryptographic secrets.
- Response/extension copies lack a general disposable secret owner; do not infer their clearing from
  session disposal. Destructive operations require target/state review, and reset remains human-timed.

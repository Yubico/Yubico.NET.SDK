# Linux Setup Guide

This guide covers the system configuration required to use the Yubico.NET.SDK on Linux.

## PC/SC for SmartCard Access

OATH, PIV, and other SmartCard operations use the PC/SC client library and `pcscd` service. USB CCID and
NFC readers also need a compatible reader driver. HID permissions are configured separately below.

Install the runtime packages for your distribution:

| Distribution | Packages |
|---|---|
| Ubuntu | `pcscd`, `libccid` |
| Fedora | `pcsc-lite`, `pcsc-lite-ccid` |
| Arch Linux | `pcsclite`, `ccid` |

The PC/SC package supplies or depends on the client library. The CCID package supports the YubiKey's USB
reader interface; an external NFC reader may require its own driver.

HID discovery also uses libudev. The current import requests the unversioned `libudev.so`, while some
runtime-only installations provide only `libudev.so.1`. For example, Ubuntu noble's amd64 package lists
place the unversioned name in `libudev-dev`, not `libudev1`. Verify the actual loader names in your
deployment image; installing only the runtime package need not satisfy this SDK import. Do not treat
an empty best-effort discovery result as proof that both HID and PC/SC loaded successfully.

On systemd-based installations that provide `pcscd.socket`, enable on-demand startup:

```bash
sudo systemctl enable --now pcscd.socket
systemctl status pcscd.socket
```

The socket should report `active (listening)`. The daemon can be inactive while idle and start when an
application connects. On other service managers, start `pcscd` according to your distribution's instructions.

If no SmartCard reader is found, check the service and reader driver first. The optional `pcsc_scan` utility
from `pcsc-tools` can check reader/card detection; run it when no SDK session is open. Detection does not
prove that an OATH operation or a particular firmware feature works.

References: [Yubico's Smart Card access guidance](https://github.com/Yubico/yubikey-manager/blob/main/doc/Device_Permissions.adoc),
[Ubuntu PC/SC packages](https://packages.ubuntu.com/noble/pcscd),
[Fedora PC/SC packages](https://packages.fedoraproject.org/pkgs/pcsc-lite/pcsc-lite/), and
[Arch Smartcards](https://wiki.archlinux.org/title/Smartcards).

## udev Rules for HID Device Access

Check your distribution's existing YubiKey/FIDO rules before adding local rules. Desktop session ACLs
may already grant the active user access. Headless services need a policy for their service account;
an active-desktop ACL is not a service-account permission grant. Scope access to the intended users
instead of granting every local account read/write access.

### Why This Is Needed

YubiKeys expose multiple HID interfaces:
- **FIDO interface** - May already work if you have `libfido2` or `libu2f-host` installed (these packages include FIDO udev rules)
- **OTP/Keyboard interface** - Typically requires additional rules

Without proper udev rules, you'll see `EACCES (Permission denied)` errors when the SDK tries to open HID devices.

### Installing udev Rules

If the distribution policy does not cover the required HID interface, an administrator can use a
dedicated group. In this example `yubikey` must already exist, and only authorized user/service accounts
should belong to it. Create `/etc/udev/rules.d/70-yubikey.rules`:

```udev
# Scope Yubico hidraw access to authorized members of the local yubikey group.
KERNEL=="hidraw*", SUBSYSTEM=="hidraw", ATTRS{idVendor}=="1050", MODE="0660", GROUP="yubikey"
```

Then reload and trigger the rules:

```bash
sudo udevadm control --reload-rules
sudo udevadm trigger
```

### Verifying Access

After installing the rules, unplug and replug your YubiKey, then verify the permissions:

```bash
# Find YubiKey hidraw devices
for f in /dev/hidraw*; do
  if udevadm info -q property "$f" 2>/dev/null | grep -q "ID_VENDOR_ID=1050"; then
    ls -la "$f"
  fi
done
```

Check the node's group and effective ACL as the account that will run the application. This example
expects group `yubikey` and mode `0660`; distribution/session ACLs can add further access. Metadata
enumeration alone does not establish that the account can open or exchange reports.

### Granting account access

After the administrator creates the group, add the intended account:
```bash
sudo usermod -aG yubikey "$USER"
# Log out and back in for group membership to take effect
```

For a service, configure its actual account/group membership and restart that service instead. Broad
USB-device permissions are not required merely to use PC/SC: access goes through its client library,
resource manager, and reader driver. Review those permissions separately. Avoid world-writable
`MODE="0666"` rules as a general deployment recipe.

## Troubleshooting

### Permission Denied Errors

If you get `EACCES` errors:
1. Verify udev rules are installed: `cat /etc/udev/rules.d/70-yubikey.rules`
2. Reload rules: `sudo udevadm control --reload-rules && sudo udevadm trigger`
3. Unplug and replug the YubiKey
4. Check device permissions: `ls -la /dev/hidraw*`

### Only FIDO Works, Not OTP

This usually means you have FIDO-specific rules (from `libfido2`) but not YubiKey-specific rules. Install the rules above to enable OTP access.

### Finding YubiKey Devices

```bash
# List all YubiKey HID devices with their properties
for f in /dev/hidraw*; do
  if udevadm info -q property "$f" 2>/dev/null | grep -q "ID_VENDOR_ID=1050"; then
    echo "=== $f ==="
    udevadm info -q property "$f" | grep -E 'ID_VENDOR|ID_MODEL|ID_USB_INTERFACE'
  fi
done
```

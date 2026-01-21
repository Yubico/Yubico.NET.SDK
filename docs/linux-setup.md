# Linux Setup Guide

This guide covers the system configuration required to use the Yubico.NET.SDK on Linux.

## udev Rules for HID Device Access

On Linux, HID devices (`/dev/hidraw*`) are typically only accessible by root. To allow non-root users to access YubiKey HID interfaces, you need to install udev rules.

### Why This Is Needed

YubiKeys expose multiple HID interfaces:
- **FIDO interface** - May already work if you have `libfido2` or `libu2f-host` installed (these packages include FIDO udev rules)
- **OTP/Keyboard interface** - Typically requires additional rules

Without proper udev rules, you'll see `EACCES (Permission denied)` errors when the SDK tries to open HID devices.

### Installing udev Rules

Create a file `/etc/udev/rules.d/70-yubikey.rules` with the following content:

```udev
# YubiKey HID devices - allow user access for OTP and FIDO interfaces
# Vendor ID 1050 = Yubico

# All YubiKey HID interfaces (OTP keyboard + FIDO)
KERNEL=="hidraw*", SUBSYSTEM=="hidraw", ATTRS{idVendor}=="1050", MODE="0666"

# USB device rules for non-HID access (smartcard, etc)
SUBSYSTEM=="usb", ATTR{idVendor}=="1050", MODE="0666"
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

You should see permissions like `crw-rw-rw-` (mode 0666) for YubiKey devices.

### Alternative: Using Groups

For tighter security, you can grant access to a specific group instead of all users:

```udev
# Grant access to 'plugdev' group (user must be a member)
KERNEL=="hidraw*", SUBSYSTEM=="hidraw", ATTRS{idVendor}=="1050", MODE="0660", GROUP="plugdev"
```

Add your user to the group:
```bash
sudo usermod -aG plugdev $USER
# Log out and back in for group membership to take effect
```

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

YubiKeyDeviceListener cache retains ghost devices after FIDO reset

Description:

After resetting the FIDO application on a YubiKey while PCS is running, YubiKeyDeviceListener creates and retains ghost YubiKeyDevice entries with SerialNumber=null and FirmwareVersion=0.0.0. These ghost entries persist across physical removal and reinsertion cycles, causing downstream consumers to receive invalid device references.

Steps to Reproduce:

Start an application that subscribes to YubiKeyDeviceListener.Instance.Arrived/Removed

Insert a YubiKey (e.g. serial 28823764, FW 5.7.2)

Using a separate tool, reset the FIDO application on the YubiKey while it remains inserted

Remove and reinsert the YubiKey

Observe the devices returned by YubiKeyDevice.FindAll()

Expected: A single IYubiKeyDevice with the correct serial number and firmware version.

Actual: The cache contains multiple entries for one physical key — including ghost entries with SerialNumber=null and FirmwareVersion=0.0.0. These ghosts cause consumers to attempt operations against invalid device handles, resulting in SCardException, APDU errors (0x6A82), or firmware validation failures.

Root Cause Analysis:

After FIDO reset, the HID Keyboard interface times out (~1100ms) when Update() attempts to read device info via serial-number matching (the slowest of the three matching strategies). This timeout creates a new YubiKeyDevice entry from HID-only data with no serial number or firmware version.

On physical removal, the mark-and-sweep in Update() correctly evicts SmartCard-associated entries, but the ghost was created from HID-only data and may not be properly correlated with the removed physical device. On reinsertion, the HID keyboard timeout recurs, creating a fresh ghost.

Relevant Code:

YubiKeyDeviceListener.Update() — the serial-number matching path (strategy 3) opens a connection to read device info. When this connection times out, the device is added to the cache with whatever partial data was available (null serial, 0.0.0 firmware).

Suggested Fix:

Discard devices with SerialNumber=null or FirmwareVersion=0.0.0 during the Update() cycle rather than adding them to _internalCache. A device that cannot be fully enumerated should not be surfaced to consumers.

Alternatively, improve the merge/correlation logic so that HID-only entries are always matched against SmartCard entries for the same physical device before being cached as separate entries.
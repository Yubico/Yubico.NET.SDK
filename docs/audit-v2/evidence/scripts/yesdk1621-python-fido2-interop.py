"""Manual, destructive-to-existing-blob interop step; run with user approval.

AUDIT_LARGE_BLOB_KEY_HEX must be obtained from a user-present getAssertion for
a discoverable credential on this SAME device. Do not print the key. Set
AUDIT_FIDO_PIN and optionally AUDIT_FIDO_SERIAL (decimal). Requires python-fido2
and cryptography on PYTHONPATH; select the correct key before running.

PYTHONPATH=python-fido2 AUDIT_LARGE_BLOB_KEY_HEX=... AUDIT_FIDO_PIN=... python3 C-interop.py
"""

import os

from fido2.ctap2 import Ctap2, ClientPin
from fido2.ctap2.blob import LargeBlobs
from fido2.hid import CtapHidDevice


key = bytearray.fromhex(os.environ["AUDIT_LARGE_BLOB_KEY_HEX"])
pin = os.environ["AUDIT_FIDO_PIN"]
serial = os.environ.get("AUDIT_FIDO_SERIAL")
devices = list(CtapHidDevice.list_devices())
if serial:
    devices = [d for d in devices if str(d.descriptor.serial_number) == serial]
if len(devices) != 1:
    raise RuntimeError("Specify AUDIT_FIDO_SERIAL to select exactly one device")
device = devices[0]
try:
    ctap = Ctap2(device)
    if not LargeBlobs.is_supported(ctap.info):
        raise RuntimeError("Selected authenticator does not support largeBlobs")
    client_pin = ClientPin(ctap)
    token = bytearray(client_pin.get_pin_token(pin, permissions=ClientPin.PERMISSION.LARGE_BLOB_WRITE))
    try:
        blobs = LargeBlobs(ctap, client_pin.protocol, bytes(token))
        blobs.put_blob(bytes(key), b"python-fido2-audit-interop")
        if blobs.get_blob(bytes(key)) != b"python-fido2-audit-interop":
            raise RuntimeError("python-fido2 verification failed")
        print("Interop blob written and verified; run the C# hardware read test")
    finally:
        token[:] = b"\x00" * len(token)
finally:
    key[:] = b"\x00" * len(key)
    device.close()

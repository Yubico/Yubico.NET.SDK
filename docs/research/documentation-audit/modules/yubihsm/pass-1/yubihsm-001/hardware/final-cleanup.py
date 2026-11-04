"""Independent vendor-client cleanup and default-key usability check on authorized serial 103."""
from ykman.device import list_all_devices
from yubikit.core.smartcard import SmartCardConnection
from yubikit.hsmauth import HsmAuthSession

# Public, fixed laboratory material; output contains only state and comparison results.
devices = [(device, info) for device, info in list_all_devices() if info.serial == 103]
assert len(devices) == 1, 'Expected exactly one authorized serial 103'
device, info = devices[0]
print(f'CLIENT=vendor-python; serial={info.serial}; metadata-version={info.version}')
with device.open_connection(SmartCardConnection) as connection:
    session = HsmAuthSession(connection)
    print(f'BEFORE credentials={len(session.list_credentials())}; retries={session.get_management_key_retries()}')
    session.reset()
with device.open_connection(SmartCardConnection) as connection:
    session = HsmAuthSession(connection)
    assert session.list_credentials() == []
    assert session.get_management_key_retries() == 8
    label = 'audit-hsm001-cleanup'
    try:
        session.put_credential_symmetric(bytes(16), label, bytes(range(16)), bytes(range(16, 32)), b'Cleanup1!'.ljust(16, b'\0'))
        credentials = session.list_credentials()
        assert len(credentials) == 1 and credentials[0].label == label
        session.delete_credential(bytes(16), label)
        assert session.list_credentials() == []
        assert session.get_management_key_retries() == 8
        print('DEFAULT-KEY usable=true; PUT/LIST/DELETE succeeded; credentials=0; retries=8')
    except BaseException:
        session.reset()
        raise
with device.open_connection(SmartCardConnection) as connection:
    session = HsmAuthSession(connection)
    assert session.list_credentials() == []
    assert session.get_management_key_retries() == 8
    print(f'FINAL fresh-connection=true; applet-version={session.version}; credentials=0; retries=8')

# Copyright 2016-2018 Yubico AB
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

"""Core classes for YubiHSM communication."""

import os
import struct
import warnings
from dataclasses import astuple, dataclass
from hashlib import sha256
from typing import ClassVar, Mapping, NamedTuple, Optional, Sequence, Set, Tuple

from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives import cmac, constant_time, hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.x963kdf import X963KDF

from . import utils
from .backends import YhsmBackend, get_backend
from .defs import (
    ALGORITHM,
    AUDIT,
    COMMAND,
    ERROR,
    FIPS_STATUS,
    LIST_FILTER,
    OBJECT,
    OPTION,
    Version,
)
from .exceptions import (
    YubiHsmAuthenticationError,
    YubiHsmConnectionError,
    YubiHsmDeviceError,
    YubiHsmInvalidRequestError,
    YubiHsmInvalidResponseError,
)
from .objects import LABEL_LENGTH, YhsmObject, _label_pack

KEY_ENC = 0x04
KEY_MAC = 0x06
KEY_RMAC = 0x07
CARD_CRYPTOGRAM = 0x00
HOST_CRYPTOGRAM = 0x01


def _derive(key: bytes, t: int, context: bytes, L: int = 0x80) -> bytes:
    # this only supports aes128
    if L != 0x80 and L != 0x40:
        raise ValueError("L must be 0x40 or 0x80")

    i = b"\0" * 11 + struct.pack("!BBHB", t, 0, L, 1) + context

    c = cmac.CMAC(algorithms.AES(key), backend=default_backend())
    c.update(i)
    return c.finalize()[: L // 8]


def _unpad_resp(resp: bytes, cmd: COMMAND) -> bytes:
    if len(resp) < 3:
        raise YubiHsmInvalidResponseError("Wrong length")
    rcmd, length = struct.unpack("!BH", resp[:3])
    if len(resp) < length + 3:
        raise YubiHsmInvalidResponseError("Wrong length")
    if rcmd == COMMAND.ERROR:
        raise YubiHsmDeviceError(resp[3])
    elif rcmd != cmd | 0x80:
        raise YubiHsmInvalidResponseError("Wrong command in response")
    return resp[3 : length + 3]


class _UnknownIntEnum(int):
    name = "UNKNOWN"

    def __repr__(self):
        return "<%s: %d>" % (self.name, self)

    def __str__(self):
        return self.name

    @property
    def value(self) -> int:
        return int(self)


class _UnknownAlgorithm(_UnknownIntEnum):
    """Wrapper for unknown ALGORITHM values.

    Provides obj.name, obj.value and and string representations."""

    name = "ALGORITHM.UNKNOWN"


def _algorithm(val: int) -> ALGORITHM:
    try:
        return ALGORITHM(val)
    except ValueError:
        return _UnknownAlgorithm(val)  # type: ignore


class _UnknownCommand(_UnknownIntEnum):
    """Wrapper for unknown COMMAND values.

    Provides obj.name, obj.value and and string representations."""

    name = "COMMAND.UNKNOWN"


@dataclass(frozen=True)
class DeviceInfo:
    """Data class holding various information about the YubiHSM.

    :ivar version: YubiHSM version tuple.
    :ivar serial: YubiHSM serial number.
    :ivar log_size: Log entry storage capacity.
    :ivar log_used: Log entries currently stored.
    :ivar supported_algorithms: List of supported algorithms.
    :ivar part_number: Chip designator.
    """

    FORMAT: ClassVar[str] = "!BBBIBB"
    LENGTH: ClassVar[int] = struct.calcsize(FORMAT)

    version: Version
    serial: int
    log_size: int
    log_used: int
    supported_algorithms: Set[ALGORITHM]
    part_number: Optional[str]

    @classmethod
    def parse(
        cls, first_page: bytes, second_page: Optional[bytes] = None
    ) -> "DeviceInfo":
        """Parse a DeviceInfo from its binary representation."""
        unpacked = struct.unpack_from(cls.FORMAT, first_page)
        version: Version = unpacked[:3]  # type: ignore
        serial, log_size, log_used = unpacked[3:]
        algorithms = {_algorithm(a) for a in first_page[cls.LENGTH :]}
        part_number = None
        if second_page:
            part_number = second_page.decode("utf-8")

        return cls(version, serial, log_size, log_used, algorithms, part_number)


def _calculate_iv(key: bytes, counter: int) -> bytes:
    encryptor = Cipher(
        algorithms.AES(key),
        modes.ECB(),  # noqa: S305
        backend=default_backend(),
    ).encryptor()
    return encryptor.update(int.to_bytes(counter, 16, "big")) + encryptor.finalize()


def _calculate_mac(key: bytes, chain: bytes, message: bytes) -> Tuple[bytes, bytes]:
    c = cmac.CMAC(algorithms.AES(key), backend=default_backend())
    c.update(chain)
    c.update(message)
    chain = c.finalize()
    return chain, chain[:8]


@dataclass(frozen=True)
class LogEntry:
    """YubiHSM log entry.

    :param int number: The sequence number of the entry.
    :param int command: The COMMAND executed.
    :param int length: The length of the command.
    :param int session_key: The ID of the Authentication Key for the session.
    :param int target_key: The ID of the key used by the command.
    :param int second_key: The ID of the secondary key used by the command, if
        applicable.
    :param int result: The result byte of the response.
    :param int tick: The YubiHSM system tick value when the command was run.
    :param bytes digest: A truncated hash of the entry and previous digest.
    """

    FORMAT: ClassVar[str] = "!HBHHHHBL16s"
    LENGTH: ClassVar[int] = struct.calcsize(FORMAT)

    number: int
    command: COMMAND
    length: int
    session_key: int
    target_key: int
    second_key: int
    result: int
    tick: int
    digest: bytes

    @property
    def data(self) -> bytes:
        """Get log entry binary data.

        :return: The binary LogEntry data, excluding the digest.
        """
        return struct.pack(self.FORMAT, *astuple(self))[:-16]

    @classmethod
    def parse(cls, data: bytes) -> "LogEntry":
        """Parse a LogEntry from its binary representation.

        :param data: Binary data to unpack from.
        :return: The parsed object.
        """
        unpacked = list(struct.unpack(cls.FORMAT, data))
        try:
            unpacked[1] = COMMAND(unpacked[1])
        except ValueError:
            unpacked[1] = _UnknownCommand(unpacked[1])
        return cls(*unpacked)

    def validate(self, previous_entry: "LogEntry") -> bool:
        """Validate the hash of a single log entry.

        Validates the hash of this entry with regard to the previous entry's
        hash. The previous entry is the LogEntry with the previous number,
        previous_entry.number == self.number - 1

        :param previous_entry: The previous log entry to validate against.
        :return: True if the digest is correct, False if not.
        """

        if (self.number - previous_entry.number) & 0xFFFF != 1:
            raise ValueError("previous_entry has wrong number!")

        digest = sha256(self.data + previous_entry.digest).digest()[:16]
        return constant_time.bytes_eq(self.digest, digest)


class LogData(NamedTuple):
    """Data class holding response data from a GET_LOGS command.

    :param n_boot: Number of unlogged boot events.
    :param n_auth: Number of unlogged authentication events.
    :param entries: List of LogEntry items.
    """

    n_boot: int
    n_auth: int
    entries: Sequence[LogEntry]


class _ClosedBackend(YhsmBackend):
    def transceive(self, msg):
        raise TypeError("The backend has been closed!")

    def close(self):
        pass


class YubiHsm:
    """An unauthenticated connection to a YubiHSM."""

    def __init__(self, backend: YhsmBackend):
        """Constructs a YubiHSM connected to the given backend.

        :param backend: A backend used to communicate with a YubiHSM.
        """
        self._backend: YhsmBackend = backend

        # Initialize the message buffer size to 2048 bytes. This may be updated
        # depending on the YubiHSM FW version (in 2.4.0 or higher the
        # buffer size is 3136) in get_device_info.
        self._msg_buf_size = 2048
        self.get_device_info()

    def __enter__(self):
        return self

    def __exit__(self, typ, value, traceback):
        self.close()

    def close(self) -> None:
        """Disconnect from the backend, freeing any resources in use by it."""
        if self._backend:
            self._backend.close()
            self._backend = _ClosedBackend()

    def _transceive(self, msg: bytes) -> bytes:
        if len(msg) > self._msg_buf_size - 1:
            raise YubiHsmInvalidRequestError("Message too long.")
        return self._backend.transceive(msg)

    def send_cmd(self, cmd: COMMAND, data: bytes = b"") -> bytes:
        """Encode and send a command byte and its associated data.

        :param cmd: The command to send.
        :param data: The command payload to send.
        :return: The response data from the YubiHSM.
        """
        msg = struct.pack("!BH", cmd, len(data)) + data
        return _unpad_resp(self._transceive(msg), cmd)

    def get_device_info(self) -> DeviceInfo:
        """Get general device information from the YubiHSM.

        :return: Device information.
        """
        first_page = self.send_cmd(COMMAND.DEVICE_INFO)
        device_info = DeviceInfo.parse(first_page)
        if device_info.version >= (2, 4, 0):
            # Update maximum message buffer size
            self._msg_buf_size = 3136
            # fetch next page
            second_page = self.send_cmd(COMMAND.DEVICE_INFO, struct.pack("!B", 1))
            device_info = DeviceInfo.parse(first_page, second_page)
        return device_info

    def get_device_public_key(self) -> ec.EllipticCurvePublicKey:
        """Retrieve the device's public key.

        :return: The device public key.
        """
        resp = self.send_cmd(COMMAND.GET_DEVICE_PUBLIC_KEY)
        algorithm, public_key = resp[0], resp[1:]
        if algorithm != ALGORITHM.EC_P256_YUBICO_AUTHENTICATION:
            raise YubiHsmInvalidResponseError()
        return ec.EllipticCurvePublicKey.from_encoded_point(
            ec.SECP256R1(), b"\x04" + public_key
        )

    def init_session(self, auth_key_id: int) -> "SymmetricAuth":
        """Initiate the symmetric authentication process for establishing
        an authenticated session with the YubiHSM.

        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :return: A negotiation of an authenticated Session with a YubiHSM.
        """
        return SymmetricAuth.init_session(self, auth_key_id)

    def init_session_asymmetric(
        self, auth_key_id: int, epk_oce: bytes
    ) -> "AsymmetricAuth":
        """Initiate the asymmetric authentication process for establishing
        an authenticated session with the YubiHSM.

        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :param epk_oce: The ephemeral public key of the OCE used
            for key agreement.
        """
        return AsymmetricAuth.init_session(self, auth_key_id, epk_oce)

    def create_session(
        self, auth_key_id: int, key_enc: bytes, key_mac: bytes
    ) -> "AuthSession":
        """Create an authenticated session with the YubiHSM.

        See also create_session_derived, which derives K-ENC and K-MAC from a
        password.

        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :param key_enc: Static K-ENC used to establish session.
        :param key_mac: Static K-MAC used to establish session.
        :return: An authenticated session.
        """
        return SymmetricAuth.create_session(self, auth_key_id, key_enc, key_mac)

    def create_session_derived(self, auth_key_id: int, password: str) -> "AuthSession":
        """Create an authenticated session with the YubiHSM.

        Uses a supplied password to derive the keys K-ENC and K-MAC.

        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :param password: The password used to derive the keys from.
        :return: An authenticated session.
        """
        key_enc, key_mac = utils.password_to_key(password)
        return self.create_session(auth_key_id, key_enc, key_mac)

    def create_session_asymmetric(
        self,
        auth_key_id: int,
        private_key: ec.EllipticCurvePrivateKey,
        public_key: Optional[ec.EllipticCurvePublicKey] = None,
    ) -> "AuthSession":
        """Create an authenticated session with the YubiHSM.

        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :param private_key: Private key corresponding to the public
            authentication key object.
        :param public_key: The device's public key. If omitted, the public key
            is fetched from the YubiHSM.
        :return: An authenticated session.
        """
        if public_key is None:
            public_key = self.get_device_public_key()
        return AsymmetricAuth.create_session(self, auth_key_id, private_key, public_key)

    @classmethod
    def connect(cls, url: Optional[str] = None) -> "YubiHsm":
        """Return a YubiHsm connected to the backend specified by the URL.

        If no URL is given this will attempt to connect to a YubiHSM connector
        running on localhost, using the default port.

        :param url: A http(s):// or yhusb:// backend URL.
        :return: A YubiHsm instance connected to the backend referenced by the url.
        """
        return cls(get_backend(url))

    def __repr__(self):
        return "{0.__class__.__name__}({0._backend})".format(self)


class SymmetricAuth:
    """A negotiation of an authenticated Session with a YubiHSM.

    This class is used to begin the mutual authentication process
    for establishing an authenticated session with the YubiHSM,
    using symmetric authentication. Typically you get an instance
    of this class by calling :func:`~YubiHsm.init_session`.
    """

    def __init__(self, hsm: YubiHsm, sid: int, context: bytes, card_crypto: bytes):
        self._hsm = hsm
        self._sid = sid
        self._context = context
        self._card_crypto = card_crypto

    @property
    def context(self) -> bytes:
        """The authentication context (host challenge + card challenge)."""
        return self._context

    @property
    def card_crypto(self) -> bytes:
        """The card cryptogram."""
        return self._card_crypto

    @classmethod
    def init_session(
        cls,
        hsm: YubiHsm,
        auth_key_id: int,
    ) -> "SymmetricAuth":
        """Initiate the mutual symmetric session authentication process.

        :param hsm: The YubiHSM connection.
        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        """
        context = os.urandom(8)

        data = hsm.send_cmd(
            COMMAND.CREATE_SESSION, struct.pack("!H", auth_key_id) + context
        )

        sid = data[0]
        context += data[1 : 1 + 8]
        card_crypto = data[9 : 9 + 8]

        return cls(hsm, sid, context, card_crypto)

    @classmethod
    def create_session(
        cls, hsm: YubiHsm, auth_key_id: int, key_enc: bytes, key_mac: bytes
    ) -> "AuthSession":
        """Construct an authenticated session.

        :param hsm: The YubiHSM connection.
        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :param key_enc: Static `K-ENC` used to establish the session.
        :param key_mac: Static `K-MAC` used to establish the session.
        """

        symmetric_auth = cls.init_session(hsm, auth_key_id)

        key_senc = _derive(key_enc, KEY_ENC, symmetric_auth.context)
        key_smac = _derive(key_mac, KEY_MAC, symmetric_auth.context)
        key_srmac = _derive(key_mac, KEY_RMAC, symmetric_auth.context)

        return symmetric_auth.authenticate(key_senc, key_smac, key_srmac)

    def authenticate(
        self, key_senc: bytes, key_smac: bytes, key_srmac: bytes
    ) -> "AuthSession":
        """Construct an authenticated session.

        :param key_senc: `S-ENC` used for data confidentiality.
        :param key_smac: `S-MAC` used for data and protocol integrity.
        :param key_srmac: `S-RMAC` used for data and protocol integrity.
        :return: An authenticated session.
        """

        gen_card_crypto = _derive(key_smac, CARD_CRYPTOGRAM, self._context, 0x40)

        if not constant_time.bytes_eq(gen_card_crypto, self._card_crypto):
            raise YubiHsmAuthenticationError()

        msg = struct.pack("!BHB", COMMAND.AUTHENTICATE_SESSION, 1 + 8 + 8, self._sid)
        msg += _derive(key_smac, HOST_CRYPTOGRAM, self._context, 0x40)
        mac_chain, mac = _calculate_mac(key_smac, b"\0" * 16, msg)
        msg += mac
        if _unpad_resp(self._hsm._transceive(msg), COMMAND.AUTHENTICATE_SESSION) != b"":
            raise YubiHsmInvalidResponseError("Non-empty response")

        return AuthSession(
            self._hsm, self._sid, key_senc, key_smac, key_srmac, mac_chain
        )


class AsymmetricAuth:
    """A negotiation of an authenticated Session with a YubiHSM.

    This class is used to begin the mutual authentication process
    for establishing an authenticated session with the YubiHSM,
    using asymmetric authentication. Typically you get an instance
    of this class by calling :func:`~YubiHsm.init_session_asymmetric`.
    """

    def __init__(
        self,
        hsm: YubiHsm,
        sid: int,
        context: bytes,
        receipt: bytes,
    ):
        self._hsm = hsm
        self._sid = sid
        self._context = context
        self._receipt = receipt

    @property
    def context(self) -> bytes:
        """The authentication context (EPK.OCE + EPK.SD)."""
        return self._context

    @property
    def receipt(self) -> bytes:
        """The receipt."""
        return self._receipt

    @property
    def epk_hsm(self) -> bytes:
        """The ephemeral public key of the YubiHSM."""
        return self._context[65:]

    @classmethod
    def init_session(
        cls,
        hsm: YubiHsm,
        auth_key_id: int,
        epk_oce: bytes,
    ) -> "AsymmetricAuth":
        """Initiate the mutual asymmetric session authentication process.

        :param hsm: The YubiHSM connection.
        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :param epk_oce: The ephemeral public key of the OCE used
            for key agreement.
        """

        public_key_len = len(epk_oce)
        msg = struct.pack("!H", auth_key_id) + epk_oce
        resp = hsm.send_cmd(COMMAND.CREATE_SESSION, msg)
        sid, epk_hsm, receipt = (
            resp[0],
            resp[1 : 1 + public_key_len],
            resp[1 + public_key_len :],
        )
        context = epk_oce + epk_hsm

        return cls(hsm, sid, context, receipt)

    @classmethod
    def create_session(
        cls,
        hsm: YubiHsm,
        auth_key_id: int,
        private_key: ec.EllipticCurvePrivateKey,
        public_key: ec.EllipticCurvePublicKey,
    ) -> "AuthSession":
        """Construct an authenticated session.

        :param hsm: The YubiHSM connection.
        :param auth_key_id: The ID of the Authentication key used to
            authenticate the session.
        :param private_key: Private key corresponding to the public
            authentication key object.
        :param public_key: The device's public key.
        """
        # Calculate shared secret from the two static keys.
        shsss = private_key.exchange(ec.ECDH(), public_key)

        # Generate an ephemeral key.
        esk_oce = ec.generate_private_key(private_key.curve, backend=default_backend())
        epk_oce = esk_oce.public_key().public_bytes(
            encoding=serialization.Encoding.X962,
            format=serialization.PublicFormat.UncompressedPoint,
        )

        # Exchange ephemereal keys with the HSM
        asymmetric_auth = cls.init_session(hsm, auth_key_id, epk_oce)

        # Calculate shared secret from the two ephemeral keys.
        shsee = esk_oce.exchange(
            ec.ECDH(),
            ec.EllipticCurvePublicKey.from_encoded_point(
                private_key.curve, asymmetric_auth.epk_hsm
            ),
        )

        # Derive session keys. Note that this generates four keys, the
        # first of which is used to verify the receipt.
        shs = X963KDF(
            hashes.SHA256(), 4 * 16, b"\x3c\x88\x10", backend=default_backend()
        ).derive(shsee + shsss)
        keys = (shs[i : i + 16] for i in range(0, len(shs), 16))

        # Verify the receipt.
        c = cmac.CMAC(algorithms.AES(next(keys)), backend=default_backend())
        c.update(asymmetric_auth.epk_hsm)
        c.update(epk_oce)
        if not constant_time.bytes_eq(c.finalize(), asymmetric_auth.receipt):
            raise YubiHsmAuthenticationError()

        return asymmetric_auth.authenticate(next(keys), next(keys), next(keys))

    def authenticate(
        self, key_senc: bytes, key_smac: bytes, key_srmac: bytes
    ) -> "AuthSession":
        """Construct an authenticated session.

        :param key_senc: `S-ENC` used for data confidentiality.
        :param key_smac: `S-MAC` used for data and protocol integrity.
        :param key_srmac: `S-RMAC` used for data and protocol integrity.
        :return: An authenticated session.
        """
        return AuthSession(
            self._hsm, self._sid, key_senc, key_smac, key_srmac, self._receipt
        )


class AuthSession:
    """An authenticated secure session with a YubiHSM.

    Typically you get an instance of this class by calling
    :func:`~YubiHsm.create_session`, :func:`~YubiHsm.create_session_derived`,
    or :func:`~YubiHsm.create_session_asymmetric`.
    """

    def __init__(
        self,
        hsm: YubiHsm,
        sid: int,
        key_enc: bytes,
        key_mac: bytes,
        key_rmac: bytes,
        mac_chain: bytes,
    ):
        self._hsm = hsm
        self._sid: Optional[int] = sid
        self._key_enc = key_enc
        self._key_mac = key_mac
        self._key_rmac = key_rmac
        self._mac_chain = mac_chain
        self._ctr = 1

    def close(self) -> None:
        """Close this session with the YubiHSM.

        Once closed, this session object can no longer be used, unless re-connected.
        """

        if self._sid is not None:
            try:
                self.send_secure_cmd(COMMAND.CLOSE_SESSION)
            finally:
                self._sid = None
                self._key_enc = self._key_mac = self._key_rmac = b""

    def __enter__(self):
        return self

    def __exit__(self, typ, value, traceback):
        self.close()

    def _secure_transceive(self, msg: bytes) -> bytes:
        padlen = 15 - len(msg) % 16
        msg += b"\x80"
        msg = msg.ljust(len(msg) + padlen, b"\0")

        wrapped = struct.pack(
            "!BHB", COMMAND.SESSION_MESSAGE, 1 + len(msg) + 8, self.sid
        )
        cipher = Cipher(
            algorithms.AES(self._key_enc),
            modes.CBC(_calculate_iv(self._key_enc, self._ctr)),
            backend=default_backend(),
        )
        encryptor = cipher.encryptor()
        wrapped += encryptor.update(msg) + encryptor.finalize()
        next_mac_chain, mac = _calculate_mac(self._key_mac, self._mac_chain, wrapped)
        wrapped += mac
        raw_resp = self._hsm._transceive(wrapped)

        data = _unpad_resp(raw_resp, COMMAND.SESSION_MESSAGE)

        if data[0] != self._sid:
            raise YubiHsmInvalidResponseError("Incorrect SID")

        rmac = _calculate_mac(self._key_rmac, next_mac_chain, raw_resp[:-8])[1]
        if not constant_time.bytes_eq(raw_resp[-8:], rmac):
            raise YubiHsmInvalidResponseError("Incorrect MAC")

        self._ctr += 1
        self._mac_chain = next_mac_chain

        decryptor = cipher.decryptor()
        return decryptor.update(data[1:-8]) + decryptor.finalize()

    @property
    def sid(self) -> Optional[int]:
        """Session ID

        :return: The ID of the session.
        """
        return self._sid

    def send_secure_cmd(self, cmd: COMMAND, data: bytes = b"") -> bytes:
        """Send a command over the encrypted session.

        :param cmd: The command to send.
        :param data: The command payload to send.
        :return: The decrypted response data from the YubiHSM.
        """
        msg = struct.pack("!BH", cmd, len(data)) + data
        return _unpad_resp(self._secure_transceive(msg), cmd)

    def list_objects(
        self,
        object_id: Optional[int] = None,
        object_type: Optional[OBJECT] = None,
        domains: Optional[int] = None,
        capabilities: Optional[int] = None,
        algorithm: Optional[ALGORITHM] = None,
        label: Optional[str] = None,
    ) -> Sequence[YhsmObject]:
        """List objects from the YubiHSM.

        This returns a list of all objects currently stored on the YubiHSM,
        which are accessible by this session. The arguments to this method can
        be used to filter the results returned.

        :param object_id: Return only objects with this ID.
        :param object_type: Return only objects of this type.
        :param domains: Return only objects belonging to one or more of these domains.
        :param capabilities: Return only objects with one or more of these capabilities.
        :param algorithm: Return only objects with this algorithm.
        :param label: Return only objects with this label.
        :return: A list of matched objects.
        """
        msg = b""
        if object_id is not None:
            msg += struct.pack("!BH", LIST_FILTER.ID, object_id)
        if object_type is not None:
            msg += struct.pack("!BB", LIST_FILTER.TYPE, object_type)
        if domains is not None:
            msg += struct.pack("!BH", LIST_FILTER.DOMAINS, domains)
        if capabilities is not None:
            msg += struct.pack("!BQ", LIST_FILTER.CAPABILITIES, capabilities)
        if algorithm is not None:
            msg += struct.pack("!BB", LIST_FILTER.ALGORITHM, algorithm)
        if label is not None:
            msg += struct.pack(
                "!B%ds" % LABEL_LENGTH, LIST_FILTER.LABEL, _label_pack(label)
            )

        resp = self.send_secure_cmd(COMMAND.LIST_OBJECTS, msg)

        objects = []
        for i in range(0, len(resp), 4):
            obj_id, typ, seq = struct.unpack("!HBB", resp[i : i + 4])
            objects.append(YhsmObject._create(typ, self, obj_id, seq))
        return objects

    def get_object(self, object_id: int, object_type: OBJECT) -> YhsmObject:
        """Get a reference to a YhsmObject with the given id and type.

        The object returned will be a subclass of YhsmObject corresponding to
        the given object_type.

        :param object_id: The ID of the object to retrieve.
        :param object_type: The type of the object to retrieve.
        :return: An object reference.
        """
        return YhsmObject._create(object_type, self, object_id)

    def get_pseudo_random(self, length: int) -> bytes:
        """Get bytes from YubiHSM PRNG.

        :param length: The number of bytes to return.
        :return: The requested number of random bytes.
        """
        msg = struct.pack("!H", length)
        return self.send_secure_cmd(COMMAND.GET_PSEUDO_RANDOM, msg)

    def reset_device(self) -> None:
        """Perform a factory reset of the YubiHSM.

        Resets and reboots the YubiHSM, deletes all Objects and restores the
        default Authkey.
        """
        try:
            if self.send_secure_cmd(COMMAND.RESET_DEVICE) != b"":
                raise YubiHsmInvalidResponseError("Non-empty response")
        except YubiHsmConnectionError:
            pass  # Assume reset went well, it may interrupt the connection.
        self._sid = None
        self._key_enc = self._key_mac = self._key_rmac = b""
        self._hsm.close()

    def get_log_entries(self, previous_entry: Optional[LogEntry] = None) -> LogData:
        """Get logs from the YubiHSM.

        This returns a tuple of the number of unlogged boot events, the number
        of unlogged authentication events, and the log entries from the YubiHSM.
        The chain of entry digests will be validated, starting from the first
        entry returned, or the one supplied as previous_entry.

        :param previous_entry: Entry to start verification against.
        :return: A tuple consisting of the number of unlogged boot and authentication
            events, and the list of log entries.
        """
        resp = self.send_secure_cmd(COMMAND.GET_LOG_ENTRIES)
        boot, auth, num = struct.unpack("!HHB", resp[:5])

        data = resp[5:]
        if len(data) != num * LogEntry.LENGTH:
            raise YubiHsmInvalidResponseError("Incorrect length")

        logs = []
        for i in range(0, len(data), LogEntry.LENGTH):
            entry = LogEntry.parse(data[i : i + LogEntry.LENGTH])
            if previous_entry:
                if not entry.validate(previous_entry):
                    raise YubiHsmInvalidResponseError("Incorrect log digest")
            logs.append(entry)
            previous_entry = entry

        return LogData(boot, auth, logs)

    def set_log_index(self, index: int) -> None:
        """Clear logs to free up space for use with forced audit.

        :param index: The log entry index to clear up to (inclusive).
        """
        msg = struct.pack("!H", index)
        if self.send_secure_cmd(COMMAND.SET_LOG_INDEX, msg) != b"":
            raise YubiHsmInvalidResponseError("Non-empty response")

    def put_option(self, option: OPTION, value: bytes) -> None:
        """Set the raw value of a YubiHSM device option.

        :param option: The OPTION to set.
        :param value: The value to set the OPTION to.
        """
        msg = struct.pack("!BH", option, len(value)) + value
        if self.send_secure_cmd(COMMAND.SET_OPTION, msg) != b"":
            raise YubiHsmInvalidResponseError("Non-empty response")

    def get_option(self, option: OPTION) -> bytes:
        """Get the raw value of a YubiHSM device option.

        :param option: The OPTION to get.
        :return: The currently set value for the given OPTION
        """
        msg = struct.pack("!B", option)
        return self.send_secure_cmd(COMMAND.GET_OPTION, msg)

    def set_force_audit(self, audit: AUDIT) -> None:
        """Set the FORCE_AUDIT mode of the YubiHSM.

        :param audit: The AUDIT mode to set.
        """
        self.put_option(OPTION.FORCE_AUDIT, struct.pack("B", audit))

    def get_force_audit(self) -> AUDIT:
        """Get the current setting for forced audit mode.

        :return: The AUDIT setting for FORCE_AUDIT.
        """
        return AUDIT(self.get_option(OPTION.FORCE_AUDIT)[0])

    def set_command_audit(self, commands: Mapping[COMMAND, AUDIT]) -> None:
        """Set audit mode of commands.

        Takes a dict of COMMAND -> AUDIT pairs and updates the audit settings
        for the commands given.

        :param commands: Settings to update.

        :Example:

        >>> session.set_comment_audit({
        ...     COMMAND.ECHO: AUDIT.OFF,
        ...     COMMAND.LIST_OBJECTS: AUDIT.ON
        ... })
        """
        msg = b"".join(struct.pack("!BB", k, v) for (k, v) in commands.items())
        self.put_option(OPTION.COMMAND_AUDIT, msg)

    def get_command_audit(self) -> Mapping[COMMAND, AUDIT]:
        """Get a mapping of all available commands and their audit settings.

        :return: Dictionary of COMMAND -> AUDIT pairs.
        """
        resp = self.get_option(OPTION.COMMAND_AUDIT)
        ret = {}
        for i in range(0, len(resp), 2):
            cmd = resp[i]
            val = AUDIT(resp[i + 1])
            try:
                ret[COMMAND(cmd)] = val
            except ValueError:
                ret[_UnknownCommand(cmd)] = val  # type: ignore
        return ret

    def set_enabled_algorithms(self, algorithms: Mapping[ALGORITHM, bool]) -> None:
        """Set audit mode of commands.

        New in YubiHSM 2.2.0.

        Algorithms can only be toggled on a "fresh" device (after reset, before adding
        objects).

        Takes a dict of ALGORITHM -> bool pairs and updates the enabled algorithm
        settings for the algorithms given.

        :param algorithms: The algorithms to update.

        :Example:

        >>> session.set_enabled_algorithms({
        ...     ALGORITHM.RSA_2048: False,
        ...     ALGORITHM.RSA_OAEP_SHA256_: True,
        ... })
        """
        msg = b"".join(struct.pack("!BB", k, v) for (k, v) in algorithms.items())
        self.put_option(OPTION.ALGORITHM_TOGGLE, msg)

    def get_enabled_algorithms(self) -> Mapping[ALGORITHM, bool]:
        """Get the algorithms available, and whether or not they are enabled.

        :return: A mapping of algorithms, to whether or not they are enabled.
        """
        try:
            resp = self.get_option(OPTION.ALGORITHM_TOGGLE)
            ret = {}
            for i in range(0, len(resp), 2):
                alg = resp[i]
                val = bool(resp[i + 1])
                try:
                    ret[ALGORITHM(alg)] = val
                except ValueError:
                    ret[_UnknownAlgorithm(alg)] = val  # type: ignore
            return ret
        except YubiHsmDeviceError as e:
            if e.code == ERROR.INVALID_DATA:
                supported = self._hsm.get_device_info().supported_algorithms
                return {alg: True for alg in supported}
            raise

    def set_fips_mode(self, mode: bool) -> None:
        """Set the FIPS mode of the YubiHSM.

        YubiHSM2 FIPS only.

        This can only be toggled on a "fresh" device (after reset, before adding
        objects).

        :param mode: Whether to be in FIPS compliant mode or not.
        """
        self.put_option(OPTION.FIPS_MODE, struct.pack("!B", mode))

    def get_fips_status(self) -> FIPS_STATUS:
        """Get the current FIPS status.

        YubiHSM2 FIPS only.

        :return: The FipsStatus value.
        """
        return FIPS_STATUS(self.get_option(OPTION.FIPS_MODE)[0])

    def get_fips_mode(self) -> bool:
        """Get the current setting for FIPS mode.

        YubiHSM2 FIPS only.

        :return: True if in FIPS mode, False if not.
        """
        warnings.warn("Deprecated, use get_fips_status instead", DeprecationWarning)
        return bool(self.get_option(OPTION.FIPS_MODE)[0])

    def __repr__(self):
        return "{0.__class__.__name__}(id={0._sid}, hsm={0._hsm})".format(self)

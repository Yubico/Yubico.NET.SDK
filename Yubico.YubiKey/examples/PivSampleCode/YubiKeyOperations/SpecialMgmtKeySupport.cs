// Copyright 2021 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class contains methods dealing with the PIN and Management Key.
    internal static class PinAndKeyOperations
    {
        // Obtain the PIN and verify it, then return that PIN in a byte array.
        // If the user cancels (the KeyCollector returns false), this method will
        // return an empty array.
        public static bool VerifyAndReturnPin(PivSession pivSession, Func<KeyEntryData, bool> SaveKeyCollector, out byte[] pin)
        {
            pin = Array.Empty<byte>();
            if (SaveKeyCollector is null)
            {
                throw new InvalidOperationException("No KeyCollector");
            }

            // Use this PIN collector so we can capture the PIN and still call
            // TryVerifyPin. We want to call TryVerifyPin (rather than call the
            // command) so that the pivSession.PinVerified boolean will be set if
            // the PIN verifies.
            using var specialPinCollector = new VerifyPinCollector();
            pivSession.KeyCollector = specialPinCollector.KeyCollector;

            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.VerifyPivPin,
            };

            try
            {
                while (SaveKeyCollector(keyEntryData) == true)
                {
                    specialPinCollector.SetPin(keyEntryData.GetCurrentValue());

                    if (pivSession.TryVerifyPin())
                    {
                        ReadOnlyMemory<byte> pinData = keyEntryData.GetCurrentValue();
                        pin = pinData.ToArray();
                        return true;
                    }

                    keyEntryData.IsRetry = true;
                }
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = SaveKeyCollector(keyEntryData);
            }

            // If the user cancels, return false.
            return false;
        }

        // Obtain the mgmt key and authenticate it, then return that mgmt key in
        // the given byte array.
        // If the user cancels (the KeyCollector returns false), this method will
        // return false.
        // The input mgmtKey is an existing buffer 24 bytes long.
        public static bool AuthenticateAndReturnMgmtKey(
            PivSession pivSession,
            Func<KeyEntryData, bool> SaveKeyCollector,
            AuthenticateMgmtKeyCollector specialKeyCollector,
            Memory<byte> mgmtKey)
        {
            if (SaveKeyCollector is null)
            {
                throw new InvalidOperationException("No KeyCollector");
            }

            // Use this mgmt key collector so we can capture the mgmt key and
            // still call TryAuthenticateManagementKey. We want to call
            // TryAuthMgmtKey (rather than call the commands) so that the
            // pivSession.ManagementKeyAuthenticated boolean will be set if
            // the mgmt key authenticates.
            pivSession.KeyCollector = specialKeyCollector.KeyCollector;
            var keyEntryData = new KeyEntryData()
            {
                Request = KeyEntryRequest.AuthenticatePivManagementKey,
            };

            try
            {
                while (SaveKeyCollector(keyEntryData) == true)
                {
                    specialKeyCollector.SetMgmtKey(keyEntryData.GetCurrentValue(), false);
                    if (pivSession.TryAuthenticateManagementKey())
                    {
                        ReadOnlyMemory<byte> keyData = keyEntryData.GetCurrentValue();
                        keyData.CopyTo(mgmtKey);
                        return true;
                    }

                    keyEntryData.IsRetry = true;
                }

                // If we reach this code the user cancelled.
                return false;
            }
            finally
            {
                keyEntryData.Clear();

                keyEntryData.Request = KeyEntryRequest.Release;
                _ = SaveKeyCollector(keyEntryData);
            }
        }
    }

    // This class can get the PRINTED data from the YubiKey and decode it. After
    // decoding, it will try to authenticate.
    // After instantiating, look at the properties to see what the data is.
    // We're looking for an encoded mgmt key as
    //   53 1C
    //      88 1A
    //         89 18
    //            <24 bytes>
    // The class can then store a management key in PRINTED. It can store either
    // a supplied key or a random key.
    internal sealed class PivPrinted : IDisposable
    {
        private bool _disposed;

        private readonly byte[] _mgmtKey = new byte[SpecialMgmtKey.MgmtKeyLength];

        // If there is data in the PRINTED storage area, this will be true.
        // Otherwise it will be false.
        public bool IsSet { get; private set; }

        // If there is data (IsSet is true), and the data is encoded as expected,
        // this will be true. Otherwise it will be false.
        public bool IsEncoded { get; private set; }

        // If there is properly encoded data, and it authenticates, this will be
        // true. Otherwise it will be false.
        public bool IsAuthenticated { get; private set; }

        // If there is data (IsSet is true) and the data is encoded (IsEncoded is
        // true), this will contain the management key data for the element with the
        // tag 89. This will always be a buffer 24 bytes long. If IsSet is true
        // and IsEncoded is True, then those bytes will be the mgmt key.
        // Otherwise they will be meaningless.
        public Memory<byte> MgmtKey { get; private set; }

        // Build a new instance. Use the pivSession to get the PRINTED data out of
        // the YubiKey.
        // This constructor will not copy a reference to the specialKeyCollector,
        // it will only use it during this function.
        // This assumes the PIN has already been verified.
        public PivPrinted(PivSession pivSession, AuthenticateMgmtKeyCollector specialKeyCollector)
        {
            _disposed = false;
            IsSet = false;
            IsEncoded = false;
            IsAuthenticated = false;
            MgmtKey = new Memory<byte>(_mgmtKey);

            var getDataCmd = new GetDataCommand(PivDataTag.Printed);
            GetDataResponse getDataRsp = pivSession.Connection.SendCommand(getDataCmd);

            // If there's no data, do nothing else.
            // If there is (Success), get the data.
            // If there's some other problem (response is not Success or NoData),
            // throw an exception (the call to GetData will throw an exception if
            // the Status is not Success).
            if (getDataRsp.Status != ResponseStatus.NoData)
            {
                IsSet = true;
                IsEncoded = TryDecodeMgmtKey(getDataRsp.GetData());
                if (IsEncoded)
                {
                    _ = TryAuthenticate(pivSession, specialKeyCollector);
                }
            }
        }

        private bool TryDecodeMgmtKey(ReadOnlyMemory<byte> encodedMgmtKey)
        {
            var tlvReader = new TlvReader(encodedMgmtKey);
            if (tlvReader.TryReadNestedTlv(out tlvReader, 0x53))
            {
                if (tlvReader.TryReadNestedTlv(out tlvReader, 0x88))
                {
                    if (tlvReader.TryReadValue(out ReadOnlyMemory<byte> mgmtKey, 0x89))
                    {
                        if (mgmtKey.Length == SpecialMgmtKey.MgmtKeyLength)
                        {
                            mgmtKey.CopyTo(MgmtKey);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // Set the management key. This will store the management key in the
        // PRINTED storage area, but it might also change the management key.
        //
        // If the newMgmtKey is not null, this method will simply store that
        // value in the PRINTED storage area. It will not authenticate. That is,
        // this method assumes any new key has already been authenticated.
        // This generally happens when a key was PIN-Derived and you want that
        // key stored as the PIN-Protected one as well.
        // The length of the new key must be 24 or the method will throw an
        // exception. This generally happens when a key was PIN-Derived and you
        //
        // If the input arg newMgmtKey is null, and there is a management key
        // already in this object, and it is authenticated (IsAuthenticated is
        // true), this  method will change it (see
        // PivSession.ChangeManagementKey).
        //
        // If the input arg newMgmtKey is null and there is not an authenticated
        // key, this method will call the original KeyCollector
        // (SaveKeyCollector) to get the current mgmt key.
        // If the current is the default, the method will generate a new, random
        // key. If it does not generate a new key, the method will simply store
        // the retrieved key in PRINTED. If it generates a new key, it will
        // change it (see PivSession.ChangeManagementKey).
        //
        // If there is a management key already in this object, and it is not
        // authenticated, this method will replace it (either with the new key
        // supplied or a random key if none is given). This means that someone
        // somewhere had stored data in PRINTED, and you will be overwriting it.
        // This will generally happen if someone sets PIN-Protected, then you are
        // now setting PIN-Derived and want to update the key in PRINTED to the
        // derived key.
        //
        // Finally, this method will store the management key in the PRINTED
        // storage area.
        public bool TryUpdateMgmtKey(
            PivSession pivSession,
            Func<KeyEntryData, bool> SaveKeyCollector,
            AuthenticateMgmtKeyCollector specialKeyCollector,
            ReadOnlyMemory<byte> newMgmtKey)
        {
            if (!newMgmtKey.IsEmpty)
            {
                if (newMgmtKey.Length != SpecialMgmtKey.MgmtKeyLength)
                {
                    throw new ArgumentException("The management key must be 24 bytes.");
                }

                newMgmtKey.CopyTo(MgmtKey);
                IsSet = true;
                IsEncoded = true;
                IsAuthenticated = true;

                return TryStorePrinted(pivSession);
            }

            bool changeKey = true;
            if (IsAuthenticated)
            {
                specialKeyCollector.SetMgmtKey(MgmtKey, false);
            }
            else
            {
                if (!PinAndKeyOperations.AuthenticateAndReturnMgmtKey(pivSession, SaveKeyCollector, specialKeyCollector, MgmtKey))
                {
                    return false;
                }
                specialKeyCollector.SetMgmtKey(MgmtKey, false);
                changeKey = specialKeyCollector.IsCurrentMgmtKeyDefault();
            }

            if (changeKey)
            {
                using (RandomNumberGenerator rng = CryptographyProviders.RngCreator())
                {
                    rng.GetBytes(_mgmtKey, 0, SpecialMgmtKey.MgmtKeyLength);
                }
                specialKeyCollector.SetMgmtKey(MgmtKey, true);

                pivSession.KeyCollector = specialKeyCollector.KeyCollector;
                pivSession.ChangeManagementKey(PivTouchPolicy.Never);
            }

            return TryStorePrinted(pivSession);
        }

        private bool TryStorePrinted(PivSession pivSession)
        {
            var encodedMgmtKey = new Memory<byte>(new byte[] {
                0x53, 0x1C, 0x88, 0x1A, 0x89, 0x18,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            try
            {
                MgmtKey.CopyTo(encodedMgmtKey[(SpecialMgmtKey.EncodedMgmtKeyLength - SpecialMgmtKey.MgmtKeyLength)..]);
                var putCommand = new SimplePutDataCommand(0x005FC109, encodedMgmtKey);
                PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);

                return putResponse.Status == ResponseStatus.Success;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encodedMgmtKey.Span);
            }
        }

        // Try to authenticate the MgmtKey.
        // If it is already authenticated (IsAuthenticated is true), this will do
        // nothing and return true.
        // If there is no key (IsEncoded is false), this will do nothing and
        // return false.
        // If it authenticates, it will set the IsAuthenticated property to true.
        public bool TryAuthenticate(PivSession pivSession, AuthenticateMgmtKeyCollector specialKeyCollector)
        {
            if (IsAuthenticated)
            {
                return true;
            }
            if (!IsEncoded)
            {
                return false;
            }

            specialKeyCollector.SetMgmtKey(MgmtKey, false);
            pivSession.KeyCollector = specialKeyCollector.KeyCollector;

            IsAuthenticated = pivSession.TryAuthenticateManagementKey();
            return IsAuthenticated;
        }

        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(_mgmtKey);
            _disposed = true;
        }
    }

    // This class can get the ADMIN DATA from the YubiKey and decode it. That is,
    // after instantiating, look at the properties to see what the data is.
    // It can then build a new encoding and store it.
    // We're looking for ADMIN DATA that is encoded as
    //   80 L
    //      81 01 (optional)
    //         bit field (01 PUK blocked, 02 PIN-Protected)
    //      82 10 (optional)
    //         salt
    //      83 04 (optional)
    //         4-byte time
    internal sealed class PivAdminData : IDisposable
    {
        private bool _disposed;
        private bool _newSalt;
        private byte _oldBitField;

        // If there is data in the ADMIN DATA storage area, this will be true.
        // Otherwise it will be false.
        public bool IsSet { get; private set; }

        // If there is data (IsSet is true), and the data is encoded as expected,
        // this will be true. Otherwise it will be false.
        public bool IsEncoded { get; private set; }

        // If there is data and it is encoded, call TryDerive. That will either
        // be able to derive a key or not (e.g. no salt means no derive). If the
        // MgmtKey buffer contains a derived key, this will be set to true.
        public bool IsDerived { get; private set; }

        // If a key was derived and if it authenticates, this will be true.
        public bool IsAuthenticated { get; private set; }

        // If there is data (IsSet is true) and the data is encoded (IsEncoded is
        // true), this will be the bit field for the element with the tag 81.
        // 0x01 is PUK blocked and 0x02 is PIN-Protected.
        public byte BitField { get; private set; }

        // If there is data (IsSet is true) and the data is encoded (IsEncoded is
        // true), this will be the salt for the element with the tag 82. It can
        // be null.
        public byte[] Salt { get; private set; }

        // If there is data (IsSet is true) and the data is encoded (IsEncoded is
        // true), this will be the time value for the element with the tag 82. If
        // the date is Jan. 1, 1970, it is not set.
        public DateTime PinLastUpdated { get; private set; }

        // If a key was derived, this will contain that data. Otherwise it is
        // meaningless.
        public Memory<byte> MgmtKey { get; private set; }

        // Build a new instance. Use the pivSession to get the ADMIN DATA out of
        // the YubiKey and set the properties. If the data can be used to derive
        // a management key, derive it and try to authenticate. If it
        // authenticates, set IsAuthenticated to true. If a key cannot be
        // derived, or if the derived key does not authenticate, just leave
        // IsAuthenticated false.
        public PivAdminData(PivSession pivSession, AuthenticateMgmtKeyCollector specialKeyCollector, byte[] pin)
            : this(pivSession)
        {
            _ = TryAuthenticate(pivSession, specialKeyCollector, pin);
        }

        // Build a new instance. Use the pivSession to get the ADMIN DATA out of
        // the YubiKey and set the properties.
        // This constructor will only get the data out of storage and decode it
        // if possible. It will not try to authenticate.
        public PivAdminData(PivSession pivSession)
        {
            _newSalt = false;
            _oldBitField = 0;
            IsSet = false;
            IsEncoded = false;
            IsDerived = false;
            IsAuthenticated = false;
            BitField = 0;
            Salt = null;
            PinLastUpdated = new DateTime(1970, 1, 1);

            MgmtKey = new Memory<byte>(new byte[SpecialMgmtKey.MgmtKeyLength]);

            var simpleGet = new SimpleGetDataCommand(0x005FFF00);
            GetDataResponse getDataRsp = pivSession.Connection.SendCommand(simpleGet);

            // If there's no data, do nothing else.
            // If there is (Success), get it.
            // If there's some other problem (response is not Success or NoData),
            // throw an exception.
            if (getDataRsp.Status != ResponseStatus.NoData)
            {
                IsSet = true;
                IsEncoded = TryDecodeAdminData(getDataRsp.GetData());
            }
        }

        // If there is a salt, this will derive the key from the pin and salt.
        // If there is no salt, it will return false.
        private bool TryDeriveMgmtKey(byte[] pin)
        {
            if (pin is null)
            {
                throw new ArgumentNullException(nameof(pin));
            }
            if ((pin.Length < 6) || (pin.Length > 8))
            {
                throw new ArgumentException("The PIN must be 6 to bytes long.");
            }

            if (Salt is null)
            {
                return false;
            }

            byte[] derivedKey = Array.Empty<byte>();
            try
            {
                // This will use PBKDF2, with the PRF of HMAC with SHA-1.
#pragma warning disable CA5379, CA5387 // These warnings complain about SHA-1 and <100,000 iterations, but we use it to be backwards-compatible.
                using var kdf = new Rfc2898DeriveBytes(pin, Salt, 10000);
                derivedKey = kdf.GetBytes(SpecialMgmtKey.MgmtKeyLength);
#pragma warning restore CA5379, CA5387
                var keyMemory = new ReadOnlyMemory<byte>(derivedKey);
                keyMemory.CopyTo(MgmtKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(derivedKey);
            }

            IsDerived = true;
            return true;
        }

        // Try to authenticate the MgmtKey.
        // If it is already authenticated (IsAuthenticated is true), this will do
        // nothing and return true.
        // If there is no salt (IsSet, IsEncoded, is false or Salt is null), this
        // will do nothing and return false.
        public bool TryAuthenticate(PivSession pivSession, AuthenticateMgmtKeyCollector specialKeyCollector, byte[] pin)
        {
            if (IsAuthenticated)
            {
                return true;
            }
            if (!TryDeriveMgmtKey(pin))
            {
                return false;
            }

            specialKeyCollector.SetMgmtKey(MgmtKey, false);
            pivSession.KeyCollector = specialKeyCollector.KeyCollector;

            IsAuthenticated = pivSession.TryAuthenticateManagementKey();

            return IsAuthenticated;
        }

        // Set the management key. This will make sure a mgmt key is derived, and
        // if so, change from the current one to the derived one.
        //
        // It is possible to pass in the current management key. If so, this
        // method assumes it has been authenticated.
        //
        // If the currentMgmtKey arg is Empty, then this method will call on the
        // SaveKeyCollector to get the current key.
        public bool TryUpdateMgmtKey(
            PivSession pivSession,
            byte[] pin,
            Func<KeyEntryData, bool> SaveKeyCollector,
            AuthenticateMgmtKeyCollector specialKeyCollector,
            Memory<byte> currentMgmtKey)
        {
            if (!currentMgmtKey.IsEmpty)
            {
                specialKeyCollector.SetMgmtKey(currentMgmtKey, false);
            }
            else
            {
                if (!PinAndKeyOperations.AuthenticateAndReturnMgmtKey(pivSession, SaveKeyCollector, specialKeyCollector, MgmtKey))
                {
                    return false;
                }
                specialKeyCollector.SetMgmtKey(MgmtKey, false);
            }

            // Build a new key.
            byte[] salt = new byte[16];
            using (RandomNumberGenerator rng = CryptographyProviders.RngCreator())
            {
                rng.GetBytes(salt, 0, salt.Length);
            }
            UpdateSalt(salt);
            if (!TryDeriveMgmtKey(pin))
            {
                return false;
            }

            specialKeyCollector.SetMgmtKey(MgmtKey, true);

            pivSession.KeyCollector = specialKeyCollector.KeyCollector;
            pivSession.ChangeManagementKey(PivTouchPolicy.Never);

            IsAuthenticated = true;

            BlockPuk(pivSession);

            return TryStoreAdminData(pivSession);
        }

        // Try to decode the admin data. If it works, set the properties. If not,
        // return false.
        // If there is data, there must be
        //   53 len
        //      80 len
        // If so, then there can be 0, 1, 2, or 3 sub elements, 81, 82, 83 (they
        // are all optional).
        private bool TryDecodeAdminData(ReadOnlyMemory<byte> adminData)
        {
            // threeTags is a bit field.
            // If we find 81, set the 1 bit
            // If we find 82, set the 2 bit
            // If we find 83, set the 4 bit
            // This is how we'll know a tag is used only once.
            int threeTags = 0;
            var tlvReader = new TlvReader(adminData);
            if (!tlvReader.TryReadNestedTlv(out tlvReader, 0x53))
            {
                return false;
            }

            if (!tlvReader.TryReadNestedTlv(out tlvReader, 0x80))
            {
                return false;
            }

            // Read the next three tags.
            // Each is optional, so if something is not there, that's fine.
            while (tlvReader.HasData)
            {
                int nextTag = tlvReader.PeekTag();
                if (!TryDecodeTag(tlvReader, nextTag, ref threeTags))
                {
                    return false;
                }
            }

            // If we reach this point, the encoding was as expected.
            return true;
        }

        // Try to decode the nextTag.
        // We've peeked, nextTag is indeed the next tag.
        // Now, is it something we support? If not return false.
        // Is it already decoded (see threeTags)? If so, return false.
        // Is it encoded correctly? If so, set the appropriate property, if not,
        // return false.
        private bool TryDecodeTag(TlvReader tlvReader, int nextTag, ref int threeTags)
        {
            switch (nextTag)
            {
                default:
                    return false;

                case 0x81:
                    if ((threeTags & 1) != 0)
                    {
                        return false;
                    }

                    if (!tlvReader.TryReadByte(out _oldBitField, nextTag))
                    {
                        return false;
                    }

                    BitField = _oldBitField;
                    threeTags |= 1;
                    break;

                case 0x82:
                    if ((threeTags & 2) != 0)
                    {
                        return false;
                    }

                    if (!tlvReader.TryReadValue(out ReadOnlyMemory<byte> salt, nextTag))
                    {
                        return false;
                    }

                    if (salt.Length != 16)
                    {
                        return false;
                    }

                    Salt = salt.ToArray();
                    threeTags |= 2;
                    break;

                case 0x83:
                    if ((threeTags & 4) != 0)
                    {
                        return false;
                    }

                    if (!tlvReader.TryReadInt32(out int theTime, nextTag, false))
                    {
                        return false;
                    }

                    PinLastUpdated = DateTimeOffset.FromUnixTimeSeconds(theTime).UtcDateTime;
                    threeTags |= 4;
                    break;
            }

            return true;
        }

        // Update the BitField to either set (isPinProtected is true) or not set
        // (false) the PIN-Protected bit.
        // The PUK blocked bit will be set when the PUK is blocked.
        public void UpdateBitField(bool isPinProtected)
        {
            BitField &= 1;
            if (isPinProtected)
            {
                BitField |= 2;
            }
        }

        // Update the Salt to the given data. If there is already a salt, this
        // will replace it. If there is none, this will create a new one and set
        // it with the given data.
        // Note that this will update the salt in this object only. If you want
        // to update the salt (and PIN) in order to set or change the PIN-Derived
        // management key, call the appropriate method in the
        // PivPinDerivedMgmtKey object.
        public void UpdateSalt(byte[] salt)
        {
            if (salt.Length != 16)
            {
                throw new ArgumentException("The ADMIN DATA salt must be 16 bytes.");
            }

            if (Salt is null)
            {
                Salt = new byte[16];
                _newSalt = true;
            }
            else
            {
                if (!salt.SequenceEqual(Salt))
                {
                    _newSalt = true;
                }
            }

            Array.Copy(salt, Salt, 16);
        }

        // Create the encoding of ADMIN DATA, based on the contents of the
        // properties. Then store that data on the YubiKey.
        // If the contents have not been changed, (no call to an Update method,
        // or calls but the data is the same), this will do nothing.
        // If the salt is set, this will block the PUK and make sure the PUB
        // blocked bit is set.
        // If the salt is set and the PinLastUpdated year is 1970, this will
        // update the date with the current time.
        public bool TryStoreAdminData(PivSession pivSession)
        {
            BitField &= 2;
            if (IsAuthenticated)
            {
                BitField |= 1;
            }

            if ((_oldBitField == BitField) && (!_newSalt))
            {
                return true;
            }

            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(0x53))
            {
                using (tlvWriter.WriteNestedTlv(0x80))
                {
                    tlvWriter.WriteByte(0x81, BitField);
                    if (!(Salt is null))
                    {
                        tlvWriter.WriteValue(0x82, Salt);
                        if ((PinLastUpdated.Year == 1970) || _newSalt)
                        {
                            PinLastUpdated = DateTime.UtcNow;
                        }
                        long unixTimeSeconds = new DateTimeOffset(PinLastUpdated).ToUnixTimeSeconds();
                        tlvWriter.WriteInt32(0x83, (int)unixTimeSeconds, false);
                    }
                }
            }

            int encodedLength = tlvWriter.GetEncodedLength();
            var encoding = new Memory<byte>(new byte[encodedLength]);
            if (!tlvWriter.TryEncode(encoding.Span, out encodedLength))
            {
                return false;
            }

            var putCommand = new SimplePutDataCommand(0x005FFF00, encoding.Slice(0, encodedLength));
            PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);

            if (putResponse.Status == ResponseStatus.Success)
            {
                IsSet = true;
                IsEncoded = true;
                return true;
            }

            return false;
        }

        // To block the PUK, use the incorrect value until running out of retries.
        // To get the PUK into a blocked state, try to change it. Each time the
        // current PUK entered is incorrect, the retries remaining count is
        // decremented. When it hits zero, it is blocked.
        // Call the ChangeReferenceDataCommand with arbitrary current and a new
        // PUK value. They must be different. If the arbitrary current value
        // happens to be correct, the first call to change the PUK will work
        // and it will become the new PUK. For the next call, use the same
        // current value, which is now the wrong current value.
        private static void BlockPuk(PivSession pivSession)
        {
            byte[] currentValue = new byte[] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
            };
            byte[] newValue = new byte[] {
                0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22
            };

            int retriesRemaining;
            do
            {
                var changeCommand = new ChangeReferenceDataCommand(PivSlot.Puk, currentValue, newValue);
                ChangeReferenceDataResponse changeResponse = pivSession.Connection.SendCommand(changeCommand);

                if (changeResponse.Status == ResponseStatus.Failed)
                {
                    return;
                }

                retriesRemaining = changeResponse.GetData() ?? 1;

            } while (retriesRemaining > 0);
        }

        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(MgmtKey.Span);
            _disposed = true;
        }
    }

    // This class can get the CHUID from the YubiKey and decode it. That is,
    // after instantiating, look at the properties to see what the data is.
    // It can also build a new encoding and store it, if there is nothing in the
    // CHUID storage area.
    // We're looking for a CHUID that is encoded as
    //   53 3B
    //      30 19
    //             d4 e7 39 da 73 9c ed 39 ce 73 9d 83 68 58 21 08
    //             42 10 84 21 c8 42 10 c3 eb
    //      34 10
    //         <16 random bytes>
    //      35 08
    //         32 30 33 30 30 31 30 31 3e 00 fe 00
    // All the bytes are fixed, except for the random 16.
    internal sealed class PivChuid
    {
        // If there is data in the CHUID storage area, this will be true.
        // Otherwise it will be false.
        public bool IsSet { get; private set; }

        // If there is data (IsSet is true), and the data is encoded as expected,
        // this will be true. Otherwise it will be false.
        public bool IsEncoded { get; private set; }

        // Build an object by getting the data out of the CHUID storage area.
        public PivChuid(PivSession pivSession)
        {
            IsSet = false;
            IsEncoded = false;

            var getDataCmd = new GetDataCommand(PivDataTag.Chuid);
            GetDataResponse getDataRsp = pivSession.Connection.SendCommand(getDataCmd);

            if (getDataRsp.Status != ResponseStatus.NoData)
            {
                ReadOnlyMemory<byte> chuidData = getDataRsp.GetData();
                IsSet = true;
                IsEncoded = PivDataTag.Chuid.IsValidEncodingForPut(chuidData);
            }
        }

        // Store a CHUID if there is currently no CHUID.
        // If IsSet is true, this will do nothing.
        // If IsSet is false, this will build a new CHUID, store it, and set
        // IsSet and IsEncoded to true.
        // If this method cannot successfully store the data, then IsSet will
        // remain false.
        public bool TryStoreChuid(PivSession pivSession)
        {
            if (IsSet)
            {
                return true;
            }

            byte[] chuidData = new byte[] {
                0x53, 0x3B,
                0x30, 0x19,
                0xd4, 0xe7, 0x39, 0xda, 0x73, 0x9c, 0xed, 0x39,
                0xce, 0x73, 0x9d, 0x83, 0x68, 0x58, 0x21, 0x08,
                0x42, 0x10, 0x84, 0x21, 0xc8, 0x42, 0x10, 0xc3,
                0xeb,
                0x34, 0x10,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x35, 0x08,
                0x32, 0x30, 0x33, 0x30, 0x30, 0x31, 0x30, 0x31,
                0x3e, 0x00, 0xfe, 0x00
            };

            using (RandomNumberGenerator rng = CryptographyProviders.RngCreator())
            {
                rng.GetBytes(chuidData, 31, 16);
            }

            var putDataCmd = new PutDataCommand(PivDataTag.Chuid, chuidData);
            PutDataResponse putDataRsp = pivSession.Connection.SendCommand(putDataCmd);
            if (putDataRsp.Status == ResponseStatus.Success)
            {
                IsSet = true;
                IsEncoded = true;
            }

            return IsSet;
        }
    }

    // This class contains the PIN data to use when verifying, along with a
    // KeyCollector method.
    // The KeyCollector will be able to do only two operations, return the
    // PIN for verification, and Release.
    internal sealed class VerifyPinCollector : IDisposable
    {
        private bool _disposed;

        private int _pinLength;
        private readonly byte[] _pinData;
        private readonly Memory<byte> _pinMemory;

        public VerifyPinCollector()
        {
            _pinData = new byte[8];
            _pinMemory = new Memory<byte>(_pinData);
        }

        // Set the PIN data in this object to the input data.
        public void SetPin(ReadOnlyMemory<byte> pin)
        {
            pin.CopyTo(_pinMemory);
            _pinLength = pin.Length;
        }

        // This is the KeyCollector delegate.
        public bool KeyCollector(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                return false;
            }

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.VerifyPivPin:
                    keyEntryData.SubmitValue(_pinMemory.Slice(0, _pinLength).Span);
                    return true;
            }
        }

        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(_pinData);
            _disposed = true;
        }
    }

    // This class contains the management key data to use when authenticating the
    // management key, along with a KeyCollector method.
    // The KeyCollector will be able to do only three operations, return the
    // management key for authentication, return the current and new key for
    // changing the management key, and Release.
    internal sealed class AuthenticateMgmtKeyCollector : IDisposable
    {
        private const int MgmtKeyLength = 24;
        private readonly Memory<byte> _defaultKey;
        private readonly Memory<byte> _currentKey;
        private readonly Memory<byte> _newKey;

        private bool _disposed;

        public AuthenticateMgmtKeyCollector()
        {
            _defaultKey = new Memory<byte>(new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
            });
            _currentKey = new Memory<byte>(new byte[MgmtKeyLength]);
            _newKey = new Memory<byte>(new byte[MgmtKeyLength]);
        }

        // Set either the current or new mgmt key to be the last 24 bytes of the
        // input. This will copy the key data, not a reference.
        // If there is no encoded key (length of 0) or the length is < 24, set
        // the key to be the default management key.
        // If isNewKey is true, set _newKey.
        // If isNewKey is false, set _mgmtKey.
        public void SetMgmtKey(ReadOnlyMemory<byte> encodedMgmtKey, bool isNewKey)
        {
            int offset = encodedMgmtKey.Length - MgmtKeyLength;
            ReadOnlyMemory<byte> source = offset < 0 ? _defaultKey : encodedMgmtKey[offset..];

            Memory<byte>destination = isNewKey ? _newKey : _currentKey;

            source.CopyTo(destination);
        }

        // Return a reference to the current management key.
        public ReadOnlyMemory<byte> GetCurrentMgmtKey() => _currentKey;

        // Check to see if the given keyData is the default mgmt key.
        // If the input key data length is > 24, check the last 24 bytes. If it
        // is < 24, return false.
        public bool IsCurrentMgmtKeyDefault() => MemoryExtensions.SequenceEqual(_defaultKey.Span, _currentKey.Span);

        // This is the KeyCollector delegate.
        public bool KeyCollector(KeyEntryData keyEntryData)
        {
            if (keyEntryData is null)
            {
                return false;
            }

            if (keyEntryData.IsRetry == true)
            {
                return false;
            }

            switch (keyEntryData.Request)
            {
                default:
                    return false;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.AuthenticatePivManagementKey:
                    keyEntryData.SubmitValue(_currentKey.Span);
                    return true;

                case KeyEntryRequest.ChangePivManagementKey:
                    keyEntryData.SubmitValues(_currentKey.Span, _newKey.Span);
                    return true;
            }
        }

        // Note that .NET recommends a Dispose method call Dispose(true) and
        // GC.SuppressFinalize(this). The actual disposal is in the
        // Dispose(bool) method.
        //
        // However, that does not apply to sealed classes.
        // So the Dispose method will simply perform the
        // "closing" process, no call to Dispose(bool) or GC.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CryptographicOperations.ZeroMemory(_currentKey.Span);
            CryptographicOperations.ZeroMemory(_newKey.Span);
            _disposed = true;
        }
    }

    // The SDK's GetDataCommand will not work for ADMIN DATA. So use this class.
    internal sealed class SimpleGetDataCommand : IYubiKeyCommand<GetDataResponse>
    {
        private readonly byte[] _data;

        public SimpleGetDataCommand(int objectId)
        {
            _data = new byte[5];
            _data[0] = 0x5C;
            _data[1] = 0x03;
            _data[2] = (byte)(objectId >> 16);
            _data[3] = (byte)(objectId >>  8);
            _data[4] = (byte) objectId;
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Ins = 0xCB,
            P1 = 0x3F,
            P2 = 0xFF,
            Data = _data,
        };

        public GetDataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new GetDataResponse(responseApdu);

        public YubiKeyApplication Application => YubiKeyApplication.Piv;
    }

    // The SDK's PutDataCommand will not work for PRINTED and ADMIN DATA. So use
    // this class.
    // For PUT DATA, the APDU will expect the data to be of the form.
    //
    //   5C 03
    //      objectId (3 octets)
    //   formatted data
    //
    // This class will build the 5C 03 object Id, but it will not format the
    // data. That is, it will copy the data as is, so the caller must format it.
    // If the data you pass in contains sensitive data, call the Clear method
    // when done with it.
    internal sealed class SimplePutDataCommand : IYubiKeyCommand<PutDataResponse>
    {
        private readonly byte[] _dataToSend;

        public SimplePutDataCommand(int objectId, ReadOnlyMemory<byte> data)
        {
            _dataToSend = new byte[5 + data.Length];
            _dataToSend[0] = 0x5C;
            _dataToSend[1] = 0x03;
            _dataToSend[2] = (byte)(objectId >> 16);
            _dataToSend[3] = (byte)(objectId >>  8);
            _dataToSend[4] = (byte) objectId;

            var dataMemory = new Memory<byte>(_dataToSend, 5, data.Length);
            data.CopyTo(dataMemory);
        }

        public CommandApdu CreateCommandApdu() =>
            new CommandApdu()
        {
            Ins = 0xDB,
            P1 = 0x3F,
            P2 = 0xFF,
            Data = _dataToSend,
        };

        public PutDataResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new PutDataResponse(responseApdu);

        public YubiKeyApplication Application => YubiKeyApplication.Piv;

        public void Clear()
        {
            CryptographicOperations.ZeroMemory(_dataToSend);
        }
    }
}

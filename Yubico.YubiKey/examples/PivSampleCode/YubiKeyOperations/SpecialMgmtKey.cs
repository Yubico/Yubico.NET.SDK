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
using System.Security.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class contains methods to set the management key to special values:
    //
    //   PIN-Protected
    //   PIN-Derived
    //
    // A PIN-Protected management key is a random key (24 bytes, a management key
    // is a Triple-DES key) that has been stored in the PRINTED storage area. In
    // order to retrieve the management key, use the GET DATA command to get the
    // data in PRINTED. That command will not work unless the PIN is verified. In
    // this way, the mgmt key is protected by the PIN. Note that certain data
    // must be stored in the CHUID storage area as well.
    // This is what the Minidriver does. The Minidriver can be loaded onto a
    // Windows platform. Applications can use certain Windows operations and with
    // the Minidriver installed, have the operation use the YubiKey for certain
    // functions. But these Windows operations only have the capacity to collect
    // the PIN. Hence, in order to run those YubiKey functions that require the
    // management key, the Minidriver will generate a new, random mgmt key, and
    // store it in the PRINTED storage area. Later on, when the Minidriver needs
    // the mgmt key, it will verify the PIN, collect the mgmt key and
    // authenticate. In this way, management key authentication can be
    // "automated".
    // Note that the mgmt key must be verified before storing something in the
    // PRINTED storage area, so it is necessary to authenticate the mgmt key at
    // least once before implementing this shortcut.
    //
    // A PIN-derived management key is 24 bytes derived from the PIN and a random
    // salt. The salt is stored in the ADMIN DATA area (which does not require
    // the PIN to retrieve). When the management key is
    // needed, get the salt, collect the PIN, then perform the key derivation
    // operation.
    // This is what earlier versions of the PIV tool and YubiKey Manager did.
    // Note that the mgmt key must be verified before storing something in the
    // ADMIN DATA storage area, so it is necessary to authenticate the mgmt key
    // at least once before implementing this shortcut.
    //
    // It is possible to set the YubiKey to be either PIN-Protected, PIN-Derived,
    // or both. But it is also necessary to set each session thereafter to
    // PIN-Protected or PIN-Derived (whichever is needed). That is, if a YubiKey
    // has been set to PIN-based, each time you create a new session, you must
    // set the session to the appropriate state. In this class, the methods that
    // set a YubiKey to PIN-based will also set a session to the same state.
    // These same methods will set only the session if the YubiKey is already
    // set. In other words, call
    //
    //    SpecialMgmtKey.RunSetMgmtKeyPinProtected
    //    SpecialMgmtKey.RunSetMgmtKeyPinDerived
    //
    // to set the YubiKey and the current session to PIN-based. But each method
    // will set only the session if the YubiKey is already set.
    //
    // This class also contains a method that checks to see if a YubiKey is
    // PIN-based. If it is, that method will set the session to the appropriate
    // state. If not, that method does nothing.
    public static class SpecialMgmtKey
    {
        public const int EncodedMgmtKeyLength = 30;
        public const int MgmtKeyLength = 24;

        // This method will check to see if the YubiKey is PIN-Protected. If it
        // is, it will set the session to be PIN-Protected. If the YubiKey is not
        // PIN-Protected, the method will not set the session to be
        // PIN-Protected, but it will then check to see if the YubiKey is
        // PIN-Derived. If it is PIN-Derived, the method will set the session to
        // be PIN-Derived. If not, it will do nothing.
        // Note that to check for PIN-Protected and/or PIN-Derived, this method
        // will require the PIN to be entered.
        //
        // If the mgmt key is neither PIN-Protected nor PIN-Derived, this method
        // will not authenticate the mgmt key. After calling,
        // pivSession.ManagementKeyAuthenticated will be false. Later on, if you
        // call on the SDK to perform an operation that requires the mgmt key,
        // your KeyCollector will be called to provide it. That is, the SDK will
        // behave normally.
        //
        // If the mgmt key session is already authenticated for this session, the
        // method will do nothing (it will not ask for the PIN).
        //
        // If this method encounters an error, it will throw an exception.
        public static void SetSessionIfMgmtKeyPinBased(PivSession pivSession)
        {
            if (pivSession is null)
            {
                throw new ArgumentNullException(nameof(pivSession));
            }

            if (pivSession.ManagementKeyAuthenticated)
            {
                return;
            }

            Func<KeyEntryData, bool> SaveKeyCollector = pivSession.KeyCollector;
            byte[] pin = Array.Empty<byte>();

            try
            {
                // We will need to verify the PIN for many of the following
                // operations. But we might also need the PIN itself, so verify the
                // PIN, even if it already is, and get a copy.
                // If the caller cancels, return false.
                if (!PinAndKeyOperations.VerifyAndReturnPin(pivSession, SaveKeyCollector, out pin))
                {
                    throw new InvalidOperationException("Could not collect PIN.");
                }

                if (!SetPinProtected(pivSession, SaveKeyCollector, pin, false))
                {
                    throw new InvalidOperationException("Could not set session for PIN-Protected.");
                }

                // If it was PIN-Protected, the mgmt key will be authenticated,
                // there is no need to check for PIN-Derived.
                if (pivSession.ManagementKeyAuthenticated)
                {
                    return;
                }

                if (!SetPinDerived(pivSession, SaveKeyCollector, pin, false))
                {
                    throw new InvalidOperationException("Could not set session for PIN-Derived.");
                }
            }
            finally
            {
                // No matter what happens, make sure we return with the
                // PivSession set with the KeyCollector it originally had.
                pivSession.KeyCollector = SaveKeyCollector;
                CryptographicOperations.ZeroMemory(pin);
            }
        }

        // Set the YubiKey to store a PIN-Protected management key.
        // Also, set the session to be PIN-Protected.
        // If the management key is already authenticated, this method will still
        // set the YubiKey to be PIN-Protected.
        //
        // Note that in order to set a YubiKey to PIN-Protected, the management
        // key must be authenticated. Once the YubiKey is set, it is possible to
        // perform YubiKey operations without entering the management key. Until
        // the management key is PIN-Protected (or PIN-Derived), this sample will
        // always call on the loaded KeyCollector to obtain the management key,
        // even if the current management key is the default.
        //
        // If the YubiKey is already set for PIN-Protected, this method will
        // try to authenticate the management key using the PIN-Protected value.
        // If it authenticates, that means the session is set for PIN-Protected
        // and the method will return true, if not, it will return false. Note
        // that in order to determine if the YubiKey is set for PIN-Protected,
        // the PIN must be entered.
        //
        // If the YubiKey is not set for PIN-Protected, and it cannot be set to
        // be so (some other application is using the PRINTED storage area), this
        // method will return false.
        //
        // If it is not set for PIN-Protected, this method will check to see if
        // the YubiKey is set for PIN-Derived.
        // If the YubiKey is set for PIN-Derived, this method will collect the
        // PIN, derive the mgmt key, and try to authenticate. If the PIN-derived
        // key authenticates, the method will set that key to be the
        // PIN-Protected key, and return true. In this case, the management key
        // for this YubiKey will be both PIN-Protected and PIN-Derived.
        //
        // If the YubiKey is not set for PIN-Derived, this method will call on
        // the KeyCollector to obtain the management key and authenticate it. If
        // the management key is the default, the method will generate a new,
        // random management key, change the YubiKey to that new value and store
        // it. If it is not the default, the method will store the value
        // retrieved. In this case, the management key for this YubiKey is only
        // PIN-protected.
        //
        // An application might choose to try to first authenticate using the
        // default mgmt key without calling the KeyCollector. If the default
        // authenticates, there was no need to ask the user to supply it. This is
        // what the Minidriver does. Because it operates in an environment where
        // input of the mgmt key is not possible, not even the default, it will
        // only initiate a YubiKey to PIN-Protected if the mgmt key is currently
        // the default. Of course, if some other application (such as this sample
        // code) initiates the YubiKey to PIN-Protected, the Minidriver will
        // recognize it and work. That is, it will see that it does not need to
        // initiate the process, it will simply authenticate the mgmt key stored
        // in the PRINTED storage area.
        //
        // An application might want to throw an exception if the YubiKey cannot
        // be set to PIN-Protected (this happens when e.g. some other application
        // already used the PRINTED storage area, so we can't save the mgmt key
        // there). For example, in the following method, if the verifyState ends
        // up being VerifyState.NotAvailable, "false" is returned. But someone
        // might prefer to do something such as the following.
        //
        //   throw new InvalidOperationException(
        //       "The given YubiKey cannot be set to PIN-Protected");
        //
        public static bool RunSetMgmtKeyPinProtected(PivSession pivSession)
        {
            if (pivSession is null)
            {
                throw new ArgumentNullException(nameof(pivSession));
            }

            Func<KeyEntryData, bool> SaveKeyCollector = pivSession.KeyCollector;
            byte[] pin = Array.Empty<byte>();

            try
            {
                // We will need to verify the PIN for many of the following
                // operations. But we might also need the PIN itself, so verify the
                // PIN, even if it already is, and get a copy.
                // If the caller cancels, return false.
                if (!PinAndKeyOperations.VerifyAndReturnPin(pivSession, SaveKeyCollector, out pin))
                {
                    throw new InvalidOperationException("Could not collect PIN.");
                }

                return SetPinProtected(pivSession, SaveKeyCollector, pin, true);
            }
            finally
            {
                // No matter what happens, make sure we return with the
                // PivSession set with the KeyCollector it originally had.
                pivSession.KeyCollector = SaveKeyCollector;
                CryptographicOperations.ZeroMemory(pin);
            }
        }

        // Common code.
        // The incoming KeyCollector is what the user had originally set
        // pivSession.KeyCollector to. This method might set
        // pivSession.KeyCollector to something else. It is the caller's
        // responsibility to restore it.
        // It is the caller's responsibility to get the PIN (see
        // VerifyAndReturnPin).
        //   setNew is true : If the YubiKey is not set for PIN-Protected,
        //                    set both the YubiKey and the session for PIN-Protected.
        //                    If the YubiKey is set for PIN-Protected,
        //                    set session for PIN-Protected.
        //   setNew is false: If the YubiKey is not set for PIN-Protected,
        //                    do nothing, just return true.
        //                    If the YubiKey is set for PIN-Protected,
        //                    set session for PIN-Protected.
        // This method might set pivSession.KeyCollector to something else. It is
        // the caller's responsibility to make sure the KeyCollector
        private static bool SetPinProtected(
            PivSession pivSession,
            Func<KeyEntryData, bool> SaveKeyCollector,
            byte[] pin,
            bool setNew)
        {
            // This special KeyCollector will allow us to use the key we
            // generate, or the key in PRINTED, or a PIN-derived value.
            using var specialKeyCollector = new AuthenticateMgmtKeyCollector();

            // Check the PRINTED data.
            using var printedData = new PivPrinted(pivSession, specialKeyCollector);
            if (!setNew)
            {
                // If the setNew is false, then all we wanted to do was see
                // if the YubiKey was set for PIN-Protected. If so,
                // authenticate (the PivPrinted will have done that). If not,
                // do nothing and return true;
                return true;
            }

            // PIN-Protected also uses the CHUID and ADMIN DATA.
            var chuid = new PivChuid(pivSession);
            using var adminData = new PivAdminData(pivSession);

            // If the YubiKey is not set for PIN-Protected, make some other
            // checks.
            if (!printedData.IsAuthenticated)
            {
                // If the YubiKey is set with PRINTED data, and it is not
                // encoded (some other format), then someone else is using
                // it. We'll leave it alone.
                if (printedData.IsSet && (!printedData.IsEncoded))
                {
                    return false;
                }

                // Is it set for PIN-Derived?
                Memory<byte> keyToUse = Memory<byte>.Empty;
                if (adminData.TryAuthenticate(pivSession, specialKeyCollector, pin))
                {
                    // Store the PIN-Derived key as the PIN-Protected
                    // MgmtKey.
                    keyToUse = adminData.MgmtKey;
                }

                // If it was PIN-Derived, this method will set the PIN-Protected
                // key to be the PIN-Derived. Otherwise, it will set the
                // PIN-Protected key to be a new, random value.
                if (!printedData.TryUpdateMgmtKey(
                    pivSession, SaveKeyCollector, specialKeyCollector, keyToUse))
                {
                    return false;
                }
            }

            // If we reach this code, we have a key in PRINTED. Now make sure
            // the CHUID and ADMIN DATA are correct. This is to guarantee
            // other applications, such as the Minidriver will work. It is
            // not likely, but possible, that some application set the
            // PRINTED area to store the management key, but did not set the
            // CHUID or ADMIN DATA. So we'll do it now to make sure.
            if (chuid.TryStoreChuid(pivSession))
            {
                adminData.UpdateBitField(true);
                return adminData.TryStoreAdminData(pivSession);
            }

            // If we reach this code, something went wrong.
            return false;
        }

        // Set the YubiKey to authenticate using a PIN-Derived management key.
        //
        // If the YubiKey is already set for PIN-Derived, this method will
        // try to authenticate the management key using the PIN-Derived value.
        // If it authenticates, it will return true, if not, it will return
        // false.
        //
        // Note that in order to set a YubiKey to PIN-Derived, the management
        // key must be authenticated. Once the YubiKey is set, it is possible to
        // perform YubiKey operations without entering the management key. Until
        // the management key is PIN-Derived (or PIN-Protected), this sample will
        // always call on the loaded KeyCollector to obtain the management key,
        // even if the current management key is the default. Some applications
        // might choose to try the default management key first before calling
        // the KeyCollector. If the current key is the default, then the user
        // never has to enter the management key.
        //
        // If the YubiKey is already set for PIN-Derived, this method will
        // try to authenticate the management key using the PIN-Derived value.
        // If it authenticates, that means the session is set for PIN-Derived
        // and the method will return true, if not, it will return false.
        //
        // If the YubiKey is not set for PIN-Derived, and it cannot be set to
        // be so (some other application is using the ADMIN DATA storage area),
        // this method will return false.
        //
        // If it is not set for PIN-Derived, this method will check to see if
        // the YubiKey is set for PIN-Protected.
        // If this YubiKey is already set for PIN-Protected, this method will
        // authenticate using the PIN-Protected key, derive a new key from the
        // PIN, change the management key to this new, derived key, then store
        // this new, derived key as the PIN-Protected key as well.
        //
        // If the YubiKey is not set for PIN-Derived or PIN-Protected, this
        // method will call on the KeyCollector to obtain the management key and
        // authenticate it. It will generate a new, random salt and derive the
        // new mgmt key from the PIN and salt. It will change the management key
        // to this new value and store the salt in the ADMIN DATA.
        //
        // An application might choose to try to first authenticate using the
        // default mgmt key without calling the KeyCollector. If the default
        // authenticates, there was no need to ask the user to supply it. This is
        // what the Minidriver does. Because it operates in an environment where
        // input of the mgmt key is not possible, not even the default, it will
        // only initiate a YubiKey to PIN-Protected if the mgmt key is currently
        // the default. Of course, if some other application (such as this sample
        // code) initiates the YubiKey to PIN-Protected, the Minidriver will
        // recognize it and work. That is, it will see that it does not need to
        // initiate the process, it will simply authenticate the mgmt key stored
        // in the PRINTED storage area.
        //
        // An application might want to throw an exception if the YubiKey cannot
        // be set to PIN-Protected (this happens when e.g. some other application
        // already used the PRINTED storage area, so we can't save the mgmt key
        // there). For example, in the following method, if the verifyState ends
        // up being VerifyState.NotAvailable, "false" is returned. But someone
        // might prefer to do something such as the following.
        //
        //   throw new InvalidOperationException(
        //       "The given YubiKey cannot be set to PIN-Derived");
        //
        public static bool RunSetMgmtKeyPinDerived(PivSession pivSession)
        {
            if (pivSession is null)
            {
                throw new ArgumentNullException(nameof(pivSession));
            }

            Func<KeyEntryData, bool> SaveKeyCollector = pivSession.KeyCollector;
            byte[] pin = Array.Empty<byte>();

            try
            {
                // We will need to verify the PIN for many of the following
                // operations. But we might also need the PIN itself, so verify the
                // PIN, even if it already is, and get a copy.
                // If the caller cancels, return false.
                if (!PinAndKeyOperations.VerifyAndReturnPin(pivSession, SaveKeyCollector, out pin))
                {
                    throw new InvalidOperationException("Could not collect PIN.");
                }

                return SetPinDerived(pivSession, SaveKeyCollector, pin, true);
            }
            finally
            {
                // No matter what happens, make sure we return with the
                // PivSession set with the KeyCollector it originally had.
                pivSession.KeyCollector = SaveKeyCollector;
                CryptographicOperations.ZeroMemory(pin);
            }
        }

        // Common code.
        // The incoming KeyCollector is what the user had originally set
        // pivSession.KeyCollector to. This method might set
        // pivSession.KeyCollector to something else. It is the caller's
        // responsibility to restore it.
        // It is the caller's responsibility to get the PIN (see
        // VerifyAndReturnPin).
        //   setNew is true : If the YubiKey is not set for PIN-Derived,
        //                    set both the YubiKey and the session for PIN-Derived.
        //                    If the YubiKey is set for PIN-Derived,
        //                    set session for PIN-Derived.
        //   setNew is false: If the YubiKey is not set for PIN-Derived,
        //                    do nothing, just return true.
        //                    If the YubiKey is set for PIN-Derived,
        //                    set session for PIN-Derived.
        private static bool SetPinDerived(
            PivSession pivSession,
            Func<KeyEntryData, bool> SaveKeyCollector,
            byte[] pin,
            bool setNew)
        {
            // This special KeyCollector will allow us to use the key we
            // generate, or the key in PRINTED, or a PIN-derived value.
            using var specialKeyCollector = new AuthenticateMgmtKeyCollector();

            // Get the data out of the YubiKey. This will authenticate if the
            // data indicates PIN-Derived.
            using var adminData = new PivAdminData(pivSession, specialKeyCollector, pin);

            // If the setNew is false, then all we wanted to do was see
            // if the YubiKey was set for PIN-Derived. If it is set, try
            // to authenticate (the PivAdminData class will have done
            // that). If not, do nothing and return true;
            //
            // If the mgmt key is authenticated, we're done.
            if (!setNew || adminData.IsAuthenticated)
            {
                return true;
            }

            // If the YubiKey is set with ADMIN DATA, and it is not
            // encoded (some other format), then someone else is using
            // it. We'll leave it alone.
            if (adminData.IsSet && (!adminData.IsEncoded))
            {
                return false;
            }

            // If it is not authenticated, create a PIN-Derived key.
            Memory<byte> currentKey = Memory<byte>.Empty;

            // See if there is a PIN-Protected mgmt key. If so, we probably
            // couldn't get the mgmt key from the KeyCollector, so we'll get
            // it from PRINTED.
            // Then we can change it to the new PIN-Derived key.
            using var printedData = new PivPrinted(pivSession, specialKeyCollector);
            if (printedData.IsAuthenticated)
            {
                currentKey = printedData.MgmtKey;
            }

            // If there is a PIN-Protected key, this method will change from that
            // key (currentKey) to the derived key. Otherwise, it will collect
            // the current mgmt key (using SaveKeyCollector), and change from
            // that key (currentKey will be empty) to the derived key.
            if (adminData.TryUpdateMgmtKey(pivSession, pin, SaveKeyCollector, specialKeyCollector, currentKey))
            {
                if (printedData.IsAuthenticated)
                {
                    // If the printedData key authenticated, we changed from that
                    // key to a new derived key. Now make sure the PRINTED
                    // storage area contains that new key.
                    return printedData.TryUpdateMgmtKey(
                        pivSession, SaveKeyCollector, specialKeyCollector, adminData.MgmtKey);
                }

                return true;
            }

            return false;
        }
    }
}

// Copyright 2022 Yubico AB
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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.TestUtilities
{
    // This class knows how to reset the U2F application on a YubiKey.
    // This will reset (clear any credentials and "remove" the PIN).
    // If you want, it will then set the PIN to a value you specify. Otherwise,
    // it will not set the PIN.
    public class Fido2ResetForTest
    {
        private const int ReinsertTimeoutSeconds = 10;
        private readonly string ReinsertTimeoutString = ReinsertTimeoutSeconds.ToString(NumberFormatInfo.InvariantInfo);

        private IYubiKeyDevice? _yubiKeyDevice;
        private readonly bool _setPin;
        private readonly ReadOnlyMemory<byte> _pin;
        private readonly KeyEntryData _keyEntryData = new KeyEntryData();


        // Set the serial number using this property. If there is no serial
        // number (the actual YubiKey's serial number is null), this will be 0.
        public int SerialNumber { get; private set; }

        public Func<KeyEntryData, bool> KeyCollector { get; private set; }

        private Fido2ResetForTest()
        {
            throw new NotImplementedException();
        }

        // Create a new instance that will be able to reset the FIDO2 application
        // on the YubiKey with the given serial number.
        // Most common usage:
        //     var resetObj = new Fido2ResetForTest(yubiKeyDevice.SerialNumber);
        // If there is no serial number for the YubiKey, either pass in null or
        // zero (note that there is no default for this arg).
        // If you want this object to set the PIN to some value after the reset
        // (even if the PIN you want to use is "123456"), then pass in the
        // newPin. The newPin must be at least 4 bytes long. There are
        // complicated rules about PINs (and length), but this test code simply
        // requires at least 4 bytes of input. If there is no input newPin arg,
        // or the arg is null), then after reset there will be no PIN set on the
        // YubiKey.
        // Note that the default key collector will only be able to return the
        // specified PIN if called with KeyEntryRequest.VerifyFido2Pin.
        // Note also that if no PIN is given, the default key collector will only
        // be able to return the PIN "123456" if called with
        // KeyEntryRequest.VerifyFido2Pin.
        // If there is no input keyCollector (or the arg is null), then this
        // object will use the default key collector. Otherwise, pass in the
        // alternate key collector you want this object to use.
        public Fido2ResetForTest(
            int? serialNumber, ReadOnlyMemory<byte>? newPin = null, Func<KeyEntryData, bool>? keyCollector = null)
        {
            SerialNumber = serialNumber ?? 0;
            if (newPin is null)
            {
                _pin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });
                _setPin = false;
            }
            else
            {
                if (newPin.Value.Length < 4)
                {
                    throw new ArgumentException("PIN is too short");
                }
                _pin = new ReadOnlyMemory<byte>(newPin.Value.ToArray());
                _setPin = true;
            }
            KeyCollector = keyCollector ?? ResetForTestKeyCollector;
        }

        public ResponseStatus RunFido2Reset()
        {
            _yubiKeyDevice = null;
            _keyEntryData.Request = KeyEntryRequest.Release;
            using var tokenSource = new CancellationTokenSource();
            var touchMessageTask = new Task(CallKeyCollectorTouch, tokenSource.Token);

            try
            {
                // The SDK comes with a listener that can tell when a YubiKey has
                // been removed or inserted.
                YubiKeyDeviceListener yubiKeyDeviceListener = YubiKeyDeviceListener.Instance;

                yubiKeyDeviceListener.Arrived += YubiKeyInserted;
                yubiKeyDeviceListener.Removed += YubiKeyRemoved;

                WriteMessageBox("Remove and re-insert the YubiKey (" + ReinsertTimeoutString + " second timeout)." +
                                "\nThen when instructed, touch the YubiKey's contact.");

                // This task simply checks to see if the YubiKey in question has been
                // reinserted or not. Once it has been reinserted, this method will
                // return the IYubiKeyDevice that was reinserted.
                var reinsert = Task<IYubiKeyDevice>.Run(() => CheckReinsert());

                // Wait for CheckReinsert to complete, or else the task times out.
                // If it returns false, the YubiKey was not reinserted within the
                // time limit, so return false.
                if (!reinsert.Wait(ReinsertTimeoutSeconds * 1000))
                {
                    return ResponseStatus.ConditionsNotSatisfied;
                }

                // The YubiKey has been rebooted, so we need to get a new session.
                using var fido2Session = new Fido2Session(reinsert.Result);

                // Now that we have a session, we can call the Reset command.
                // Once the YubiKey has received the Reset command, it will
                // either return immediately with an error (e.g. it is connected
                // via NFC) or it will need user touch to complete. If there is
                // no error, the YubiKey will not return until the user touches
                // and the Reset is complete, or it times out, ten seconds
                // according to the standard.

                // We want to call the KeyCollector with the Request of Touch, so
                // the user knows they need to touch, and to touch now.
                // However, if we call the Reset command first, it will block, so
                // no other call can be made until it returns.
                // Hence, we must call the KeyCollector before calling the Reset
                // command.
                // The problem is that the Reset command might return an error,
                // in which case we don't want to call the KeyCollector.

                // What we really want is to call the Reset command and see if it
                // returns immediately or not. If immediately, there was an
                // error, so there's no need for touch, and so no need to call
                // the KeyCollector. If the command does not return immediately,
                // then it is waiting for touch, so at that point we know we want
                // to call the KeyCollector.
                // However, that's not possible.
                // There are a number of ways to resolve this. Here are two.
                // One, just call the KeyCollector with a Request of Touch
                // because the probability of a quick error is very small. In the
                // unlikely case there is an error, we will quickly call the
                // KeyCollector again with a Request of Release. Furthermore, the
                // call to the Reset command returns an error code so that the
                // caller has enough information to inform the user that there
                // was an error and touch is not needed after all.
                // Two, create a separate thread with the call to the
                // KeyCollector, but delay the call (e.g. Thread.Sleep) to give
                // the Reset command some time to determine if there's an error.
                // This might work, but the length of time to wait is dependent
                // on the specific chip's speed, how fast the platform can
                // communicate with a YubiKey, and the order of operations
                // executed in each thread. Hence, in some situations, it is
                // possible that despite the delay, the KeyCollector is called
                // even when there is a Reset error.

                // This sample code will use the first option.

                // On a separate thread, call the KeyCollector to announce we
                // need touch.
                // Generally we don't call the KeyCollector on another thread,
                // but for touch, we don't know for sure whether the KeyCollector
                // is going to return immediately or not. For example, a
                // KeyCollector might create a window with a message for touch
                // that also includes an OK button. Until that button is clicked,
                // the KeyCollector does not return to the caller. In other
                // words, it blocks. The documentation recommends not doing this,
                // but it is possible.
                // For touch, we want to call the KeyCollector, but then move on
                // to do other work. Hence, call it on another thread.
                touchMessageTask.Start();

                var resetCmd = new ResetCommand();
                ResetResponse resetRsp = fido2Session.Connection.SendCommand(resetCmd);

                if (resetRsp.Status == ResponseStatus.Success && _setPin)
                {
                    if (!fido2Session.TrySetPin(_pin))
                    {
                        return ResponseStatus.Failed;
                    }
                }

                return resetRsp.Status;
            }
            finally
            {
                // It's possible touchMessageTask never started because there was
                // an error. If so, there's nothing left to do. If the task's
                // Status is Created, that means it never started.
                // If it was started, but the KeyCollector had not been called,
                // there's nothing left to do.
                // If the KeyCollector was called, we want to call Release.
                // If we are going to call Release, we really want to wait until
                // the touchMessageTask completes. However, we can't guarantee
                // that the KeyCollector has returned or will return from the
                // call that requested Touch. It is not likely, but possible, the
                // KeyCollector provided will not return from a call. For
                // example, maybe it won't return until the user clicks an OK
                // button.
                // Hence, we'll cancel the task if needed.
                if (touchMessageTask.Status != TaskStatus.Created)
                {
                    if (!touchMessageTask.IsCompleted)
                    {
                        tokenSource.Cancel();
                    }

                    // If the Request field of the _keyEntryData is TouchRequest,
                    // we did indeed call the KeyCollector, so we need to call it
                    // again in order to release.
                    // Otherwise, we never did call the KeyCollector (there
                    // was likely some error before we could call), so there's no
                    // need to call to release.
                    if (_keyEntryData.Request == KeyEntryRequest.TouchRequest)
                    {
                        _keyEntryData.Request = KeyEntryRequest.Release;
                        _ = KeyCollector(_keyEntryData);
                    }
                }
            }
        }

        // We want to call the KeyCollector indicating touch is needed. However,
        // we don't want to call it if the Reset has already completed.
        // Unfortunately, that's not really possible with the YubiKey.
        private void CallKeyCollectorTouch()
        {
            _keyEntryData.Request = KeyEntryRequest.TouchRequest;
            _ = KeyCollector(_keyEntryData);
        }

        private IYubiKeyDevice CheckReinsert()
        {
            while (_yubiKeyDevice is null)
            {
                Thread.Sleep(100);
            }

            return (IYubiKeyDevice)_yubiKeyDevice;
        }

        private void YubiKeyRemoved(object? sender, YubiKeyDeviceEventArgs eventArgs)
        {
            int serialNumberRemoved = eventArgs.Device.SerialNumber ?? 0;

            if (serialNumberRemoved != SerialNumber)
            {
                WriteMessageBox("The YubiKey removed is not the expected YubiKey." +
                                "\nexpected serial number = " + SerialNumber.ToString(NumberFormatInfo.InvariantInfo) +
                                "\n removed serial number = " + serialNumberRemoved.ToString(NumberFormatInfo.InvariantInfo));
            }
            else
            {
                WriteMessageBox(" removed serial number = " + serialNumberRemoved.ToString(NumberFormatInfo.InvariantInfo));
            }
        }

        private void YubiKeyInserted(object? sender, YubiKeyDeviceEventArgs eventArgs)
        {
            int serialNumberInserted = eventArgs.Device.SerialNumber ?? 0;

            if (serialNumberInserted != SerialNumber)
            {
                WriteMessageBox("The YubiKey inserted is not the expected YubiKey." +
                                "\nexpected serial number = " + SerialNumber.ToString(NumberFormatInfo.InvariantInfo) +
                                "\ninserted serial number = " + serialNumberInserted.ToString(NumberFormatInfo.InvariantInfo));
            }
            else
            {
                WriteMessageBox("inserted serial number = " + serialNumberInserted.ToString(NumberFormatInfo.InvariantInfo));
                _yubiKeyDevice = eventArgs.Device;
            }
        }

        public bool ResetForTestKeyCollector(KeyEntryData keyEntryData)
        {
            return ResetKeyCollector(keyEntryData, _pin);
        }

        public static bool ResetForTestKeyCollectorDelegate(KeyEntryData keyEntryData)
        {
            return ResetKeyCollector(keyEntryData, null);
        }

        public static bool ResetKeyCollector(KeyEntryData keyEntryData, ReadOnlyMemory<byte>? pin)
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
                    break;

                case KeyEntryRequest.Release:
                    return true;

                case KeyEntryRequest.VerifyFido2Pin:
                    ReadOnlyMemory<byte> toSubmit = pin ?? new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });
                    keyEntryData.SubmitValue(toSubmit.Span);
                    return true;

                case KeyEntryRequest.TouchRequest:
                    WriteMessageBox("Touch the YubiKey's contact to complete the operation.");
                    return true;
            }

            return false;
        }

        private static void WriteMessageBox(string msg)
        {
            //_ = System.Windows.MessageBox.Show(msg, "FIDO2 Integration Test");
            Console.WriteLine(msg);
        }
    }
}

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
using Yubico.YubiKey.Sample.SharedCode;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.Sample.U2fSampleCode
{
#nullable enable
    // This class knows how to reset the U2F application on a YubiKey.
    public class U2fReset
    {
        private const int ReinsertTimeoutSeconds = 10;
        private readonly string ReinsertTimeoutString = ReinsertTimeoutSeconds.ToString(NumberFormatInfo.InvariantInfo);

        private int? _serialNumber;
        private IYubiKeyDevice? _yubiKeyDevice;

        // Set the serial number using this property. If there is no serial
        // number (the actual YubiKey's serial number is null), this will be 0.
        public int SerialNumber
        {
            get => _serialNumber ?? 0;
            set => _serialNumber = value;
        }

        public U2fReset()
        {
        }

        // Create a new instance that will be able to reset the YubiKey with the
        // given serial number.
        public U2fReset(int? serialNumber)
        {
            _serialNumber = serialNumber;
        }

        // This implementation requires a valid KeyCollector. Although the SDK
        // does not require a KeyCollector, this sample code does.
        public bool RunU2fReset(Func<KeyEntryData, bool> KeyCollector)
        {
            if (KeyCollector is null)
            {
                throw new ArgumentNullException(nameof(KeyCollector));
            }

            _yubiKeyDevice = null;
            var keyEntryData = new KeyEntryData();
            KeyEntryRequest keyEntryRequest = KeyEntryRequest.TouchRequest;
            Task? touchMessageTask = null;

            YubiKeyDeviceListener yubiKeyDeviceListener = YubiKeyDeviceListener.Instance;

            yubiKeyDeviceListener.Arrived += YubiKeyInserted;
            yubiKeyDeviceListener.Removed += YubiKeyRemoved;

            SampleMenu.WriteMessage(MessageType.Title, 0, "Remove and re-insert the YubiKey (" + ReinsertTimeoutString + " second timeout).\n");

            // This task simply checks to see if the YubiKey in question has been
            // reinserted or not. Once it has been reinserted, this method will
            // return the IYubiKeyDevice that was reinserted.
            var reinsert = Task<IYubiKeyDevice>.Run(() => CheckReinsert());

            // Wait for CheckReinsert to complete, or else the task times out.
            // If it returns false, the YubiKey was not reinserted within the
            // time limit, so return false.
            if (!reinsert.Wait(ReinsertTimeoutSeconds * 1000))
            {
                return false;
            }

            // The YubiKey has been rebooted, so we need to quickly reset.
            try
            {
                using IYubiKeyConnection connection = reinsert.Result.Connect(YubiKeyApplication.FidoU2f);
                var resetCmd = new ResetCommand();
                ResetResponse resetRsp = connection.SendCommand(resetCmd);

                while (resetRsp.Status == ResponseStatus.ConditionsNotSatisfied)
                {
                    // On a separate thread, call the KeyCollector to announce we
                    // need touch.
                    // We're not calling the KeyCollector until we know for sure
                    // that the reset command generated a ConditionsNotSatisfied.
                    if (keyEntryRequest == KeyEntryRequest.TouchRequest)
                    {
                        keyEntryData.Request = keyEntryRequest;
                        touchMessageTask = Task.Run(() => _ = KeyCollector(keyEntryData));

                        // Set this so the next iteration we don't call the
                        // KeyCollector.
                        keyEntryRequest = KeyEntryRequest.Release;
                    }

                    Thread.Sleep(100);
                    resetRsp = connection.SendCommand(resetCmd);
                }

                return resetRsp.Status == ResponseStatus.Success;
            }
            finally
            {
                touchMessageTask?.Wait();
                // If this is Release, we did indeed call the KeyCollector, so we
                // need to call it again in order to release.
                // If this is not Release, we never did call the KeyCollector
                // (there was likely some error before we could call), so there's
                // no need to call to release.
                if (keyEntryRequest == KeyEntryRequest.Release)
                {
                    keyEntryData.Request = keyEntryRequest;
                    _ = KeyCollector(keyEntryData);
                }
            }
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
                SampleMenu.WriteMessage(MessageType.Special, 0, "The YubiKey removed is not the expected YubiKey.");
                SampleMenu.WriteMessage(MessageType.Title, 0, "expected serial number = " + SerialNumber.ToString(NumberFormatInfo.InvariantInfo));
                SampleMenu.WriteMessage(MessageType.Title, 0, " removed serial number = " + serialNumberRemoved.ToString(NumberFormatInfo.InvariantInfo));
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, " removed serial number = " + serialNumberRemoved.ToString(NumberFormatInfo.InvariantInfo));
            }
        }

        private void YubiKeyInserted(object? sender, YubiKeyDeviceEventArgs eventArgs)
        {
            int serialNumberInserted = eventArgs.Device.SerialNumber ?? 0;

            if (serialNumberInserted != SerialNumber)
            {
                SampleMenu.WriteMessage(MessageType.Special, 0, "The YubiKey inserted is not the expected YubiKey.");
                SampleMenu.WriteMessage(MessageType.Title, 0, "expected serial number = " + SerialNumber.ToString(NumberFormatInfo.InvariantInfo));
                SampleMenu.WriteMessage(MessageType.Title, 0, "inserted serial number = " + serialNumberInserted.ToString(NumberFormatInfo.InvariantInfo));
            }
            else
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "inserted serial number = " + serialNumberInserted.ToString(NumberFormatInfo.InvariantInfo));
                _yubiKeyDevice = eventArgs.Device;
            }
        }
    }
#nullable restore
}

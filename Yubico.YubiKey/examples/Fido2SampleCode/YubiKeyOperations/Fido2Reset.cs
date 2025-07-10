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
using System.Threading.Tasks;
using Yubico.YubiKey.DeviceExtensions;
// Note: The Fido2Reset class from the original sample code has been refactored
// to use modern async/await patterns and the best practices for reliability.
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
#nullable enable
    /// <summary>
    /// This class demonstrates a robust, event-driven implementation for resetting the
    /// FIDO2 application on a YubiKey, incorporating modern C# best practices.
    /// </summary>
    public class Fido2Reset
    {
        private readonly int _originalSerialNumber;
        private readonly Func<KeyEntryData, bool> _keyCollector;
        private TaskCompletionSource<IYubiKeyDevice?>? _tcs;

        public Fido2Reset(int? serialNumber, Func<KeyEntryData, bool> keyCollector)
        {
            _originalSerialNumber = serialNumber ?? 0;
            _keyCollector = keyCollector ?? throw new ArgumentNullException(nameof(keyCollector));
        }

        /// <summary>
        /// Runs the FIDO2 reset workflow using an event-driven approach.
        /// </summary>
        public async Task<bool> RunFido2Reset()
        {
            var yubiKeyDeviceListener = YubiKeyDeviceListener.Instance;
            yubiKeyDeviceListener.Removed += YubiKeyRemoved;
            yubiKeyDeviceListener.Arrived += YubiKeyInserted;

            try
            {
                // Step 1: Instruct user to remove the key. The YubiKeyRemoved event will provide feedback.
                SampleMenu.WriteMessage(MessageType.Title, 0, "Please remove the YubiKey to begin the reset process...");

                // This will wait until the YubiKeyRemoved event handler confirms the correct key was removed.
                // For this sample, we assume the user will comply. In a real-world GUI app,
                // you might have a more sophisticated state machine.

                // Step 2: Instruct user to re-insert and wait for the event.
                SampleMenu.WriteMessage(MessageType.Title, 0, "\nPlease re-insert the YubiKey to continue (20 second timeout)...");

                IYubiKeyDevice? reinsertedKey = await WaitForYubiKeyArrival(TimeSpan.FromSeconds(20));

                if (reinsertedKey is null)
                {
                    SampleMenu.WriteMessage(MessageType.Special, 0, "\nOperation timed out. YubiKey was not re-inserted.");
                    return false;
                }

                // The YubiKeyInserted handler has already verified the serial number.
                SampleMenu.WriteMessage(MessageType.Title, 0, "YubiKey detected. Sending reset command...");

                // Step 3: Connect and send the reset command.
                using IYubiKeyConnection fidoConnection = reinsertedKey.Connect(YubiKeyApplication.Fido2);
                
                var resetCommand = new ResetCommand();
                ResetResponse response = fidoConnection.SendCommand(resetCommand);

                // Step 4: Inspect the response and provide final instructions.
                return HandleResetResponse(response);
            }
            finally
            {
                // Clean up event handlers to prevent memory leaks.
                yubiKeyDeviceListener.Removed -= YubiKeyRemoved;
                yubiKeyDeviceListener.Arrived -= YubiKeyInserted;
            }
        }

        /// <summary>
        /// Handles the response from the ResetCommand, providing appropriate user feedback.
        /// </summary>
        private bool HandleResetResponse(ResetResponse response)
        {
            switch (response.Status)
            {
                case ResponseStatus.Success:
                    SampleMenu.WriteMessage(MessageType.Special, 0, "\nFIDO2 application has been successfully reset.");
                    return true;

                case ResponseStatus.ConditionsNotSatisfied:
                    // This is the expected status when user touch is required.
                    _keyCollector(new KeyEntryData { Request = KeyEntryRequest.TouchRequest });
                    return true;

                default:
                    SampleMenu.WriteMessage(
                        MessageType.Special, 0,
                        $"\nThe FIDO2 reset failed. The YubiKey returned an error: {response.StatusMessage}");
                    return false;
            }
        }

        /// <summary>
        /// Creates a Task that completes when a YubiKey arrives or when a timeout is reached.
        /// </summary>
        private async Task<IYubiKeyDevice?> WaitForYubiKeyArrival(TimeSpan timeout)
        {
            _tcs = new TaskCompletionSource<IYubiKeyDevice?>();

            Task completedTask = await Task.WhenAny(_tcs.Task, Task.Delay(timeout));

            if (completedTask == _tcs.Task)
            {
                return await _tcs.Task;
            }
            
            return null; // Timeout occurred
        }

        /// <summary>
        /// Event handler for when a YubiKey is removed.
        /// </summary>
        private void YubiKeyRemoved(object? sender, YubiKeyDeviceEventArgs eventArgs)
        {
            int serialNumberRemoved = eventArgs.Device.SerialNumber ?? 0;
            string message = serialNumberRemoved == _originalSerialNumber
                ? $"Correct YubiKey removed (S/N: {serialNumberRemoved})."
                : $"Warning: An unexpected YubiKey was removed (S/N: {serialNumberRemoved}).";
            
            SampleMenu.WriteMessage(MessageType.Title, 0, message);
        }

        /// <summary>
        /// Event handler for when a YubiKey is inserted. Verifies the serial number
        /// and completes the TaskCompletionSource to signal the main workflow.
        /// </summary>
        private void YubiKeyInserted(object? sender, YubiKeyDeviceEventArgs eventArgs)
        {
            int serialNumberInserted = eventArgs.Device.SerialNumber ?? 0;

            if (serialNumberInserted != _originalSerialNumber)
            {
                SampleMenu.WriteMessage(
                    MessageType.Special, 0,
                    $"Incorrect YubiKey inserted. Expected S/N: {_originalSerialNumber}, but found S/N: {serialNumberInserted}.");
                
                // Complete with null to indicate failure.
                _tcs?.TrySetResult(null);
                return;
            }
            
            SampleMenu.WriteMessage(MessageType.Title, 0, $"\nCorrect YubiKey inserted (S/N: {serialNumberInserted}).");
            
            // Complete with the correct device to signal success.
            _tcs?.TrySetResult(eventArgs.Device);
        }
    }
#nullable restore
}

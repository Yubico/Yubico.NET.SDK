// Copyright 2026 Yubico AB
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Yubico.YubiKey;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // Demonstrates CTAP 2.1 authenticatorSelection (0x0B) over USB HID FIDO.
    // Each connected authenticator runs selection in its own Fido2Session. The
    // first touched authenticator wins; the others are canceled through the
    // SDK's SignalUserCancel -> CTAPHID_CANCEL path.
    public static class Fido2AuthenticatorSelection
    {
        private static readonly object OutputLock = new object();

        public static bool Run(
            Func<KeyEntryData, bool> keyCollector,
            ref IYubiKeyDevice yubiKeyChosen)
        {
            if (keyCollector is null)
            {
                throw new ArgumentNullException(nameof(keyCollector));
            }

            IYubiKeyDevice[] devices = YubiKeyDevice.FindByTransport(Transport.HidFido).ToArray();
            if (devices.Length == 0)
            {
                WriteLine("\nNo YubiKeys found over HID FIDO.\n");
                PauseBeforeMainMenu();
                return true;
            }

            WriteLine("\nAuthenticator selection");
            WriteLine("Touch the YubiKey you want to use for the next FIDO2 operation.\n");
            WriteDevices(devices);

            var coordinator = new AuthenticatorSelectionCoordinator();
            Task<SelectionAttemptResult>[] attempts = devices
                .Select(device => Task.Run(() => SelectAuthenticator(device, keyCollector, coordinator)))
                .ToArray();

            SelectionAttemptResult[] results = WaitForSelection(attempts, coordinator);
            IYubiKeyDevice selectedDevice = coordinator.SelectedDevice;
            if (selectedDevice is not null)
            {
                yubiKeyChosen = selectedDevice;
                WriteLine("\nSelected " + GetDeviceDisplayName(selectedDevice) + ".");

                int canceledCount = results.Count(result => result.Outcome == SelectionAttemptOutcome.Canceled);
                if (canceledCount > 0)
                {
                    WriteLine(
                        "Canceled " + canceledCount.ToString(CultureInfo.InvariantCulture) +
                        " non-selected authenticator session(s).");
                    WriteLine("The SDK sends CTAPHID_CANCEL (0x11) when SignalUserCancel is invoked.");
                }

                PauseBeforeMainMenu();
                return true;
            }

            WriteIncompleteSelectionSummary(results);
            PauseBeforeMainMenu();
            return true;
        }

        private static SelectionAttemptResult[] WaitForSelection(
            Task<SelectionAttemptResult>[] attempts,
            AuthenticatorSelectionCoordinator coordinator)
        {
            var pendingAttempts = new List<Task<SelectionAttemptResult>>(attempts);

            while (pendingAttempts.Count > 0)
            {
                Task<SelectionAttemptResult> completedAttempt = Task
                    .WhenAny(pendingAttempts)
                    .GetAwaiter()
                    .GetResult();

                _ = pendingAttempts.Remove(completedAttempt);

                if (completedAttempt.GetAwaiter().GetResult().Outcome == SelectionAttemptOutcome.Selected)
                {
                    coordinator.CancelLosers();
                    break;
                }
            }

            coordinator.CancelLosers();
            Task.WaitAll(attempts);

            return attempts.Select(attempt => attempt.Result).ToArray();
        }

        private static SelectionAttemptResult SelectAuthenticator(
            IYubiKeyDevice device,
            Func<KeyEntryData, bool> keyCollector,
            AuthenticatorSelectionCoordinator coordinator)
        {
            try
            {
                bool selected = TrySelection(
                    device,
                    CreateSelectionKeyCollector(device, keyCollector, coordinator),
                    out AuthenticatorSelectionResponse response);

                if (selected)
                {
                    return coordinator.TrySelectWinner(device)
                        ? SelectionAttemptResult.Selected()
                        : SelectionAttemptResult.NotSelected();
                }

                return response.CtapStatus switch
                {
                    CtapStatus.InvalidCommand => SelectionAttemptResult.Unsupported(),
                    CtapStatus.OperationDenied => SelectionAttemptResult.Denied(),
                    _ => SelectionAttemptResult.NotSelected(),
                };
            }
            catch (OperationCanceledException) when (coordinator.IsExpectedLoserCancellation(device))
            {
                return SelectionAttemptResult.Canceled();
            }
            catch (TimeoutException)
            {
                return SelectionAttemptResult.TimedOut();
            }
            catch (Exception ex)
            {
                return SelectionAttemptResult.Error(device, ex.Message);
            }
        }

        private static Func<KeyEntryData, bool> CreateSelectionKeyCollector(
            IYubiKeyDevice device,
            Func<KeyEntryData, bool> keyCollector,
            AuthenticatorSelectionCoordinator coordinator) =>
            keyEntryData =>
            {
                if (keyEntryData is null)
                {
                    return false;
                }

                if (keyEntryData.Request == KeyEntryRequest.TouchRequest)
                {
                    coordinator.CaptureCancel(device, keyEntryData.SignalUserCancel);
                    if (coordinator.HasWinner)
                    {
                        return true;
                    }

                    lock (OutputLock)
                    {
                        WriteLine("Waiting for touch on " + GetDeviceDisplayName(device) + ".");
                        return keyCollector(keyEntryData);
                    }
                }

                return keyEntryData.Request == KeyEntryRequest.Release || keyCollector(keyEntryData);
            };

        private static bool TrySelection(
            IYubiKeyDevice device,
            Func<KeyEntryData, bool> keyCollector,
            out AuthenticatorSelectionResponse response)
        {
            using var session = new Fido2Session(device)
            {
                KeyCollector = keyCollector,
            };

            return session.TryAuthenticatorSelection(out response);
        }

        private static void WriteIncompleteSelectionSummary(SelectionAttemptResult[] results)
        {
            foreach (SelectionAttemptResult result in results.Where(result => result.Outcome == SelectionAttemptOutcome.Error))
            {
                WriteLine(result.Message);
            }

            if (results.Any(result => result.Outcome == SelectionAttemptOutcome.Unsupported))
            {
                WriteLine("\nOne or more YubiKeys does not support authenticatorSelection.");
                WriteLine("This command requires YubiKey firmware 5.5.1 or later.");
                return;
            }

            if (results.Any(result => result.Outcome == SelectionAttemptOutcome.Denied))
            {
                WriteLine("\nSelection was denied.");
                return;
            }

            if (results.Any(result => result.Outcome == SelectionAttemptOutcome.TimedOut))
            {
                WriteLine("\nSelection timed out before a YubiKey was touched.");
                return;
            }

            WriteLine("\nSelection did not complete.");
        }

        private static void WriteDevices(IYubiKeyDevice[] devices)
        {
            WriteLine("Connected HID FIDO YubiKeys:");
            foreach (IYubiKeyDevice device in devices)
            {
                WriteLine("  " + GetDeviceDisplayName(device));
            }

            WriteLine(string.Empty);
        }

        private static string GetDeviceDisplayName(IYubiKeyDevice device)
        {
            if (device.SerialNumber.HasValue)
            {
                return "YubiKey serial " + device.SerialNumber.Value.ToString(CultureInfo.InvariantCulture);
            }

            return "YubiKey " + device.FirmwareVersion.ToString();
        }

        private static void WriteLine(string message) =>
            SampleMenu.WriteMessage(MessageType.Title, 0, message);

        private static void PauseBeforeMainMenu()
        {
            WriteLine("\nPress Enter to return to the main menu.");
            _ = SampleMenu.ReadResponse(out string _);
        }

        private enum SelectionAttemptOutcome
        {
            Selected,
            NotSelected,
            Unsupported,
            Denied,
            TimedOut,
            Canceled,
            Error,
        }

        private sealed class SelectionAttemptResult
        {
            private SelectionAttemptResult(
                SelectionAttemptOutcome outcome,
                string message)
            {
                Outcome = outcome;
                Message = message;
            }

            public SelectionAttemptOutcome Outcome { get; }

            public string Message { get; }

            public static SelectionAttemptResult Selected() =>
                new SelectionAttemptResult(SelectionAttemptOutcome.Selected, string.Empty);

            public static SelectionAttemptResult NotSelected() =>
                new SelectionAttemptResult(SelectionAttemptOutcome.NotSelected, string.Empty);

            public static SelectionAttemptResult Unsupported() =>
                new SelectionAttemptResult(SelectionAttemptOutcome.Unsupported, string.Empty);

            public static SelectionAttemptResult Denied() =>
                new SelectionAttemptResult(SelectionAttemptOutcome.Denied, string.Empty);

            public static SelectionAttemptResult TimedOut() =>
                new SelectionAttemptResult(SelectionAttemptOutcome.TimedOut, string.Empty);

            public static SelectionAttemptResult Canceled() =>
                new SelectionAttemptResult(SelectionAttemptOutcome.Canceled, string.Empty);

            public static SelectionAttemptResult Error(IYubiKeyDevice device, string message) =>
                new SelectionAttemptResult(
                    SelectionAttemptOutcome.Error,
                    GetDeviceDisplayName(device) + ": " + message);
        }
    }
}

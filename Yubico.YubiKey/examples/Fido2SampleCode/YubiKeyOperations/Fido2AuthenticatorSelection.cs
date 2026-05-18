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
using System.Linq;
using Yubico.YubiKey;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    // This file demonstrates CTAP 2.2 authenticatorSelection (0x0B) via
    // Fido2Session.TryAuthenticatorSelection over USB (HID FIDO). The YubiKey
    // prompts for user presence (touch) by blinking; on success the host can 
    // treat that YubiKey as the chosen authenticator for subsequent commands.
    public static class Fido2AuthenticatorSelection
    {
        public static bool Run(
            Func<KeyEntryData, bool> keyCollector,
            ref IYubiKeyDevice yubiKeyChosen)
        {
            if (keyCollector is null)
            {
                throw new ArgumentNullException(nameof(keyCollector));
            }

            // Look for YubiKeys over the FIDO HID (USB) transport.
            IYubiKeyDevice[] keys = YubiKeyDevice.FindByTransport(Transport.HidFido).ToArray();
            if (keys.Length == 0)
            {
                SampleMenu.WriteMessage(MessageType.Title, 0, "\nNo YubiKeys found over HID FIDO.\n");
                PauseBeforeMainMenu();
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "\nTouch a YubiKey when it blinks.\n");

            bool anyUnsupported = false;
            foreach (IYubiKeyDevice device in keys)
            {
                try
                {
                    if (TrySelection(device, keyCollector, out AuthenticatorSelectionResponse response))
                    {
                        yubiKeyChosen = device;
                        SampleMenu.WriteMessage(MessageType.Title, 0, "\nOK\n");
                        PauseBeforeMainMenu();
                        return true;
                    }

                    // CTAP INVALID_COMMAND: this firmware does not support CTAP 2.2; try the next key.
                    if (response.CtapStatus == CtapStatus.InvalidCommand)
                    {
                        anyUnsupported = true;
                        continue;
                    }
                }
                catch (TimeoutException)
                {
                    // No touch (or wrong YubiKey) before the session timeout; try another YubiKey.
                }
                catch (OperationCanceledException ex)
                {
                    // Key collector returned false (e.g. user ignored the prompt).
                    SampleMenu.WriteMessage(MessageType.Title, 0, ex.Message + "\n");
                    PauseBeforeMainMenu();
                    return true;
                }
                catch (Fido2Exception ex)
                {
                    // Other FIDO2 errors: show and continue to the next YubiKey, if any.
                    SampleMenu.WriteMessage(MessageType.Title, 0, ex.Message + "\n");
                }
            }

            if (anyUnsupported)
            {
                SampleMenu.WriteMessage(
                    MessageType.Title,
                    0,
                    "\nOne or more YubiKeys does not support CTAP 2.2.\n");
                PauseBeforeMainMenu();
                return true;
            }

            SampleMenu.WriteMessage(MessageType.Title, 0, "\nSelection did not complete.\n");
            PauseBeforeMainMenu();
            return true;
        }

        // Wait so the user can read messages before the sample redraws the main menu.
        private static void PauseBeforeMainMenu()
        {
            SampleMenu.WriteMessage(MessageType.Title, 0, "Press Enter to return to the main menu.");
            _ = SampleMenu.ReadResponse(out string _);
        }

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
    }
}
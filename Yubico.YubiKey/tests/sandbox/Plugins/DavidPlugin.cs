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
using System.Collections.Generic;
using System.Linq;
using Yubico.YubiKey.YubiHsmAuth;
using Yubico.YubiKey.YubiHsmAuth.Commands;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class DavidPlugin : PluginBase
    {
        public override string Name => "David";

        public override string Description => "A place for David's test code";

        public DavidPlugin(IOutput output) : base(output)
        {
            Parameters["command"].Required = true;
        }

        public override bool Execute()
        {
            return Command.ToLower() switch
            {
                "connectyha" => ConnectYubiHsmAuth(),
                "listcreds" => ListCredentials(),
                "deletecred" => DeleteCredential(),
                _ => throw new ArgumentException($"Invalid command [{ Command }] specified")
            };
        }

        private bool ConnectYubiHsmAuth()
        {
            bool result = default;
            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(Transport.All);

            if (keys.Any())
            {
                foreach (IYubiKeyDevice device in keys)
                {
                    Output.WriteLine($"Using YubiKey v{device.FirmwareVersion} S/N {device.SerialNumber}...");

                    bool yubiHsmAuthCapable = device.HasFeature(YubiKeyFeature.YubiHsmAuthApplication);
                    bool yubiHsmAuthEnabled = device.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.YubiHsmAuth);

                    Output.WriteLine($"YubiHSM Auth app, has feature: {yubiHsmAuthCapable}");
                    Output.WriteLine($"YubiHSM Auth app, is enabled: {yubiHsmAuthEnabled}");

                    result = yubiHsmAuthEnabled ? device.TryConnect(YubiKeyApplication.YubiHsmAuth, out _) : false;

                    if (result)
                    {
                        Output.WriteLine($"Successfully connected to YubiHSM Auth");
                    }
                    else
                    {
                        Output.WriteLine($"Failed to connect to YubiHSM Auth");
                    }

                    Output.WriteLine();
                }
            }

            return result;
        }

        private bool ListCredentials()
        {
            bool result = default;
            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(Transport.All);

            if (keys.Any())
            {
                int deviceCount = 1;
                foreach (IYubiKeyDevice device in keys)
                {
                    bool yubiHsmAuthEnabled = device.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.YubiHsmAuth);
                    if (!yubiHsmAuthEnabled)
                    {
                        continue;
                    }

                    Output.WriteLine($"\n{deviceCount++}) Using YubiKey v{device.FirmwareVersion} S/N {device.SerialNumber}...");

                    using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                    {
                        ListCredentialsCommand cmd = new ListCredentialsCommand();
                        ListCredentialsResponse response = hsmAuthConnection.SendCommand(cmd);
                        if (response.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed, response status: {response.Status}");
                            continue;
                        }

                        List<CredentialRetryPair> credRetryPairs = response.GetData();

                        Output.WriteLine($"Credential count: {credRetryPairs.Count}");
                        int credentialIndex = 1;
                        foreach (CredentialRetryPair credRetryPair in credRetryPairs)
                        {
                            Output.WriteLine($"Credential {credentialIndex++}");
                            Output.WriteLine($"\tLabel: {credRetryPair.Credential.Label}");
                            Output.WriteLine($"\tAlgorithm: {credRetryPair.Credential.KeyType}");
                            Output.WriteLine($"\tTouch: {credRetryPair.Credential.TouchRequired}");
                            Output.WriteLine($"\tRetries: {credRetryPair.Retries}");
                            Output.WriteLine();
                        }
                    }
                }

                result = true;
            }

            return result;
        }

        private bool DeleteCredential()
        {
            byte[] mgmtKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            bool result = default;
            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(Transport.All);

            if (keys.Any())
            {
                IYubiKeyDevice device = keys.First();

                Output.WriteLine($"\nUsing YubiKey v{device.FirmwareVersion} S/N {device.SerialNumber}...");

                bool yubiHsmAuthEnabled = device.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.YubiHsmAuth);
                if (!yubiHsmAuthEnabled)
                {
                    Output.WriteLine($"YubiHSM Auth not enabled. Exiting...");
                    return result;
                }

                using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                {
                    Output.WriteLine("\nBefore:");
                    if (!HelperWriteCreds(hsmAuthConnection))
                    {
                        return result;
                    }

                    List<CredentialRetryPair>? credRetryPairs = HelperGetCreds(hsmAuthConnection);
                    if (credRetryPairs is null || !credRetryPairs.Any())
                    {
                        return result;
                    }

                    CredentialRetryPair credRetryPair = credRetryPairs.First();

                    DeleteCredentialCommand cmd =
                        new DeleteCredentialCommand(mgmtKey, credRetryPair.Credential.Label);

                    Output.WriteLine($"\nAttempting to delete credential \"{cmd.Label}\"...");

                    DeleteCredentialResponse response = hsmAuthConnection.SendCommand(cmd);

                    if (response.Status != ResponseStatus.Success)
                    {
                        Output.WriteLine($"Failed to add cred, response status: {response.Status}, {response.StatusMessage}");
                        return result;
                    }

                    Output.WriteLine("\nAfter:");
                    if (!HelperWriteCreds(hsmAuthConnection))
                    {
                        return result;
                    }
                }

                result = true;
            }

            if (!result)
            {
                Output.WriteLine($"No YubiKeys found with YubiHSM Auth enabled.");
            }

            return result;
        }

        private bool HelperWriteCreds(IYubiKeyConnection hsmAuthConnection)
        {
            List<CredentialRetryPair>? credRetryPairs = HelperGetCreds(hsmAuthConnection);
            if (credRetryPairs is null)
            {
                return false;
            }

            Output.WriteLine($"Credential count: {credRetryPairs.Count}");
            int credentialIndex = 1;
            foreach (CredentialRetryPair credRetryPair in credRetryPairs)
            {
                Output.WriteLine($"Credential {credentialIndex++}) '{credRetryPair.Credential.Label}'");
            }

            Output.WriteLine();

            return true;
        }

        private List<CredentialRetryPair>? HelperGetCreds(IYubiKeyConnection hsmAuthConnection)
        {
            ListCredentialsCommand listCmd = new ListCredentialsCommand();
            ListCredentialsResponse listResponse = hsmAuthConnection.SendCommand(listCmd);
            if (listResponse.Status != ResponseStatus.Success)
            {
                Output.WriteLine($"Failed to list creds, response status: {listResponse.Status}");
                return null;
            }

            return listResponse.GetData();
        }
    }
}

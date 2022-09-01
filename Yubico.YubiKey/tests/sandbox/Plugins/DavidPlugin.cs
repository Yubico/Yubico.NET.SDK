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
using System.Net.Security;
using System.Text;
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
                "addcred" => AddCredential(),
                "testlabelstuff" => TestLabelStuff(),
                "getmgmtretries" => GetMgmtRetries(),
                "testmgmtretries" => TestMgmtRetries(),
                "appversion" => GetAppVersion(),
                "changemgmt" => ChangeManagementKey(),
                _ => throw new ArgumentException($"Invalid command [{Command}] specified")
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

        public bool AddCredential()
        {
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
                    return false;
                }

                using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                {
                    byte[] mgmtKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    byte[] password = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                    byte[] encKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                    byte[] macKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

                    string label = "test cred 1832";
                    bool touchRequired = false;

                    var aesCred = new Aes128CredentialWithSecrets(password, encKey, macKey, label, touchRequired);
                    AddCredentialCommand cmd = new AddCredentialCommand(mgmtKey, aesCred);
                    AddCredentialResponse response = hsmAuthConnection.SendCommand(cmd);

                    if (response.Status != ResponseStatus.Success)
                    {
                        Output.WriteLine($"Failed, response status: {response.Status}, {response.StatusMessage}");
                        return false;
                    }
                }

                result = true;
            }

            if (result)
            {
                Output.WriteLine($"Credential added successfully");
            }
            else
            {
                Output.WriteLine($"No YubiKeys found with YubiHSM Auth enabled.");
            }

            return result;
        }

        // Spec testing

        // Label length, incl formatting with and without trailing null character
        public bool TestLabelStuff()
        {
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
                    return false;
                }

                using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                {
                    Output.WriteLine("Label = 'a/0'");
                    LabelTesting(hsmAuthConnection, new byte[] { 0x61, 0x00 });

                    Output.WriteLine("Label = 'a'");
                    LabelTesting(hsmAuthConnection, new byte[] { 0x61 });

                    Output.WriteLine("(duplicate) Label = 'a'");
                    LabelTesting(hsmAuthConnection, new byte[] { 0x61 });

                    Output.WriteLine("Label = '/0'");
                    LabelTesting(hsmAuthConnection, new byte[] { 0x00 });

                    Output.WriteLine("Label = ''");
                    LabelTesting(hsmAuthConnection, new byte[] { });

                    Output.WriteLine("Label = '/0a'");
                    LabelTesting(hsmAuthConnection, new byte[] { 0x00, 0x61 });

                    Output.WriteLine("Label = '/0a/0'");
                    LabelTesting(hsmAuthConnection, new byte[] { 0x00, 0x61, 0x00 });

                    Output.WriteLine("Label = (63 char) + '/0'");
                    LabelTesting(hsmAuthConnection, new byte[] {
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x00,
                    });

                    Output.WriteLine("Label = (64 char) + '/0'");
                    LabelTesting(hsmAuthConnection, new byte[] {
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x00,
                    });

                    Output.WriteLine("Label = (64 char)");
                    LabelTesting(hsmAuthConnection, new byte[] {
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                    });
                }

                result = true;
            }

            if (!result)
            {
                Output.WriteLine($"No YubiKeys found with YubiHSM Auth enabled.");
            }

            return result;
        }

        private void LabelTesting(IYubiKeyConnection hsmAuthConnection, byte[] label)
        {
            byte[] mgmtKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] password = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            byte[] encKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            byte[] macKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            string strLabel = Encoding.UTF8.GetString(label);
            bool touchRequired = false;

            var aesCred = new Aes128CredentialWithSecrets(password, encKey, macKey, strLabel, touchRequired);

            Output.WriteLine($"\n\nAttempting to add credential with the following label:");
            Output.Write("Byte array: ");
            foreach (byte b in label)
            {
                Output.Write($"{b} ");
            }
            Output.Write($"\nString: '{aesCred.Label}' => ");
            foreach (byte b in Encoding.UTF8.GetBytes(aesCred.Label))
            {
                Output.Write($"{b} ");
            }

            Output.WriteLine("\nBefore:");
            if (!HelperListCreds(hsmAuthConnection))
            {
                return;
            }

            AddCredentialCommand addCmd = new AddCredentialCommand(mgmtKey, aesCred);
            AddCredentialResponse response = hsmAuthConnection.SendCommand(addCmd);

            if (response.Status != ResponseStatus.Success)
            {
                Output.WriteLine($"Failed to add cred, response status: {response.Status}, {response.StatusMessage}");
                return;
            }

            Output.WriteLine("After:");
            if (!HelperListCreds(hsmAuthConnection))
            {
                return;
            }
        }

        private bool HelperListCreds(IYubiKeyConnection hsmAuthConnection)
        {
            ListCredentialsCommand listCmd = new ListCredentialsCommand();
            ListCredentialsResponse listResponse = hsmAuthConnection.SendCommand(listCmd);
            if (listResponse.Status != ResponseStatus.Success)
            {
                Output.WriteLine($"Failed to list creds, response status: {listResponse.Status}");
                return false;
            }

            List<CredentialRetryPair> credRetryPairs = listResponse.GetData();

            Output.WriteLine($"Credential count: {credRetryPairs.Count}");
            int credentialIndex = 1;
            foreach (CredentialRetryPair credRetryPair in credRetryPairs)
            {
                Output.WriteLine($"Credential {credentialIndex++}) '{credRetryPair.Credential.Label}'");
            }

            Output.WriteLine();

            return true;
        }

        // Capacity (# of creds)

        // Add a 33rd cred (limit should be 32)
        public bool TestCredLimit()
        {
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
                    return false;
                }

                byte[] mgmtKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                byte[] password = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                byte[] encKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                byte[] macKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                string strLabel = "abc";
                bool touchRequired = false;

                var aesCred = new Aes128CredentialWithSecrets(password, encKey, macKey, strLabel, touchRequired);

                using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                {
                    ListCredentialsCommand listCmd = new ListCredentialsCommand();
                    ListCredentialsResponse listResponse = hsmAuthConnection.SendCommand(listCmd);
                    if (listResponse.Status != ResponseStatus.Success)
                    {
                        Output.WriteLine($"Failed to list creds, response status: {listResponse.Status}");
                        return false;
                    }

                    List<CredentialRetryPair> credRetryPairs = listResponse.GetData();

                    int beforeCount = credRetryPairs.Count;

                    for (int i = beforeCount + 1; i < 34; i++)
                    {
                        Output.WriteLine($"Adding cred #{i}");

                        aesCred.Label = $"Test Cred {i}";
                        AddCredentialCommand cmd = new AddCredentialCommand(mgmtKey, aesCred);
                        AddCredentialResponse response = hsmAuthConnection.SendCommand(cmd);

                        if (response.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed, response status: {response.Status}, {response.StatusMessage}");
                            return false;
                        }
                        else
                        {
                            Output.WriteLine($"Cred added successfully");
                        }
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

        private bool GetMgmtRetries()
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
                        GetManagementKeyRetriesCommand cmd = new GetManagementKeyRetriesCommand();
                        GetManagementKeyRetriesResponse response = hsmAuthConnection.SendCommand(cmd);
                        if (response.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed, response status: {response.Status}");
                            continue;
                        }

                        int retries = response.GetData();

                        Output.WriteLine($"{retries} retries remaining.");
                    }
                }

                result = true;
            }

            return result;
        }

        private bool GetAppVersion()
        {
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
                    return false;
                }

                using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                {
                    GetApplicationVersionCommand cmd = new GetApplicationVersionCommand();
                    GetApplicationVersionResponse response = hsmAuthConnection.SendCommand(cmd);

                    if (response.Status != ResponseStatus.Success)
                    {
                        Output.WriteLine($"Failed, response status: {response.Status}, {response.StatusMessage}");
                        return false;
                    }
                    else
                    {
                        Output.WriteLine($"YubiHSM Auth v{response.GetData()}");
                    }
                }

                result = true;
            }

            if (!result)
            {
                Output.WriteLine($"No YubiKeys found.");
            }

            return result;
        }

        private bool ChangeManagementKey()
        {
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
                    return false;
                }

                using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                {
                    byte[] currentManagementKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    byte[] newManagementKey = new byte[16] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4 };

                    ChangeManagementKeyCommand cmd = new ChangeManagementKeyCommand(currentManagementKey, newManagementKey);
                    ChangeManagementKeyResponse response = hsmAuthConnection.SendCommand(cmd);

                    if (response.Status != ResponseStatus.Success)
                    {
                        Output.WriteLine($"Failed changing from default to new, response status: {response.Status}, {response.StatusMessage}");
                        return false;
                    }

                    cmd = new ChangeManagementKeyCommand(newManagementKey, currentManagementKey);
                    response = hsmAuthConnection.SendCommand(cmd);

                    if (response.Status != ResponseStatus.Success)
                    {
                        Output.WriteLine($"Failed changing back to default, response status: {response.Status}, {response.StatusMessage}");
                        return false;
                    }
                }

                result = true;
            }

            if (result)
            {
                Output.WriteLine($"Management key successfully changed, and then back to default.");
            }
            else
            {
                Output.WriteLine($"No YubiKeys found with YubiHSM Auth enabled.");
            }

            return result;
        }

        private bool TestMgmtRetries()
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
                        // Get initial mgmt key retries remaining
                        GetManagementKeyRetriesCommand cmdRetries = new GetManagementKeyRetriesCommand();
                        GetManagementKeyRetriesResponse responseRetries = hsmAuthConnection.SendCommand(cmdRetries);
                        if (responseRetries.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed to get mgmt retries, response status: {responseRetries.Status}");
                            continue;
                        }

                        int retries = responseRetries.GetData();

                        Output.WriteLine($"{retries} retries remaining. About to supply WRONG management key...");

                        // Supply wrong current mgmt key 
                        byte[] currentManagementKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        byte[] newManagementKey = new byte[16] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4 };

                        ChangeManagementKeyCommand cmdChangeMgmt = new ChangeManagementKeyCommand(newManagementKey, currentManagementKey);
                        ChangeManagementKeyResponse responseChangeMgmt = hsmAuthConnection.SendCommand(cmdChangeMgmt);

                        if (responseChangeMgmt.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed change mgmt key (1), response status: {responseChangeMgmt.Status}, {responseChangeMgmt.StatusMessage}");
                        }

                        // Check mgmt retries
                        responseRetries = hsmAuthConnection.SendCommand(cmdRetries);
                        if (responseRetries.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed to get mgmt retries, response status: {responseRetries.Status}");
                            continue;
                        }

                        retries = responseRetries.GetData();

                        Output.WriteLine($"{retries} retries remaining. Now supplying CORRECT management key...");

                        // Supply correct current mgmt key 
                        cmdChangeMgmt = new ChangeManagementKeyCommand(currentManagementKey, currentManagementKey);
                        responseChangeMgmt = hsmAuthConnection.SendCommand(cmdChangeMgmt);

                        if (responseChangeMgmt.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed change mgmt key (2), response status: {responseChangeMgmt.Status}, {responseChangeMgmt.StatusMessage}");
                        }

                        // Check mgmt retries
                        responseRetries = hsmAuthConnection.SendCommand(cmdRetries);
                        if (responseRetries.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed to get mgmt retries, response status: {responseRetries.Status}");
                            continue;
                        }

                        retries = responseRetries.GetData();

                        Output.WriteLine($"{retries} retries remaining.");
                    }
                }

                result = true;
            }

            return result;
        }
    }
}

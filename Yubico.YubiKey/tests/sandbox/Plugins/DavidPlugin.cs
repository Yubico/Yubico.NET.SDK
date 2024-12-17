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
                "resetyha" => ResetYubiHsmAuth(),
                "getsessionkeys" => GetSessionKeys(),

                "sessionappmethods" => SessionAppMethods(),
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

                    if (!yubiHsmAuthEnabled)
                    {
                        device.SetEnabledUsbCapabilities(device.EnabledUsbCapabilities | YubiKeyCapabilities.YubiHsmAuth);

                        yubiHsmAuthCapable = device.HasFeature(YubiKeyFeature.YubiHsmAuthApplication);
                        yubiHsmAuthEnabled = device.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.YubiHsmAuth);

                        Output.WriteLine($"YubiHSM Auth app, has feature: {yubiHsmAuthCapable}");
                        Output.WriteLine($"YubiHSM Auth app, is enabled: {yubiHsmAuthEnabled}");
                    }

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
                        var cmd = new ListCredentialsCommand();
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
                    var cmd = new AddCredentialCommand(mgmtKey, aesCred);
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
                    LabelTesting(hsmAuthConnection, Array.Empty<byte>());

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

            var addCmd = new AddCredentialCommand(mgmtKey, aesCred);
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
            var listCmd = new ListCredentialsCommand();
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
                    var listCmd = new ListCredentialsCommand();
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
                        var cmd = new AddCredentialCommand(mgmtKey, aesCred);
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

                    var cmd =
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
            var listCmd = new ListCredentialsCommand();
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
                        var cmd = new GetManagementKeyRetriesCommand();
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
                    var cmd = new GetApplicationVersionCommand();
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

                    var cmd = new ChangeManagementKeyCommand(currentManagementKey, newManagementKey);
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
                        var cmdRetries = new GetManagementKeyRetriesCommand();
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

                        var cmdChangeMgmt = new ChangeManagementKeyCommand(newManagementKey, currentManagementKey);
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

        private bool ResetYubiHsmAuth()
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

                    byte[] mgmtKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    byte[] password = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                    byte[] encKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                    byte[] macKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                    string strLabel = "abc";
                    bool touchRequired = false;

                    var aesCred = new Aes128CredentialWithSecrets(password, encKey, macKey, strLabel, touchRequired);

                    Output.WriteLine($"\n{deviceCount++}) Using YubiKey v{device.FirmwareVersion} S/N {device.SerialNumber}...");

                    using (var yhaSession = new YubiHsmAuthSession(device))
                    {
                        if (!HelperGetCreds(yhaSession.Connection)!.Any())
                        {
                            var cmdAddCred = new AddCredentialCommand(mgmtKey, aesCred);
                            AddCredentialResponse responseAddCred = yhaSession.Connection.SendCommand(cmdAddCred);

                            if (responseAddCred.Status != ResponseStatus.Success)
                            {
                                Output.WriteLine($"Failed to add a credential, response status: {responseAddCred.Status}, {responseAddCred.StatusMessage}");
                                return false;
                            }
                        }

                        Output.WriteLine("\nBefore:");
                        if (!HelperWriteCreds(yhaSession.Connection))
                        {
                            return result;
                        }

                        yhaSession.ResetApplication();

                        Output.WriteLine("\nAfter:");
                        if (!HelperWriteCreds(yhaSession.Connection))
                        {
                            return result;
                        }
                    }
                }

                result = true;
            }

            return result;
        }

        private bool GetSessionKeys()
        {
            bool result = default;
            IEnumerable<IYubiKeyDevice> keys = YubiKeyDevice.FindByTransport(Transport.All);

            if (keys.Any())
            {
                int deviceCount = 1;
                foreach (IYubiKeyDevice device in keys)
                {
                    Output.WriteLine($"\n{deviceCount++}) Using YubiKey v{device.FirmwareVersion} S/N {device.SerialNumber}...");

                    bool yubiHsmAuthEnabled = device.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.YubiHsmAuth);
                    if (!yubiHsmAuthEnabled)
                    {
                        Output.WriteLine($"YubiHSM Auth not enabled.");
                        continue;
                    }

                    byte[] mgmtKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    byte[] password = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                    byte[] encKey = new byte[16] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };
                    byte[] macKey = new byte[16] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };
                    string strLabel = "abc";
                    bool touchRequired = false;

                    var aesCred = new Aes128CredentialWithSecrets(password, encKey, macKey, strLabel, touchRequired);

                    byte[] hostChallenge = new byte[8] { 1, 0, 1, 0, 1, 0, 1, 0 };
                    byte[] hsmDeviceChallenge = new byte[8] { 2, 4, 2, 4, 2, 4, 2, 4 };

                    using (IYubiKeyConnection hsmAuthConnection = device.Connect(YubiKeyApplication.YubiHsmAuth))
                    {
                        // Reset app
                        var cmd = new ResetApplicationCommand();
                        ResetApplicationResponse response = hsmAuthConnection.SendCommand(cmd);
                        if (response.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed to reset application, response status: {response.Status}");
                            continue;
                        }
                        else
                        {
                            Output.WriteLine("Succeeded in resetting the YubiHSM Auth application.");
                        }

                        // Add cred
                        var cmdAddCred = new AddCredentialCommand(mgmtKey, aesCred);
                        AddCredentialResponse responseAddCred = hsmAuthConnection.SendCommand(cmdAddCred);

                        if (responseAddCred.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed to add a credential, response status: {responseAddCred.Status}, {responseAddCred.StatusMessage}");
                            return false;
                        }
                        else
                        {
                            Output.WriteLine($"Succeeded in adding credential \"{strLabel}\".");
                        }

                        // Get session keys
                        var cmdGetSessionKeys = new GetAes128SessionKeysCommand(strLabel, password, hostChallenge, hsmDeviceChallenge);
                        GetAes128SessionKeysResponse rspGetSessionKeys = hsmAuthConnection.SendCommand(cmdGetSessionKeys);

                        if (responseAddCred.Status != ResponseStatus.Success)
                        {
                            Output.WriteLine($"Failed to get session keys, response status: {responseAddCred.Status}, {responseAddCred.StatusMessage}");
                            return false;
                        }
                        else
                        {
                            SessionKeys sessionKeys = rspGetSessionKeys.GetData();

                            Output.WriteLine($"Succeeded in getting session keys:");
                            Output.Write($"S-ENC:");
                            foreach (byte b in sessionKeys.EncryptionKey.Span)
                            {
                                Output.Write($" {b.ToString("X4")}");
                            }

                            Output.Write($"\nS-MAC:");
                            foreach (byte b in sessionKeys.MacKey.Span)
                            {
                                Output.Write($" {b.ToString("X4")}");
                            }

                            Output.Write($"\nS-RMAC:");
                            foreach (byte b in sessionKeys.RmacKey.Span)
                            {
                                Output.Write($" {b.ToString("X4")}");
                            }
                        }
                    }
                }

                result = true;
            }

            return result;
        }

        private bool SessionAppMethods()
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

                    using (var yhaSession = new YubiHsmAuthSession(device))
                    {
                        Output.WriteLine("Resetting YubiHSM Auth application...");
                        yhaSession.ResetApplication();
                        Output.WriteLine();
                        Output.WriteLine();
                        Output.WriteLine();

                        Output.WriteLine("Adding a credential...");
                        byte[] currentMgmtKey = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        byte[] password = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                        byte[] encKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                        byte[] macKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                        string label1 = "test cred 1";
                        string label2 = "test cred 2";
                        bool touchRequired = false;
                        var aesCred = new Aes128CredentialWithSecrets(password, encKey, macKey, label1, touchRequired);
                        yhaSession.AddCredential(currentMgmtKey, aesCred);
                        aesCred = new Aes128CredentialWithSecrets(password, encKey, macKey, label2, touchRequired);
                        yhaSession.AddCredential(currentMgmtKey, aesCred);
                        Output.WriteLine();

                        Output.WriteLine("Attempting to get list of credentials...");
                        IReadOnlyList<CredentialRetryPair>? creds = yhaSession.ListCredentials();
                        Output.WriteLine($"{creds.Count} credentials found.");
                        int credLineCount = 1;
                        foreach (CredentialRetryPair? cred in creds)
                        {
                            Output.WriteLine($"{credLineCount++}) {cred.Credential.Label}, retries = {cred.Retries}");
                        }
                        Output.WriteLine();
                        Output.WriteLine();
                        Output.WriteLine();

                        //Output.WriteLine("Blocking management key...");
                        byte[] newMgmtKey = new byte[16] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4 };
                        //int? retriesRemaining = yhaSession.GetManagementKeyRetries();
                        //while (retriesRemaining > 0)
                        //{
                        //    _ = yhaSession.TryChangeManagementKey(newMgmtKey, currentMgmtKey, out retriesRemaining);
                        //}
                        //Output.WriteLine("Management key now blocked.");
                        //Output.WriteLine();

                        //Output.WriteLine("Attempting to change mgmt key (correct current mgmt key)...");
                        //ChangeManagementKeyCommand cmdChangeMgmt = new ChangeManagementKeyCommand(currentMgmtKey, newMgmtKey);
                        //ChangeManagementKeyResponse responseChangeMgmt = yhaSession.Connection.SendCommand(cmdChangeMgmt);
                        //Output.WriteLine($"Response status: {responseChangeMgmt.Status}, {responseChangeMgmt.StatusMessage}; retries = {responseChangeMgmt.RetriesRemaining}");
                        //Output.WriteLine();
                        //Output.WriteLine();
                        //Output.WriteLine();

                        //Output.WriteLine("Attempting to get list of credentials...");
                        //creds = yhaSession.ListCredentials();
                        //Output.WriteLine($"{creds.Count} credentials found.");
                        //credLineCount = 1;
                        //foreach (var cred in creds)
                        //{
                        //    Output.WriteLine($"{credLineCount++}) {cred.Credential.Label}, retries = {cred.Retries}");
                        //}
                        //Output.WriteLine();

                        //string targetCredLabel = creds.First().Credential.Label;
                        //Output.WriteLine($"Attempting to get session keys from {targetCredLabel}...");
                        byte[] hostChallenge = new byte[8] { 1, 0, 1, 0, 1, 0, 1, 0 };
                        byte[] hsmDeviceChallenge = new byte[8] { 2, 4, 2, 4, 2, 4, 2, 4 };
                        //_ = yhaSession.GetAes128SessionKeys(targetCredLabel, password, hostChallenge, hsmDeviceChallenge);
                        //Output.WriteLine("Successfully retrieved session keys.");
                        //Output.WriteLine();

                        //Output.WriteLine($"Blocking cred {targetCredLabel}");
                        //retriesRemaining = creds.First(cred => cred.Credential.Label == targetCredLabel).Retries;
                        //GetAes128SessionKeysCommand getSessionKeys = new GetAes128SessionKeysCommand(targetCredLabel, newMgmtKey, hostChallenge, hsmDeviceChallenge);
                        //GetAes128SessionKeysResponse responseSessionKeys;
                        //while (retriesRemaining > 0)
                        //{
                        //    // Pass in wrong password to block cred
                        //    responseSessionKeys = yhaSession.Connection.SendCommand(getSessionKeys);
                        //    Output.WriteLine($"Response status: {responseSessionKeys.Status}, {responseSessionKeys.StatusMessage}; retries = {responseSessionKeys.RetriesRemaining}");

                        //    retriesRemaining = responseSessionKeys.RetriesRemaining;
                        //}
                        //Output.WriteLine($"Credential {targetCredLabel} is now blocked.");
                        //Output.WriteLine();

                        //Output.WriteLine("Attempting to get list of credentials...");
                        //creds = yhaSession.ListCredentials();
                        //Output.WriteLine($"{creds.Count} credentials found.");
                        //credLineCount = 1;
                        //foreach (var cred in creds)
                        //{
                        //    Output.WriteLine($"{credLineCount++}) {cred.Credential.Label}, retries = {cred.Retries}");
                        //}
                        //Output.WriteLine();

                        //Output.WriteLine($"Attempting to get session keys from {targetCredLabel} (correct password)...");
                        //getSessionKeys = new GetAes128SessionKeysCommand(targetCredLabel, password, hostChallenge, hsmDeviceChallenge);
                        //responseSessionKeys = yhaSession.Connection.SendCommand(getSessionKeys);
                        //Output.WriteLine($"Response status: {responseSessionKeys.Status}, {responseSessionKeys.StatusMessage}; retries = {responseSessionKeys.RetriesRemaining}");
                        //Output.WriteLine();
                        //Output.WriteLine();

                        string targetCredLabel = creds[0].Credential.Label;
                        Output.WriteLine($"Reducing {targetCredLabel} retries to 6...");
                        int? retriesRemaining = creds.First(cred => cred.Credential.Label == targetCredLabel).Retries;
                        var getSessionKeys = new GetAes128SessionKeysCommand(targetCredLabel, newMgmtKey, hostChallenge, hsmDeviceChallenge);
                        GetAes128SessionKeysResponse responseSessionKeys;
                        while (retriesRemaining > 6)
                        {
                            // Pass in wrong password to cred
                            responseSessionKeys = yhaSession.Connection.SendCommand(getSessionKeys);
                            Output.WriteLine($"Response status: {responseSessionKeys.Status}, {responseSessionKeys.StatusMessage}; retries = {responseSessionKeys.RetriesRemaining}");

                            retriesRemaining = responseSessionKeys.RetriesRemaining;
                        }
                        Output.WriteLine($"Cred {targetCredLabel} now has {retriesRemaining} retries remaining.");
                        Output.WriteLine();

                        Output.WriteLine("Attempting to get list of credentials...");
                        creds = yhaSession.ListCredentials();
                        Output.WriteLine($"{creds.Count} credentials found.");
                        credLineCount = 1;
                        foreach (CredentialRetryPair? cred in creds)
                        {
                            Output.WriteLine($"{credLineCount++}) {cred.Credential.Label}, retries = {cred.Retries}");
                        }
                        Output.WriteLine();

                        //Output.WriteLine($"Attempting to get session keys from {targetCredLabel} (correct password)...");
                        //getSessionKeys = new GetAes128SessionKeysCommand(targetCredLabel, password, hostChallenge, hsmDeviceChallenge);
                        //responseSessionKeys = yhaSession.Connection.SendCommand(getSessionKeys);
                        //Output.WriteLine($"Response status: {responseSessionKeys.Status}, {responseSessionKeys.StatusMessage}; retries = {responseSessionKeys.RetriesRemaining}");
                        //Output.WriteLine();

                        Output.WriteLine("Attempting to change mgmt key (correct current mgmt key)...");
                        var cmdChangeMgmt = new ChangeManagementKeyCommand(currentMgmtKey, newMgmtKey);
                        ChangeManagementKeyResponse responseChangeMgmt = yhaSession.Connection.SendCommand(cmdChangeMgmt);
                        Output.WriteLine($"Response status: {responseChangeMgmt.Status}, {responseChangeMgmt.StatusMessage}; retries = {responseChangeMgmt.RetriesRemaining}");
                        Output.WriteLine();

                        Output.WriteLine("Attempting to get list of credentials...");
                        creds = yhaSession.ListCredentials();
                        Output.WriteLine($"{creds.Count} credentials found.");
                        credLineCount = 1;
                        foreach (CredentialRetryPair? cred in creds)
                        {
                            Output.WriteLine($"{credLineCount++}) {cred.Credential.Label}, retries = {cred.Retries}");
                        }
                        Output.WriteLine();
                    }
                }

                result = true;
            }

            return result;
        }
    }
}

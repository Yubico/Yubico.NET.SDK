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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.U2f;
using Yubico.YubiKey.U2f.Commands;

namespace Yubico.YubiKey.TestApp.Plugins
{
    class U2fPlugin : PluginBase
    {
        public override string Name => "U2f";

        public override string Description => "Runs U2f tests.";

        public U2fPlugin(IOutput output) : base(output)
        {
            Parameters["command"].Description =
                "[command] The U2f command case to run. Current choices are EchoTest and "
                + "RegisterCommand. If not specified, both are run.";
        }

        public override bool Execute()
        {
            // Note: on Windows, you must be running as administrator to run this test.
            if (!IsAdministrator)
            {
                throw new InvalidOperationException("You must run this plugin as an administrator");
            }

            IList<IYubiKeyDevice> keys = YubiKeyDevice.FindAll().ToList();
            for (int i = 0; i < keys.Count; ++i)
            {
                IYubiKeyDevice key = keys[i];

                Output.WriteLine($"YubiKey #{i + 1} [ Serial number: {key.SerialNumber} ]");

                using (IYubiKeyConnection u2fConnection = key.Connect(YubiKeyApplication.FidoU2f))
                {
                    var command = new VersionCommand();
                    VersionResponse response = u2fConnection.SendCommand(command);
                    FirmwareVersion version = response.GetData();
                    Output.WriteLine($"FWV   : {version.Major}.{version.Minor}.{version.Patch}");
                }

                if (_runEchoTest)
                {
                    using (IYubiKeyConnection u2fConnection = key.Connect(YubiKeyApplication.FidoU2f))
                    {
                        EchoCommandTestSuite(u2fConnection);
                    }
                }
                if (_runRegisterTest)
                {
                    using (IYubiKeyConnection u2fConnection = key.Connect(YubiKeyApplication.FidoU2f))
                    {
                        byte[] challenge = new byte[32];
                        RandomNumberGenerator.Fill(challenge);
                        byte[] appId = new byte[32];
                        RandomNumberGenerator.Fill(appId);
                        var registerCommand = new RegisterCommand(challenge, appId);

                        RegisterResponse registerResponse;
                        do
                        {
                            registerResponse = u2fConnection.SendCommand(registerCommand);
                        } while (registerResponse.Status == ResponseStatus.ConditionsNotSatisfied);

                        RegistrationData registrationData = registerResponse.GetData();

                        Output.WriteLine($"verifies?: {registrationData.IsSignatureValid(challenge, appId)}");
                    }
                }
            }
            return true;
        }

        public override void HandleParameters()
        {
            switch (Command.ToLower())
            {
                case "echotest":
                    _runEchoTest = true;
                    break;
                case "registertest":
                    _runRegisterTest = true;
                    break;
                case "":
                    _runEchoTest = true;
                    _runRegisterTest = true;
                    break;
                default:
                    throw new ArgumentException($"[{ Command }] is not a valid command for this plugin");
            }
        }

        private bool _runEchoTest, _runRegisterTest;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void EchoCommandTestSuite(IYubiKeyConnection u2fConnection)
        {
            Output.WriteLine("\n\n***TestEchoCommand***");

            // Run each of the tests
            // -Default constructor
            TestEchoCommand(u2fConnection);

            // -0 length
            TestEchoCommand(u2fConnection, 0);

            // -1 length
            TestEchoCommand(u2fConnection, 1);

            // -n length
            foreach (int l in Enumerable.Range(1, 10))
            {
                TestEchoCommand(u2fConnection, (int)Math.Pow(2, l));
            }
        }

        private void TestEchoCommand(IYubiKeyConnection u2fConnection, int? dataLength = null)
        {
            // Create data to echo
            byte[]? sendData = GenerateRandBytes(dataLength);

            // Create echo command
            EchoCommand echoCommand = sendData is null ? new EchoCommand() : new EchoCommand(sendData);

            // Send command, get response
            EchoResponse echoResponse = u2fConnection.SendCommand(echoCommand);

            // Get data out of response
            ReadOnlyMemory<byte> echoData = echoResponse.GetData();

            // Check that response data matches sent data
            Output.Write($"Test Length = {dataLength,5} || ");
            _ = TestSpanSequenceEqual(sendData, echoData.Span);
            Output.WriteLine();
        }

        private static byte[]? GenerateRandBytes(int? length)
        {
            if (length is null)
            {
                return null;
            }

            byte[] randBytes = new byte[(int)length];
            RandomNumberGenerator.Fill(randBytes);
            return randBytes;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private bool TestSpanSequenceEqual<T>(ReadOnlySpan<T> span1, ReadOnlySpan<T> span2) where T : System.IComparable<T>
        {
            if (span1.Length != span2.Length)
            {
                Output.WriteLine("FAIL: Sequence lengths not equal.");
                return false;
            }

            for (int i = 0; i < span1.Length; i++)
            {
                if (span1[i].CompareTo(span2[i]) != 0)
                {
                    Output.WriteLine($"FAIL: Sequence does not match at index {i}.");
                    PrintContext(span1, span2, i, 5);
                    return false;
                }
            }

            Output.WriteLine("PASS");
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void PrintContext<T>(ReadOnlySpan<T> span1, ReadOnlySpan<T> span2, int? index, int plusMinusContext = 0)
        {
            int startIndex, endIndex;
            int largerLength = span1.Length > span2.Length ? span1.Length : span2.Length;

            if (index is null)
            {
                startIndex = 0;
                endIndex = largerLength;
            }
            else
            {
                startIndex = Math.Max((int)index - plusMinusContext, 0);
                endIndex = Math.Min((int)index + plusMinusContext, largerLength);
            }

            Output.WriteLine($"|{"Index",7}|{"span1",7}|{"span2",7}|");

            for (int i = startIndex; i < endIndex; i++)
            {
                Output.WriteLine
                    (
                        $"|{i,7}" +
                        $"|{(i < span1.Length ? span1[i]?.ToString() : "--"),7}" +
                        $"|{(i < span2.Length ? span2[i]?.ToString() : "--"),7}|"
                    );
            }
        }
    }
}

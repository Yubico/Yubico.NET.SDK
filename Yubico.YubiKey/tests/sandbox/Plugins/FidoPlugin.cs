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
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class FidoPlugin : PluginBase
    {
        public override string Name => "FIDO";

        public override string Description => "Do Fido-type things. Note, this must be run as administrator.";

        // This plugin doesn't seem to do anything configurable for now.
        public override Dictionary<string, Parameter> Parameters { get; } =
            new Dictionary<string, Parameter>();

        public FidoPlugin(IOutput output) : base(output) { }

        // No-op.
        public override void HandleParameters() { }

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

                using (IYubiKeyConnection fidoConnection = key.Connect(YubiKeyApplication.Fido2))
                {
                    var command = new VersionCommand();
                    VersionResponse response = fidoConnection.SendCommand(command);
                    FirmwareVersion version = response.GetData();
                    Output.WriteLine($"FWV   : {version.Major}.{version.Minor}.{version.Patch}");

                    GetInfoResponse response2 = fidoConnection.SendCommand(new GetInfoCommand());
                    DeviceInfo deviceInfo = response2.GetData();
                    Output.WriteLine($"ctap2 : {string.Join(',', deviceInfo.Versions)} "
                        + $"{BitConverter.ToString(deviceInfo.AAGuid)} opts: {deviceInfo.Options}");

                    //
                    // Run Fido tests
                    //
                    TestCredentialSetGet(fidoConnection);
                }
            }

            return true;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void TestCredentialSetGet(IYubiKeyConnection FidoConnection)
        {
            Output.WriteLine(Eol + Eol + "***TestFidoCredentialSetGet***");

            // Reset fido app
            Output.WriteLine(Eol + "Resetting Fido app...");
            ResetApp(FidoConnection);

            // Load new credentials
            Output.WriteLine(Eol + "Loading new credentials...");
            int qtyCredentials = 4;
            List<MakeCredentialInput> loadedCredentials = LoadNewCredentials(FidoConnection, qtyCredentials);

            // Retrieve all credentials
            Output.WriteLine(Eol + "Retrieving all credentials...");
            string relyingPartyId = loadedCredentials[0].RelyingParty.Id;
            byte[] clientDataHash = loadedCredentials[0].ClientDataHash;
            List<GetAssertionOutput> retrievedCredentials = RetrieveAllCredentials(FidoConnection, relyingPartyId, clientDataHash);

            // Test that loaded credentials match retrieved credentials
            Output.WriteLine(Eol + "Testing whether loaded credentials matches retrieved credentials...");
            CredentialListsMatchByUserId(loadedCredentials, retrievedCredentials);

            Output.WriteLine(Eol + Eol + "");
        }

        private List<MakeCredentialInput> LoadNewCredentials(IYubiKeyConnection FidoConnection, int qtyCredentials)
        {
            // Generate credential inputs where the RP is the same, but the user is different
            var makeCredentialInputs = new List<MakeCredentialInput>(qtyCredentials);
            for (int j = 0; j < qtyCredentials; j++)
            {
                makeCredentialInputs.Add(GenerateMakeCredentialInput("", null, j.ToString(CultureInfo.InvariantCulture)));
            }

            // Create make credential commands from set of credential inputs
            var makeCredentialCommands = new List<MakeCredentialCommand>(makeCredentialInputs.Count);
            foreach (MakeCredentialInput input in makeCredentialInputs)
            {
                makeCredentialCommands.Add(new MakeCredentialCommand(input));
            }

            // Send commands to make credentials, and store responses
            var makeCredentialResponses = new List<MakeCredentialResponse>(makeCredentialCommands.Count);
            int commandCount = 1;
            foreach (MakeCredentialCommand cmd in makeCredentialCommands)
            {
                Output.WriteLine($"Sending MakeCredential Command #{commandCount}...");
                makeCredentialResponses.Add(FidoConnection.SendCommand(cmd));
                commandCount++;
            }

            // Get the outputs from the command responses
            var makeCredentialOutputs = new List<IMakeCredentialOutput>(makeCredentialResponses.Count);
            foreach (MakeCredentialResponse mcResponse in makeCredentialResponses)
            {
                makeCredentialOutputs.Add(mcResponse.GetData());
            }

            //if (mco is MakeCredentialOutput<PackedAttestation> mcop)
            //{
            //    Output.WriteLine($"mc out: fmt:{mcop.AttestationFormatIdentifier} auth:{Hex.BytesToHex(mcop.AuthenticatorData)} attstmt: (alg:{mcop.AttestationStatement.Algorithm} " +
            //        $"sig:{Hex.BytesToHex(mcop.AttestationStatement.Signature)} x5c:{Hex.BytesToHex(mcop.AttestationStatement.X509Certificates[0].RawData)}");
            //}

            return makeCredentialInputs;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private List<GetAssertionOutput> RetrieveAllCredentials(IYubiKeyConnection FidoConnection, string RelyingPartyId, byte[] ClientDataHash)
        {
            // Generate valid assertion input
            GetAssertionInput validGetAssertionInput = GenerateValidGetAssertionInput(RelyingPartyId, ClientDataHash);

            // Send command for assertion, and save responses
            Output.WriteLine("Sending GetAssertion Command...");
            GetAssertionResponse getAssertionResponse = FidoConnection.SendCommand(new GetAssertionCommand(validGetAssertionInput));

            // Get outputs from command responses
            var getAssertionOutputs = new List<GetAssertionOutput>
                    {
                        getAssertionResponse.GetData()
                    };

            int credentialCount = getAssertionOutputs[0].NumberOfCredentials ?? 1;
            for (int j = 0; j < credentialCount - 1; j++)
            {
                Output.WriteLine($"Sending GetNextAssertion Command for credential #{j + 2}...");
                getAssertionResponse = FidoConnection.SendCommand(new GetNextAssertionCommand());
                getAssertionOutputs.Add(getAssertionResponse.GetData());
            }

            //Output.WriteLine($"ga out: auth:{Hex.BytesToHex(gao.AuthenticatorData)} sig:{Hex.BytesToHex(gao.Signature)}");

            return getAssertionOutputs;
        }

        private static MakeCredentialInput GenerateMakeCredentialInput(string RpPostfix, byte[]? UserId, string UserPostfix)
        {
            return new MakeCredentialInput()
            {
                ClientDataHash = GenerateRandBytes(32),
                RelyingParty = new RelyingParty()
                {
                    Name = $"Acme{RpPostfix}",
                    Id = $"example{RpPostfix}.com"
                },
                User = new PublicKeyCredentialUserEntity()
                {
                    Id = UserId ?? GenerateRandBytes(64),
                    //Icon = "https://pics.example.com/00/p/aBjjjpqPb.png",
                    Name = $"johnpsmith{UserPostfix}@example.com",
                    DisplayName = $"John P. Smith {UserPostfix}"
                },
                PublicKeyCredentialParameters = new PublicKeyCredentialParameter[]
                {
                            new PublicKeyCredentialParameter()
                            {
                                Algorithm = CoseAlgorithmIdentifier.ES256,
                                Type = "public-key"
                            }
                },
                Options = new Dictionary<string, bool>()
                    {
                        { "rk", true }
                    }
            };
        }

        private static GetAssertionInput GenerateValidGetAssertionInput(string RelyingPartyId, byte[] ClientDataHash)
        {
            return new GetAssertionInput
            {
                RelyingPartyId = RelyingPartyId,
                ClientDataHash = ClientDataHash,
                AllowList = null,
                Extensions = null,
                Options = null,
                PinUserVerificationAuthenticatorParameter = null,
                PinUserVerificationAuthenticatorProtocol = null,
            };
        }

        /// <summary>
        /// Must be called within 10 seconds of authenticator powerup
        /// </summary>
        /// <param name="FidoConnection"></param>
        private static void ResetApp(IYubiKeyConnection FidoConnection)
        {
            Fido2Response resetResponse = FidoConnection.SendCommand(new ResetCommand());

            resetResponse.ThrowIfFailed();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private void CredentialListsMatchByUserId(List<MakeCredentialInput> makeCredentialInputs, List<GetAssertionOutput> getAssertionOutputs)
        {
            if (makeCredentialInputs.Count != getAssertionOutputs.Count)
            {
                Output.WriteLine("Counts of credentials do not match.");
                Output.WriteLine($"# inputs:  {makeCredentialInputs.Count}");
                Output.WriteLine($"# outputs: {getAssertionOutputs.Count}");
            }
            else
            {
                Output.WriteLine($"Counts of credentials match (count = {makeCredentialInputs.Count})");
            }

            foreach (MakeCredentialInput input in makeCredentialInputs)
            {
                Output.WriteLine(Eol + string.Format("Input credential \"{0}\" exists (by User.ID): {1}",
                    input.User.DisplayName,
                    getAssertionOutputs.Exists(x => !(x.User is null) && Enumerable.SequenceEqual(x.User.Id, input.User.Id))));
            }
        }

        private static byte[] GenerateRandBytes(int length)
        {
            byte[] randBytes = new byte[length];
            RandomNumberGenerator.Fill(randBytes);
            return randBytes;
        }
    }
}

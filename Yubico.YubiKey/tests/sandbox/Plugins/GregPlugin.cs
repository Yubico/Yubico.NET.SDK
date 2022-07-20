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
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Yubico.Core.Logging;

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class GregPlugin : PluginBase
    {
        public override string Name => "Greg";
        public override string Description => "A place for Greg's test code";

        public GregPlugin(IOutput output) : base(output) { }

        public override bool Execute()
        {
            Log.LoggerFactory = LoggerFactory.Create(
                builder => builder.AddSimpleConsole(
                        options =>
                        {
                            options.IncludeScopes = true;
                            options.SingleLine = true;
                            options.TimestampFormat = "hh:mm:ss";
                        })
                    .AddFilter(level => level >= LogLevel.Information));

            var yubiKey = YubiKeyDevice.FindAll().First();

            Thread.Sleep(3000);

            using (var connection = yubiKey.Connect(YubiKeyApplication.Fido2))
            {
                var response = connection.SendCommand(new Fido2.Commands.GetUvRetriesCommand());

                Console.WriteLine($"FIDO2 UV retries: {response.GetData()}");
            }

            return true;
        }
    }
}

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
using Yubico.PlatformInterop;

// Feature hold-back
#if false

namespace Yubico.YubiKey.TestApp.Plugins
{
    internal class SCardListenerPlugin : PluginBase
    {
        public override string Name => "SCardListener";
        public override string Description => "A place for SCardListener test code";

        public SCardListenerPlugin(IOutput output) : base(output) { }

        public override bool Execute()
        {
            SCardListener sCardListener = new SCardListener();

            sCardListener.CardArrival += (s, e) =>
            {
                Console.WriteLine("Card arrived:");
                Console.WriteLine(e.ReaderName);
                Console.WriteLine(e.Atr.ToString());
            };


            sCardListener.CardRemoval += (s, e) =>
            {
                Console.WriteLine("Card removed:");
                Console.WriteLine(e.ReaderName);
                Console.WriteLine(e.Atr.ToString());
            };

            _ = Console.ReadLine();

            sCardListener.StopListening();

            return true;
        }
    }
}

#endif

// Copyright 2025 Yubico AB
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
// ReSharper disable once RedundantUsingDirective Used on line 44
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.Fido2SampleCode
{
    internal sealed class StartProgram
    {
        // To run this sample as a command-line application, simply run the
        // executable created.
        //   $ Fido2Sample
        // To run it as a GUI application, run the executable with an argument of g.
        //   $ Fido2Sample g
        // Note that the GUI version is available only on Windows platforms.
        static void Main(string[] args)
        {
            bool useGui = false;

            if (args.Length != 0)
            {
                useGui = args[0].Equals("g", StringComparison.OrdinalIgnoreCase);
            }

            if (useGui)
            {
#if WINDOWS
                using var fido2SampleGui = new Fido2SampleGui();
                fido2SampleGui.RunSample();
#else
                SampleMenu.WriteMessage(
                    MessageType.Title, 0,
                    "\n---The GUI version of this sample is not available on this plaform---\n");
#endif
            }
            else
            {
                var fido2SampleRun = new Fido2SampleRun(maxInvalidCount: 2);
#if WINDOWS
                fido2SampleRun.RunSample();
#else
                fido2SampleRun.RunSample(false);
#endif
            }
        }
    }
}

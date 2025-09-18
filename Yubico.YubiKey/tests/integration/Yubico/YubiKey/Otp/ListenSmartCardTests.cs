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

using Xunit;
using Xunit.Abstractions;
using Yubico.Core.Devices.SmartCard;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.PlatformInterop;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class ListenSmartCardTests
{
    private readonly ITestOutputHelper _output;
    private int _counter;

    public ListenSmartCardTests(
        ITestOutputHelper output)
    {
        _output = output;
        _counter = 5;
    }

    [Fact]
    public void SmartCardDeviceListen_Succeeds()
    {
        var listener = SmartCardDeviceListener.Create();
        listener.Arrived += HandleEventFromListener;

        int choice;
        do
        {
            choice = RunMenu();
            _output.WriteLine("  choice = " + choice);
        } while (choice != 0);
    }

    private void HandleEventFromListener(
        object? sender,
        SmartCardDeviceEventArgs eventArgs)
    {
        _output.WriteLine("    eventArgs.Device = " + eventArgs.Device);
    }

    // This simulates a menu. There's no ReadLine (or some such) in XUnit, so
    // run this in debug mode to be able to pause execution so we can insert
    // and remove YubiKeys to watch what happens.
    private int RunMenu()
    {
        if (_counter == 0)
        {
            _counter = 6;
        }
        else if (_counter > 6 || _counter < 0)
        {
            _counter = 1;
        }

        _counter--;
        _output.WriteLine("Enter positive integer or 0 to quit");
        _output.WriteLine(_counter.ToString());

        return _counter;
    }
}

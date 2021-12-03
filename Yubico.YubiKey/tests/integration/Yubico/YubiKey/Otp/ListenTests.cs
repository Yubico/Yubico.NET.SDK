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

// Feature hold-back
#if false
using System;
using System.Collections.Generic;
using Yubico.Core.Devices.Hid;
using Xunit;
using Xunit.Abstractions;

namespace Yubico.PlatformInterop
{
    public class ListenTests
    {
        private int _counter;
        private readonly ITestOutputHelper _output;

        public ListenTests(ITestOutputHelper output)
        {
            _output = output;
            _counter = 5;
        }

        [Fact]
        public void SCardListen_Succeeds()
        {
            using var scardListener = new SCardListener();
            scardListener.CardArrival += HandleEventFromSCard;

            int choice;
            do
            {
                choice = RunMenu();
                _output.WriteLine("  choice = " + choice);
            } while (choice != 0);
        }

        private void HandleEventFromSCard(object? sender, SCardEventArgs eventArgs)
        {
            _output.WriteLine("    eventArgs.ReaderName = " + eventArgs.ReaderName);
        }

        [Fact]
        public void UdevListen_Succeeds()
        {
            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            using var udevListener = new LinuxUdevListener();
            Assert.NotNull(udevListener);
            udevListener.CardArrival += HandleEventFromUdev;
            udevListener.CardRemoval += HandleEventFromUdev;
            udevListener.StartListening();

            int choice;
            do
            {
                choice = RunMenu();
                _output.WriteLine("  choice = " + choice);
            } while (choice != 0);

            udevListener.StartListening();
            udevListener.StopListening();
            udevListener.StopListening();
            udevListener.StartListening();

            devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);
        }

        private void HandleEventFromUdev(object? sender, LinuxUdevEventArgs eventArgs)
        {
            _output.WriteLine("    eventArgs.Path = " + eventArgs.Device.Path);
        }

        [Fact]
        public void CmListen_Succeeds()
        {
            IEnumerable<HidDevice> devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);

            using var cmListener = new CmDeviceListener(CmInterfaceGuid.Hid);
            Assert.NotNull(cmListener);
            cmListener.DeviceArrived += HandleEventFromCm;
            cmListener.DeviceRemoved += HandleEventFromCm;

            int choice;
            do
            {
                choice = RunMenu();
                _output.WriteLine("  choice = " + choice);
            } while (choice != 0);

            devices = HidDevice.GetHidDevices();
            Assert.NotNull(devices);
        }

        private void HandleEventFromCm(object? sender, CmDeviceEventArgs eventArgs)
        {
            _output.WriteLine("    eventArgs.Path = " + eventArgs.DeviceInterfacePath);
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
            else if ((_counter > 6) || (_counter < 0))
            {
                _counter = 1;
            }

            _counter--;
            _output.WriteLine("Enter positive integer or 0 to quit");
            _output.WriteLine(_counter.ToString());

            return _counter;
        }
    }
}
#endif

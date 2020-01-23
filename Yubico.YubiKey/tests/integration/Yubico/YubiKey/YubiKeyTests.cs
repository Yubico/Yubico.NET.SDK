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
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Yubico.YubiKey
{
    public class YubiKeyTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public YubiKeyTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void GetYubiKeys_NoneTransport_ThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => YubiKeyDevice.FindByTransport(Transport.None).ToList());
        }

        [Fact]
        public void GetYubiKeys_ExplicitAllTransports_MoreThanOneConnectedKey()
        {
            var keys = YubiKeyDevice.FindByTransport(Transport.All).ToList();

            foreach (IYubiKeyDevice key in keys)
            {
                var concrete = key as YubiKeyDevice;
                Assert.NotNull(concrete);
                Assert.True(concrete!.HasHidFido || concrete.HasHidKeyboard || concrete.HasSmartCard);
            }

            Assert.True(keys.Count > 1);
        }

        [Fact]
        // Good test for checking matching logic.
        public void GetYubiKeys_ExplicitAllTransports_OneConnectedKey()
        {
            var keys = YubiKeyDevice.FindByTransport(Transport.All).ToList();

            foreach (IYubiKeyDevice key in keys)
            {
                var concrete = key as YubiKeyDevice;
                Assert.NotNull(concrete);
                Assert.True(concrete!.HasHidFido || concrete.HasHidKeyboard || concrete.HasSmartCard);
            }

            Assert.True(keys.Count == 1);
        }

        [Fact]
        public void GetYubiKeys_DefaultTransport_MoreThanOneConnectedKey()
        {
            var keys = YubiKeyDevice.FindByTransport().ToList();

            foreach (IYubiKeyDevice key in keys)
            {
                var concrete = key as YubiKeyDevice;
                Assert.NotNull(concrete);
                Assert.True(concrete!.HasHidFido || concrete.HasHidKeyboard || concrete.HasSmartCard);
            }

            Assert.True(keys.Count > 1);
        }

        [Fact]
        // Good test for checking matching logic.
        public void GetYubiKeys_DefaultTransport_OneConnectedKey()
        {
            var keys = YubiKeyDevice.FindByTransport().ToList();

            foreach (IYubiKeyDevice key in keys)
            {
                var concrete = key as YubiKeyDevice;
                Assert.NotNull(concrete);
                Assert.True(concrete!.HasHidFido || concrete.HasHidKeyboard || concrete.HasSmartCard);
            }

            Assert.True(keys.Count == 1);
        }

        [Theory]
        //[InlineData(Transport.HidFido)]
        [InlineData(Transport.HidKeyboard)]
        [InlineData(Transport.UsbSmartCard)]
        [InlineData(Transport.NfcSmartCard)]
        [InlineData(Transport.SmartCard)]
        public void GetYubiKeys_SingleTransport_MoreThanOneConnectedKey(Transport transport)
        {
            var keys = YubiKeyDevice.FindByTransport(transport).ToList();

            foreach (IYubiKeyDevice key in keys)
            {
                var concrete = key as YubiKeyDevice;
                Assert.NotNull(concrete);

                switch (transport)
                {
                    case Transport.HidFido:
                        Assert.True(concrete!.HasHidFido && !concrete.HasHidKeyboard && !concrete.HasSmartCard);
                        break;
                    case Transport.HidKeyboard:
                        Assert.True(!concrete!.HasHidFido && concrete.HasHidKeyboard && !concrete.HasSmartCard);
                        break;
                    case Transport.UsbSmartCard:
                    case Transport.NfcSmartCard:
                    case Transport.SmartCard:
                        Assert.True(!concrete!.HasHidFido && !concrete.HasHidKeyboard && concrete.HasSmartCard);
                        break;
                }
            }

            Assert.True(keys.Count > 1);
        }

        [Theory]
        //[InlineData(Transport.HidFido)]
        [InlineData(Transport.HidKeyboard)]
        [InlineData(Transport.UsbSmartCard)]
        [InlineData(Transport.NfcSmartCard)]
        [InlineData(Transport.SmartCard)]
        public void GetYubiKeys_SingleTransport_OneConnectedKey(Transport transport)
        {
            var keys = YubiKeyDevice.FindByTransport(transport).ToList();

            foreach (IYubiKeyDevice key in keys)
            {
                var concrete = key as YubiKeyDevice;
                Assert.NotNull(concrete);

                switch (transport)
                {
                    case Transport.HidFido:
                        Assert.True(concrete!.HasHidFido && !concrete.HasHidKeyboard && !concrete.HasSmartCard);
                        break;
                    case Transport.HidKeyboard:
                        Assert.True(!concrete!.HasHidFido && concrete.HasHidKeyboard && !concrete.HasSmartCard);
                        break;
                    case Transport.UsbSmartCard:
                    case Transport.NfcSmartCard:
                    case Transport.SmartCard:
                        Assert.True(!concrete!.HasHidFido && !concrete.HasHidKeyboard && concrete.HasSmartCard);
                        break;
                }
            }

            Assert.True(keys.Count == 1);
        }

        [Theory]
        [InlineData(Transport.All)]
        //[InlineData(Transport.HidFido)]
        [InlineData(Transport.HidKeyboard)]
        [InlineData(Transport.UsbSmartCard)]
        [InlineData(Transport.NfcSmartCard)]
        [InlineData(Transport.SmartCard)]
        public void GetYubiKeys_ExplicitTransport_ZeroConnectedKeys(Transport transport)
        {
            var keys = YubiKeyDevice.FindByTransport(transport).ToList();

            Assert.True(keys.Count == 0);
        }

        [Fact]
        public void GetYubiKeys_DefaultTransport_ZeroConnectedKeys()
        {
            var keys = YubiKeyDevice.FindByTransport().ToList();

            Assert.True(keys.Count == 0);
        }

        [Fact]
        public void GetYubiKeys_SingleTransport_RapidSwitching()
        {
            int numberOfRounds = 40;

            var rand = new Random();
            var transportValues =
                new Transport[] { /*Transport.HidFido,*/ Transport.HidKeyboard, Transport.SmartCard };

            var transportTestValues = new Transport[numberOfRounds];
            for (int i = 0; i < transportTestValues.Length; i++)
            {
                int randIndex = rand.Next(transportValues.Length);
                transportTestValues[i] = transportValues[randIndex];
            }

            var sw = new Stopwatch();

            List<IYubiKeyDevice> keys;
            int n = 0;
            foreach (Transport ct in transportTestValues)
            {
                _testOutputHelper.WriteLine("{0,-5}{1}",
                    $"{++n}:",
                    $"{Enum.GetName(typeof(Transport), ct) ?? "<null>"}");
                sw.Restart();
                keys = YubiKeyDevice.FindByTransport(ct).ToList();
                sw.Stop();
                _testOutputHelper.WriteLine($"\t({keys.Count}) -{sw.ElapsedMilliseconds, 5}ms");
            }
        }
    }
}

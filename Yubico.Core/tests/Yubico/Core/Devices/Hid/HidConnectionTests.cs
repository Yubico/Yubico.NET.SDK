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

namespace Yubico.Core.Devices.Hid.UnitTests
{

#if false
    public class HidConnectionTests
    {
        private static IHidDDevice GetMockedDevice()
        {
            var mock = Substitute.For<IHidDDevice>();
            return mock;
        }

        private static byte[] GetFeatureReport() => Hex.HexToBytes("000102030405060708090A0B0C0D0E0F");
        private static byte[] GetInputReport() => Hex.HexToBytes("0001020304050607");
        private static byte[] GetOutputReport() => Hex.HexToBytes("00010203");

        private static IHidDDevice GetMockedFeatureDevice()
        {
            var mock = Substitute.For<IHidDDevice>();
            _ = mock.When(hdd => hdd.FeatureReportByteLength).Returns(16);
            _ = mock.InputReportByteLength).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.OutputReportByteLength).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.GetInputReport()).Throw(new Exception());
            _ = mock.When(hdd => hdd.GetFeatureReport().Returns(GetFeatureReport());
            return mock;
        }

        private static IHidDDevice GetMockedIODevice()
        {
            var mock = Substitute.For<IHidDDevice>();
            _ = mock.InputReportByteLength.Returns(8);
            _ = mock.OutputReportByteLength.Returns(4);
            _ = mock.FeatureReportByteLength).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.GetFeatureReport()).Throw(new Exception());
            _ = mock.Setup(hdd => hdd.GetInputReport().Returns(GetInputReport());
            return mock;
        }

        [Fact]
        public void Constructor_GivenDevice_Succeeds()
        {
            using var hc = new HidFeatureReportConnection(GetMockedDevice());
        }

        [Fact]
        public void Constructor_GivenFeatureDevice_CallsOpenFeatureConnection()
        {
            IHidDDevice mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock);
            mock.Received().OpenFeatureConnection();
        }

        [Fact]
        public void Constructor_GivenFeatureDevice_SetsInputReportSize()
        {
            IHidDDevice mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock);

            Assert.Equal(hc.InputReportSize, mock.FeatureReportByteLength);
        }

        [Fact]
        public void Constructor_GivenFeatureDevice_SetsOutputReportSize()
        {
            IHidDDevice mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock);

            Assert.Equal(hc.OutputReportSize, mock.FeatureReportByteLength);
        }

        [Fact]
        public void SetReport_GivenNullReport_ThrowsArgumentNullException()
        {
            IHidDDevice mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => hc.SetReport(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void SetReport_GivenFeatureReports_CallsSetFeatureReport()
        {
            IHidDDevice mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock);

            hc.SetReport(GetFeatureReport());

            mock.Received().SetFeatureReport(IsSeqEqual(GetFeatureReport()));
        }

        [Fact]
        public void GetReport_GivenFeatureReports_CallsGetFeatureReport()
        {
            IHidDDevice mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock);

            byte[] report = hc.GetReport();

            mock.Received().GetFeatureReport();
            Assert.Equal(GetFeatureReport(), report);
        }

        [Fact]
        public void Constructor_GivenIODevice_CallsOpenIOConnection()
        {
            IHidDDevice mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock);
            mock.Received().OpenIOConnection();
        }

        [Fact]
        public void Constructor_GivenIODevice_SetsInputReportSize()
        {
            IHidDDevice mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock);

            Assert.Equal(hc.InputReportSize, mock.InputReportByteLength);
        }

        [Fact]
        public void Constructor_GivenIODevice_SetsOutputReportSize()
        {
            IHidDDevice mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock);

            Assert.Equal(hc.OutputReportSize, mock.OutputReportByteLength);
        }

        [Fact]
        public void SetReport_GivenIOReports_CallsSetOutputReport()
        {
            IHidDDevice mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock);

            hc.SetReport(GetOutputReport());

            mock.Received().SetOutputReport(IsSeqEqual(GetOutputReport()));
        }

        [Fact]
        public void GetReport_GivenIOReports_CallsGetInputReport()
        {
            IHidDDevice mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock);

            byte[] report = hc.GetReport();

            mock.Received().GetInputReport();
            Assert.Equal(GetInputReport(), report);
        }

        private static byte[] IsSeqEqual(byte[] val)
        {
            return Arg.Is<byte[]>(b => b.SequenceEqual(val));
        }
    }
#endif
}

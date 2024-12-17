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
            var mock = new Mock<IHidDDevice>();
            return mock.Object;
        }

        private static byte[] GetFeatureReport() => Hex.HexToBytes("000102030405060708090A0B0C0D0E0F");
        private static byte[] GetInputReport() => Hex.HexToBytes("0001020304050607");
        private static byte[] GetOutputReport() => Hex.HexToBytes("00010203");

        private static Mock<IHidDDevice> GetMockedFeatureDevice()
        {
            var mock = new Mock<IHidDDevice>();
            _ = mock.Setup(hdd => hdd.FeatureReportByteLength).Returns(16);
            _ = mock.Setup(hdd => hdd.InputReportByteLength).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.OutputReportByteLength).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.GetInputReport()).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.GetFeatureReport()).Returns(GetFeatureReport());
            return mock;
        }

        private static Mock<IHidDDevice> GetMockedIODevice()
        {
            var mock = new Mock<IHidDDevice>();
            _ = mock.Setup(hdd => hdd.InputReportByteLength).Returns(8);
            _ = mock.Setup(hdd => hdd.OutputReportByteLength).Returns(4);
            _ = mock.Setup(hdd => hdd.FeatureReportByteLength).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.GetFeatureReport()).Throws(new Exception());
            _ = mock.Setup(hdd => hdd.GetInputReport()).Returns(GetInputReport());
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
            Mock<IHidDDevice> mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock.Object);
            mock.Verify(hdd => hdd.OpenFeatureConnection(), Times.Once());
        }

        [Fact]
        public void Constructor_GivenFeatureDevice_SetsInputReportSize()
        {
            Mock<IHidDDevice> mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock.Object);

            Assert.Equal(hc.InputReportSize, mock.Object.FeatureReportByteLength);
        }

        [Fact]
        public void Constructor_GivenFeatureDevice_SetsOutputReportSize()
        {
            Mock<IHidDDevice> mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock.Object);

            Assert.Equal(hc.OutputReportSize, mock.Object.FeatureReportByteLength);
        }

        [Fact]
        public void SetReport_GivenNullReport_ThrowsArgumentNullException()
        {
            Mock<IHidDDevice> mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock.Object);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => hc.SetReport(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void SetReport_GivenFeatureReports_CallsSetFeatureReport()
        {
            Mock<IHidDDevice> mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock.Object);

            hc.SetReport(GetFeatureReport());

            mock.Verify(hdd => hdd.SetFeatureReport(IsSeqEqual(GetFeatureReport())), Times.Once());
        }

        [Fact]
        public void GetReport_GivenFeatureReports_CallsGetFeatureReport()
        {
            Mock<IHidDDevice> mock = GetMockedFeatureDevice();
            using var hc = new HidFeatureReportConnection(mock.Object);

            byte[] report = hc.GetReport();

            mock.Verify(hdd => hdd.GetFeatureReport(), Times.Once());
            Assert.Equal(GetFeatureReport(), report);
        }

        [Fact]
        public void Constructor_GivenIODevice_CallsOpenIOConnection()
        {
            Mock<IHidDDevice> mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock.Object);
            mock.Verify(hdd => hdd.OpenIOConnection(), Times.Once());
        }

        [Fact]
        public void Constructor_GivenIODevice_SetsInputReportSize()
        {
            Mock<IHidDDevice> mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock.Object);

            Assert.Equal(hc.InputReportSize, mock.Object.InputReportByteLength);
        }

        [Fact]
        public void Constructor_GivenIODevice_SetsOutputReportSize()
        {
            Mock<IHidDDevice> mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock.Object);

            Assert.Equal(hc.OutputReportSize, mock.Object.OutputReportByteLength);
        }

        [Fact]
        public void SetReport_GivenIOReports_CallsSetOutputReport()
        {
            Mock<IHidDDevice> mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock.Object);

            hc.SetReport(GetOutputReport());

            mock.Verify(hdd => hdd.SetOutputReport(IsSeqEqual(GetOutputReport())), Times.Once());
        }

        [Fact]
        public void GetReport_GivenIOReports_CallsGetInputReport()
        {
            Mock<IHidDDevice> mock = GetMockedIODevice();
            using var hc = new HidIOReportConnection(mock.Object);

            byte[] report = hc.GetReport();

            mock.Verify(hdd => hdd.GetInputReport(), Times.Once());
            Assert.Equal(GetInputReport(), report);
        }

        private static byte[] IsSeqEqual(byte[] val)
        {
            return It.Is<byte[]>(b => b.SequenceEqual(val));
        }
    }
#endif
}

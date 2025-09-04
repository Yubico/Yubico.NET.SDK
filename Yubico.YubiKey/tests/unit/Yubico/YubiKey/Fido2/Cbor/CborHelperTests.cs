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

using System.Formats.Cbor;
using Xunit;

namespace Yubico.YubiKey.Fido2.Cbor
{
    public class CborHelperTests
    {
        [Fact]
        public void EncodeDecode_MapWithIntKey_Succeeds()
        {
            var w = new CborWriter(CborConformanceMode.Ctap2Canonical, true);

            CborHelpers.BeginMap<int>(w)
                .Entry(1, "test")
                .Entry(2, "foo")
                .Entry(-4, "bar")
                .EndMap();

            byte[] encoded = w.Encode();

            var m = new CborMap<int>(encoded);

            Assert.Equal("test", m.ReadTextString(1));
            Assert.Equal("foo", m.ReadTextString(2));
            Assert.Equal("bar", m.ReadTextString(-4));
        }

        [Fact]
        public void EncodeDecode_MapWithStringKey_Succeeds()
        {
            var w = new CborWriter(CborConformanceMode.Ctap2Canonical, true);

            CborHelpers.BeginMap<string>(w)
                .Entry("a", "test")
                .Entry("b", "foo")
                .Entry("xyz", "bar")
                .EndMap();

            byte[] encoded = w.Encode();

            var m = new CborMap<string>(encoded);

            Assert.Equal("test", m.ReadTextString("a"));
            Assert.Equal("foo", m.ReadTextString("b"));
            Assert.Equal("bar", m.ReadTextString("xyz"));
        }
        
        [Fact]
        public void ToCbor_succeeds()
        {
            Assert.Equal(new byte[] { 0xf5 }, true.ToCbor());
            Assert.Equal(new byte[] { 0xf4 }, false.ToCbor());
            Assert.Equal(new byte[] { 0x64, 0x74, 0x65, 0x73, 0x74 }, "test".ToCbor());
            Assert.Equal(new byte[] { 0x00 }, 0.ToCbor());
            Assert.Equal(new byte[] { 0x01 }, 1.ToCbor());
            Assert.Equal(new byte[] { 0x0a }, 10.ToCbor());
            Assert.Equal(new byte[] { 0x17 }, 23.ToCbor());
            Assert.Equal(new byte[] { 0x18, 0x18 }, 24.ToCbor());
            Assert.Equal(new byte[] { 0x18, 0x19 }, 25.ToCbor());
            Assert.Equal(new byte[] { 0x18, 0xfe }, 254.ToCbor());
            Assert.Equal(new byte[] { 0x19, 0x01, 0x00 }, 256.ToCbor());
            Assert.Equal(new byte[] { 0x1a, 0x00, 0x01, 0x11, 0x70 }, 70000.ToCbor());
        }
    }
}

﻿// Copyright 2022 Yubico AB
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
    }
}

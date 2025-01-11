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

using System.Linq;
using Xunit;

namespace Yubico.Core.Buffers
{
    public class Base16Tests
    {
        [Fact]
        public void TestDecodeBase16()
        {
            byte[] bytes = Base16.DecodeText("baaddeadf00d");
            byte[] expected = new byte[] { 0xba, 0xad, 0xde, 0xad, 0xf0, 0x0d };
            Assert.True(expected.SequenceEqual(bytes));
        }

        [Fact]
        public void TestEncodeModHex()
        {
            string base16 = Base16.EncodeBytes(new byte[] { 0xba, 0xad, 0xde, 0xad, 0xf0, 0x0d });
            string expected = "BAADDEADF00D";
            Assert.Equal(expected, base16);
        }

        [Fact]
        public void TestDecodeVaryingCase()
        {
            byte[] bytes = Base16.DecodeText("bAaDdEaDf00d");
            byte[] expected = new byte[] { 0xba, 0xad, 0xde, 0xad, 0xf0, 0x0d };
            Assert.True(expected.SequenceEqual(bytes));
        }
    }
}

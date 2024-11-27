﻿// Copyright 2024 Yubico AB
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
using System.Linq;
using Xunit;
using Yubico.YubiKit.Core.Util;

namespace Yubico.Core.Tlv.UnitTests
{
    public class TlvObjectTest
    {
        [Fact]
        public void TestDoubleByteTags()
        {
            var tlv = TlvObject.Parse(new byte[] { 0x7F, 0x49, 0 });
            Assert.Equal(0x7F49, tlv.Tag);
            Assert.Equal(0, tlv.Length);

            tlv = TlvObject.Parse(new byte[] { 0x80, 0 });
            Assert.Equal(0x80, tlv.Tag);
            Assert.Equal(0, tlv.Length);

            tlv = new TlvObject(0x7F49, null);
            Assert.Equal(0x7F49, tlv.Tag);
            Assert.Equal(0, tlv.Length);
            Assert.Equal(new byte[] { 0x7F, 0x49, 0 }, tlv.GetBytes());

            tlv = new TlvObject(0x80, null);
            Assert.Equal(0x80, tlv.Tag);
            Assert.Equal(0, tlv.Length);
            Assert.Equal(new byte[] { 0x80, 0 }, tlv.GetBytes());
        }

        [Fact]
        public void TestUnwrap()
        {
            TlvObjects.UnpackValue(0x80, new byte[] { 0x80, 0 });

            TlvObjects.UnpackValue(0x7F49, new byte[] { 0x7F, 0x49, 0 });

            var value = TlvObjects.UnpackValue(0x7F49, new byte[] { 0x7F, 0x49, 3, 1, 2, 3 });
            Assert.Equal(new byte[] { 1, 2, 3 }, value);
        }

        [Fact]
        public void TestUnwrapThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => TlvObjects.UnpackValue(0x7F48, new byte[] { 0x7F, 0x49, 0 }));
        }
        
        [Fact]
        public void DecodeList_ValidInput_ReturnsCorrectTlvs()
        {
            var input = new byte[] { 0x01, 0x01, 0xFF, 0x02, 0x02, 0xAA, 0xBB };
            var result = TlvObjects.DecodeList(input);

            Assert.Equal(2, result.Count);
            Assert.Equal(0x01, result[0].Tag);
            Assert.Equal(new byte[] { 0xFF }, result[0].Value.ToArray());
            Assert.Equal(0x02, result[1].Tag);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, result[1].Value.ToArray());
        }

        [Fact]
        public void DecodeList_EmptyInput_ReturnsEmptyList()
        {
            var result = TlvObjects.DecodeList(Array.Empty<byte>());
            Assert.Empty(result);
        }

        [Fact]
        public void DecodeMap_ValidInput_ReturnsCorrectDictionary()
        {
            var input = new byte[] { 0x01, 0x01, 0xFF, 0x02, 0x02, 0xAA, 0xBB };
            var result = TlvObjects.DecodeMap(input);

            Assert.Equal(2, result.Count);
            Assert.Equal(new byte[] { 0xFF }, result[0x01].ToArray());
            Assert.Equal(new byte[] { 0xAA, 0xBB }, result[0x02].ToArray());
        }

        [Fact]
        public void DecodeMap_DuplicateTags_KeepsLastValue()
        {
            var input = new byte[] { 0x01, 0x01, 0xFF, 0x01, 0x01, 0xEE };
            var result = TlvObjects.DecodeMap(input);

            Assert.Single(result);
            Assert.Equal(new byte[] { 0xEE }, result[0x01].ToArray());
        }

        [Fact]
        public void EncodeList_ValidInput_ReturnsCorrectBytes()
        {
            var tlvs = new List<TlvObject>
            {
                new TlvObject(0x01, new byte[] { 0xFF }),
                new TlvObject(0x02, new byte[] { 0xAA, 0xBB })
            };

            var result = TlvObjects.EncodeList(tlvs);
            Assert.Equal(new byte[] { 0x01, 0x01, 0xFF, 0x02, 0x02, 0xAA, 0xBB }, result.ToArray());
        }

        [Fact]
        public void EncodeList_EmptyInput_ReturnsEmptyArray()
        {
            var result = TlvObjects.EncodeList(new List<TlvObject>());
            Assert.Empty(result.ToArray());
        }

        [Fact]
        public void EncodeMap_ValidInput_ReturnsCorrectBytes()
        {
            var map = new Dictionary<int, ReadOnlyMemory<byte>>
            {
                { 0x01, new byte[] { 0xFF } },
                { 0x02, new byte[] { 0xAA, 0xBB } }
            };

            var result = TlvObjects.EncodeMap(map);
            Assert.Equal(new byte[] { 0x01, 0x01, 0xFF, 0x02, 0x02, 0xAA, 0xBB }, result.ToArray());
        }

        [Fact]
        public void EncodeMap_EmptyInput_ReturnsEmptyArray()
        {
            var result = TlvObjects.EncodeMap(new Dictionary<int, ReadOnlyMemory<byte>>());
            Assert.Empty(result.ToArray());
        }

        [Fact]
        public void UnpackValue_CorrectTag_ReturnsValue()
        {
            var input = new byte[] { 0x01, 0x02, 0xAA, 0xBB };
            var result = TlvObjects.UnpackValue(0x01, input);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, result.ToArray());
        }

        [Fact]
        public void UnpackValue_IncorrectTag_ThrowsBadResponseException()
        {
            var input = new byte[] { 0x01, 0x02, 0xAA, 0xBB };
            Assert.Throws<InvalidOperationException>(() => TlvObjects.UnpackValue(0x02, input));
        }

        [Fact]
        public void UnpackValue_EmptyValue_ReturnsEmptyArray()
        {
            var input = new byte[] { 0x01, 0x00 };
            var result = TlvObjects.UnpackValue(0x01, input);
            Assert.Empty(result.ToArray());
        }
    }
}

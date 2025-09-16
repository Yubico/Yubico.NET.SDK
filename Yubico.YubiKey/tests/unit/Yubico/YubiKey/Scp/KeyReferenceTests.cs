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
using Xunit;

namespace Yubico.YubiKey.Scp
{
    public class KeyReferenceTests
    {
        [Fact]
        public void Constructor_ValidParameters_SetsProperties()
        {
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);

            Assert.Equal(0x13, keyReference.Id);
            Assert.Equal(0x1, keyReference.VersionNumber);
        }

        [Fact]
        public void FactoryMethod_ValidParameters_SetsProperties()
        {
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0x1);

            Assert.Equal(0x13, keyReference.Id);
            Assert.Equal(0x1, keyReference.VersionNumber);
        }

        [Fact]
        public void FactoryMethod_InvalidParameters_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => KeyReference.Create(ScpKeyIds.Scp11B, 128));
        }

        [Fact]
        public void Constructor_0xFF_SetsProperties()
        {
            var keyReference = new KeyReference(ScpKeyIds.Scp11B, 0xFF);

            Assert.Equal(0x13, keyReference.Id);
            Assert.Equal(0xFF, keyReference.VersionNumber);
        }

        [Fact]
        public void FactoryMethod_InvalidKvn_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => KeyReference.Create(ScpKeyIds.Scp11B, 0xFF));
        }
    }
}

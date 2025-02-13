﻿// Copyright 2025 Yubico AB
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

using Yubico.Core.Buffers;

namespace Yubico.YubiKey.TestApp.Converters
{
    /// <summary>
    /// Supports parameter parsing base-32 strings for the sandbox app.
    /// </summary>
    public class Base32Bytes : BytesBase
    {
        public static Base32Bytes Bytes(byte[] d) => new Base32Bytes { Value = d };

        public static Base32Bytes Encode(string s) => new Base32Bytes { Value = Base32.DecodeText(s) };

        public override string ToString() => Base32.EncodeBytes(Value);
    }
}

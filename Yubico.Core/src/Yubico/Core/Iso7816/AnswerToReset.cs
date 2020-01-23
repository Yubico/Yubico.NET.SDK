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
using System.Linq;

namespace Yubico.Core.Iso7816
{
    public class AnswerToReset
    {
        private readonly byte[] _bytes;

        public AnswerToReset(ReadOnlySpan<byte> bytes)
        {
            _bytes = bytes.ToArray();
        }

        public override bool Equals(object? obj) => obj switch
        {
            AnswerToReset atr => this == atr,
            _ => false
        };

        public override int GetHashCode() => _bytes.GetHashCode();

        public override string ToString() => BitConverter.ToString(_bytes.ToArray());

        public static bool operator ==(AnswerToReset l, AnswerToReset r) => (l, r) switch
        {
            (AnswerToReset _, null) => false,
            (null, AnswerToReset _) => false,
            (AnswerToReset left, AnswerToReset right) => left._bytes.AsSpan().SequenceEqual(right._bytes.AsSpan()),
            _ => false
        };

        public static bool operator !=(AnswerToReset l, AnswerToReset r) => !(l == r);
    }
}

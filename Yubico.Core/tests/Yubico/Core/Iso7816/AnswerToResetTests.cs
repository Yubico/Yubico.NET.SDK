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

using Xunit;

namespace Yubico.Core.Iso7816.UnitTests
{
    public class AnswerToResetTests
    {
        [Fact]
        public void Equality_SameInstance_EqualToItself()
        {
            var atr1 = new AnswerToReset(new byte[] { 1, 2, 3, 4 });
            AnswerToReset atr2 = atr1;

            Assert.True(atr1.Equals(atr2));
            Assert.True(atr2.Equals(atr1));
            Assert.True(atr1 == atr2);
            Assert.True(atr2 == atr1);
        }

        [Fact]
        public void Equality_DifferentInstanceSameValue_IsEqual()
        {
            var atr1 = new AnswerToReset(new byte[] { 1, 2, 3, 4 });
            var atr2 = new AnswerToReset(new byte[] { 1, 2, 3, 4 });

            Assert.True(atr1.Equals(atr2));
            Assert.True(atr2.Equals(atr1));
            Assert.True(atr1 == atr2);
            Assert.True(atr2 == atr1);
        }

        [Fact]
        public void Equality_DifferentValues_AreNotEqual()
        {
            var atr1 = new AnswerToReset(new byte[] { 1, 2, 3, 4 });
            var atr2 = new AnswerToReset(new byte[] { 4, 3, 2, 1 });

            Assert.False(atr1.Equals(atr2));
            Assert.False(atr2.Equals(atr1));
            Assert.True(atr1 != atr2);
            Assert.True(atr2 != atr1);
        }

        [Fact]
        public void ToString_PrettyPrintsAtrValue()
        {
            var atr1 = new AnswerToReset(new byte[] { 1, 2, 10, 255 });
            string expectedString = "01-02-0A-FF";

            Assert.Equal(expectedString, atr1.ToString());
        }
    }
}

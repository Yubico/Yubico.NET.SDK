// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public sealed class OpenPgpCollectionContractTests
{
    [Theory]
    [InlineData(typeof(KeyInformation), typeof(IReadOnlyDictionary<KeyRef, KeyStatus>))]
    [InlineData(typeof(KeyFingerprints), typeof(IReadOnlyDictionary<KeyRef, ReadOnlyMemory<byte>>))]
    [InlineData(typeof(GenerationTimes), typeof(IReadOnlyDictionary<KeyRef, int>))]
    public void KeyCollectionType_ExposesOnlyReadOnlyDictionaryContract(Type type, Type expectedContract)
    {
        Assert.True(type.IsSealed);
        Assert.Equal(typeof(object), type.BaseType);
        Assert.True(expectedContract.IsAssignableFrom(type));
        Assert.False(typeof(IDictionary).IsAssignableFrom(type));
        Assert.DoesNotContain(type.GetInterfaces(), static contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        Assert.Empty(type.GetConstructors());
    }
}
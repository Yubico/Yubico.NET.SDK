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

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Trait discoverer that automatically adds "RequiresHardware" category
///     to any test method decorated with <see cref="WithYubiKeyAttribute"/>.
/// </summary>
public class WithYubiKeyTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) =>
    [
        new KeyValuePair<string, string>(TestCategories.Category, TestCategories.RequiresHardware),
        new KeyValuePair<string, string>(TestCategories.Category, TestCategories.Integration)
    ];
}

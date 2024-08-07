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

namespace Yubico.YubiKey.InterIndustry.Commands
{
    /// <summary>
    /// Selecting a YubiKey application will always result in data being returned. This type represents the return data
    /// if a more specific, structured type is not available.  If a specific application has a specific data need, such
    /// as OATH, it will have its own data type which parses this data.
    /// </summary>
    public interface ISelectApplicationData
    {
        ReadOnlyMemory<byte> RawData { get; }
    }
}

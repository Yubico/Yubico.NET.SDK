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

namespace Yubico.Core.Devices.Hid;
#pragma warning disable CA1707 // Justification: Cannot use

// underscore (_) character for variable name
public enum KeyboardLayout
{
    ModHex = 0,
    en_US = 1,
    en_UK = 2,
    de_DE = 3,
    fr_FR = 4,
    it_IT = 5,
    es_US = 6,
    sv_SE = 7
    #pragma warning restore CA1707
}

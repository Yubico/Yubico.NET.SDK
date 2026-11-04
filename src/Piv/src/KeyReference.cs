// Copyright 2026 Yubico AB
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

namespace Yubico.YubiKit.Piv;

/// <summary>Special applet references, distinct from public asymmetric-key slots.</summary>
internal static class KeyReference
{
    internal const byte Pin = 0x80;
    internal const byte Puk = 0x81;
    internal const byte Management = 0x9B;
    internal const byte Bio = 0x96;
}
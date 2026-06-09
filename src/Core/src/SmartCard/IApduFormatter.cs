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

namespace Yubico.YubiKit.Core.SmartCard;

public interface IApduFormatter
{
    /// <summary>
    ///     Formats an APDU into a freshly allocated, exclusively owned wire payload buffer.
    /// </summary>
    /// <remarks>
    ///     Transmitters may zero the returned buffer after transmission because formatted APDUs can contain secrets.
    /// </remarks>
    Memory<byte> Format(
        byte cla, byte ins, byte p1, byte p2, ReadOnlyMemory<byte> data, int le);

    /// <summary>
    ///     Formats an APDU into a freshly allocated, exclusively owned wire payload buffer.
    /// </summary>
    /// <remarks>
    ///     Transmitters may zero the returned buffer after transmission because formatted APDUs can contain secrets.
    /// </remarks>
    Memory<byte> Format(ApduCommand apdu);
}
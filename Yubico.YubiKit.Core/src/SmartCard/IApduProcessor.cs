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

public interface IApduProcessor
{
    IApduFormatter Formatter { get; }
    // FirmwareVersion? FirmwareVersion { get; } // TODO Could be nice to be able to do additional version checks within the processors.

    Task<ApduResponse> TransmitAsync(ApduCommand command, bool useScp,
        CancellationToken cancellationToken = default);
}
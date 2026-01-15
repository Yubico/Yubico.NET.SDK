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

using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.SecurityDomain;

public interface ISecurityDomainSession : IApplicationSession
{
    Task<IReadOnlyList<KeyInfo>> GetKeyInfoAsync(
        CancellationToken cancellationToken = default);

    Task PutKeyAsync(
        KeyReference keyReference,
        StaticKeys staticKeys,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default);

    Task PutKeyAsync(
        KeyReference keyReference,
        ECPublicKey publicKey,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default);

    Task<ECPublicKey> GenerateKeyAsync(
        KeyReference keyReference,
        byte replaceKvn = 0,
        CancellationToken cancellationToken = default);

    Task DeleteKeyAsync(
        KeyReference keyReference,
        bool deleteLast = false,
        CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}

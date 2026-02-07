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

using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
/// Factory for creating composite YubiKey devices from transport references.
/// </summary>
/// <remarks>
/// <para>
/// The factory correlates multiple <see cref="IYubiKeyReference"/> instances that
/// represent the same physical device into a single <see cref="IYubiKey"/> composite.
/// </para>
/// <para>
/// Correlation is performed using the <see cref="DeviceCorrelationKey"/> which combines
/// serial number (when available), firmware version, form factor, supported capabilities,
/// and configuration fingerprint.
/// </para>
/// </remarks>
public interface ICompositeYubiKeyFactory
{
    /// <summary>
    /// Creates composite YubiKey devices by correlating transport references.
    /// </summary>
    /// <param name="references">The transport references to correlate.</param>
    /// <param name="identityReader">
    /// A delegate that reads device identity from a reference. This is typically provided
    /// by the Management module to read <see cref="IDeviceIdentity"/> via ManagementSession.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A list of <see cref="IYubiKey"/> composites, where each represents a unique physical device.
    /// References that cannot be correlated are returned as singleton composites.
    /// </returns>
    Task<IReadOnlyList<IYubiKey>> CreateCompositesAsync(
        IReadOnlyList<IYubiKeyReference> references,
        Func<IYubiKeyReference, CancellationToken, Task<IDeviceIdentity>> identityReader,
        CancellationToken cancellationToken = default);
}

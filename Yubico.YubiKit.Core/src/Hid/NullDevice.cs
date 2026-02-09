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

using Yubico.YubiKit.Core.Hid.Interfaces;

namespace Yubico.YubiKit.Core.Hid;

/// <summary>
/// A placeholder HID device used for removal events when the full device information is unavailable.
/// </summary>
/// <remarks>
/// On some platforms (e.g., Windows), device removal callbacks do not provide enough information
/// to reconstruct the full device object. This singleton is used as a placeholder in such cases.
/// All connection methods throw <see cref="NotSupportedException"/>.
/// </remarks>
public sealed class NullDevice : IHidDevice
{
    private static readonly Lazy<NullDevice> LazyInstance = new(() => new NullDevice());

    /// <summary>
    /// Gets the singleton instance of <see cref="NullDevice"/>.
    /// </summary>
    public static NullDevice Instance => LazyInstance.Value;

    private NullDevice()
    {
    }

    /// <inheritdoc />
    /// <remarks>Returns an empty string for the null device.</remarks>
    public string ReaderName => string.Empty;

    /// <inheritdoc />
    /// <remarks>Returns a default descriptor with no useful information.</remarks>
    public HidDescriptorInfo DescriptorInfo { get; } = new()
    {
        UsagePage = 0,
        Usage = 0,
        DevicePath = string.Empty,
        VendorId = 0,
        ProductId = 0
    };

    /// <inheritdoc />
    /// <remarks>Returns <see cref="HidInterfaceType.Unknown"/>.</remarks>
    public HidInterfaceType InterfaceType => HidInterfaceType.Unknown;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown - NullDevice cannot be connected.</exception>
    public IHidConnection ConnectToFeatureReports() =>
        throw new NotSupportedException("NullDevice cannot be connected. It is a placeholder for removal events.");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown - NullDevice cannot be connected.</exception>
    public IHidConnection ConnectToIOReports() =>
        throw new NotSupportedException("NullDevice cannot be connected. It is a placeholder for removal events.");
}

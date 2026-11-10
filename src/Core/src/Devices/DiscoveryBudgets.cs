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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Bounds discovery callers' waits, not native work. Abandoned work completes and cleans up on its own;
///     callers need not clean it up. Accessing these budgets does not throw.
/// </summary>
internal static class DiscoveryBudgets
{
    // Measured on Linux (pcsc-lite 2.5.3): steady scans 75–83 ms, scans overlapping a pcscd restart or
    // socket activation under 300 ms. Five seconds leaves an order of magnitude of headroom.
    internal static readonly TimeSpan Enumeration = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan IdentityRead = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan MetadataRead = TimeSpan.FromSeconds(3);

    /// <summary>The combined wait budget for a scan's enumeration, identity, and metadata stages.</summary>
    internal static TimeSpan ScanUpperBound => Enumeration + IdentityRead + MetadataRead;
}
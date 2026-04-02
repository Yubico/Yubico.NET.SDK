// Copyright (C) 2024 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Runtime.InteropServices;

namespace Yubico.YubiKit.Core.Utils;

/// <summary>
///     A disposable collection of TLV objects that ensures all contained TLVs are properly disposed.
/// </summary>
/// <remarks>
///     This class is used to manage the lifetime of multiple TLV objects, ensuring that sensitive
///     cryptographic data is properly zeroed out when no longer needed.
/// </remarks>
public sealed class DisposableTlvList : IDisposable, IReadOnlyList<Tlv>
{
    private readonly List<Tlv> _tlvs;
    private bool _disposed;

    /// <summary>
    ///     Creates a new collection with the specified TLV objects.
    /// </summary>
    /// <param name="tlvs">The TLV objects to include in the collection.</param>
    public DisposableTlvList(params Tlv[] tlvs)
    {
        _tlvs = new List<Tlv>(tlvs);
    }

    /// <summary>
    ///     Creates a new collection from an enumerable of TLV objects.
    /// </summary>
    /// <param name="tlvs">The TLV objects to include in the collection.</param>
    public DisposableTlvList(IEnumerable<Tlv> tlvs)
    {
        _tlvs = new List<Tlv>(tlvs);
    }

    #region IDisposable Members

    /// <summary>
    ///     Disposes all TLV objects in the collection, securely zeroing their data.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        foreach (var tlv in _tlvs) tlv.Dispose();

        _disposed = true;
    }

    #endregion

    #region IReadOnlyList<Tlv> Members

    /// <summary>
    ///     Gets the TLV object at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the TLV to get.</param>
    /// <returns>The TLV at the specified index.</returns>
    public Tlv this[int index] => _tlvs[index];

    /// <summary>
    ///     Gets the number of TLV objects in the collection.
    /// </summary>
    public int Count => _tlvs.Count;

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<Tlv> GetEnumerator() => _tlvs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    /// <summary>
    ///     Returns a span view of the collection for efficient iteration.
    /// </summary>
    /// <returns>A span containing all TLV objects in the collection.</returns>
    public ReadOnlySpan<Tlv> AsSpan() => CollectionsMarshal.AsSpan(_tlvs);
}
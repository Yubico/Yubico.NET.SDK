// // Copyright (C) 2024 Yubico.
// //
// // Licensed under the Apache License, Version 2.0 (the "License");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
//
// using System.Collections;
// using System.Diagnostics.CodeAnalysis;
//
// namespace Yubico.YubiKit.Core.Utils;
//
// /// <summary>
// ///     A disposable collection of TLV objects that ensures all contained TLVs are properly disposed.
// /// </summary>
// /// <remarks>
// ///     This class is used to manage the lifetime of multiple TLV objects, ensuring that sensitive
// ///     cryptographic data is properly zeroed out when no longer needed.
// /// </remarks>
// public sealed class DisposableTlvDictionary : IDisposable, IDictionary<int, Tlv>
// {
//     private readonly Dictionary<int, Tlv> _tlvs;
//     private bool _disposed;
//
//     /// <summary>
//     ///     Creates a new collection with the specified TLV objects.
//     /// </summary>
//     /// <param name="tlvs">The TLV objects to include in the collection.</param>
//     public DisposableTlvDictionary(params Tlv[] tlvs)
//     {
//         _tlvs = tlvs.ToDictionary(k => k.Tag, v => v);
//     }
//
//     public DisposableTlvDictionary() { _tlvs = new Dictionary<int, Tlv>(); }
//
//     #region IDictionary<int,Tlv> Members
//
//     public void Add(int key, Tlv value) => _tlvs.Add(key, value);
//     public bool ContainsKey(int key) => _tlvs.ContainsKey(key);
//     public bool Remove(int key) => _tlvs.Remove(key);
//
//     public bool TryGetValue(int key, [MaybeNullWhen(false)] out Tlv value) => _tlvs.TryGetValue(key, out value);
//
//     /// <summary>
//     ///     Gets the TLV object at the specified index.
//     /// </summary>
//     /// <param name="index">The zero-based index of the TLV to get.</param>
//     /// <returns>The TLV at the specified index.</returns>
//     public Tlv this[int index]
//     {
//         get => _tlvs[index];
//         set => _tlvs[index] = value;
//     }
//
//     ICollection<Tlv> IDictionary<int, Tlv>.Values => _tlvs.Values;
//
//     ICollection<int> IDictionary<int, Tlv>.Keys => _tlvs.Keys;
//
//     public void Add(KeyValuePair<int, Tlv> item) => _tlvs.Add(item.Key, item.Value);
//     public void Clear() => _tlvs.Clear();
//     public bool Contains(KeyValuePair<int, Tlv> item) => _tlvs.Contains(item);
//     public void CopyTo(KeyValuePair<int, Tlv>[] array, int arrayIndex) => throw new NotImplementedException();
//     public bool Remove(KeyValuePair<int, Tlv> item) => throw new NotImplementedException();
//
//     /// <summary>
//     ///     Gets the number of TLV objects in the collection.
//     /// </summary>
//     public int Count => _tlvs.Count;
//
//     public bool IsReadOnly => false;
//
//     /// <summary>
//     ///     Returns an enumerator that iterates through the collection.
//     /// </summary>
//     /// <returns>An enumerator for the collection.</returns>
//     public IEnumerator<KeyValuePair<int, Tlv>> GetEnumerator() => _tlvs.GetEnumerator();
//
//     IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//
//     #endregion
//
//     #region IDisposable Members
//
//     /// <summary>
//     ///     Disposes all TLV objects in the collection, securely zeroing their data.
//     /// </summary>
//     public void Dispose()
//     {
//         if (_disposed) return;
//
//         foreach (var tlv in _tlvs) tlv.Value.Dispose();
//
//         _disposed = true;
//     }
//
//     #endregion
//
//     // /// <summary>
//     // ///     Creates a new collection from an enumerable of TLV objects.
//     // /// </summary>
//     // /// <param name="tlvs">The TLV objects to include in the collection.</param>
//     // public DisposableTlvDictionary(IEnumerable<Tlv> tlvs)
//     // {
//     //     _tlvs = tlvs.ToDictionary(k => k.Tag, v => v);
//     // }
// }


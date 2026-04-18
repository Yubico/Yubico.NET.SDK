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

using System.Formats.Cbor;

namespace Yubico.YubiKit.Fido2.Cbor;

/// <summary>
/// Shared helper for writing COSE_Key maps to CBOR.
/// </summary>
internal static class CoseKeyWriter
{
    /// <summary>
    /// Writes a COSE_Key dictionary as a CBOR map with numerically sorted keys.
    /// </summary>
    /// <param name="writer">The CBOR writer.</param>
    /// <param name="coseKey">The COSE key as a dictionary of integer keys to values.</param>
    /// <exception cref="InvalidOperationException">If an unsupported value type is encountered.</exception>
    internal static void WriteCoseKey(CborWriter writer, IReadOnlyDictionary<int, object?> coseKey)
    {
        var sortedKeys = coseKey.Keys.OrderBy(k => k).ToList();

        writer.WriteStartMap(coseKey.Count);

        foreach (var k in sortedKeys)
        {
            writer.WriteInt32(k);

            switch (coseKey[k])
            {
                case int intVal:
                    writer.WriteInt32(intVal);
                    break;
                case byte[] bytes:
                    writer.WriteByteString(bytes);
                    break;
                case null:
                    writer.WriteNull();
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported COSE key value type: {coseKey[k]?.GetType().Name}");
            }
        }

        writer.WriteEndMap();
    }
}

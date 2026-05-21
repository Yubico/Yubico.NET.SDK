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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     Identifies the four OpenPGP key slots.
/// </summary>
public enum KeyRef : byte
{
    /// <summary>
    ///     The Signature key slot.
    /// </summary>
    Sig = 0x01,

    /// <summary>
    ///     The Decryption key slot.
    /// </summary>
    Dec = 0x02,

    /// <summary>
    ///     The Authentication key slot.
    /// </summary>
    Aut = 0x03,

    /// <summary>
    ///     The Attestation key slot.
    /// </summary>
    Att = 0x81,
}

/// <summary>
///     Extension methods for <see cref="KeyRef" /> providing computed Data Object tag properties.
/// </summary>
public static class KeyRefExtensions
{
    /// <summary>
    ///     Consolidated lookup mapping each <see cref="KeyRef" /> to its associated Data Object tags and CRT.
    /// </summary>
    private static (DataObject AlgoAttr, DataObject Uif, DataObject GenTime, DataObject Fingerprint, ReadOnlyMemory<byte> Crt)
        GetKeyRefData(KeyRef keyRef) => keyRef switch
        {
            KeyRef.Sig => (DataObject.AlgorithmAttributesSig, DataObject.UifSig, DataObject.GenerationTimeSig, DataObject.FingerprintSig, Crt.Sig),
            KeyRef.Dec => (DataObject.AlgorithmAttributesDec, DataObject.UifDec, DataObject.GenerationTimeDec, DataObject.FingerprintDec, Crt.Dec),
            KeyRef.Aut => (DataObject.AlgorithmAttributesAut, DataObject.UifAut, DataObject.GenerationTimeAut, DataObject.FingerprintAut, Crt.Aut),
            KeyRef.Att => (DataObject.AlgorithmAttributesAtt, DataObject.UifAtt, DataObject.GenerationTimeAtt, DataObject.FingerprintAtt, Crt.Att),
            _ => throw new ArgumentOutOfRangeException(nameof(keyRef)),
        };

    /// <summary>
    ///     Gets the <see cref="DataObject" /> tag for this key slot's algorithm attributes.
    /// </summary>
    public static DataObject AlgorithmAttributesDo(this KeyRef keyRef) =>
        GetKeyRefData(keyRef).AlgoAttr;

    /// <summary>
    ///     Gets the <see cref="DataObject" /> tag for this key slot's User Interaction Flag.
    /// </summary>
    public static DataObject UifDo(this KeyRef keyRef) =>
        GetKeyRefData(keyRef).Uif;

    /// <summary>
    ///     Gets the <see cref="DataObject" /> tag for this key slot's generation timestamp.
    /// </summary>
    public static DataObject GenerationTimeDo(this KeyRef keyRef) =>
        GetKeyRefData(keyRef).GenTime;

    /// <summary>
    ///     Gets the <see cref="DataObject" /> tag for this key slot's fingerprint.
    /// </summary>
    public static DataObject FingerprintDo(this KeyRef keyRef) =>
        GetKeyRefData(keyRef).Fingerprint;

    /// <summary>
    ///     Gets the Control Reference Template bytes for this key slot.
    /// </summary>
    public static ReadOnlyMemory<byte> GetCrt(this KeyRef keyRef) =>
        GetKeyRefData(keyRef).Crt;
}
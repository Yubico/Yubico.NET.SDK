// Copyright 2023 Yubico AB
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

using System;
using System.IO;
using System.Security.Cryptography;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Scp03.Commands
{
    /// <summary>
    /// Use this command to put or replace SCP03 keys on the YubiKey.
    /// </summary>
    /// <remarks>
    /// See the <xref href="UsersManualScp03">User's Manual entry</xref> on SCP03.
    /// <para>
    /// On each YubiKey that supports SCP03, there is space for three sets of
    /// keys. Each set contains three keys: "ENC", "MAC", and "DEK" (Channel
    /// Encryption, Channel MAC, and Data Encryption).
    /// <code language="adoc">
    ///    slot 1:   ENC   MAC   DEK
    ///    slot 2:   ENC   MAC   DEK
    ///    slot 3:   ENC   MAC   DEK
    /// </code>
    /// Each key is 16 bytes. YubiKeys do not support any other key size.
    /// </para>
    /// <para>
    /// Note that the standard allows changing one key in a key set. However,
    /// YubiKeys only allow calling this command with all three keys. That is,
    /// with a YubiKey, it is possible only to set or change all three keys of a
    /// set with this command.
    /// </para>
    /// <para>
    /// Standard YubiKeys are manufactured with one key set, and each key in that
    /// set is the default value.
    /// <code language="adoc">
    ///    slot 1:   ENC(default)  MAC(default)  DEK(default)
    ///    slot 2:   --empty--
    ///    slot 3:   --empty--
    /// </code>
    /// The default value is 0x40 41 42 ... 4F.
    /// </para>
    /// <para>
    /// The key sets are not specified using a "slot number", rather, each key
    /// set is given a Key Version Number (KVN). Each key in the set is given a
    /// Key Identifier (KeyId). If the YubiKey contains the default key, the KVN
    /// is 255 (0xFF) and the KeyIds are 1, 2, and 3.
    /// <code language="adoc">
    ///    slot 1: KVN=0xff  KeyId=1:ENC(default)  KeyId=2:MAC(default)  KeyId=3:DEK(default)
    ///    slot 2:   --empty--
    ///    slot 3:   --empty--
    /// </code>
    /// </para>
    /// <para>
    /// It is possible to use this command to replace or add a key set. However,
    /// if the YubiKey contains only the initial, default keys, then it is only
    /// possible to replace that set. For example, suppose you have a YubiKey
    /// with the default keys and you try to set the keys in slot 2. The YubiKey
    /// will not allow that and will return an error.
    /// </para>
    /// <para>
    /// When you replace the initial, default keys, you must specify the KVN of
    /// the new keys, and the KeyId of the ENC key. The KeyId(MAC) is, per the
    /// standard, KeyId(ENC) + 1, and the KeyId(DEK) is KeyId(ENC) + 2. For the
    /// YubiKey, the KVN must be 1. Also, the YubiKey only allows the number 1 as
    /// the KeyId of the ENC key. If you supply any other values for the KVN or
    /// KeyId, the YubiKey will return an error. Hence, after replacing the
    /// initial, default keys, your three sets of keys will be the following:
    /// <code language="adoc">
    ///    slot 1: KVN=1  KeyId=1:ENC  KeyId=2:MAC  KeyId=3:DEK
    ///    slot 2:   --empty--
    ///    slot 3:   --empty--
    /// </code>
    /// </para>
    /// <para>
    /// In order to add or change the keys, you must supply one of the existing
    /// key sets in order to build the SCP03 command and to encrypt and
    /// authenticate the new keys. When replacing the initial, default keys, you
    /// only have the choice to supply the keys with the KVN of 0xFF.
    /// </para>
    /// <para>
    /// Once you have replaced the original key set, you can use that set to add
    /// a second set to slot 2. It's KVN must be 2 and the KeyId of the ENC key
    /// must be 1.
    /// <code language="adoc">
    ///    slot 1: KVN=1  KeyId=1:ENC  KeyId=2:MAC  KeyId=3:DEK
    ///    slot 2: KVN=2  KeyId=1:ENC  KeyId=2:MAC  KeyId=3:DEK
    ///    slot 3:   --empty--
    /// </code>
    /// </para>
    /// <para>
    /// You can use either key set to add a set to slot 3. You can use a key set
    /// to replace itself.
    /// </para>
    /// </remarks>
    internal class PutKeyCommand : IYubiKeyCommand<PutKeyResponse>
    {
        private const byte GpPutKeyCla = 0x84;
        private const byte GpPutKeyIns = 0xD8;
        private const byte KeyIdentifier = 0x81;

        private const int DataLength = 70;

        private const byte KeyType = 0x88;
        private const byte BlockSize = 17;
        private const byte AesBlockSize = 16;
        private const byte KeyCheckSize = 3;

        private const int ChecksumLength = (3 * KeyCheckSize) + 1;
        private const int ChecksumOffsetEnc = 1;
        private const int ChecksumOffsetMac = ChecksumOffsetEnc + KeyCheckSize;
        private const int ChecksumOffsetDek = ChecksumOffsetMac + KeyCheckSize;

        private readonly byte[] _data;
        private readonly byte[] _checksum;
        private readonly byte _p1Value;

        public YubiKeyApplication Application => YubiKeyApplication.InterIndustry;

        /// <summary>
        /// This is the expected result returned by the YubiKey after completing
        /// the command. If you want, compare the <c>PutKeyResponse.GetData</c>
        /// with this value to verify the command did indeed do what you expected.
        /// </summary>
        public ReadOnlyMemory<byte> ExpectedChecksum => _checksum;

        // The default constructor explicitly defined. We don't want it to be
        // used.
        private PutKeyCommand()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new instance of the command. When this command is executed,
        /// the <c>newKeys</c> will be installed as the keys in slot specified by
        /// <c>newKeys.KeyVersionNumber</c>. The <c>currentKeys</c> contains the
        /// keys used to make the connection. This class needs its
        /// <c>KeyVersionNumber</c> and its DEK.
        /// </summary>
        public PutKeyCommand(StaticKeys currentKeys, StaticKeys newKeys)
        {
            // If the currentKeys' KVN is not the same as the newKeys' KVN, then
            // this command is adding a key set and P1 is zero. Otherwise, this
            // command is replacing a key, so set P1 to the KVN of the currentKeys.
            _p1Value = currentKeys.KeyVersionNumber == newKeys.KeyVersionNumber ? currentKeys.KeyVersionNumber : (byte)0;

            // Build the data portion of the APDU
            //  new kvn || ENC data || MAC data || DEK data
            // where the data for each key is
            //  key type || len of block || block || len of check || check
            // The key type is AES, which is the byte 0x88
            // The block is
            //  keyLen || encrypted key data
            // The check is 3 bytes long.
            // So the data will be
            //  88 || 11 || 10 || <16 bytes> || 03 || <3-byte check>
            //            |<--   block    -->|
            // Because the YubiKey supports only AES-128, and because the key is
            // a multiple of 16 bytes, no padding is necessary. Because the
            // YubiKey only supports putting all three keys, we can know in
            // advance all the lengths, and they will be the same each time. We
            // also know in advance all the offsets where every element will go.
            _data = new byte[DataLength];
            using var memStream = new MemoryStream(_data);
            using var binaryWriter = new BinaryWriter(memStream);
            binaryWriter.Write(newKeys.KeyVersionNumber);

            _checksum = new byte[ChecksumLength];
            _checksum[0] = newKeys.KeyVersionNumber;

            byte[] currentDek = currentKeys.DataEncryptionKey.ToArray();

            try
            {
                BuildKeyDataField(binaryWriter, newKeys.ChannelEncryptionKey, ChecksumOffsetEnc, currentDek);
                BuildKeyDataField(binaryWriter, newKeys.ChannelMacKey, ChecksumOffsetMac, currentDek);
                BuildKeyDataField(binaryWriter, newKeys.DataEncryptionKey, ChecksumOffsetDek, currentDek);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(currentDek.AsSpan());
            }
        }

        public CommandApdu CreateCommandApdu() => new CommandApdu()
        {
            Cla = GpPutKeyCla,
            Ins = GpPutKeyIns,
            P1 = _p1Value,
            P2 = KeyIdentifier,
            Data = _data
        };

        public PutKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
            new PutKeyResponse(responseApdu);

        // Build the key's data field. Place it into _data beginning at
        // dataOffset.
        // Use the keyToBlock to encrypt the 16 bytes 0101...01, use the ms 3
        // bytes as the keyCheck
        // Use the encryptionKey to encrypt the keyToBlock.
        //
        //  keyType || block len || block                   || checkLen || check
        //    0x88  ||   0x17    || 0x10 || 16-byte encData ||   0x03   || 3 bytes
        private void BuildKeyDataField(
            BinaryWriter binaryWriter,
            ReadOnlyMemory<byte> keyToBlock,
            int checksumOffset,
            byte[] encryptionKey)
        {
            byte[] keyData = keyToBlock.ToArray();

            try
            {
                byte[] dataToEncrypt = new byte[AesBlockSize] {
                    1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1
                };
                byte[] checkBlock = AesUtilities.BlockCipher(keyData, dataToEncrypt.AsSpan());
                byte[] encryptedKey = AesUtilities.BlockCipher(encryptionKey, keyToBlock.Span);

                binaryWriter.Write(KeyType);
                binaryWriter.Write(BlockSize);
                binaryWriter.Write(AesBlockSize);
                binaryWriter.Write(encryptedKey);
                binaryWriter.Write(KeyCheckSize);
                binaryWriter.Write(checkBlock, 0, KeyCheckSize);
                Array.Copy(checkBlock, 0, _checksum, checksumOffset, KeyCheckSize);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyData.AsSpan());
            }
        }
    }
}

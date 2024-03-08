// Copyright 2021 Yubico AB
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

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// The valid PIV slots.
    /// </summary>
    /// <remarks>
    /// Each slot has a name and number. This class provides names to go along
    /// with the numbers.
    /// <para>
    /// For example, if you want to use the Authentication slot, specify it as
    /// <c>PivSlot.Authentication</c>. If you want to use slot 9A, specify
    /// <c>0x9A</c>. The Authentication slot and 9A are actually one and the
    /// same, but some applications or standards documents might refer to it as
    /// "Slot 9A" and others might refer to it as the "Authentication Slot".
    /// </para>
    /// <para>
    /// See the User's Manual entry on
    /// <xref href="UsersManualPivSlots"> PIV slots</xref> for more details on
    /// each of the possible slots.
    /// </para>
    /// </remarks>
    public static class PivSlot
    {
        /// <summary>
        /// PIN slot, number 0x80.<br/>This is a valid slot only with the command
        /// <see cref="Commands.GetMetadataCommand"/>.
        /// <br/>There is no cert in this slot.
        /// </summary>
        public const byte Pin = 0x80;

        /// <summary>
        /// PUK slot, number 0x81.<br/>This is a valid slot only with the command
        /// <see cref="Commands.GetMetadataCommand"/>.
        /// <br/>There is no cert in this slot.
        /// </summary>
        public const byte Puk = 0x81;

        /// <summary>
        /// Management Key slot, number 0x9B, before YubiKey 5.4.2, it can only
        /// be a Triple-DES key. Beginning with 5.4.2 it can be Triple-DES or AES.
        /// <br/>This is a valid slot only with the command
        /// <see cref="Commands.GetMetadataCommand"/>.
        /// <br/>There is no cert in this slot.
        /// <br/>Note that this is NOT the <c>KeyManagement</c> slot, which is a
        /// separate property in this class.
        /// </summary>
        public const byte Management = 0x9B;

        /// <summary>
        /// Slot 9A, the certificate and its associated private key are used to
        /// authenticate
        /// <br/>the card and the cardholder, usually for system login.
        /// </summary>
        public const byte Authentication = 0x9A;

        /// <summary>
        /// Slot 9C, the certificate and its associated private key are used for
        /// creating
        /// <br/>digital signatures, such as signing files and executables.
        /// </summary>
        public const byte Signing = 0x9C;

        /// <summary>
        /// Slot 9D, the certificate and its associated private key are are used
        /// for encryption
        /// <br/>for the purpose of confidentiality. It is generally used for
        /// things such as
        /// <br/>decrypting e-mails or encrypting/decrypting files.
        /// <br/>Note that this is NOT the "Management Key" slot, which is a
        /// separate property in this class.
        /// </summary>
        public const byte KeyManagement = 0x9D;

        /// <summary>
        /// Slot 9E, the certificate and its associated private key are used to
        /// support additional
        /// <br/>physical access applications, such as providing physical access
        /// to buildings via
        /// <br/>PIV-enabled door locks.
        /// </summary>
        public const byte CardAuthentication = 0x9E;

        /// <summary>
        /// Slot F9, the cert and key can be used to attest keys 9A, 9C, 9D, and
        /// 9E, if they were generated on the device.
        /// <br/>This is only available on YubiKey version 4.3 and later.
        /// </summary>
        public const byte Attestation = 0xF9;

        /// <summary>
        /// Slot 82, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired1 = 0x82;

        /// <summary>
        /// Slot 83, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired2 = 0x83;

        /// <summary>
        /// Slot 84, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired3 = 0x84;

        /// <summary>
        /// Slot 85, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired4 = 0x85;

        /// <summary>
        /// Slot 86, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired5 = 0x86;

        /// <summary>
        /// Slot 87, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired6 = 0x87;

        /// <summary>
        /// Slot 88, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired7 = 0x88;

        /// <summary>
        /// Slot 89, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired8 = 0x89;

        /// <summary>
        /// Slot 8A, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired9 = 0x8A;

        /// <summary>
        /// Slot 8B, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired10 = 0x8B;

        /// <summary>
        /// Slot 8C, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired11 = 0x8C;

        /// <summary>
        /// Slot 8D, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired12 = 0x8D;

        /// <summary>
        /// Slot 8E, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired13 = 0x8E;

        /// <summary>
        /// Slot 8F, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired14 = 0x8F;

        /// <summary>
        /// Slot 90, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired15 = 0x90;

        /// <summary>
        /// Slot 91, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired16 = 0x91;

        /// <summary>
        /// Slot 92, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired17 = 0x92;

        /// <summary>
        /// Slot 93, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired18 = 0x93;

        /// <summary>
        /// Slot 94, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired19 = 0x94;

        /// <summary>
        /// Slot 95, the retired key slots are meant for previously used Key
        /// Management keys to be
        /// <br/>able to decrypt earlierencrypted documents or emails.
        /// <br/>In the YubiKey all 20 of the retired slots are fully available
        /// for use.
        /// <br/>This is only available on YubiKey version 4 and later.
        /// </summary>
        public const byte Retired20 = 0x95;

        /// <summary>
        /// Is the given number a valid slot number?
        /// </summary>
        /// <remarks>
        /// This verifies that a number given is a valid slot number. For example,
        /// if the input is <c>0x9A</c>, it will return <c>true</c>. If the input
        /// is <c>0x01</c> or <c>0x77</c>, it will return <c>false</c>.
        /// <para>
        /// See the User's Manual entry on
        /// <xref href="UsersManualPivSlots"> PIV slots</xref>
        /// for more details on each of the possible slots.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The number to check.
        /// </param>
        /// <returns>
        /// True if <c>slotNumber</c> is a valid PIV slot, or False otherwise.
        /// </returns>
        public static bool IsValidSlotNumber(byte slotNumber)
        {
            // The slots are 80, 81, 82, 83, ..., 95 --- 9A, ..., 9E --- F9
            //              PIN PUK  |<- retired ->|   |<- primary ->|  Attestation
            // Ideally we would like to have a system of checking for valid slot
            // numbers not dependent on this specific layout, but rather
            // something that is only dependent on some private const values, but
            // the slots are almost certainly never going to change. If they do
            // change, this code will need to be revisited.
            return ((slotNumber >= Pin) && (slotNumber <= Retired20))
                || ((slotNumber >= Authentication) && (slotNumber <= CardAuthentication))
                || (slotNumber == Attestation);
        }

        /// <summary>
        /// Is the given number a valid slot number for generating asymmetric
        /// keys.
        /// </summary>
        /// <remarks>
        /// Note that if a slot is valid for generate, it is also valid for
        /// importing.
        /// <para>
        /// This verifies that a number given is not only a valid slot number,
        /// but a valid slot number for a slot that can generate an asymmetric
        /// key pair. For example, if the input is <c>0x9A</c>, it will return
        /// <c>true</c>. If the input is <c>0x80</c> or <c>0x9B</c>, it will
        /// return <c>false</c>. Even though <c>80</c> and <c>9B</c> are valid
        /// slot numbers, they are for slots that cannot generate asymmetric keys.
        /// </para>
        /// <para>
        /// Note that it is possible to generate a key pair in slot <c>F9</c>
        /// (attestation key). However, that would make attestation no longer
        /// possible, unless you obtain, for that key, a proper attestation
        /// certificate that chains to a supported root.
        /// </para>
        /// <para>
        /// See the User's Manual entry on
        /// <xref href="UsersManualPivSlots"> PIV slots</xref>
        /// for more details on each of the possible slots.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The number to check.
        /// </param>
        /// <returns>
        /// True if <c>slotNumber</c> is a valid PIV asymmetric key slot that can
        /// generate a new key pair, or False otherwise.
        /// </returns>
        public static bool IsValidSlotNumberForGenerate(byte slotNumber)
        {
            return (slotNumber != Management)
                && (((slotNumber >= Retired1) && (slotNumber <= Retired20))
                || ((slotNumber >= Authentication) && (slotNumber <= CardAuthentication))
                || (slotNumber == Attestation));
        }

        /// <summary>
        /// Is the given number a valid slot number for signing arbitrary data.
        /// </summary>
        /// <remarks>
        /// Note that if a slot is valid for signing, it is also valid for
        /// decrypting, key exchange, and obtaining an attestation statement as
        /// well.
        /// <para>
        /// This verifies that a number given is not only a valid slot number,
        /// but a valid slot number for a slot that can perform signing. For
        /// example, if the input is <c>0x9A</c>, it will return
        /// <c>true</c>. If the input is <c>0x80</c>, <c>0x9B</c>, or <c>F9</c>,
        /// it will return <c>false</c>. Even though <c>80</c> and <c>9B</c> are valid
        /// slot numbers, they are for slots that cannot sign. And even though
        /// <c>F9</c> holds an asymmetric key, and it will sign certificates it
        /// creates for attestation, it cannot sign arbitrary data.
        /// </para>
        /// <para>
        /// See the User's Manual entry on
        /// <xref href="UsersManualPivSlots"> PIV slots</xref>
        /// for more details on each of the possible slots.
        /// </para>
        /// </remarks>
        /// <param name="slotNumber">
        /// The number to check.
        /// </param>
        /// <returns>
        /// True if <c>slotNumber</c> is a valid PIV asymmetric key slot that can
        /// sign, or False otherwise.
        /// </returns>
        public static bool IsValidSlotNumberForSigning(byte slotNumber)
        {
            // Only slots that hold a private asymmetric key, other than F9 can
            // sign. That is, slots 9A, 9C, 9D, 9E, and 82 - 95.
            // Ideally we would like to have a system of checking for valid slot
            // numbers not dependent on the specific layout, but rather
            // something that is only dependent on some private const values, but
            // the slots are almost certainly never going to change. If they do
            // change, this code will need to be revisited.
            return (slotNumber != Management)
                && (((slotNumber >= Retired1) && (slotNumber <= Retired20))
                || ((slotNumber >= Authentication) && (slotNumber <= CardAuthentication)));
        }
    }
}

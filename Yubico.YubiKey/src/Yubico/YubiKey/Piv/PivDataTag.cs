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
    ///     The PIV standard-defined tags for the GET DATA and PUT DATA commands.
    /// </summary>
    /// <remarks>
    ///     The GET DATA and PUT DATA commands take in a "tag" indicating what data
    ///     is requested (GET) or supplied (PUT). This enum has values for each of
    ///     the tags defined in the PIV standard.
    /// </remarks>
    public enum PivDataTag
    {
        Unknown = 0,

        /// <summary>
        ///     Tag for CHUID, the Cardholder Unique ID.
        /// </summary>
        Chuid = 0x005FC102,

        /// <summary>
        ///     Tag for Cardholder Capability Container.
        /// </summary>
        Capability = 0x005FC107,

        /// <summary>
        ///     Tag for Information PIV AID plus PIN usage policy. Not available for
        ///     <c>PutDataCommand</c>
        /// </summary>
        Discovery = 0x0000007E,

        /// <summary>
        ///     Tag for the cert for the key in slot 9A.
        /// </summary>
        Authentication = 0x005FC105,

        /// <summary>
        ///     Tag for the cert for the key in slot 9C.
        /// </summary>
        Signature = 0x005FC10A,

        /// <summary>
        ///     Tag for the cert for the key in slot 9D.
        /// </summary>
        KeyManagement = 0x005FC10B,

        /// <summary>
        ///     Tag for the cert for the key in slot 9E.
        /// </summary>
        CardAuthentication = 0x005FC101,

        /// <summary>
        ///     Tag for the cert for the key in slot 82.
        /// </summary>
        Retired1 = 0x005FC10D,

        /// <summary>
        ///     Tag for the cert for the key in slot 83.
        /// </summary>
        Retired2 = 0x005FC10E,

        /// <summary>
        ///     Tag for the cert for the key in slot 84.
        /// </summary>
        Retired3 = 0x005FC10F,

        /// <summary>
        ///     Tag for the cert for the key in slot 85.
        /// </summary>
        Retired4 = 0x005FC110,

        /// <summary>
        ///     Tag for the cert for the key in slot 86.
        /// </summary>
        Retired5 = 0x005FC111,

        /// <summary>
        ///     Tag for the cert for the key in slot 87.
        /// </summary>
        Retired6 = 0x005FC112,

        /// <summary>
        ///     Tag for the cert for the key in slot 88.
        /// </summary>
        Retired7 = 0x005FC113,

        /// <summary>
        ///     Tag for the cert for the key in slot 89.
        /// </summary>
        Retired8 = 0x005FC114,

        /// <summary>
        ///     Tag for the cert for the key in slot 8A.
        /// </summary>
        Retired9 = 0x005FC115,

        /// <summary>
        ///     Tag for the cert for the key in slot 8B.
        /// </summary>
        Retired10 = 0x005FC116,

        /// <summary>
        ///     Tag for the cert for the key in slot 8C.
        /// </summary>
        Retired11 = 0x005FC117,

        /// <summary>
        ///     Tag for the cert for the key in slot 8D.
        /// </summary>
        Retired12 = 0x005FC118,

        /// <summary>
        ///     Tag for the cert for the key in slot 8E.
        /// </summary>
        Retired13 = 0x005FC119,

        /// <summary>
        ///     Tag for the cert for the key in slot 8F.
        /// </summary>
        Retired14 = 0x005FC11A,

        /// <summary>
        ///     Tag for the cert for the key in slot 90.
        /// </summary>
        Retired15 = 0x005FC11B,

        /// <summary>
        ///     Tag for the cert for the key in slot 91.
        /// </summary>
        Retired16 = 0x005FC11C,

        /// <summary>
        ///     Tag for the cert for the key in slot 92.
        /// </summary>
        Retired17 = 0x005FC11D,

        /// <summary>
        ///     Tag for the cert for the key in slot 93.
        /// </summary>
        Retired18 = 0x005FC11E,

        /// <summary>
        ///     Tag for the cert for the key in slot 94.
        /// </summary>
        Retired19 = 0x005FC11F,

        /// <summary>
        ///     Tag for the cert for the key in slot 95.
        /// </summary>
        Retired20 = 0x005FC120,

        /// <summary>
        ///     Tag for information printed on the card. Not available for
        ///     <c>PutDataCommand</c>
        /// </summary>
        /// <remarks>
        ///     Note that Yubico uses this tag, and if you GET the data stored in the
        ///     Printed tag, it might contain non-standard information.
        /// </remarks>
        Printed = 0x005FC109,

        /// <summary>
        ///     Tag for the security object.
        /// </summary>
        SecurityObject = 0x005FC106,

        /// <summary>
        ///     Tag for the key history object.
        /// </summary>
        KeyHistory = 0x005FC10C,

        /// <summary>
        ///     Tag for the cardholder iris images.
        /// </summary>
        IrisImages = 0x005FC121,

        /// <summary>
        ///     Tag for the cardholder facial image.
        /// </summary>
        FacialImage = 0x005FC108,

        /// <summary>
        ///     Tag for the cardholder fingerprints.
        /// </summary>
        Fingerprints = 0x005FC103,

        /// <summary>
        ///     Tag for the biometric information group template.
        /// </summary>
        BiometricGroupTemplate = 0x00007F61,

        /// <summary>
        ///     Tag for the secure messaging certificate signer.
        /// </summary>
        SecureMessageSigner = 0x005FC122,

        /// <summary>
        ///     Tag for the pairing code reference data.
        /// </summary>
        PairingCodeReferenceData = 0x005FC123
    }
}

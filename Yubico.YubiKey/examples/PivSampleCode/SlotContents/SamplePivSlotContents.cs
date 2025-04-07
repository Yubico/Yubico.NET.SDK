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

using System;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Piv;
using Yubico.YubiKey.Sample.SharedCode;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class stores information about what is in a slot.
    public class SamplePivSlotContents
    {
        public byte SlotNumber { get; set; }

        public PivAlgorithm Algorithm { get; set; }

        public PivPinPolicy PinPolicy { get; set; }

        public PivTouchPolicy TouchPolicy { get; set; }

        public PivPublicKey PublicKey { get; set; }

        public CertificateRequest CertRequest { get; set; }

        private byte[] _certRequestDer;

        public SamplePivSlotContents()
        {
            Algorithm = PivAlgorithm.None;
            PublicKey = new PivPublicKey();
            _certRequestDer = Array.Empty<byte>();
        }

        public void PrintPublicKeyPem()
        {
            char[] pubKeyPem;

            if (PublicKey.Algorithm is PivAlgorithm.EccX25519 or PivAlgorithm.EccEd25519) 
            {
                var publicKeyParameters = KeyParametersPivHelper.CreatePublicKeyParameters(
                    PublicKey.PivEncodedPublicKey, 
                    PublicKey.Algorithm.GetKeyType());
                
                pubKeyPem = PemOperations.BuildPem("PUBLIC KEY", publicKeyParameters.ExportSubjectPublicKeyInfo());
            } else {
                pubKeyPem = KeyConverter.GetPemFromPivPublicKey(PublicKey);
            }
            SampleMenu.WriteMessage(MessageType.Title, 0, "\n" + new string(pubKeyPem) + "\n");
        }

        public char[] GetCertRequestPem()
        {
            return PemOperations.BuildPem("CERTIFICATE REQUEST", _certRequestDer);
        }

        public void SetCertRequestDer(byte[] certRequestDer)
        {
            _certRequestDer = certRequestDer;
        }

        public byte[] GetCertRequestDer()
        {
            return _certRequestDer;
        }

        public void PrintCertRequestPem()
        {
            if (_certRequestDer.Length > 0)
            {
                char[] requestPem = GetCertRequestPem();
                SampleMenu.WriteMessage(MessageType.Title, 0, "\n" + new string(requestPem) + "\n");
            }
        }
    }
}

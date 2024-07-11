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
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    // Use this class to "convert" between formats. This currently supports certs
    // in the following formats:
    //
    //   PEM string (BEGIN CERTIFICATE)
    //   DER encoding of the certificate
    //   X509Certificate2
    //
    // Once the object is built containing the cert, it will be possible to get
    // the other formats out. It will also be possible to get the public key out
    // as well. Get the public key as an instance of the following classes:
    //   RSA
    //   ECDsa
    //   PivPublicKey
    public class CertConverter : IDisposable
    {
        private const string CertificateStart = "-----BEGIN CERTIFICATE-----\n";
        private const string CertificateEnd = "\n-----END CERTIFICATE-----";
        private const int CertificateStartLength = 27;
        private const int CertificateEndLength = 25;

        private readonly X509Certificate2 _certificateObject;
        private bool _disposedValue;

        // Get the algorithm of the subject public key.
        public PivAlgorithm Algorithm { get; private set; }

        // Get the bit size of the subject public key.
        public int KeySize { get; private set; }

        // Build a local cert object from the "string".
        // Note that this takes in a char array. This is to match KeyConverter,
        // which uses a char array so that you can overwrite sensitive data if
        // you want. The certificate PEM string will not hold any sensitive data,
        // so there is no need to overwrite, but the input is the same
        // nonetheless.
        // This constructor expects the buffer to contain one cert only, and
        // must be of the form
        //    -----BEGIN CERTIFICATE-----
        //    <base64 data>
        //    -----END CERTIFICATE-----
        // If there are any "stray" characters at the beginning or end, this
        // constructor will not build a cert.
        public CertConverter(char[] pemCertString)
        {
            byte[] certDer = Convert.FromBase64CharArray(
                pemCertString,
                CertificateStartLength,
                pemCertString.Length - (CertificateStartLength + CertificateEndLength));
            _certificateObject = new X509Certificate2(certDer);
            SetAlgorithm();
        }

        // Build a local cert object from the DER encoding of an X.509 cert.
        public CertConverter(byte[] certDer)
        {
            _certificateObject = new X509Certificate2(certDer);
            SetAlgorithm();
        }

        // Build a local cert object from the given input object.
        // This method will build its own copy of the cert object, it will not
        // copy a reference.
        // An X509Certificate2 object can contain the private key. If the input
        // object does indeed contain the private key, this constructor will not
        // copy it into the local object.
        public CertConverter(X509Certificate2 cert)
        {
            byte[] certDer = cert.GetRawCertData();
            _certificateObject = new X509Certificate2(certDer);
            SetAlgorithm();
        }

        // Get the cert in the form of an X509Certificate2 object.
        // This will return a new object, not a reference.
        // If this CertConverter class had been built using an X509Certificate2
        // object, and that object had contained the private key, the returned
        // object here will not contain that private key.
        public X509Certificate2 GetCertObject()
        {
            byte[] certDer = _certificateObject.GetRawCertData();
            return new X509Certificate2(certDer);
        }

        // Return the DER encoding of the X.509 certificate.
        public byte[] GetCertDer()
        {
            return _certificateObject.GetRawCertData();
        }

        public char[] GetCertPem()
        {
            byte[] certDer = _certificateObject.GetRawCertData();
            char[] prefix = CertificateStart.ToCharArray();
            char[] suffix = CertificateEnd.ToCharArray();

            // The length of the char array will be the lengths of the
            // prefix and suffix, along with the length of the Base64
            // data, and new line characters. Create an upper bound.
            int blockCount = (certDer.Length + 2) / 3;
            int totalLength = blockCount * 4;
            int lineCount = (totalLength + 75) / 76;
            totalLength += lineCount * 4;
            totalLength += prefix.Length;
            totalLength += suffix.Length;

            char[] temp = new char[totalLength];
            Array.Copy(prefix, 0, temp, 0, prefix.Length);
            int count = Convert.ToBase64CharArray(
                certDer, 0, certDer.Length,
                temp, prefix.Length,
                Base64FormattingOptions.InsertLineBreaks);
            Array.Copy(suffix, 0, temp, prefix.Length + count, suffix.Length);
            totalLength = prefix.Length + suffix.Length + count;
            char[] returnValue = new char[totalLength];
            Array.Copy(temp, 0, returnValue, 0, totalLength);

            return returnValue;
        }

        // Get the public key out of the cert. Return it as a PivPublicKey.
        // This method will return a new object, it will not return a reference.
        public PivPublicKey GetPivPublicKey()
        {
            if (Algorithm.IsRsa())
            {
                RSA? rsaObject = _certificateObject.PublicKey.GetRSAPublicKey()!;
                RSAParameters rsaParams = rsaObject.ExportParameters(false);
                return new PivRsaPublicKey(rsaParams.Modulus, rsaParams.Exponent);
            }
            if (Algorithm.IsEcc())
            {
                return new PivEccPublicKey(_certificateObject.PublicKey.EncodedKeyValue.RawData);
            }
            throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm);
        }

        // Return a new RSA object.
        // This will create a new object each time it is called, you will likely
        // want to use the using key word.
        //   using RSA rsaObject = certConverter.GetRsaObject();
        // The resulting RSA object will contain only public key data.
        // If this CertConverter object does not contain an RSA key, it will
        // throw an exception.
        public RSA GetRsaObject()
        {
            if (!Algorithm.IsRsa())
            {
                throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm);
            }

            RSA? rsaObject = _certificateObject.PublicKey.GetRSAPublicKey()!;
            RSAParameters rsaParams = rsaObject.ExportParameters(false);

            return RSA.Create(rsaParams);
        }

        // Return a new ECDsa object.
        // This will create a new object each time it is called, you will likely
        // want to use the using key word.
        //   using ECDsa eccObject = certConverter.GetEccObject();
        // The resulting ECDsa object will contain only public key data.
        // If this CertConverter object does not contain an ECC key, it will
        // throw an exception.
        public ECDsa GetEccObject()
        {
            int coordLength = KeySize / 8;
            var eccCurve = ECCurve.CreateFromValue("1.2.840.10045.3.1.7");
            if (Algorithm != PivAlgorithm.EccP256)
            {
                if (Algorithm != PivAlgorithm.EccP384)
                {
                    throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm);
                }
                eccCurve = ECCurve.CreateFromValue("1.3.132.0.34");
            }
            var eccParams = new ECParameters
            {
                Curve = (ECCurve)eccCurve
            };

            byte[] xCoord = new byte[coordLength];
            byte[] yCoord = new byte[coordLength];
            Array.Copy(_certificateObject.PublicKey.EncodedKeyValue.RawData, 1, xCoord, 0, coordLength);
            Array.Copy(_certificateObject.PublicKey.EncodedKeyValue.RawData, coordLength + 1, yCoord, 0, coordLength);

            eccParams.Q.X = xCoord;
            eccParams.Q.Y = yCoord;

            return ECDsa.Create(eccParams);
        }

        // If the algorithm and/or key size is not supported, throw an exception.
        private void SetAlgorithm()
        {
            KeySize = 0;

            if (string.Compare(_certificateObject.PublicKey.Oid.FriendlyName, "RSA") == 0)
            {
                KeySize = _certificateObject.PublicKey.GetRSAPublicKey()!.KeySize;
            }
            else if (string.Compare(_certificateObject.PublicKey.Oid.FriendlyName, "ECC") == 0)
            {
                byte[] oid256 = new byte[] { 0x06, 0x08, 0x2a, 0x86, 0x48, 0xce, 0x3d, 0x03, 0x01, 0x07 };
                byte[] oid384 = new byte[] { 0x06, 0x05, 0x2b, 0x81, 0x04, 0x00, 0x22 };
                if (oid256.SequenceEqual(_certificateObject.PublicKey.EncodedParameters.RawData))
                {
                    KeySize = 256;
                }
                else if (oid384.SequenceEqual(_certificateObject.PublicKey.EncodedParameters.RawData))
                {
                    KeySize = 384;
                }
            }

            Algorithm = KeySize switch
            {
                256 => PivAlgorithm.EccP256,
                384 => PivAlgorithm.EccP384,
                1024 => PivAlgorithm.Rsa1024,
                2048 => PivAlgorithm.Rsa2048,
                3072 => PivAlgorithm.Rsa3072,
                4096 => PivAlgorithm.Rsa4096,
                _ => throw new ArgumentException(ExceptionMessages.UnsupportedAlgorithm),
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _certificateObject.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

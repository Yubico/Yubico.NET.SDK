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
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    // Use this class to "convert" between formats. This currently supports keys
    // in the following formats:
    //
    //   PEM string (PRIVATE KEY or PUBLIC KEY)
    //   PivPublicKey
    //   PivPrivateKey
    //   System.Security.Cryptography.RSA
    //   System.Security.Cryptography.ECDsa
    //
    // Once the object has the key data, it can build new objects in any of the
    // supported formats. For example, you can convert a PEM string into a
    // PivPublicKey or a PivPrivateKey into a C# RSA object (assuming the
    // PivPrivateKey contained an RSA object to begin with).
    //
    // Note that there are three conversions that are not possible in this class:
    //   ECC PivPrivateKey to ECDsa
    //   ECC PivPrivateKey to PEM string
    //   ECC PivPrivateKey to PivPublicKey
    // That is, if you use the KeyConverter constructor that takes in a
    // PivPrivateKey object and that object is an ECC private key, then you will
    // not be able to get an ECDsa object (see method GetEccObject), nor a PEM
    // string (see method GetPemKeyString), nor a PivPublicKey object (see method
    // GetPivPublicKey). The reason is that the PivPrivateKey class contains the
    // ECC private key only. However, the default ECC implementation from C# (the
    // implementation this class uses) will only operate on ECC private keys that
    // also contain the public key as well (even though the public key is not
    // needed to perform ECC private key operations). Furthermore, the default
    // implementation of ECC does not provide a way to derive the public key from
    // the private key (although that is a simple operation).
    // If you build a KeyConverter object using the constructor that takes in the
    // private key PEM string or the ECDsa object containing the private key, you
    // will be able to build the PEM string and a new ECDsa object as well as a
    // new PivPublicKey or PivPrivateKey.
    //
    // Build the object, then check the IsPrivate and Algorithm properties. From
    // those you will be able to know what formats are possible to get.
    // For example, build an object from the PEM string and get the RSA, ECDsa,
    // PivPublicKey, or PivPrivateKey objects.
    // Build an object from RSA or ECDsa, and get a PEM string or Piv key object.
    // Build an object from a PivPublicKey object and get a PEM string or RSA or
    // ECDsa object.
    // It is also possible to get a public key from a private key.
    public class KeyConverter
    {
        private const string RequestedKeyMessage = "Requested key was unavailable.";
        private const string InvalidKeyDataMessage = "The input key data was not recognized.";
        private const string PrivateKeyStart = "-----BEGIN PRIVATE KEY-----";
        private const string PrivateKeyEnd = "-----END PRIVATE KEY-----";
        private const string PublicKeyStart = "-----BEGIN PUBLIC KEY-----";
        private const string PublicKeyEnd = "-----END PUBLIC KEY-----";
        private const int PrivateStartLength = 27;
        private const int PrivateEndLength = 25;
        private const int PublicStartLength = 26;
        private const int PublicEndLength = 24;

        // Use these values in the method IsKeyAvailable to query whether a
        // particular key can be returned.
        public const int KeyTypePemPublic = 1;
        public const int KeyTypePemPrivate = 2;
        public const int KeyTypeRsaPublic = 3;
        public const int KeyTypeRsaPrivate = 4;
        public const int KeyTypeECDsaPublic = 5;
        public const int KeyTypeECDsaPrivate = 6;
        public const int KeyTypePivPublic = 7;
        public const int KeyTypePivPrivate = 8;

        public bool IsPrivate { get; private set; }
        public PivAlgorithm Algorithm { get; private set; }

        private PivPrivateKey _pivPrivateKey = new PivPrivateKey();
        private PivPublicKey _pivPublicKey = new PivPublicKey();

        // Build a local key object from the "string". If the string is for a
        // private key, it will build a private key, if it is for a public key,
        // it will build a public key.
        // Note that this takes in a char array. This is so that you can overwrite
        // sensitive data if you want. The string class is immutable, so if you
        // have a private key in PEM format, you cannot overwrite it when it's no
        // longer needed. But if you have your private key as a char[], you can
        // overwrite it when you're done with it (Array.Fill).
        // If you want to deal with strings, and are not concerned with
        // overwriting buffers, you can still use strings, just use the
        // ToCharArray method if your PEM data is a string, and if you have
        // output from this class as a char[], use the String constructor that
        // takes in a char[].
        // This constructor expects the buffer to contain one key only, and
        // must be of the form
        //    -----BEGIN PRIVATE KEY-----
        //    <base64 data>
        //    -----END PRIVATE KEY-----
        // or
        //    -----BEGIN PUBLIC KEY-----
        //    <base64 data>
        //    -----END PUBLIC KEY-----
        // If there are any "stray" characters at the beginning or end, this
        // constructor will not build a key.
        // If the PEM is a private key, the constructor will build its own local
        // copy of a private key and a public key.
        public KeyConverter(
            string pemKeyString) :
            this(pemKeyString.ToCharArray())
        {
        }

        public KeyConverter(
            char[] pemKeyString)
        {
            // Search for the PublicKeyStart and End or PrivateKeyStart and End.
            if (VerifyPemHeaderAndFooter(pemKeyString, PublicKeyStart.ToCharArray(), PublicKeyEnd.ToCharArray()))
            {
                BuildPivPublicKey(pemKeyString);
            }
            else if (VerifyPemHeaderAndFooter(pemKeyString, PrivateKeyStart.ToCharArray(), PrivateKeyEnd.ToCharArray()))
            {
                BuildPivPrivateKey(pemKeyString);
            }

            SetProperties(true);
        }

        // This will clone the input key object, so that the KeyConverter object
        // will have its own copy.
        public KeyConverter(
            PivPublicKey pivPublicKey)
        {
            if (pivPublicKey.Algorithm != PivAlgorithm.None)
            {
                _pivPublicKey = PivPublicKey.Create(pivPublicKey.PivEncodedPublicKey);
            }

            SetProperties(true);
        }

        // This will clone the input key object, so that the KeyConverter object
        // will have its own copy.
        // Note that if you build a KeyConverter object using this constructor
        // with an ECC key, you will not be able to get an ECDsa object (see
        // method GetEccObject). The PivPrivateKey class does not contain the
        // public point (a private key does not need the public key in order to
        // perform its operations), but the default implementation of ECC in C#
        // (what this class uses) requires the public point as well.
        public KeyConverter(
            PivPrivateKey pivPrivateKey)
        {
            if (pivPrivateKey.Algorithm != PivAlgorithm.None)
            {
                _pivPrivateKey = PivPrivateKey.Create(pivPrivateKey.EncodedPrivateKey);
            }

            SetProperties(true);
        }

        // A System.Security.Cryptography.RSA object can contain a private key, a
        // public key or both.
        // If you expect this key to be private, set isPrivate to true. If that
        // arg is true, the constructor will build its own local copy of a
        // private key and a public key.
        // If the arg is true and the RSA object does not contain a private key,
        // the constructor will throw an exception.
        // If that arg is false, this constructor will build only a public key,
        // even if the RSA object contains the private key.
        public KeyConverter(
            RSA rsaObject,
            bool isPrivate)
        {
            if (isPrivate)
            {
                BuildPivPrivateKey(rsaObject);
            }

            BuildPivPublicKey(rsaObject);

            SetProperties(true);
        }

        // A System.Security.Cryptography.ECDsa object can contain a private key,
        // a public key or both.
        // If you expect this key to be private, set isPrivate to true. If that
        // arg is true, the constructor will build its own local copy of a
        // private key and a public key.
        // If the arg is true and the ECDsa object does not contain a private key,
        // the constructor will throw an exception.
        // If that arg is false, this constructor will build only a public key,
        // even if the ECDsa object contains the private key.
        public KeyConverter(
            ECDsa eccObject,
            bool isPrivate)
        {
            if (isPrivate)
            {
                BuildPivPrivateKey(eccObject);
            }

            BuildPivPublicKey(eccObject);

            SetProperties(true);
        }


        // This lets you know if you will be able to get a particular key out of
        // this object.
        // The keyType argument is one of the "KeyType" values defined in this
        // class, such as KeyConverter.KeyTypePemPublic and
        // KeyConverter.KeyTypeECDsaPrivate.
        // If the return from this method is true, then you know that if you
        // call the Get method for that particular type, you will be able to get
        // a key. If it is false, you know it is not available.
        // For example, suppose you create a KeyConverter using the constructor
        // that takes in a PEM key. Suppose the PEM string was for an RSA public
        // key. Now call this method with KeyTypeRsaPublic or KeyTypePivPublic
        // and the return will be true, because you will be able to call
        // GetRsaObject or GetPivPublicKey.
        // But suppose you call this method with KeyTypeECDsaPublic, it will
        // return false, because this instance of KeyConverter will not be able
        // to build an ECDsa object from an RSA public key.
        // Note that if you build a KeyConverter object using the constructor
        // that takes in a PivPrivateKey and that key is an EC private key.
        // Calling this method with KeyTypeECDsaPrivate will return false. This
        // is because this class cannot build an ECDsa object from the data found
        // in a PivPrivateKey.
        public bool IsKeyAvailable(
            int keyType)
        {
            bool returnValue = false;

            switch (keyType)
            {
                default:
                    break;

                case KeyTypePemPublic:
                    if (_pivPublicKey.Algorithm != PivAlgorithm.None)
                    {
                        returnValue = true;
                    }

                    break;

                case KeyTypePemPrivate:
                    if (_pivPrivateKey.Algorithm == PivAlgorithm.None)
                    {
                        break;
                    }

                    // If the algorithm is ECC there has to be a public key as
                    // well, or else we can't return a PEM key string.
                    if (Algorithm == PivAlgorithm.EccP256 || Algorithm == PivAlgorithm.EccP384)
                    {
                        if (_pivPublicKey.Algorithm == PivAlgorithm.None)
                        {
                            break;
                        }
                    }

                    returnValue = true;
                    break;

                case KeyTypeRsaPublic:
                    if (Algorithm == PivAlgorithm.Rsa1024 || Algorithm == PivAlgorithm.Rsa2048)
                    {
                        returnValue = true;
                    }

                    break;

                case KeyTypeRsaPrivate:
                    if (_pivPrivateKey.Algorithm == PivAlgorithm.Rsa1024 ||
                        _pivPrivateKey.Algorithm == PivAlgorithm.Rsa2048)
                    {
                        returnValue = true;
                    }

                    break;

                case KeyTypeECDsaPublic:
                    if (_pivPublicKey.Algorithm == PivAlgorithm.EccP256 ||
                        _pivPublicKey.Algorithm == PivAlgorithm.EccP384)
                    {
                        returnValue = true;
                    }

                    break;

                case KeyTypeECDsaPrivate:
                    if (_pivPrivateKey.Algorithm == PivAlgorithm.EccP256 ||
                        _pivPrivateKey.Algorithm == PivAlgorithm.EccP384)
                    {
                        if (_pivPublicKey.Algorithm != PivAlgorithm.None)
                        {
                            returnValue = true;
                        }
                    }

                    break;

                case KeyTypePivPublic:
                    if (_pivPublicKey.Algorithm != PivAlgorithm.None)
                    {
                        returnValue = true;
                    }

                    break;

                case KeyTypePivPrivate:
                    if (_pivPrivateKey.Algorithm != PivAlgorithm.None)
                    {
                        returnValue = true;
                    }

                    break;
            }

            return returnValue;
        }

        // Get a new instance of a PivPublicKey from this object.
        // If this object cannot return a PivPublicKey, throw a exception.
        // If it can build a PivPublicKey, this method will build a new object,
        // it will not return a reference.
        // This method will be able to build an RSA public key from an RSA
        // private key, but it might not be able to build an ECC public key from
        // an ECC private key.
        public PivPublicKey GetPivPublicKey()
        {
            // if (_pivPublicKey.Algorithm == PivAlgorithm.EccX25519)
            // {
            //     var testPublicKey = TestKeys.GetPublicKey(_pivPublicKey.Algorithm);
            //     var last32Bytes = testPublicKey.KeyBytes.AsSpan()[^32..];
            //     var pivPublicKey = new PivEccPublicKey(last32Bytes, KeyType.Ed25519);
            //     return pivPublicKey;
            // }

            if (_pivPublicKey.Algorithm != PivAlgorithm.None)
            {
                return PivPublicKey.Create(_pivPublicKey.PivEncodedPublicKey);
            }

            if (_pivPrivateKey.Algorithm == PivAlgorithm.Rsa1024 || _pivPrivateKey.Algorithm == PivAlgorithm.Rsa2048)
            {
                byte[] primeP = Array.Empty<byte>();
                byte[] primeQ = Array.Empty<byte>();
                try
                {
                    var rsaPrivate = (PivRsaPrivateKey)_pivPrivateKey;
                    primeP = rsaPrivate.PrimeP.ToArray();
                    primeQ = rsaPrivate.PrimeQ.ToArray();
                    byte[] modulus = GetModulusFromPrimes(primeP, primeQ);
                    byte[] exponent = new byte[] { 0x01, 0x00, 0x01 };
                    var rsaPublic = new PivRsaPublicKey(modulus, exponent);
                    return (PivPublicKey)rsaPublic;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(primeP);
                    CryptographicOperations.ZeroMemory(primeQ);
                }
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RequestedKeyMessage));
        }

        // Get a new instance of a PivPrivateKey from this object.
        // If this object does not contain a PivPrivateKey, this method will
        // throw an exception.
        // If it has a private key, this method will build a new object, it will
        // not return a reference.
        public PivPrivateKey GetPivPrivateKey()
        {
            // if (_pivPrivateKey.Algorithm == PivAlgorithm.EccEd25519) // This is the simple one
            // {
            //     var testPrivateKey = TestKeys.GetPrivateKey(_pivPrivateKey.Algorithm);
            //     var last32Bytes = testPrivateKey.KeyBytes.AsSpan()[^32..];
            //     var pivPrivateKey = new PivEccPrivateKey(last32Bytes.ToArray(), PivAlgorithm.EccEd25519);
            //     return pivPrivateKey;
            // }
            //
            // if (_pivPrivateKey.Algorithm == PivAlgorithm.EccX25519) // This is the simple one
            // {
            //     var testPrivateKey = TestKeys.GetPrivateKey(_pivPrivateKey.Algorithm);
            //     var last32Bytes = testPrivateKey.KeyBytes.AsSpan()[^32..];
            //     var pivPrivateKey = new PivEccPrivateKey(last32Bytes.ToArray(), PivAlgorithm.EccEd25519);
            //     return pivPrivateKey;
            // }
            //
            // if (_pivPrivateKey.Algorithm ==
            //     PivAlgorithm
            //         .EccEd25519) // This is good as well, but a bit too complex. However it could be used to replace keyconverter
            // {
            //     var testPrivateKey = TestKeys.GetPrivateKey(_pivPrivateKey.Algorithm);
            //     var parser = new PrivateKeyInfoParser();
            //     var keyInfo = parser.ParsePrivateKey<EdPrivateKeyInfo>(testPrivateKey.KeyBytes);
            //     var pivPrivateKey = new PivEccPrivateKey(keyInfo.PrivateKey, _pivPrivateKey.Algorithm);
            //     return pivPrivateKey;
            // }

            if (_pivPrivateKey.Algorithm != PivAlgorithm.None)
            {
                return PivPrivateKey.Create(_pivPrivateKey.EncodedPrivateKey);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RequestedKeyMessage));
        }

        // Return a new RSA object.
        // This will create a new object each time it is called, you will likely
        // want to use the using key word.
        //   using RSA rsaObject = keyConverter.GetRsaObject();
        // If this KeyConverter object contains only a public key, the resulting
        // RSA object will contain only public key data.
        // If this KeyConverter object does not contain an RSA key, it will
        // throw an exception.
        public RSA GetRsaObject()
        {
            var rsaParams = new RSAParameters();

            try
            {
                if (_pivPrivateKey.Algorithm.IsRsa())
                {
                    var rsaPrivate = (PivRsaPrivateKey)_pivPrivateKey;
                    rsaParams.P = rsaPrivate.PrimeP.ToArray();
                    rsaParams.Q = rsaPrivate.PrimeQ.ToArray();
                    rsaParams.DP = rsaPrivate.ExponentP.ToArray();
                    rsaParams.DQ = rsaPrivate.ExponentQ.ToArray();
                    rsaParams.InverseQ = rsaPrivate.Coefficient.ToArray();
                    rsaParams.Exponent = new byte[] { 0x01, 0x00, 0x01 };
                    rsaParams.Modulus = GetModulusFromPrimes(rsaParams.P, rsaParams.Q);
                    rsaParams.D = GetPrivateExponentFromPrimes(rsaParams.P, rsaParams.Q, rsaParams.Exponent);

                    return RSA.Create(rsaParams);
                }

                if (_pivPublicKey.Algorithm.IsRsa())
                {
                    var rsaPublic = (PivRsaPublicKey)_pivPublicKey;
                    rsaParams.Modulus = rsaPublic.Modulus.ToArray();
                    rsaParams.Exponent = rsaPublic.PublicExponent.ToArray();

                    return RSA.Create(rsaParams);
                }
            }
            finally
            {
                CryptoSupport.ClearRsaParameters(rsaParams);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RequestedKeyMessage));
        }

        // Return a new ECDsa object.
        // This will create a new object each time it is called, you will likely
        // want to use the using key word.
        //   using ECDsa eccObject = keyConverter.GetEccObject();
        // If this KeyConverter object contains a public key, the ECDsa object will
        // only contain public key data.
        // If this KeyConverter object contains only a private key (the
        // KeyConverter class was constructed using a PivPrivateKey), it will not
        // be able to build an ECDsa object. The reason is that the ECDsa class
        // requires either the public key only or both the public and private key
        // to build it.
        // The PivPrivateKey object contains only the private key (and C# does
        // not provide a way to derive the public key from the private). But if
        // this KeyConverter object was built from an ECDsa object or a PEM
        // string, it will be able to return the ECDsa object.
        // If this KeyConverter cannot build an ECC key, it will throw an
        // exception.
        public ECDsa GetEccObject()
        {
            var eccCurve = ECCurve.CreateFromValue(KeyDefinitions.CryptoOids.P256);
            if (_pivPublicKey.Algorithm != PivAlgorithm.EccP256)
            {
                if (_pivPublicKey.Algorithm != PivAlgorithm.EccP384)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            RequestedKeyMessage));
                }

                eccCurve = ECCurve.CreateFromValue(KeyDefinitions.CryptoOids.P384);
            }

            var eccParams = new ECParameters
            {
                Curve = (ECCurve)eccCurve
            };

            try
            {
                var eccPublic = (PivEccPublicKey)_pivPublicKey;

                int coordLength = (eccPublic.PublicPoint.Length - 1) / 2;
                eccParams.Q.X = eccPublic.PublicPoint.Slice(1, coordLength).ToArray();
                eccParams.Q.Y = eccPublic.PublicPoint.Slice(1 + coordLength, coordLength).ToArray();
                if (_pivPrivateKey.Algorithm != PivAlgorithm.None)
                {
                    var eccPrivate = (PivEccPrivateKey)_pivPrivateKey;
                    eccParams.D = eccPrivate.PrivateValue.ToArray();
                }

                return ECDsa.Create(eccParams);
            }
            finally
            {
                CryptoSupport.ClearEccParameters(eccParams);
            }
        }

        // Return a new string containing the PEM key. If this object holds a
        // private key (see property (IsPrivate), it will return a string
        // beginning with
        //  -----BEGIN PRIVATE KEY-----
        // If it holds a public key only, it will return a string with
        //  -----BEGIN PUBLIC KEY-----
        // Note that if you built the KeyConverter object using the constructor
        // that takes in a PivPrivateKey, and that key is ECC, then you will not
        // be able to build the PEM string.
        // Note that this returns a char array. This is so that you can overwrite
        // sensitive data if you want. The string class is immutable, so if you
        // have a private key in PEM format, you cannot overwrite it when it's no
        // longer needed. But if you have your private key as a char[], you can
        // overwrite it when you're done with it (Array.Fill).
        // If you want to deal with strings, and are not concerned with
        // overwriting buffers, you can still use strings, just use the
        // ToCharArray method if your PEM data is a string, and if you have
        // output from this class as a char[], use the String constructor that
        // takes in a char[].
        // If this method cannot build the PEM, it will throw an exception.
        public char[] GetPemKeyString()
        {
            byte[] encodedKey = Array.Empty<byte>();
            char[] temp = Array.Empty<char>();
            char[] prefix;
            char[] suffix;
            if (IsPrivate)
            {
                string keyStart = PrivateKeyStart + "\n";
                string keyEnd = "\n" + PrivateKeyEnd;
                prefix = keyStart.ToCharArray();
                suffix = keyEnd.ToCharArray();
            }
            else
            {
                string keyStart = PublicKeyStart + "\n";
                string keyEnd = "\n" + PublicKeyEnd;
                prefix = keyStart.ToCharArray();
                suffix = keyEnd.ToCharArray();
            }

            try
            {
                if (Algorithm == PivAlgorithm.Rsa1024 || Algorithm == PivAlgorithm.Rsa2048)
                {
                    using RSA rsaObject = GetRsaObject();
                    if (IsPrivate)
                    {
                        encodedKey = rsaObject.ExportPkcs8PrivateKey();
                    }
                    else
                    {
                        encodedKey = rsaObject.ExportSubjectPublicKeyInfo();
                    }
                }
                else if (Algorithm == PivAlgorithm.EccP256 || Algorithm == PivAlgorithm.EccP384)
                {
                    using ECDsa eccObject = GetEccObject();
                    if (IsPrivate)
                    {
                        encodedKey = eccObject.ExportPkcs8PrivateKey();
                    }
                    else
                    {
                        encodedKey = eccObject.ExportSubjectPublicKeyInfo();
                    }
                }

                if (encodedKey.Length != 0)
                {
                    // The length of the char array will be the lengths of the
                    // prefix and suffix, along with the length of the Base64
                    // data, and new line characters. Create an upper bound.
                    int blockCount = (encodedKey.Length + 2) / 3;
                    int totalLength = blockCount * 4;
                    int lineCount = (totalLength + 75) / 76;
                    totalLength += lineCount * 4;
                    totalLength += prefix.Length;
                    totalLength += suffix.Length;

                    temp = new char[totalLength];
                    Array.Copy(prefix, 0, temp, 0, prefix.Length);
                    int count = Convert.ToBase64CharArray(
                        encodedKey, 0, encodedKey.Length,
                        temp, prefix.Length,
                        Base64FormattingOptions.InsertLineBreaks);
                    Array.Copy(suffix, 0, temp, prefix.Length + count, suffix.Length);
                    totalLength = prefix.Length + suffix.Length + count;
                    char[] returnValue = new char[totalLength];
                    Array.Copy(temp, 0, returnValue, 0, totalLength);

                    return returnValue;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encodedKey);
                Array.Fill<char>(temp, (char)0);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RequestedKeyMessage));
        }

        // When done with the object, clear it if you want.
        public void Clear()
        {
            _pivPrivateKey?.Clear();

            _pivPrivateKey = new PivPrivateKey();
            _pivPublicKey = new PivPublicKey();

            SetProperties(false);
        }

        // Make sure any properties are correctly set.
        // If exceptionOnNoData is true, then throw an exception if this class
        // has no data. No data happens when both pub and pri keys are empty and
        // the algorithm is None.
        // But if the arg is false, go ahead and set the properties, even if that
        // means the keys are empty and the Algorithm is None.
        private void SetProperties(
            bool exceptionOnNoData)
        {
            IsPrivate = false;
            Algorithm = _pivPrivateKey.Algorithm;

            if (_pivPrivateKey.Algorithm != PivAlgorithm.None)
            {
                IsPrivate = true;
            }
            else
            {
                Algorithm = _pivPublicKey.Algorithm;
            }

            if (exceptionOnNoData && Algorithm == PivAlgorithm.None)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        InvalidKeyDataMessage));
            }
        }

        // Build the private key from the PRIVATE KEY PEM. If possible, build the
        // public key as well.
        private void BuildPivPrivateKey(
            char[] pemKeyString)
        {
            // Read everything between the labels.
            byte[] encodedKey = Convert.FromBase64CharArray(
                pemKeyString,
                PrivateStartLength,
                pemKeyString.Length - (PrivateStartLength + PrivateEndLength));

            int offset = ReadTagLen(encodedKey, 0, false);
            offset = ReadTagLen(encodedKey, offset, true);
            offset = ReadTagLen(encodedKey, offset, false);
            offset = ReadTagLen(encodedKey, offset, false);
            if (offset > 0)
            {
                var tag = encodedKey[offset + 3];
                if (tag == 0x86)
                {
                    using var rsaObject = RSA.Create();
                    rsaObject.ImportPkcs8PrivateKey(encodedKey, out _);

                    BuildPivPublicKey(rsaObject);
                    BuildPivPrivateKey(rsaObject);
                }
                else if (tag == 0xCE)
                {
                    using var eccObject = ECDsa.Create();
                    eccObject.ImportPkcs8PrivateKey(encodedKey, out _);

                    BuildPivPublicKey(eccObject);
                    BuildPivPrivateKey(eccObject);
                }
                // else if (tag == 0x04)
                // {
                //     var keyDataRange = ^32..;
                //     _pivPrivateKey = new PivEccPrivateKey(encodedKey.AsSpan()[keyDataRange], PivAlgorithm.EccEd25519);
                //     // BuildPivPublicKey(eccObject); // TODO How do get this? I can get it from the file data. Possibly compute from the private key?
                // }
                // else if (tag == 0x03) // Not sure if this is correct. It appears ED and X keys share this byte
                // {
                //     var keyDataRange = ^32..;
                //     _pivPrivateKey = new PivEccPrivateKey(encodedKey.AsSpan()[keyDataRange], PivAlgorithm.EccX25519);
                //     // BuildPivPublicKey(eccObject); // TODO How do get this? I can get it from the file data. Possibly compute from the private key?
                // }
            }
        }
        
        private void BuildPivPublicKey(
            char[] pemKeyString)
        {
            // Read everything between the labels.
            byte[] encodedKey = Convert.FromBase64CharArray(
                pemKeyString,
                PublicStartLength,
                pemKeyString.Length - (PublicStartLength + PublicEndLength));

            int offset = ReadTagLen(encodedKey, 0, false);
            offset = ReadTagLen(encodedKey, offset, false);
            offset = ReadTagLen(encodedKey, offset, false);
            if (offset > 0)
            {
                if (encodedKey[offset + 3] == 0x86)
                {
                    using var rsaObject = RSA.Create();
                    rsaObject.ImportSubjectPublicKeyInfo(encodedKey, out _);

                    BuildPivPublicKey(rsaObject);
                }
                else if (encodedKey[offset + 3] == 0xCE)
                {
                    using var eccObject = ECDsa.Create();
                    eccObject.ImportSubjectPublicKeyInfo(encodedKey, out _);

                    BuildPivPublicKey(eccObject);
                }
                // else if (encodedKey[offset + 3] == 0x04) // Ed25519
                // {
                //     _pivPublicKey = PivEccPublicKey.CreateFromPublicPoint(encodedKey.AsMemory()[^32..], KeyType.Ed25519);
                // }
                // else if (encodedKey[offset + 3] == 0x03) // X25519
                // {
                //     _pivPublicKey = PivEccPublicKey.CreateFromPublicPoint(encodedKey.AsMemory()[^32..], KeyType.Ed25519);
                // }
            }
        }

        private void BuildPivPrivateKey(
            RSA rsaObject)
        {
            RSAParameters rsaParams = rsaObject.ExportParameters(true);

            try
            {
                var rsaPriKey = new PivRsaPrivateKey(
                    rsaParams.P,
                    rsaParams.Q,
                    rsaParams.DP,
                    rsaParams.DQ,
                    rsaParams.InverseQ);

                _pivPrivateKey = (PivPrivateKey)rsaPriKey;
            }
            finally
            {
                CryptoSupport.ClearRsaParameters(rsaParams);
            }
        }

        private void BuildPivPublicKey(
            RSA rsaObject)
        {
            RSAParameters rsaParams = rsaObject.ExportParameters(false);

            var rsaPubKey = new PivRsaPublicKey(rsaParams.Modulus, rsaParams.Exponent);
            _pivPublicKey = (PivPublicKey)rsaPubKey;
        }

        private void BuildPivPrivateKey(
            ECDsa eccObject)
        {
            // We need to build the private value and it must be exactly
            // the keySize.
            var keySizeBytes = (int)Math.Ceiling((double)eccObject.KeySize / 8);
            var eccParams = eccObject.ExportParameters(true);
            var offset = keySizeBytes - eccParams.D!.Length;

            var privateValue = new byte[keySizeBytes];
            Array.Copy(eccParams.D, 0, privateValue, offset, eccParams.D.Length);
            // var eccOid = eccParams.Curve.Oid.Value!;
            // var keyDefinition = KeyDefinitions.GetByOid(eccOid);
            // var eccPriKey = new PivEccPrivateKey(privateValue, keyDefinition.KeyType.GetPivAlgorithm());
            var eccPriKey = new PivEccPrivateKey(privateValue);

            _pivPrivateKey = eccPriKey;
        }

        private void BuildPivPublicKey(
            ECDsa eccObject)
        {
            var keySizeBytes = (int)Math.Ceiling((double)eccObject.KeySize / 8);

            // We need to build the public point as
            //  04 || x-coord || y-coord
            // Each coordinate must be the exact length.
            // Prepend 00 bytes if the coordinate is not long enough.
            var eccParams = eccObject.ExportParameters(false);
            var point = new byte[(keySizeBytes * 2) + 1];
            var offset = 1;

            point[0] = 0x4;
            Array.Copy(eccParams.Q.X!, 0, point, offset, eccParams.Q.X!.Length);
            offset += keySizeBytes;
            Array.Copy(eccParams.Q.Y!, 0, point, offset, eccParams.Q.Y!.Length);

            // var keyDefinition = KeyDefinitions.GetByOid(eccParams.Curve.Oid.Value!);
            // var eccPubKey = PivEccPublicKey.CreateFromPublicPoint(point, keyDefinition.KeyType);
            var eccPubKey = new PivEccPublicKey(point.AsSpan());

            _pivPublicKey = eccPubKey;
        }

        // Multiply p and q to get the modulus
        // return the modulus as a canonical byte array.
        // NOTE!! The BigInteger struct is not safe to use for cryptography.
        // There is no way to overwrite sensitive data. This needs to be updated
        // to use a multi-precision arithmetic library that is good to use with
        // crypto.
        private static byte[] GetModulusFromPrimes(
            byte[] primeP,
            byte[] primeQ)
        {
            byte[] pValue = Array.Empty<byte>();
            byte[] qValue = Array.Empty<byte>();
            byte[] modulus = Array.Empty<byte>();

            try
            {
                pValue = new byte[primeP.Length + 1];
                Array.Copy(primeP, 0, pValue, 1, primeP.Length);
                Array.Reverse(pValue);

                qValue = new byte[primeQ.Length + 1];
                Array.Copy(primeQ, 0, qValue, 1, primeQ.Length);
                Array.Reverse(qValue);

                var pInteger = new BigInteger(pValue);
                var qInteger = new BigInteger(qValue);
                var result = BigInteger.Multiply(pInteger, qInteger);

                modulus = result.ToByteArray(true, true);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pValue);
                CryptographicOperations.ZeroMemory(qValue);
            }

            return modulus;
        }

        // Multiply find inverse of pubExpo mod (p-1)(q-1) to get the private
        // exponent.
        // return the result as a canonical byte array.
        // NOTE!! The BigInteger struct is not safe to use for cryptography.
        // There is no way to overwrite sensitive data. This needs to be updated
        // to use a multi-precision arithmetic library that is good to use with
        // crypto.
        private static byte[] GetPrivateExponentFromPrimes(
            byte[] primeP,
            byte[] primeQ,
            byte[] publicExponent)
        {
            byte[] pValue = new byte[primeP.Length + 1];
            Array.Copy(primeP, 0, pValue, 1, primeP.Length);
            Array.Reverse(pValue);
            var pInteger = new BigInteger(pValue);
            var pMinus1 = BigInteger.Subtract(pInteger, BigInteger.One);

            byte[] qValue = new byte[primeQ.Length + 1];
            Array.Copy(primeQ, 0, qValue, 1, primeQ.Length);
            Array.Reverse(qValue);
            var qInteger = new BigInteger(qValue);
            var qMinus1 = BigInteger.Subtract(qInteger, BigInteger.One);

            var phi = BigInteger.Multiply(pMinus1, qMinus1);

            byte[] eValue = new byte[publicExponent.Length + 1];
            Array.Copy(publicExponent, 0, eValue, 1, publicExponent.Length);
            Array.Reverse(eValue);
            var eInteger = new BigInteger(eValue);

            BigInteger dInteger = ModInverse(eInteger, phi);
            byte[] result = dInteger.ToByteArray(true, true);

            return result;
        }

        // NOTE!! The BigInteger struct is not safe to use for cryptography.
        // There is no way to overwrite sensitive data. This needs to be updated
        // to use a multi-precision arithmetic library that is good to use with
        // crypto.
        public static BigInteger ModInverse(
            BigInteger value,
            BigInteger modulo)
        {
            if (1 != Egcd(value, modulo, out BigInteger x, out _))
            {
                return BigInteger.Zero;
            }

            if (x < 0)
            {
                x += modulo;
            }

            return x % modulo;
        }

        // NOTE!! The BigInteger struct is not safe to use for cryptography.
        // There is no way to overwrite sensitive data. This needs to be updated
        // to use a multi-precision arithmetic library that is good to use with
        // crypto.
        public static BigInteger Egcd(
            BigInteger left,
            BigInteger right,
            out BigInteger leftFactor,
            out BigInteger rightFactor)
        {
            leftFactor = 0;
            rightFactor = 1;
            BigInteger u = 1;
            BigInteger v = 0;
            BigInteger gcd = 0;

            while (left != 0)
            {
                BigInteger q = right / left;
                BigInteger r = right % left;

                BigInteger m = leftFactor - (u * q);
                BigInteger n = rightFactor - (v * q);

                right = left;
                left = r;
                leftFactor = u;
                rightFactor = v;
                u = m;
                v = n;

                gcd = right;
            }

            return gcd;
        }

        // Read the tag in the buffer at the given offset. Then read the length
        // octet(s). If the readValue argument is false, return the offset into
        // the buffer where the value begins. If the readValue argument is true,
        // skip the value (that will be length octets) and return the offset into
        // the buffer where the next TLV begins.
        // If the length octets are invalid, return -1.
        private static int ReadTagLen(
            byte[] buffer,
            int offset,
            bool readValue)
        {
            // Make sure there are enough bytes to read.
            if (offset < 0 || buffer.Length < offset + 2)
            {
                return -1;
            }

            // Skip the tag, look at the first length octet.
            // If the length is 0x7F or less, the length is one octet.
            // If the length octet is 0x80, that's BER and we shouldn't see it.
            // Otherwise the length octet should be 81, 82, or 83 (technically it
            // could be 84 or higher, but this method does not support anything
            // beyond 83). This says the length is the next 1, 2, or 3 octets.
            int length = buffer[offset + 1];
            int increment = 2;
            if (length == 0x80 || length > 0x83)
            {
                return -1;
            }

            if (length > 0x80)
            {
                int count = length & 0xf;
                if (buffer.Length < offset + increment + count)
                {
                    return -1;
                }

                increment += count;
                length = 0;
                while (count > 0)
                {
                    length <<= 8;
                    length += (int)buffer[offset + increment - count] & 0xFF;
                    count--;
                }
            }

            if (readValue)
            {
                if (buffer.Length < offset + increment + length)
                {
                    return -1;
                }

                increment += length;
            }

            return offset + increment;
        }

        // Verify that the given string begins with the targetStart and ends with
        // the targetEnd.
        private static bool VerifyPemHeaderAndFooter(
            char[] pemKeyString,
            char[] targetStart,
            char[] targetEnd)
        {
            bool returnValue = false;
            if (pemKeyString.Length > targetStart.Length + targetEnd.Length)
            {
                if (CompareToTarget(pemKeyString, 0, targetStart))
                {
                    returnValue = CompareToTarget(pemKeyString, pemKeyString.Length - targetEnd.Length, targetEnd);
                }
            }

            return returnValue;
        }

        // Compare the chars in buffer beginning at offset with the chars in
        // target.
        private static bool CompareToTarget(
            char[] buffer,
            int offset,
            char[] target)
        {
            int index = 0;
            for (; index < target.Length; index++)
            {
                if (buffer[index + offset] != target[index])
                {
                    break;
                }
            }

            return index >= target.Length;
        }
    }
}

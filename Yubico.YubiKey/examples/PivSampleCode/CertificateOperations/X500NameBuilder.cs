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

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.Sample.PivSampleCode
{
    // This class demonstrates how to build an X500DistinguishedName object from
    // a collection of individual elements.
    // In the .NET Base Class Library (BCL), a name in a Cert Request or Cert
    // class is represented by either a string or an X500DistinguishedName
    // object. The string form will look something like this.
    //
    //    string sampleRootName = "C=US,ST=CA,L=Palo Alto,O=Fake,CN=Fake Root";
    //
    // This is not really documented in the .NET classes, but this format is
    // described in RFC 4514. The only documentation in the .NET classes says the
    // following.
    //
    //    Properties
    //    Name    Gets the comma-delimited distinguished name from an X500 certificate.
    //
    // However, this string form is not supported on the Mac version of .NET. On
    // a Mac, this form will cause an exception to be thrown. On Mac, another
    // form (a form NOT listed in RFC 4514) will purportedly work.
    //
    //    string sampleRootName = "C=US/ST=CA/L=Palo Alto/O=Fake/CN=Fake Root";
    //
    // While using this form, .NET's implementation of certificate classes on Mac
    // will not throw an exception, unfortunately, it will not work. It ignores
    // the / separating character and considers this string to contain only one
    // element, namely the country of "US/ST=CA/L=Palo Alto/O=Fake/CN=Fake Root".
    // On a Mac, therefore, the only way to provide the name for a cert or
    // request is as an X500DistinguishedName object. Unfortunately, the only way
    // to build an X500DistinguishedName object is with a string or the DER
    // encoding of the name.
    // Hence, the only portable way to supply a name to the .NET classes is to
    // create the DER encoding of the name and use that to build the
    // X500DistinguishedName object.
    // This class will take in the individual elements and build the
    // X500DistinguishedName object. That is, the caller supplies each individual
    // name element, and this class will build the DER encoding, and then from
    // that encoding, build the Name object.
    // To use this class, instantiate, then call the Add method for each element.
    // Call the GetEncodedName method to get the DER encoding of the name, or
    // else call the GetNameObject method to get an X500DistinguishedName object.
    public class X500NameBuilder
    {
        internal const string InvalidElementMessage = "Invalid X500Name element";
        internal const string InvalidNameMessage = "Invalid X500Name";

        private readonly Dictionary<X500NameElement, byte[]> _elements;

        // The constructor creates an empty object. Call the Add method for each
        // element of the name you wish to add.
        // When you have added all the elements you want to add, call a Get
        // method.
        public X500NameBuilder()
        {
            _elements = new Dictionary<X500NameElement, byte[]>();
        }

        // Add the given string value as the specified X500NameElement.
        // If you Add the same element twice, this method will throw an exception.
        public void AddNameElement(X500NameElement nameElement, string value)
        {
            if (!_elements.ContainsKey(nameElement))
            {
                _elements.Add(nameElement, nameElement.GetDerEncoding(value));
                return;
            }

            throw new ArgumentException(InvalidElementMessage);
        }

        // Get the DER encoding of the name.
        // If no elements had been added, this method will throw an exception.
        public byte[] GetEncodedName()
        {
            var enumValues = Enum.GetValues(typeof(X500NameElement));

            // The DER encoding is simply the SEQUENCE of each element.
            // Get each encoding in order. That is, no matter what order they
            // were added to _elements, get them out in the order of the Enum.
            int count = 0;
            var tlvWriter = new TlvWriter();
            using (tlvWriter.WriteNestedTlv(0x30))
            {
                foreach (X500NameElement nameElement in enumValues)
                {
                    if (_elements.TryGetValue(nameElement, out byte[] encodedValue))
                    {
                        tlvWriter.WriteEncoded(encodedValue);
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                return tlvWriter.Encode();
            }

            throw new ArgumentException(InvalidNameMessage);
        }

        // Get the name as an object of type X500DistinguishedName.
        // If no elements had been added, this method will throw an exception.
        public X500DistinguishedName GetDistinguishedName()
        {
            return new X500DistinguishedName(GetEncodedName());
        }
    }

    // Currently supported name elements.
    // If you ever want to add an element, just add it. But don't forget to add
    // the extensions (OID, etc.).
    public enum X500NameElement
    {
        Country = 0,
        State = 1,
        Locality = 2,
        Organization = 3,
        CommonName = 4,
    }

    public static class X500NameElementExtensions
    {
        // Get the DER encoding of one name element.
        // This method currently treats all elements the same. It treats each
        // element as an ASCII string, and they are all encoded as the
        // following.
        //   SET {
        //     SEQ {
        //       OID,
        //       PrintableString
        //     }
        //   }
        // If you ever add an element that is encoded differently, update this
        // method.
        // This method will simply get the string as a CharArray (see String.ToCharArray),
        // then convert those chars into bytes by keeping only the low order byte.
        public static byte[] GetDerEncoding(this X500NameElement nameElement, string value)
        {
            byte[] valueBytes = Array.Empty<byte>();

            if (!(value is null))
            {
                // Convert the string to a byte array.
                char[] valueArray = value.ToCharArray();
                valueBytes = new byte[valueArray.Length];
                for (int index = 0; index < valueArray.Length; index++)
                {
                    valueBytes[index] = (byte)valueArray[index];
                }
            }

            if (nameElement.IsValidValueLength(valueBytes.Length))
            {
                var tlvWriter = new TlvWriter();
                using (tlvWriter.WriteNestedTlv(0x31))
                {
                    using (tlvWriter.WriteNestedTlv(0x30))
                    {
                        tlvWriter.WriteValue(0x06, nameElement.GetOid());
                        tlvWriter.WriteValue(0x13, valueBytes);
                    }
                }

                return tlvWriter.Encode();
            }

            throw new ArgumentException(X500NameBuilder.InvalidElementMessage);
        }

        public static byte[] GetOid(this X500NameElement nameElement) => nameElement switch
        {
            X500NameElement.Country => new byte[] { 0x55, 0x04, 0x06 },
            X500NameElement.State => new byte[] { 0x55, 0x04, 0x08 },
            X500NameElement.Locality => new byte[] { 0x55, 0x04, 0x07 },
            X500NameElement.Organization => new byte[] { 0x55, 0x04, 0x0A },
            X500NameElement.CommonName => new byte[] { 0x55, 0x04, 0x03 },
            _ => throw new ArgumentException(X500NameBuilder.InvalidElementMessage),
        };

        // Is the given length valid for the specified nameElement?
        public static bool IsValidValueLength(this X500NameElement nameElement, int length) => nameElement switch
        {
            X500NameElement.Country => length == 2,
            X500NameElement.State => length > 0 && length < 32,
            X500NameElement.Locality => length > 0 && length < 32,
            X500NameElement.Organization => length > 0 && length < 64,
            X500NameElement.CommonName => length > 0 && length < 64,
            _ => throw new ArgumentException(X500NameBuilder.InvalidElementMessage),
        };
    }
}

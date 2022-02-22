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
using System.Security.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;
using Microsoft.Extensions.Logging;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code for dealing with
    // PIV Objects.
    public sealed partial class PivSession : IDisposable
    {
        /// <summary>
        /// Read the data at the storage location specified by the defined data
        /// tag for the <see cref="Objects.PivDataObject"/> <c>T</c>, storing and
        /// returning this data in a new object of type <c>T</c>.
        /// </summary>
        /// <remarks>
        /// For example,
        /// <code>
        ///    using CardholderUniqueId chuid = pivSession.ReadObject&lt;CardholderUniqueId&gt;();
        /// </code>
        /// <para>
        /// See also the user's manual entry on
        /// <xref href="UsersManualPivObjects"> PIV data objects</xref>.
        /// </para>
        /// <para>
        /// It is possible that there is no data on the YubiKey in the specified
        /// storage location. If so, this method will return a new object with
        /// the <see cref="Objects.PivDataObject.IsEmpty"/> property set to <c>true</c>.
        /// </para>
        /// <para>
        /// If there is data stored under the specified data tag, this method
        /// will expect it to be formatted as defined by the class <c>T</c>. If
        /// it is not formatted as expected, this method will throw an exception.
        /// </para>
        /// <para>
        /// Note that there is a method that can read using an alternate tag.
        /// This method will use the defined tag for the given class.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">
        /// The type of <see cref="Objects.PivDataObject"/> to create, set, and
        /// return.
        /// </typeparam>
        /// <returns>
        /// A new object of the type <c>T</c>. It contains the data stored on the
        /// YubiKey under the object's defined data tag. If there is no data
        /// stored, the returned object's <c>IsEmpty</c> property will be
        /// <c>true</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The data is not properly encoded for the data object.
        /// </exception>
        public T ReadObject<T>() where T : PivDataObject, new()
        {
            _log.LogInformation("Read PivObject " + typeof(T) + ".");
            var returnValue = new T();

            ReadObject(returnValue);
            return returnValue;
        }

        /// <summary>
        /// Read the data at the storage location specified by the given
        /// <c>dataTag</c>, storing and returning this data in a new object of
        /// type <c>T</c>.
        /// </summary>
        /// <remarks>
        /// For example,
        /// <code>
        ///    using CardholderUniqueId chuid = pivSession.ReadObject&lt;CardholderUniqueId&gt;(0x005FFF55);
        /// </code>
        /// <para>
        /// See also the user's manual entry on
        /// <xref href="UsersManualPivObjects"> PIV data objects</xref>.
        /// </para>
        /// <para>
        /// This is the same as the <see cref="ReadObject()"/> method that takes no
        /// arguments, except this one will get the data in the storage location
        /// specified by <c>dataTag</c>, as opposed to the defined data tag for
        /// the class <c>T</c>. This method will still expect the data to be
        /// formatted as defined in the class <c>T</c>, it's just that the data
        /// is stored under a different tag.
        /// </para>
        /// <para>
        /// This method is used when you have stored data under an alternate tag.
        /// It is likely you will never need this feature, and it is dangerous,
        /// because if done incorrectly, sensitive data that should require PIN
        /// verification to access will be available without a PIN). But there
        /// are rare cases when this feature is useful.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">
        /// The type of <see cref="Objects.PivDataObject"/> to create, set, and
        /// return.
        /// </typeparam>
        /// <returns>
        /// A new object of the type <c>T</c>. It contains the data stored on the
        /// YubiKey under the object's defined data tag. If there is no data
        /// stored, the returned object's <c>IsEmpty</c> property will be
        /// <c>true</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The <c>dataTag</c> in not allowed to be an alternate tag, or the data
        /// from the YubiKey is not properly encoded for the data object.
        /// </exception>
        public T ReadObject<T>(int dataTag) where T : PivDataObject, new()
        {
            _log.LogInformation("Read PivObject " + typeof(T) + " using dataTag 0x{0:X8}.", dataTag);
            var returnValue = new T
            {
                DataTag = dataTag
            };

            ReadObject(returnValue);
            return returnValue;
        }

        // Shared code. This will perform the GET DATA and Decode.
        // It will use the pivObject.DataTag. So if the caller wants a different
        // data tag than the defined (default), set it in the PivObject first,
        // then call this method.
        private void ReadObject(PivDataObject pivDataObject)
        {
            var getDataCommand = new GetDataCommand(pivDataObject.DataTag);
            GetDataResponse getDataResponse = Connection.SendCommand(getDataCommand);

            // If GetDataCommand requires the PIN and it had not been verified,
            // verify it now and run it again.
            if (getDataResponse.Status == ResponseStatus.AuthenticationRequired)
            {
                VerifyPin();
                getDataCommand = new GetDataCommand(pivDataObject.DataTag);
                getDataResponse = Connection.SendCommand(getDataCommand);
            }

            // If there is no data, simply return the object created, the IeEmpty
            // property will be set to true.
            // If the response is not NoData, get the data. Either we will get
            // the data or we will get an exception because of an error.
            if (getDataResponse.Status != ResponseStatus.NoData)
            {
                ReadOnlyMemory<byte> encodedData = getDataResponse.GetData();
                pivDataObject.Decode(encodedData);
            }
        }

        /// <summary>
        /// Write the contents of the given object to the storage location
        /// specified by the <see cref="Objects.PivDataObject.DataTag"/> property.
        /// </summary>
        /// <remarks>
        /// This method will call on the <c>pivObject</c> to encode the data
        /// following its definition, then store that data on the YubiKey under
        /// the <c>PivObject.DataTag</c> storage location.
        /// <para>
        /// See also the user's manual entry on
        /// <xref href="UsersManualPivObjects"> PIV data objects</xref>.
        /// </para>
        /// <para>
        /// Note that it is possible to change the <c>DataTag</c>, and this method
        /// will store the data under whatever <c>DataTag</c> is set to.
        /// </para>
        /// <para>
        /// Note also that this method will overwrite any data already on the
        /// YubiKey, if there is any. Hence, make sure you know what data is
        /// stored there currently, and that it is safe to overwrite, before
        /// calling this method.
        /// </para>
        /// <para>
        /// If the object has no data (the <c>IsEmpty</c> property is
        /// <c>true</c>), then this method will not store anything and not throw
        /// an exception. It will just return. This means that if there is
        /// already data in the storage location, it will remain there. In other
        /// words, this method will not "clear" a storage location.
        /// </para>
        /// </remarks>
        /// <param name="pivDataObject">
        /// The object containing the data to store (and the definition of its
        /// formatting), along with the data tag under which it is to be stored.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>pivDataObject</c> argument is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The method was not able to store the data onto the YubiKey.
        /// </exception>
        public void WriteObject(PivDataObject pivDataObject)
        {
            if (pivDataObject is null)
            {
                throw new ArgumentNullException(nameof(pivDataObject));
            }
            _log.LogInformation("Write PivObject " + pivDataObject.GetType() + " to data tag 0x{0:X8}.");

            if (pivDataObject.IsEmpty)
            {
                return;
            }

            if (!ManagementKeyAuthenticated)
            {
                AuthenticateManagementKey(true);
            }

            byte[] dataToStore = Array.Empty<byte>();

            try
            {
                dataToStore = pivDataObject.Encode();
                var putDataCommand = new PutDataCommand(pivDataObject.DataTag, dataToStore);
                PutDataResponse putDataResponse = Connection.SendCommand(putDataCommand);

                if (putDataResponse.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(putDataResponse.StatusMessage);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataToStore);
            }
        }
    }
}

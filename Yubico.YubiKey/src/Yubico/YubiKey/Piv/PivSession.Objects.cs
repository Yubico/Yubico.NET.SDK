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
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.Piv.Objects;

namespace Yubico.YubiKey.Piv;

// This portion of the PivSession class contains code for dealing with
// PIV Objects.
public sealed partial class PivSession : IDisposable
{
    /// <summary>
    ///     Read the data at the storage location specified by the defined data
    ///     tag for the <see cref="Objects.PivDataObject" /> <c>T</c>, storing and
    ///     returning this data in a new object of type <c>T</c>.
    /// </summary>
    /// <remarks>
    ///     For example,
    ///     <code language="csharp">
    ///    using CardholderUniqueId chuid = pivSession.ReadObject&lt;CardholderUniqueId&gt;();
    /// </code>
    ///     <para>
    ///         See also the user's manual entry on
    ///         <xref href="UsersManualPivObjects"> PIV data objects</xref>.
    ///     </para>
    ///     <para>
    ///         It is possible that there is no data on the YubiKey in the specified
    ///         storage location. If so, this method will return a new object with
    ///         the <see cref="Objects.PivDataObject.IsEmpty" /> property set to <c>true</c>.
    ///     </para>
    ///     <para>
    ///         If there is data stored under the specified data tag, this method
    ///         will expect it to be formatted as defined by the class <c>T</c>. If
    ///         it is not formatted as expected, this method will throw an exception.
    ///     </para>
    ///     <para>
    ///         Note that there is a method that can read using an alternate tag.
    ///         This method will use the defined tag for the given class.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of <see cref="Objects.PivDataObject" /> to create, set, and
    ///     return.
    /// </typeparam>
    /// <returns>
    ///     A new object of the type <c>T</c>. It contains the data stored on the
    ///     YubiKey under the object's defined data tag. If there is no data
    ///     stored, the returned object's <c>IsEmpty</c> property will be
    ///     <c>true</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     The data is not properly encoded for the data object.
    /// </exception>
    public T ReadObject<T>() where T : PivDataObject, new()
    {
        Logger.LogInformation("Read PivObject " + typeof(T) + ".");
        var returnValue = new T();

        if (TryReadObject(returnValue))
        {
            return returnValue;
        }

        throw new ArgumentException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.InvalidDataEncoding));
    }

    /// <summary>
    ///     Try to read the data at the storage location specified by the defined
    ///     data tag for the <see cref="Objects.PivDataObject" /> <c>T</c>,
    ///     storing and returning this data in a new object of type <c>T</c>.
    /// </summary>
    /// <remarks>
    ///     For example,
    ///     <code language="csharp">
    ///    bool isValid = pivSession.TryReadObject&lt;PinProtectedData&gt;(out PinProtectedData pinProtect);
    ///    if (isValid)
    ///    {
    ///        // perform operations
    ///    }
    ///    pinProtect.Dispose();
    /// </code>
    ///     or alternatively,
    ///     <code language="csharp">
    ///    bool isValid = pivSession.ReadObject(out PinProtectedData pinProtect);
    ///    using (pinProtect)
    ///    {
    ///        if (isValid)
    ///        {
    ///            // perform operations
    ///        }
    ///    }
    /// </code>
    ///     <para>
    ///         Note that the <c>PivDataObject</c> is <c>Disposable</c> so make sure
    ///         you use the <c>using</c> keyword or call <c>Dispose</c> directly.
    ///     </para>
    ///     <para>
    ///         See also the user's manual entry on
    ///         <xref href="UsersManualPivObjects"> PIV data objects</xref>.
    ///     </para>
    ///     <para>
    ///         The method will look in the storage location specified by the
    ///         defined data tag of the <c>pivDataObject</c>.
    ///     </para>
    ///     <para>
    ///         It is possible that there is no data on the YubiKey in the specified
    ///         storage location. If so, this method will build a new, empty object
    ///         as the <c>pivDataObject</c> and return false.
    ///     </para>
    ///     <para>
    ///         If there is data and it is formatted as expected, the method will
    ///         build a new object set with the data and return <c>true</c>.
    ///     </para>
    ///     <para>
    ///         If there is data stored under the specified data tag, but it is not
    ///         formatted as expected, this method will build a new, empty object as
    ///         the <c>pivDataObject</c> and return <c>false</c>.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of <see cref="Objects.PivDataObject" /> to create, set, and
    ///     return.
    /// </typeparam>
    /// <param name="pivDataObject">
    ///     An output argument, this method will create a new object containing
    ///     the data from the storage location, as long as it is formatted as
    ///     expected.
    /// </param>
    /// <returns>
    ///     A boolean, <c>true</c> if the storage location contains data
    ///     formatted as expected, or no data, and <c>false</c> if the storage
    ///     location contains data formatted in an unexpected way.
    /// </returns>
    public bool TryReadObject<T>(out T pivDataObject) where T : PivDataObject, new()
    {
        Logger.LogInformation("Try to read PivObject " + typeof(T) + ".");
        pivDataObject = new T();

        return TryReadObject(pivDataObject);
    }

    /// <summary>
    ///     Read the data at the storage location specified by the given
    ///     <c>dataTag</c>, storing and returning this data in a new object of
    ///     type <c>T</c>.
    /// </summary>
    /// <remarks>
    ///     For example,
    ///     <code language="csharp">
    ///    using CardholderUniqueId chuid = pivSession.ReadObject&lt;CardholderUniqueId&gt;(0x005FFF55);
    /// </code>
    ///     <para>
    ///         See also the user's manual entry on
    ///         <xref href="UsersManualPivObjects"> PIV data objects</xref>.
    ///     </para>
    ///     <para>
    ///         This is the same as the <see cref="ReadObject{T}()" /> method that takes no
    ///         arguments, except this one will get the data in the storage location
    ///         specified by <c>dataTag</c>, as opposed to the defined data tag for
    ///         the class <c>T</c>. This method will still expect the data to be
    ///         formatted as defined in the class <c>T</c>, it's just that the data
    ///         is stored under a different tag.
    ///     </para>
    ///     <para>
    ///         This method is used when you have stored data under an alternate tag.
    ///         It is likely you will never need this feature, and it is dangerous,
    ///         because if done incorrectly, sensitive data that should require PIN
    ///         verification to access will be available without a PIN). But there
    ///         are rare cases when this feature is useful.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of <see cref="Objects.PivDataObject" /> to create, set, and
    ///     return.
    /// </typeparam>
    /// <param name="dataTag">
    ///     The alternate data tag, the tag of the storage location to look.
    /// </param>
    /// <returns>
    ///     A new object of the type <c>T</c>. It contains the data stored on the
    ///     YubiKey under the object's defined data tag. If there is no data
    ///     stored, the returned object's <c>IsEmpty</c> property will be
    ///     <c>true</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     The <c>dataTag</c> in not allowed to be an alternate tag, or the data
    ///     from the YubiKey is not properly encoded for the data object.
    /// </exception>
    public T ReadObject<T>(int dataTag) where T : PivDataObject, new()
    {
        Logger.LogInformation("Read PivObject " + typeof(T) + " using dataTag 0x{0:X8}.", dataTag);
        var returnValue = new T
        {
            DataTag = dataTag
        };

        if (TryReadObject(returnValue))
        {
            return returnValue;
        }

        throw new ArgumentException(
            string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.InvalidDataEncoding));
    }

    /// <summary>
    ///     Try to read the data at the storage location specified by the defined
    ///     data tag for the <see cref="Objects.PivDataObject" /> <c>T</c>,
    ///     storing and returning this data in a new object of type <c>T</c>.
    /// </summary>
    /// <remarks>
    ///     For example,
    ///     <code language="csharp">
    ///    bool isValid = pivSession.TryReadObject&lt;KeyHistory&gt;(out KeyHistory keyHistory);
    ///    if (isValid)
    ///    {
    ///        // perform operations
    ///    }
    ///    keyHistory.Dispose();
    /// </code>
    ///     or alternatively,
    ///     <code language="csharp">
    ///    bool isValid = pivSession.ReadObject&lt;KeyHistory&gt;(out KeyHistory keyHistory);
    ///    using (keyHistory)
    ///    {
    ///        if (isValid)
    ///        {
    ///            // perform operations
    ///        }
    ///    }
    /// </code>
    ///     <para>
    ///         Note that the <c>PivDataObject</c> is <c>Disposable</c> so make sure
    ///         you use the <c>using</c> keyword or call <c>Dispose</c> directly.
    ///     </para>
    ///     <para>
    ///         See also the user's manual entry on
    ///         <xref href="UsersManualPivObjects"> PIV data objects</xref>.
    ///     </para>
    ///     <para>
    ///         The method will look in the storage location specified by the
    ///         <c>dataTag</c> argument.
    ///     </para>
    ///     <para>
    ///         It is possible that there is no data on the YubiKey in the specified
    ///         storage location. If so, this method will build a new, empty object
    ///         as the <c>pivDataObject</c> and return false.
    ///     </para>
    ///     <para>
    ///         If there is data and it is formatted as expected, the method will
    ///         build a new object set with the data and return <c>true</c>.
    ///     </para>
    ///     <para>
    ///         If there is data stored under the specified data tag, and it is not
    ///         formatted as expected, this method will build a new, empty object as
    ///         the <c>pivDataObject</c> and return <c>false</c>.
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of <see cref="Objects.PivDataObject" /> to create, set, and
    ///     return.
    /// </typeparam>
    /// <param name="dataTag">
    ///     The alternate data tag, the tag of the storage location to look.
    /// </param>
    /// <param name="pivDataObject">
    ///     An output argument, this method will create a new object containing
    ///     the data from the storage location, as long as it is formatted as
    ///     expected.
    /// </param>
    /// <returns>
    ///     A boolean, <c>true</c> if the storage location contains data
    ///     formatted as expected, or no data, and <c>false</c> if the storage
    ///     location contains data formatted in an unexpected way.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     The <c>dataTag</c> in not allowed to be an alternate tag.
    /// </exception>
    public bool TryReadObject<T>(int dataTag, out T pivDataObject) where T : PivDataObject, new()
    {
        Logger.LogInformation("Try to read PivObject " + typeof(T) + ".");
        pivDataObject = new T
        {
            DataTag = dataTag
        };

        return TryReadObject(pivDataObject);
    }

    // Shared code.
    // This expects the pivDataObject to be Clear, in a state just after
    // instantiation.
    private bool TryReadObject(PivDataObject pivDataObject)
    {
        var command = new GetDataCommand(pivDataObject.DataTag);
        var response = Connection.SendCommand(command);

        // If GetDataCommand requires the PIN and it had not been verified,
        // verify it now and run it again.
        if (response.Status == ResponseStatus.AuthenticationRequired)
        {
            VerifyPin();
            response = Connection.SendCommand(command);
        }

        // If there is no data, simply return the object created, the IsEmpty
        // property will be set to true.
        // If the response is not NoData, get the data. Either we will get
        // the data or we will get an exception because of an error in the
        // GetData, which is the kind of exception we want to throw, even
        // though this is a Try method.
        if (response.Status != ResponseStatus.NoData)
        {
            var encodedData = response.GetData();
            return pivDataObject.TryDecode(encodedData);
        }

        return true;
    }

    /// <summary>
    ///     Write the contents of the given object to the storage location
    ///     specified by the <see cref="Objects.PivDataObject.DataTag" /> property.
    /// </summary>
    /// <remarks>
    ///     This method will call on the <c>pivObject</c> to encode the data
    ///     following its definition, then store that data on the YubiKey under
    ///     the <c>PivObject.DataTag</c> storage location.
    ///     <para>
    ///         See also the user's manual entry on
    ///         <xref href="UsersManualPivObjects"> PIV data objects</xref>.
    ///     </para>
    ///     <para>
    ///         Note that it is possible to change the <c>DataTag</c>, and this method
    ///         will store the data under whatever <c>DataTag</c> is set to.
    ///     </para>
    ///     <para>
    ///         Note also that this method will overwrite any data already on the
    ///         YubiKey, if there is any. Hence, make sure you know what data is
    ///         stored there currently, and that it is safe to overwrite, before
    ///         calling this method.
    ///     </para>
    ///     <para>
    ///         If the <c>pivDataObject</c> has no data (the <c>IsEmpty</c> property
    ///         is <c>true</c>), then this method will "clear" a storage location. If
    ///         there was data in the storage location before the call to
    ///         <c>WriteObject</c>, it will be gone after.
    ///     </para>
    /// </remarks>
    /// <param name="pivDataObject">
    ///     The object containing the data to store (and the definition of its
    ///     formatting), along with the data tag under which it is to be stored.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     The <c>pivDataObject</c> argument is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The method was not able to store the data onto the YubiKey.
    /// </exception>
    public void WriteObject(PivDataObject pivDataObject)
    {
        if (pivDataObject is null)
        {
            throw new ArgumentNullException(nameof(pivDataObject));
        }

        Logger.LogInformation("Write PivObject " + pivDataObject.GetType() + " to data tag 0x{0:X8}.");

        byte[] dataToStore = Array.Empty<byte>();

        try
        {
            dataToStore = pivDataObject.Encode();
            var command = new PutDataCommand(pivDataObject.DataTag, dataToStore);
            var response = Connection.SendCommand(command);

            // The PutDataCommand requires mgmt key auth, if it has not been
            // authenticated, do so now and run it again.
            if (response.Status == ResponseStatus.AuthenticationRequired)
            {
                AuthenticateManagementKey();
                response = Connection.SendCommand(command);
            }

            if (response.Status != ResponseStatus.Success)
            {
                throw new InvalidOperationException(response.StatusMessage);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataToStore);
        }
    }
}

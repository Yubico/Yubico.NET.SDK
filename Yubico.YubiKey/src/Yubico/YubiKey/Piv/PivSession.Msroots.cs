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
using System.IO;
using System.Security;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Piv.Commands;

namespace Yubico.YubiKey.Piv
{
    // This portion of the PivSession class contains code for dealing with
    // MSROOTS.
    public sealed partial class PivSession : IDisposable
    {
        private const int MsrootsTag = 0x005fff11;
        private const int MsrootsObjectCount = 5;
        private const int MsrootsMiddleTag = 0x83;
        private const int MsrootsLastTag = 0x82;

        // The amount of actual data stored in a data object is limited.
        // The limit is calculated as the maximum message buffer size minus the
        // TLV bookkeeping.
        //   max data object length =
        //       max message buffer size - max TLV bookkeeping length
        // On older YubiKeys (before version 4), the max message buffer size
        // (call this MBSO) is 2048. The newer YubiKeys (4.0 and later) have a
        // max message buffer size (call this MBSN) of 3072.
        // The TLV bookkeeping is
        //   5C L1 <data object tag> || 53 L2 { 83 L3 <data> }
        //     or
        //   5C L1 <data object tag> || 53 L2 { 82 L3 <data> }
        // where L3 is the length of the data to store and L2 is the length of
        // the "Msroots-encoded" data.
        //   5C L1
        //      <data object tag>
        //   5C L2
        //      83 L3
        //         <data>
        // The data object tag for MSROOTS is 5F FF 11 to 5F FF 15, three bytes:
        //   5C 03 5F FF 1x
        // So the total TLV bookkeeping will be
        //   5C 03 5F FF 1x 5C L2 { 83 L3 <data> }
        // That's
        //   5C 03
        //      5F FF 1x
        //   5C L2
        //      83 L3
        //         <data>
        // We now need to solve for L3 and L2. The length of L3 can be 1, 2, 3,
        // or 4 bytes. We know that the data length will have to be some value
        // less than MBSO=2048 or MBSN=3072, so let's look at the length of L3
        // with a data length of 2048 or 3072. Yes, the actual length will be
        // shorter, but let's start there to get an initial estimate.
        // The L for a length of 2048 will be encoded as 82 08 00 and for 3072 it
        // will be 82 0C 00. Hence for both cases, the length of L3 is 3.
        // Put this together:
        //   5C 03 5F FF 1x 53 82 <2-byte length> { 83 82 <2-byte length> <data> }
        // That's a total of 13 bytes for the TLV bookkeeping. Hence, our
        // estimate of the max length of data is
        //   MBSO=2048 - 13 = 2035
        //   MBSN=3072 - 13 = 3059
        // Now let's verify that even with these updated lengths our numbers
        // match.
        //   2048 : 5C 03
        //             5F FF 1x
        //          53 82 07 F7
        //             83 82 07 F3
        //                <data>
        //   3072 : 5C 03
        //             5F FF 1x
        //          53 82 0B F7
        //             83 82 0B F3
        //                <data>
        // For each case that's 13 bytes of TLV bookkeeping, so yes, our max
        // object length is indeed 2035 or 3059, depending on the version.
        // Note that we need to pass to the PutDataCommand the encoded
        //   53 L2 contents
        // The PutDataCommand will build the full encoding with the 5C tag.
        // This means that if we build a buffer big enough to hold any possible
        // encoding that we pass to the PutDataCommand, it will need to be max
        // object length + 8.
        // Note that there is still some undiscovered bug that causes problems
        // with block sizes over 2030 (old) and 2800 (new). For now, the limits
        // are 2030 and 2800, but that should return to 2035 and 3059 when the
        // problems are fixed.
        private const int OldMaximumObjectLength = 2030;
        private const int NewMaximumObjectLength = 2800;
        private const int MaximumTlvLength = 8;

        /// <summary>
        /// Write <c>contents</c> to the MSROOTS data objects. This will replace
        /// any data already stored in the MSROOTS data objects.
        /// </summary>
        /// <remarks>
        /// The YubiKey PIV application can store data objects. There is a set of
        /// data elements defined by the PIV standard. See the User's Manual
        /// entry on <xref href="UsersManualPivCommands#get-data">GET DATA</xref>
        /// for information on these elements and their tags. The standard also
        /// allows for vendor-defined data objects. MSROOTS is one such
        /// vendor-defined element.
        /// <para>
        /// The intention of the MSROOTS data object is to store (and retrieve) a
        /// PKCS 7 constuction containing a set of root certificates. These
        /// certificates will make it easier for the SDK to interface with the
        /// Microsoft Smart Card Base Crypto Service Provider (CSP).
        /// </para>
        /// <para>
        /// Very few applications will need to use this feature. If you don't
        /// already know what the MSROOTS are, how to use them, and that they are
        /// part of your application already, then you almost certainly will
        /// never need to use this method.
        /// </para>
        /// <para>
        /// This method will take whatever data it is given and store it on the
        /// YubiKey under the tag "MSROOTS". This method will not verify that the
        /// data is a PKCS 7 construction, or that it contains root certificates,
        /// it will simply write the bytes given.
        /// </para>
        /// <para>
        /// Note that in order to store any data on the YubiKey, it must be
        /// formatted as a TLV (tag-length-value):
        /// <code>
        ///   tag || length || value
        ///
        ///   for example, it might be
        ///
        ///   53 20 (contents, 32 bytes)
        ///
        ///   or
        ///
        ///   7F 61 20 (contents, 32 bytes)
        /// </code>
        /// The tag used varies depending on the data being stored. This method
        /// builds the TLV. That is, the caller supplies the contents only, this
        /// method will format it into a construction the YubiKey expects. This
        /// method knows what tag to use for MSROOTS and how to specify the
        /// length.
        /// </para>
        /// <para>
        /// Note also that there is a limit to the number of bytes that can be
        /// stored in a data object. If the contents to store is longer, then
        /// this method will break the data into blocks and store each block in a
        /// different data object. The caller simply supplies all the data as a
        /// single byte array, this method will take care of the bookkeeping of
        /// breaking it into blocks and storing them in separate data objects.
        /// </para>
        /// <para>
        /// There is a limit on the number of data objects, however, so there is
        /// indeed a limit on the total size of the data.
        /// <code>
        ///    There is a limit of 5 MSROOTS data objects
        ///    The limit on data length for each data object is
        ///      pre 4.0 YubiKeys (e.g. NEO)  :  2030 bytes
        ///      4.0 and later YubiKeys       :  2800 bytes
        ///    The total input limit is
        ///      pre 4.0 YubiKeys (e.g. NEO)  :  2030*5 bytes  :  10,150
        ///      4.0 and later YubiKeys       :  2800*5 bytes  :  14,000
        /// </code>
        /// If the data passed in is too long, this method will throw an
        /// exception.
        /// </para>
        /// <para>
        /// Note that the input is a <c>ReadOnlySpan</c>. If you have the data in
        /// a byte array (<c>byte[]</c>), just pass it in as the argument, it
        /// will be cast to a <c>ReadOnlySpan</c> automatically. Also, if you
        /// have no data to store (which is the same as <c>DeleteMsroots</c>),
        /// you can pass in <c>null</c>, but the preferred input in this case is
        /// <c>ReadOnlySpan.Empty</c>.
        /// </para>
        /// <para>
        /// Input with a length of 0 is equivalent to calling
        /// <c>DeleteMsroots</c>.
        /// </para>
        /// <para>
        /// In order to perform this operation, the management key must be
        /// authenticated during this session. If it has not been authenticated,
        /// this method will call <see cref="AuthenticateManagementKey"/>. That
        /// is, your application does not need to authenticate the management key
        /// separately (i.e., call <c>TryAuthenticateManagementKey</c> or
        /// <c>AuthenticateManagementKey</c>), this method will determine if the
        /// management key has been authenticated or not, and if not, it will
        /// make the call to perform mutual authentication.
        /// </para>
        /// <para>
        /// The authentication method will collect the management key using the
        /// <c>KeyCollector</c> delegate. If no such delegate has been set, it
        /// will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, the <c>AuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the management key, and it is possible
        /// that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to the authentication method noting
        /// the cancellation. In that case, it will throw an exception. If you
        /// want the authentication to return <c>false</c> on user cancellation,
        /// you must call <see cref="TryAuthenticateManagementKey(bool)"/> directly
        /// before calling this method.
        /// </para>
        /// </remarks>
        /// <param name="contents">
        /// The data to store, represented as a <c>ReadOnlySpan</c> (a byte
        /// array).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The input data was too long.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The YubiKey returned malformed data and authentication, either single
        /// or double, could not be performed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public void WriteMsroots(ReadOnlySpan<byte> contents)
        {
            int maxLength = CheckWriteLength(nameof(contents), contents.Length);

            WriteMsrootsSpan(contents, maxLength);
        }

        /// <summary>
        /// Write <c>contents</c> to the MSROOTS data objects. This will replace
        /// any data already stored in the MSROOTS data objects.
        /// </summary>
        /// <remarks>
        /// This is the same as <see cref="WriteMsroots"/> except the contents
        /// are provided as a Stream.
        /// </remarks>
        /// <param name="contents">
        /// The data to store, represented as a <c>Stream</c> (the <c>CanRead</c>
        /// property is <c>true</c> and the data read will be bytes).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <c>contents</c> argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <c>stream</c> is not readable.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The input data was too long.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The YubiKey returned malformed data and authentication, either single
        /// or double, could not be performed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public void WriteMsrootsStream(Stream contents)
        {
            var contentsSpan = GetSpanFromStream(contents, out int maxLength);
            WriteMsrootsSpan(contentsSpan, maxLength);
        }

        // Build a Span<byte> from the data in the Stream
        // This will also check the arg to make sure it is valid, and throw
        // appropriate exceptions if it is not.
        private Span<byte> GetSpanFromStream(Stream contents, out int maxLength)
        {
            maxLength = 0;
            if (contents is null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            if (contents.CanRead == false)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.StreamNotReadable));
            }

            maxLength = CheckWriteLength(nameof(contents), contents.Length);

            using (var binaryReader = new BinaryReader(contents))
            {
                byte[] buffer = binaryReader.ReadBytes((int)contents.Length);
                var contentsSpan = new Span<byte>(buffer);

                return contentsSpan;
            }
        }

        // Is the given length valid? That is, can we store length bytes in the
        // MSROOTS data objects?
        // Return the maximum length of a block. This is not the total length of
        // input data, nor is it the length of an encoded buffer, but the maximum
        // number of content bytes that can be stored in an MSROOTS data object.
        // That is, it is the maximum pre-encoding length.
        // If it is valid, return the maximum length, if not, throw an exception
        // using the contentsName.
        private int CheckWriteLength(string contentsName, long length)
        {
            int maxLength = OldMaximumObjectLength;
            if (YubiKey.FirmwareVersion.Major >= 4)
            {
                maxLength = NewMaximumObjectLength;
            }
            if (length > maxLength * MsrootsObjectCount)
            {
                throw new ArgumentOutOfRangeException(
                    contentsName, length,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ExceptionMessages.InvalidPivPutDataLength,
                        length, maxLength * MsrootsObjectCount));
            }

            return maxLength;
        }

        // Write the data in the Span to the MSROOTS data objects.
        // This is shared code, performing the operation after the input data has
        // been validated.
        private void WriteMsrootsSpan(ReadOnlySpan<byte> contents, int maxLength)
        {
            if (ManagementKeyAuthenticated == false)
            {
                AuthenticateManagementKey();
            }

            int offset = 0;
            byte[] buffer = new byte[maxLength + MaximumTlvLength];
            var encoding = new Memory<byte>(buffer);
            // Write to every MSROOTS data object. If there is no data left,
            // we'll write no data, meaning we're making sure an object is empty.
            // Do this in case there was any data left over from a previous write.
            for (int index = 0; index < MsrootsObjectCount; index++)
            {
                // Build
                //  53 L1 { 83 L2 data }
                //    or
                //  53 L1 { 82 L2 data } if this is the last entry.
                int dataLength = contents.Length - offset;
                int msrootsDataTag = MsrootsLastTag;
                if (dataLength > maxLength)
                {
                    dataLength = maxLength;
                    msrootsDataTag = MsrootsMiddleTag;
                }

                // If there is no data left, we want to store 53 00.
                var tlvWriter = new TlvWriter();
                using (tlvWriter.WriteNestedTlv(PivEncodingTag))
                {
                    if (dataLength != 0)
                    {
                        tlvWriter.WriteValue(msrootsDataTag, contents.Slice(offset, dataLength));
                    }
                }

                if (tlvWriter.TryEncode(encoding.Span, out int bytesWritten) == false)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.InvalidDataEncoding));
                }

                var command = new PutDataCommand(MsrootsTag + index, encoding.Slice(0, bytesWritten));
                var response = Connection.SendCommand(command);
                if (response.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.CommandResponseApduUnexpectedResult,
                            response.StatusWord.ToString("X4", CultureInfo.InvariantCulture)));
                }

                offset += dataLength;
            }
        }

        /// <summary>
        /// Returns the <c>contents</c> of the MSROOTS data objects.
        /// </summary>
        /// <remarks>
        /// The YubiKey PIV application can store data objects. There is a set of
        /// data elements defined by the PIV standard. See the User's Manual
        /// entry on <xref href="UsersManualPivCommands#get-data"> GET DATA</xref>
        /// for information on these elements and their tags. The standard also
        /// allows for vendor-defined data objects. MSROOTS is one such
        /// vendor-defined element.
        /// <para>
        /// The intention of the MSROOTS data object is to store and retrieve a
        /// PKCS 7 construction containing a set of root certificates. These
        /// certificates will make it easier for the SDK to interface with the
        /// Microsoft Smart Card Base Crypto Service Provider (CSP).
        /// </para>
        /// <para>
        /// Very few applications will need to use this feature. If you don't
        /// already know what the MSROOTS are, how to use them, and that they are
        /// part of your application already, then you almost certainly will
        /// never need to use this method.
        /// </para>
        /// <para>
        /// This method will return whatever data is stored in the YubiKey under
        /// the tag "MSROOTS". This method will not verify that the data is a
        /// PKCS 7 construction, or that it contains root certificates, it will
        /// simply return the bytes from the data object.
        /// </para>
        /// <para>
        /// While it is necessary to authenticate the management key in order to
        /// store the MSROOTS data (see <see cref="WriteMsroots"/>), it is not
        /// needed to retrieve this data. Anyone with access to a YubiKey can
        /// retrieve this data.
        /// </para>
        /// <para>
        /// The method will return the data as a new byte array. It is possible
        /// there is no data on the YubiKey in the MSROOTS data objects. In that
        /// case, this method will return an empty byte array (Length of 0).
        /// </para>
        /// <para>
        /// Note that YubiKey stores the data formatted as a TLV:
        /// <code>
        ///   tag || length || value
        ///
        ///   for example, it might be
        ///
        ///   53 20 (contents, 32 bytes)
        ///
        ///   or
        ///
        ///   7F 61 20 (contents, 32 bytes)
        /// </code>
        /// The tag used varies depending on the data being stored. This method
        /// returns the contents, not the full TLV.
        /// </para>
        /// <para>
        /// Note that the full amount of data might be stored in more than one
        /// data object. This method will collect all the data in all the MSROOTS
        /// data objects (in order) and concatenate.
        /// </para>
        /// </remarks>
        /// <returns>
        /// A new byte array containing the data stored in the MSROOTS data
        /// objects.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey encountered an error, such as an unreliable connection.
        /// </exception>
        public byte[] ReadMsroots()
        {
            int totalLength = 0;
            int count = 0;
            var contentArray = (ReadOnlyMemory<byte>[])Array.CreateInstance(typeof(ReadOnlyMemory<byte>), MsrootsObjectCount);

            for (int index = 0; index < MsrootsObjectCount; index++)
            {
                var command = new GetDataCommand(MsrootsTag + index);
                var response = Connection.SendCommand(command);

                if (response.Status == ResponseStatus.NoData)
                {
                    break;
                }

                if (response.Status != ResponseStatus.Success)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.CommandResponseApduUnexpectedResult,
                            response.StatusWord.ToString("X4", CultureInfo.InvariantCulture)));
                }

                var tlvReader = new TlvReader(response.GetData());
                var nestedReader = tlvReader.ReadNestedTlv(PivEncodingTag);
                int msrootsDataTag = nestedReader.PeekTag();
                contentArray[index] = nestedReader.ReadValue(msrootsDataTag);

                totalLength += contentArray[index].Length;
                count++;
            }

            byte[] retrievedData = new byte[totalLength];
            var temp = new Memory<byte>(retrievedData);
            int offset = 0;
            for (int index = 0; index < count; index++)
            {
                contentArray[index].CopyTo(temp[offset..]);
                offset += contentArray[index].Length;
            }

            return retrievedData;
        }

        /// <summary>
        /// Returns the contents of the MSROOTS data objects as a <c>Stream</c>.
        /// </summary>
        /// <remarks>
        /// This is the same as the <see cref="ReadMsroots"/> method that returns a byte
        /// array, except this method returns a <c>Stream</c>.
        /// </remarks>
        /// <returns>
        /// A new <c>Stream</c> which will be readable and will contain the
        /// MSROOTS contents.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The YubiKey encountered an error, such as an unreliable connection.
        /// </exception>
        public Stream ReadMsrootsStream()
        {
            byte[] contents = ReadMsroots();

            return new MemoryStream(contents);
        }

        /// <summary>
        /// Delete any contents stored in the MSROOTS data objects.
        /// </summary>
        /// <remarks>
        /// In order to perform this operation, the management key must be
        /// authenticated during this session. If it has not been authenticated,
        /// this method will call <see cref="AuthenticateManagementKey"/>. That
        /// is, your application does not need to authenticate the management key
        /// separately (i.e., call <c>TryAuthenticateManagementKey</c> or
        /// <c>AuthenticateManagementKey</c>), this method will determine if the
        /// management key has been authenticated or not, and if not, it will
        /// make the call to perform mutual authentication.
        /// <para>
        /// The authentication method will collect the management key using the
        /// <c>KeyCollector</c> delegate. If no such delegate has been set, it
        /// will throw an exception.
        /// </para>
        /// <para>
        /// The <c>KeyCollector</c> has an option to cancel the operation. That
        /// is, the <c>AuthenticateManagementKey</c> method will call the
        /// <c>KeyCollector</c> requesting the management key, and it is possible
        /// that during the collection operations, the user cancels. The
        /// <c>KeyCollector</c> will return to the authentication method noting
        /// the cancellation. In that case, it will throw an exception. If you
        /// want the authentication to return <c>false</c> on user cancellation,
        /// you must call <see cref="TryAuthenticateManagementKey(bool)"/> directly
        /// before calling this method.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// There is no <c>KeyCollector</c> loaded, the key provided was not a
        /// valid Triple-DES key, or the YubiKey had some other error, such as
        /// unreliable connection.
        /// </exception>
        /// <exception cref="MalformedYubiKeyResponseException">
        /// The YubiKey returned malformed data and authentication, either single
        /// or double, could not be performed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The user canceled management key collection.
        /// </exception>
        /// <exception cref="SecurityException">
        /// Mutual authentication was performed and the YubiKey was not
        /// authenticated.
        /// </exception>
        public void DeleteMsroots() => WriteMsrootsSpan(ReadOnlySpan<byte>.Empty, OldMaximumObjectLength);
    }
}

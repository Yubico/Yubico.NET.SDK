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

namespace Yubico.PlatformInterop
{
    internal interface ISCard
    {
        /// <summary>
        /// <para>
        /// The SCardBeginTransaction function starts a transaction.
        /// </para>
        /// <para>
        /// The function waits for the completion of all other transactions before it begins. After
        /// the transaction starts, all other applications are blocked from accessing the smart card
        /// while the transaction is in progress.
        /// </para>
        /// </summary>
        /// <param name="cardHandle">
        /// A reference value obtained from a previous call to SCardConnect.
        /// </param>
        uint BeginTransaction(
            SCardCardHandle cardHandle
            );

        /// <summary>
        /// <para>
        /// The SCardCancel function terminates all outstanding actions within a certain resource
        /// manager context.
        /// </para>
        /// <para>
        /// The only requests that you can cancel are those that require waiting for external action
        /// by the smart card or user. Any such outstanding action requests will terminate with a
        /// status indication that the action was canceled. This is especially useful to force
        /// outstanding SCardGetStatusChange calls to terminate.
        /// </para>
        /// </summary>
        /// <param name="context"></param>
        /// Handle that identifies the resource manager context. The resource manager context is set
        /// by a previous call to <c>SCardEstablishContext</c>.
        /// <returns></returns>
        uint Cancel(SCardContext context);

        /// <summary>
        /// The SCardConnect function establishes a connection (using a specific resource manager
        /// context) between the calling application and a smart card contained by a specific reader.
        /// If no card exists in the specified reader, an error is returned.
        /// </summary>
        /// <param name="context">
        /// A handle that identifies the resource manager context. The resource manager context is set
        /// by a previous call to <c>SCardEstablishContext</c>.
        /// </param>
        /// <param name="readerName">
        /// The name of the reader that contains the target card.
        /// </param>
        /// <param name="shareMode">
        /// A flag that indicates whether other applications may form connections to the card.
        /// </param>
        /// <param name="preferredProtocols">
        /// A bitmask of acceptable protocols for the connection. Possible values may be combined
        /// with the OR operation.
        /// </param>
        /// <param name="cardHandle">
        /// A handle that identifies the connection to the smart card in the designated reader.
        /// </param>
        /// <param name="activeProtocol">
        /// A flag that indicates the established active protocol.
        /// </param>
        uint Connect(
            SCardContext context,
            string readerName,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            out SCardCardHandle cardHandle,
            out SCARD_PROTOCOL activeProtocol
            );

        uint Disconnect(IntPtr cardHandle, SCARD_DISPOSITION disposition);

        /// <summary>
        /// The SCardEndTransaction function completes a previously declared transaction, allowing
        /// other applications to resume interactions with the card.
        /// </summary>
        /// <param name="cardHandle">
        /// Reference value obtained from a previous call to SCardConnect. This value would also have
        /// been used in an earlier call to SCardBeginTransaction.
        /// </param>
        /// <param name="disposition">
        /// Action to take on the card in the connected reader on close.
        /// </param>
        /// <returns></returns>
        uint EndTransaction(
            SCardCardHandle cardHandle,
            SCARD_DISPOSITION disposition
            );

        /// <summary>
        /// The SCardEstablishContext function establishes the resource manager context (the scope)
        /// within which database operations are performed.
        /// </summary>
        /// <param name="scope">
        /// Scope of the resource manager context.
        /// </param>
        /// <param name="context">
        /// A safe-handle to the established resource manager context. This handle can now be
        /// supplied to other functions attempting to do work within this context.
        /// </param>
        uint EstablishContext(
            SCARD_SCOPE scope,
            out SCardContext context
            );

        /// <summary>
        /// <para>
        /// The SCardGetStatusChange function blocks execution until the current availability of the
        /// cards in a specific set of readers changes.
        /// </para>
        /// <para>
        /// The caller supplies a list of readers to be monitored by an <c>SCARD_READERSTATE"</c>.
        /// array and the maximum amount of time (in milliseconds) that it is willing to wait for an
        /// action to occur on one of the listed readers. Note that SCardGetStatusChange uses the
        /// user-supplied values in the dwCurrentState members of the rgReaderStates array as the
        /// definition of the current state of the readers. The function returns when there is a change
        /// in availability, having filled in the dwEventState members of rgReaderStates appropriately.
        /// </para>
        /// </summary>
        /// <param name="context">
        /// A handle that identifies the resource manager context. The resource manager context is
        /// set by a previous call to the SCardEstablishContext function.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for an action. A value of zero causes
        /// the function to return immediately. A value of INFINITE (-1) causes this function never
        /// to time out.
        /// </param>
        /// <param name="readerStates">
        /// <para>
        /// A collection of SCARD_READERSTATE structures that specify the readers to watch, and that
        /// receives the results.
        /// </para>
        /// <para>
        /// To be notified of the arrival of a new smart card reader, set the szReader member of a
        /// SCARD_READERSTATE structure to "\\?PnP?\Notification", and set all of the other members
        /// of that structure to zero.
        /// </para>
        /// </param>
        uint GetStatusChange(
            SCardContext context,
            int timeout,
            SCardReaderStates readerStates
            );

        /// <summary>
        /// <para>
        /// The SCardListReaders function provides the list of readers within a set of named reader
        /// groups, eliminating duplicates.
        /// </para>
        /// <para>
        /// The caller supplies a list of reader groups, and receives the list of readers within the
        /// named groups. Unrecognized group names are ignored. This function only returns readers
        /// within the named groups that are currently attached to the system and available for use.
        /// </para>
        /// </summary>
        /// <param name="context">
        /// Handle that identifies the resource manager context for the query. The resource manager
        /// context can be set by a previous call to SCardEstablishContext. If this parameter is set
        /// to NULL, the search for readers is not limited to any context.
        /// </param>
        /// <param name="groups">
        /// Names of the reader groups defined to the system, as a multi-string. Use a NULL value to
        /// list all readers in the system (that is, the SCard$AllReaders group).
        /// </param>
        /// <param name="readerNames">
        /// Multi-string that lists the card readers within the supplied reader groups. If this value
        /// is NULL, SCardListReaders ignores the buffer length supplied in readerNamesLength, writes
        /// the length of the buffer that would have been returned if this parameter had not been
        /// NULL to readerNamesLength, and returns a success code.
        /// </param>
        uint ListReaders(
            SCardContext context,
            string[]? groups,
            out string[] readerNames
            );

        /// <summary>
        /// The SCardReconnect function reestablishes an existing connection between the calling
        /// application and a smart card. This function moves a card handle from direct access to
        /// general access, or acknowledges and clears an error condition that is preventing further
        /// access to the card.
        /// </summary>
        /// <param name="cardHandle">
        /// Reference value obtained from a previous call to <c>SCardConnect</c>.
        /// </param>
        /// <param name="shareMode">
        /// Flag that indicates whether other applications may form connections to this card.
        /// </param>
        /// <param name="preferredProtocols">
        /// <para>
        /// Bitmask of acceptable protocols for this connection. Possible values may be combined with
        /// the OR operation.
        /// </para>
        /// <para>
        /// The value of this parameter should include the current protocol. Attempting to reconnect
        /// with a protocol other than the current protocol will result in an error.
        /// </para>
        /// </param>
        /// <param name="initialization">
        /// Type of initialization that should be performed on the card.
        /// </param>
        /// <param name="activeProtocol">
        /// Flag that indicates the established active protocol.
        /// </param>
        uint Reconnect(
            SCardCardHandle cardHandle,
            SCARD_SHARE shareMode,
            SCARD_PROTOCOL preferredProtocols,
            SCARD_DISPOSITION initialization,
            out SCARD_PROTOCOL activeProtocol
            );

        uint ReleaseContext(
            IntPtr context
            );

        /// <summary>
        /// The SCardStatus function provides the current status of a smart card in a reader. You can
        /// call it any time after a successful call to SCardConnect and before a successful call to
        /// SCardDisconnect. It does not affect the state of the reader or reader driver.
        /// </summary>
        /// <param name="card">
        /// Reference value returned from SCardConnect.
        /// </param>
        /// <param name="readerNames">
        /// List of display names (multiple string) by which the currently connected reader is known.
        /// </param>
        /// <param name="status">
        /// Current state of the smart card in the reader.
        /// </param>
        /// <param name="protocol">
        /// Current protocol, if any. The returned value is meaningful only if the returned value of
        /// status is SPECIFIC.
        /// </param>
        /// <param name="atr">
        /// Pointer to a 32-byte buffer that receives the ATR string from the currently inserted card,
        /// if available.
        /// </param>
        uint Status(
            SCardCardHandle card,
            out string[] readerNames,
            out SCARD_STATUS status,
            out SCARD_PROTOCOL protocol,
            out byte[]? atr
            );

        /// <summary>
        /// The SCardTransmit function sends a service request to the smart card and expects to receive
        /// data back from the card.
        /// </summary>
        /// <param name="card">
        /// A reference value returned from the SCardConnect function.
        /// </param>
        /// <param name="ioSendPci">
        /// <para>
        /// A pointer to the protocol header structure for the instruction. This buffer is in the
        /// format of an SCARD_IO_REQUEST structure, followed by the specific protocol control
        /// information (PCI).
        /// </para>
        /// <para>
        /// For the T=0, T=1, and Raw protocols, the PCI structure is constant.
        /// </para>
        /// </param>
        /// <param name="sendBuffer">
        /// The actual data to be written to the card.
        /// </param>
        /// <param name="ioRecvPci">
        /// Pointer to the protocol header structure for the instruction, followed by a buffer in
        /// which to receive any returned protocol control information (PCI) specific to the protocol
        /// in use. This parameter can be NULL if no PCI is returned.
        /// </param>
        /// <param name="recvBuffer">
        /// <para>
        /// Pointer to any data returned from the card.
        /// </para>
        /// <para>
        /// For T=0, the data is immediately followed by the SW1 and SW2 status bytes. If no data is
        /// returned from the card, then this buffer will only contain the SW1 and SW2 status bytes.
        /// </para>
        /// </param>
        /// <param name="bytesReceived">
        /// Receives the actual number of bytes received from the smart card. For T=0, the receive
        /// buffer must be at least two bytes long to receive the SW1 and SW2 status bytes.
        /// </param>
        uint Transmit(
            SCardCardHandle card,
            SCARD_IO_REQUEST ioSendPci,
            ReadOnlySpan<byte> sendBuffer,
            IntPtr ioRecvPci,
            Span<byte> recvBuffer,
            out int bytesReceived
            );
    }
}

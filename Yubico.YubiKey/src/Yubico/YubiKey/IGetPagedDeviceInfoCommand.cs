using System;
using System.Collections.Generic;

namespace Yubico.YubiKey
{
    /// <summary>
    ///     Defines the contract for a command to retrieve paged device information associated with a YubiKey.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the response expected from the command, which must implement
    ///     the <see cref="IYubiKeyResponseWithData{TData}" /> interface, where TData is a dictionary
    ///     mapping integers to <see cref="ReadOnlyMemory{T}" /> of bytes. This dictionary represents
    ///     the paged data, with each entry corresponding to a different page of Tlv-encoded device information.
    /// </typeparam>
    /// <remarks>
    ///     Implementors should ensure that the command handles pagination correctly and that the <see cref="Page" />
    ///     property is utilized to request specific pages of device information.
    /// </remarks>
    public interface IGetPagedDeviceInfoCommand<out T> : IYubiKeyCommand<T>
        where T : IYubiKeyResponseWithData<Dictionary<int, ReadOnlyMemory<byte>>>
    {
        /// <summary>
        ///     Gets or sets the page number of the device information to retrieve
        /// </summary>
        public byte Page { get; set; }
    }
}

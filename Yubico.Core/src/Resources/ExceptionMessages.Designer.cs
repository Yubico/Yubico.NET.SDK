﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Yubico.Core {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class ExceptionMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ExceptionMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Resources.ExceptionMessages", typeof(ExceptionMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The AES-GCM operation failed..
        /// </summary>
        internal static string AesGcmFailed {
            get {
                return ResourceManager.GetString("AesGcmFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A command APDU has returned an error in the status word. Check ApduStatus and other properties for more details..
        /// </summary>
        internal static string ApduError {
            get {
                return ResourceManager.GetString("ApduError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The CMAC operation failed..
        /// </summary>
        internal static string CmacFailed {
            get {
                return ResourceManager.GetString("CmacFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Encountered an error in the Config Manager library..
        /// </summary>
        internal static string CmError {
            get {
                return ResourceManager.GetString("CmError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified device property is not in the set of supported properties for this API..
        /// </summary>
        internal static string CmPropertyNotSupported {
            get {
                return ResourceManager.GetString("CmPropertyNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CommandApdu field [{0}] out of range for [{1}] APDU encoding..
        /// </summary>
        internal static string CommandApduFieldOutOfRangeEncoding {
            get {
                return ResourceManager.GetString("CommandApduFieldOutOfRangeEncoding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Ne must be a non-negative integer..
        /// </summary>
        internal static string CommandApduNeRangeError {
            get {
                return ResourceManager.GetString("CommandApduNeRangeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No valid ApduEncoding found..
        /// </summary>
        internal static string CommandApduNoValidEncoding {
            get {
                return ResourceManager.GetString("CommandApduNoValidEncoding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The output span is not sufficient to contain the decoded output..
        /// </summary>
        internal static string DecodingOverflow {
            get {
                return ResourceManager.GetString("DecodingOverflow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Computation of the ECDH derived data failed..
        /// </summary>
        internal static string EcdhComputationFailed {
            get {
                return ResourceManager.GetString("EcdhComputationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Generation of ECDH key failed..
        /// </summary>
        internal static string EcdhKeygenFailed {
            get {
                return ResourceManager.GetString("EcdhKeygenFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The output span is not sufficient to contain the encoded output..
        /// </summary>
        internal static string EncodingOverflow {
            get {
                return ResourceManager.GetString("EncodingOverflow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to resolve the native function [{0}]..
        /// </summary>
        internal static string GetUnmanagedFunctionFailed {
            get {
                return ResourceManager.GetString("GetUnmanagedFunctionFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The provided string was not of even length..
        /// </summary>
        internal static string HexNotEvenLength {
            get {
                return ResourceManager.GetString("HexNotEvenLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Encountered a USB interface which appeared to be supported by the HID driver, but was unable to attach the driver..
        /// </summary>
        internal static string HidDriverCantAttach {
            get {
                return ResourceManager.GetString("HidDriverCantAttach", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A HIDRAW operation failed..
        /// </summary>
        internal static string HidrawFailed {
            get {
                return ResourceManager.GetString("HidrawFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [{0}] is not a supported HID class. Only generic HidClass and Keyboard are supported at this time..
        /// </summary>
        internal static string HidUnsupportedDeviceClass {
            get {
                return ResourceManager.GetString("HidUnsupportedDeviceClass", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Illegal character: {0}..
        /// </summary>
        internal static string IllegalCharacter {
            get {
                return ResourceManager.GetString("IllegalCharacter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more AES-GCM inputs were invalid..
        /// </summary>
        internal static string InvalidAesGcmInput {
            get {
                return ResourceManager.GetString("InvalidAesGcmInput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value, 0x{0x2}, is not a valid Base32 digit..
        /// </summary>
        internal static string InvalidBase32Digit {
            get {
                return ResourceManager.GetString("InvalidBase32Digit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Keyboard layout [{0}] doesn&apos;t have HID code [0x{1}].
        /// </summary>
        internal static string InvalidCharForHidCode {
            get {
                return ResourceManager.GetString("InvalidCharForHidCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more CMAC inputs were invalid..
        /// </summary>
        internal static string InvalidCmacInput {
            get {
                return ResourceManager.GetString("InvalidCmacInput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid digit (0x{0}). Digit value must be 0x0 to 0x{1}..
        /// </summary>
        internal static string InvalidDigit {
            get {
                return ResourceManager.GetString("InvalidDigit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Keyboard layout [{0}] doesn&apos;t have an HID code for [{1}]..
        /// </summary>
        internal static string InvalidHidCode {
            get {
                return ResourceManager.GetString("InvalidHidCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The buffer length must be large enough to hold the report, plus one additional byte that specifies a nonzero report ID or zero..
        /// </summary>
        internal static string InvalidReportBufferLength {
            get {
                return ResourceManager.GetString("InvalidReportBufferLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file handle cannot be null..
        /// </summary>
        internal static string InvalidSafeFileHandle {
            get {
                return ResourceManager.GetString("InvalidSafeFileHandle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Can&apos;t open IOKit device..
        /// </summary>
        internal static string IOKitCannotOpenDevice {
            get {
                return ResourceManager.GetString("IOKitCannotOpenDevice", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The IOKit operation [{0}] failed..
        /// </summary>
        internal static string IOKitOperationFailed {
            get {
                return ResourceManager.GetString("IOKitOperationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No IOKit registry entry was found for the device..
        /// </summary>
        internal static string IOKitRegistryEntryNotFound {
            get {
                return ResourceManager.GetString("IOKitRegistryEntryNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Encountered unexpected property type on an IOKit device. Expected [{0}] but encountered [{1}]..
        /// </summary>
        internal static string IOKitTypeMismatch {
            get {
                return ResourceManager.GetString("IOKitTypeMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not load library [{0}]..
        /// </summary>
        internal static string LibraryLoadFailed {
            get {
                return ResourceManager.GetString("LibraryLoadFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not open HID node on Linux..
        /// </summary>
        internal static string LinuxHidOpenFailed {
            get {
                return ResourceManager.GetString("LinuxHidOpenFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Encountered an error in the Linux udev library..
        /// </summary>
        internal static string LinuxUdevError {
            get {
                return ResourceManager.GetString("LinuxUdevError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to It is not possible to restart listening for device updates after stopping..
        /// </summary>
        internal static string ListenRestartNotAllowed {
            get {
                return ResourceManager.GetString("ListenRestartNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to open connection to NFC device. The tag may have been removed..
        /// </summary>
        internal static string NfcConnectionFailed {
            get {
                return ResourceManager.GetString("NfcConnectionFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Response APDUs must contain at least 2 bytes for status. The data passed in has a [{0}] byte length..
        /// </summary>
        internal static string ResponseApduNotEnoughBytes {
            get {
                return ResourceManager.GetString("ResponseApduNotEnoughBytes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to establish a connection to the platform&apos;s smartcard subsystem..
        /// </summary>
        internal static string SCardCantEstablish {
            get {
                return ResourceManager.GetString("SCardCantEstablish", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to establish a connection to the smart card in [{0}]..
        /// </summary>
        internal static string SCardCardCantConnect {
            get {
                return ResourceManager.GetString("SCardCardCantConnect", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Encountered an error communicating with the smart card subsystem..
        /// </summary>
        internal static string SCardError {
            get {
                return ResourceManager.GetString("SCardError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while attempting to get the current status of the smart cards available to the system..
        /// </summary>
        internal static string SCardGetStatusChangeFailed {
            get {
                return ResourceManager.GetString("SCardGetStatusChangeFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while attempting to retrieve the list of smart card readers..
        /// </summary>
        internal static string SCardListReadersFailed {
            get {
                return ResourceManager.GetString("SCardListReadersFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The smart card subsystem indicated success when enumerating smart cards, however no data was returned..
        /// </summary>
        internal static string SCardListReadersUnexpectedLength {
            get {
                return ResourceManager.GetString("SCardListReadersUnexpectedLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to reconnect to the smart card..
        /// </summary>
        internal static string SCardReconnectFailed {
            get {
                return ResourceManager.GetString("SCardReconnectFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to begin a transaction with the given smart card..
        /// </summary>
        internal static string SCardTransactionFailed {
            get {
                return ResourceManager.GetString("SCardTransactionFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Encountered an error while attempting to transmit data to a smart card..
        /// </summary>
        internal static string SCardTransmitFailure {
            get {
                return ResourceManager.GetString("SCardTransmitFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The input does not build a valid schema..
        /// </summary>
        internal static string TlvInvalidSchema {
            get {
                return ResourceManager.GetString("TlvInvalidSchema", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The TLV reading code encountered an unexpected tag or value..
        /// </summary>
        internal static string TlvUnexpectedEncoding {
            get {
                return ResourceManager.GetString("TlvUnexpectedEncoding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected end of buffer when trying to parse a TLV data structure..
        /// </summary>
        internal static string TlvUnexpectedEndOfBuffer {
            get {
                return ResourceManager.GetString("TlvUnexpectedEndOfBuffer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot parse element with either an initial octet of 80 or a length field longer than 4 bytes..
        /// </summary>
        internal static string TlvUnsupportedLengthField {
            get {
                return ResourceManager.GetString("TlvUnsupportedLengthField", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot parse element with tag longer than 2 bytes..
        /// </summary>
        internal static string TlvUnsupportedTag {
            get {
                return ResourceManager.GetString("TlvUnsupportedTag", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unspecified error has been raised during the issuance of a command APDU..
        /// </summary>
        internal static string UnknownApduError {
            get {
                return ResourceManager.GetString("UnknownApduError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Encountered a Platform API exception of unknown origin..
        /// </summary>
        internal static string UnknownPlatformApiError {
            get {
                return ResourceManager.GetString("UnknownPlatformApiError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempted to access interface index [{0}]. The interface list only contains [{1}] elements..
        /// </summary>
        internal static string UsbInterfaceOutOfRange {
            get {
                return ResourceManager.GetString("UsbInterfaceOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The runloop used for the IOKit operation did not complete successfully. Result was [{0}]..
        /// </summary>
        internal static string WrongIOKitRunLoopMode {
            get {
                return ResourceManager.GetString("WrongIOKitRunLoopMode", resourceCulture);
            }
        }
    }
}

<!-- Copyright 2021 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# Exception usage

Exceptions are the mechanism for relaying error information throughout the .NET platform and in languages
such as C#.

> Applications must be able to handle errors that occur during execution in a consistent manner. .NET provides
> a model for notifying applications of errors in a uniform way: .NET operations indicate failure by throwing
> exceptions.
>
> [Handling and throwing exceptions in .NET](https://docs.microsoft.com/en-us/dotnet/standard/exceptions/)
 

## Prerequisites

This page is not meant to be an exhaustive reference on exception behavior and design. The expectation is that
you have read through the following pages and have a grasp of the fundamentals of exceptions in .NET.

1. [Handling and throwing exceptions in .NET](https://docs.microsoft.com/en-us/dotnet/standard/exceptions/)
2. [Best Practices for exceptions - .NET](https://docs.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
3. [Exceptions and Exception Handling](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/)
4. [Using Exceptions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/using-exceptions)
5. [Creating and Throwing Exceptions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/creating-and-throwing-exceptions)
6. [How to execute cleanup code using finally](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/how-to-execute-cleanup-code-using-finally)

Instead, this guide will aim to be a quick reference guide for how exceptions can be used effectively and
efficiently within the SDK project.

## Using exceptions

Your *default* mindset should be: If I can’t reasonably handle a problem in the immediate scope of the function,
I should **throw an exception**!

The rest of this section will go into details about the specifics on how you should use exceptions.

### Why use exceptions?

- Can provide rich information to the caller
- Errors cannot be ignored
- Uniform type tree, no colliding numberings or different encoding schemes (POSIX vs. Win32 vs. HRESULT, etc.)
- Happy paths and error paths are explicitly defined
- Developer expectations: It is how .NET does things
- Debugger will automatically break on exceptions

### Error condition? or program flow?

> Don't use exceptions to change the flow of a program as part of ordinary execution. Use exceptions to report
> and handle error conditions.
>
> [Creating and Throwing Exceptions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/creating-and-throwing-exceptions#things-to-avoid-when-throwing-exceptions)

In some cases, it is extremely clear when something is an error condition (argument is null) and when it is control
flow (last item of an array encountered while looping). Many times it is not.

One very important distinction here is to understand what the above quote is not saying. It is not saying that you
should never catch and respond to error conditions.

### Exceptional conditions

It is impossible to enumerate all possible exception conditions, or even classifications of exception conditions.
This list represents the exceptions that have been encountered by the SDK thus far, and how many times they appear.
The number of appearances is a very rough measurement and is used here to aid readability of the table. How many
times an exception type appears should not be used to justify the selection/use of an exception type.

| Condition | Example | Exception type | Appearances |
| --- | --- | --- | --- |
| A method can’t complete because the object’s state is not valid. | CreateCommandApdu on a command class which has not had one of the required properties set ahead of time. | [InvalidOperationException](https://docs.microsoft.com/en-us/dotnet/api/system.invalidoperationexception?view=net-5.0) | 193 |
| A method or property can’t complete due to an invalid parameter value. | A reference-type parameter (such as a class object) is null when it is expected to have a value. | [ArgumentException](https://docs.microsoft.com/en-us/dotnet/api/system.argumentexception?view=net-5.0)<br>[ArgumentNullException](https://docs.microsoft.com/en-us/dotnet/api/system.argumentnullexception?view=net-5.0) | 139<br>136 |
| Specific to DotNetPolyfill’s System.Formats.Cbor (unable to read/write CBOR format data) | There was an unexpected end of CBOR encoding data | CborContentException | 48 |
| A method can’t safely continue due to unexpected or corrupted state. | A data packet received from the YubiKey is malformed. Processing it would result in nonsensical results. | *Other exception types possible*<br>[MalformedYubiKeyResponseException](https://docs.yubico.com/yesdk/api/Yubico.YubiKey.MalformedYubiKeyResponseException.html) | --<br>35 |
An enumeration value type is out of bounds, or an unknown flag is set. | | [ArgumentOutOfRangeException](https://docs.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception?view=net-5.0) | 29 |
| A requested method or operation is not implemented | The default constructor is explicitly defined, but we don’t want it to be used. | [NotImplementedException](https://docs.microsoft.com/en-us/dotnet/api/system.notimplementedexception?view=net-5.0) | 26 |
| A method owned by the platform returned an error code or unexpected state | Failed to resolve the requested native function. | PlatformApiException | 25 |
| Invalid or missing data passed to a CTAP2 command | As part of a command to be sent to the YubiKey, a supplied property contained invalid data for CTAP2 | Ctap2DataException | 19 |
| An invoked method is not supported | The requested command is not supported by YubiKeys < v4.3.0 | [NotSupportedException](https://docs.microsoft.com/en-us/dotnet/api/system.notsupportedexception?view=net-5.0) | 15 |
| This should not be used, and needs to be replaced by a more specific exception type | -- | [Exception](https://docs.microsoft.com/en-us/dotnet/api/system.exception?view=net-5.0) | 12 |
| Specific to SCP03 encoding and decoding | The response from the device contained an incorrect RMAC. This could be due to incorrect static keys, a skipped or missing response, or man-in-the-middle attack. | SecureChannelException | 9 |
| An operation was canceled by the user | A callback for user input such as a PIN prompt, was dismissed without completing successfully | [OperationCanceledException](https://docs.microsoft.com/en-us/dotnet/api/system.operationcanceledexception?view=net-5.0) | 9 |
| An operation could not complete because the caller isn’t authenticated. |  | [SecurityException](https://docs.microsoft.com/en-us/dotnet/api/system.security.securityexception?view=net-5.0) | 8 |
| The underlying platform smart card subsystem encounters an error | The error code returned by the SCard function was not “success” | SCardException | 8 |
| A feature does not run on a particular platform | A new smart card device is requested, but the platform is not supported | [PlatformNotSupportedException](https://docs.microsoft.com/en-us/dotnet/api/system.platformnotsupportedexception?view=net-5.0) | 7 |
| Specific to Yubico.Core.Tlv | Cannot parse element with tag longer than 2 bytes | TlvException | 6 |
|  |  | [OverflowException](https://docs.microsoft.com/en-us/dotnet/api/system.overflowexception?view=net-5.0)<br>KeyboardConnectionException<br>[ObjectDisposedException](https://docs.microsoft.com/en-us/dotnet/api/system.objectdisposedexception?view=net-5.0)<br>[KeyNotFoundException](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.keynotfoundexception?view=net-5.0)<br>[IndexOutOfRangeException](https://docs.microsoft.com/en-us/dotnet/api/system.indexoutofrangeexception?view=net-5.0)<br>[CryptographicException](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicexception?view=net-5.0)<br>BadFido2StatusException<br>ApduException | 5> |

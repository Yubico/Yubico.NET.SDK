---
uid: UsersManualCommands
---

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

# Commands

Any operation the YubiKey performs will be a collection of commands. Think of a command
as the smallest unit of function on a YubiKey.

For example, suppose the operation you want to perform is getting the serial number of
the key. There is a PIV command to do that:
[GetSerialNumberCommand](xref:Yubico.YubiKey.Piv.Commands.GetSerialNumberCommand).

But suppose you want to create a digital signature using PIV. That operation is made up
of work done off the YubiKey (digesting the data and formatting a block), then calling
on the YubiKey AUTHENTICATE:SIGN command. But the operation also requires authentication
by entering the PIN, which is the VERIFY command. So the signing operation is made up of
two commands.

There are SDK APIs for operations. Those classes will call on the appropriate YubiKey
commands "under the covers". But if you want to perform a particular command, there is the
command API. You can find the details of all the commands in each application's namespace,
such as [Yubico.YubiKey.Piv.Commands](xref:Yubico.YubiKey.Piv.Commands).

## The command and APDU

A command is the function you want the YubiKey to perform. The message sent to the
YubiKey instructing it to perform the command is a "command APDU" (Application Protocol
Data Unit, see the User's Manual entry on [APDUs](xref:UsersManualApdu)). The YubiKey performs the
command and returns the result in the form of a "response APDU".

The command API in the SDK is a collection of classes that represent each of the
commands a YubiKey can perform, and each of the responses. Under the covers, each
command class will know how to build the command APDU, and each response class will know
how to parse and present the response APDU. You don't really need to know about the
specific APDUs, just the command and response classes.

## Commands and applications

There are seven YubiKey applications:

* Management
* OTP
* FIDO U2F
* FIDO2
* PIV
* OpenPGP Card
* OATH

Each application has its own set of commands. Generally, you will want to perform an
operation for a specific application. For example, your application might want to
perform a Yubico Challenge-Response OTP generation. Look in the OTP commands section to
find the commands you need to perform to do so. You will not find analagous commands in
the PIV or OpenPGP Card applications because computing Yubico OTP Challenge-Response
OTPs is not something they can do.

## Executing a command in the SDK

In general, to execute a command, you will call on the `SendCommand` method in one of
the classes that implements the
[IYubiKeyConnection](xref:Yubico.YubiKey.IYubiKeyConnection) interface.

```C#
TResponse SendCommand<TResponse>(IYubiKeyCommand<TResponse> yubiKeyCommand)
  where TResponse : IYubiKeyResponse;
```

Visit the [Making a Connection](xref:UsersManualMakingAConnection) page in the User's Manual for
more information on connections.

What this means is that you are going to build an instance of a class that implements
the [IYubiKeyCommand](xref:Yubico.YubiKey.IYubiKeyCommand`1)
interface. You will then pass that object to the `SendCommand` method. The return from
that call will be an object that implements the
[IYubiKeyResponse](xref:Yubico.YubiKey.IYubiKeyResponse) interface.

### The command and response pair

When you find the class that represents the command you want to perform, read the
documentation for that class. It will describe its partner response class. You now know
what type the return value to the `SendCommand` will be.

For example, suppose you want to execute the PIV application's get version command.
Find the class: [VersionCommand](xref:Yubico.YubiKey.Piv.Commands.VersionCommand).
The documentation says,

> Get the YubiKey's firmware version.<br/>
> The partner response class is VersionResponse.

You can now write the following

```C#
VersionCommand versionCommand = new VersionCommand();
VersionResponse versionResponse = connection.SendCommand(versionCommand);
```

### The response

With the response object, you will be able to see the results of the command. There are
generally three things you want to see:

* The Status
* A message explaining the status
* The Data

Note: The Status Word is the APDU's response code (see [APDU](xref:UsersManualApdu)). Most of the time you
will likely not worry about this. But it is provided in case you do need to examine it.

The Status is the SDK response code. It describes the result of the what the command
object did. The possible values are in the
[ResponseStatus](xref:Yubico.YubiKey.ResponseStatus) enum.

The StatusMessage gives similar information to the Status, but is intended to be
displayed to the end-user when an unhandled error occurs.

The Data is the actual data returned by the YubiKey. The response class parsed it and
now offers to return it in a more consumable form. You get the data by calling the
response class's `GetData` method. If the object's state is invalid, the `GetData`
method will throw an exception. Therefore it is best to first check
`Status` before calling `GetData`. That way errors can be handled without relying
on exceptions.

Each response class will specify the format of the
data returned. For example, the response to the get version command is

```C#
public class FirmwareVersion
{
    public byte Major { get; set; }
    public byte Minor { get; set; }
    public byte Patch { get; set; }
     . . .
}
```

After calling the `SendCommand` method, you now have a response object. First
check the `Status` to ensure it executed as expected, and then call that
object's `GetData` method.

```C#
VersionCommand versionCommand = new VersionCommand();
VersionResponse versionResponse = connection.SendCommand(versionCommand);

if (versionResponse.Status != ResponseStatus.Success)
{
  // In this example, we're not trying to recover; simply throw an exception
  throw new Exception(versionResponse.StatusMessage)
}

FirmwareVersion versionNum = versionResponse.GetData();
```

You can now examine the version number as

```
versionNum.Major, versionNum.Minor, versionNum.Patch
```

### Command failure data

Sometimes, the SDK encounters an error when executing a command. For example, suppose
you call the PIV's get serial number command and an error was encountered.
The Response object ([GetSerialNumberResponse](xref:Yubico.YubiKey.Piv.Commands.GetSerialNumberResponse))
will be successfully constructed, but the `Status` will be set to a non "Success"
value such as `ResponseStatus.Failed`. In this state, the `GetData` method will throw
an exception because it will not be able to successfuly parse the result. Therefore it
is best to first check `Status` before calling `GetData`. That way errors can be
handled without relying on exceptions.

```C#
int serialNumber;

GetSerialNumberCommand serialCommand = new GetSerialNumberCommand();
GetSerialNumberResponse serialResponse = connection.SendCommand(serialCommand);

if (serialResponse.Status == ResponseStatus.Success)
{
    serialNumber = serialResponse.GetData();
}
```

### Data not found

It is possible that you call a command to get data from the YubiKey, yet there is no
data. For example, suppose you call the PIV application's GET DATA command,
requesting the cert in slot 9C -- but there is no cert in slot 9C.

In this situation the `Status` will be set to `ResponseStatus.NoData`. This means
that the command was simply unable to find the requested data. This should be
sufficient to understand the result of the command. Because no data could
be returned, if you call `GetData` it will throw an exception.

### No response data

Some responses have no data. For example, the PIV applications PUT DATA command will
load some information onto the YubiKey. It simply returns a `Status` indicating
whether it was successful or encountered an error. That's it.

For responses that do not have data, the response class will implement the
[IYubiKeyResponse](xref:Yubico.YubiKey.IYubiKeyResponse) interface
only. That interface does not have a `GetData` method. If you try to get data out of a
response class that has no data, your code will not compile.

Incidentally, notice that response classes that do return data implement two interfaces:

* [IYubiKeyResponse](xref:Yubico.YubiKey.IYubiKeyResponse)
* [IYubiKeyResponseWithData](xref:Yubico.YubiKey.IYubiKeyResponseWithData`1)

The "WithData" interface is where the `GetData` method is declared.

The documentation for the command class led you to its partner, the response class. The
documentation for that class will indicate whether it has data or not, and if so, in
what format the data is returned.

### Input data to the command

In the `VersionCommand` example, there was no input data. Some commands require user
input. For example, to perform the PIV's AUTHENTICATE:SIGN command, you need the PIN,
the data to sign (formatted), and the slot number of the key to use.

Generally, you will provide the input data in the command class's constructor.

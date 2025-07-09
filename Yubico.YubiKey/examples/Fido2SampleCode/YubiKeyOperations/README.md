# Best Practices: Implementing FIDO2 Reset with Yubico.NET.SDK

This document provides a guide for developers on how to reliably implement the FIDO2 Reset functionality in a .NET application using the Yubico.NET.SDK. A FIDO2 reset is a sensitive hardware operation that requires a robust, user-friendly implementation to avoid failure and user confusion.

This guide refers to the `Fido2ResetService.cs` class as a reference implementation that embodies these best practices.

## Core Challenges of FIDO2 Reset

A successful FIDO2 reset involves more than a single API call. Developers must handle three primary challenges:

1.  **Hardware Re-enumeration**: The reset process requires the user to physically unplug and re-insert the YubiKey. Your application must be able to reliably detect the exact moment the device becomes available to the operating system again. A simple, synchronous check is prone to race conditions.

2.  **The 10-Second Window**: The YubiKey requires the `ResetCommand` to be sent within a short time window (approximately 10 seconds) after it powers on. Failing to meet this window will cause the operation to fail.

3.  **User Presence (Touch)**: The final step of the reset requires the user to physically touch the YubiKey's contact. Your application must instruct the user to do this at the correct time.

## Architectural Best Practices

To overcome these challenges, your implementation should follow these architectural principles.

### 1. Use an Event-Driven Approach for Device Detection

**The Problem:** Using a simple polling loop (`while(true) { ... Task.Delay(); }`) to check for the re-inserted YubiKey is inefficient and can miss the 10-second reset window.

**The Solution:** Use the `Yubico.YubiKey.DeviceExtensions.YubiKeyDeviceListener`. This class hooks directly into the operating system's hardware events, allowing for immediate notification when a YubiKey is inserted. This is the most reliable way to meet the timing requirement.

Our reference `Fido2ResetService.cs` accomplishes this with a helper method that converts the event into an awaitable `Task`.

**Reference Implementation (`Fido2ResetService.cs`):**
```csharp
private async Task<IYubiKeyDevice?> WaitForYubiKeyArrival(TimeSpan timeout)
{
    YubiKeyDeviceListener yubiKeyDeviceListener = YubiKeyDeviceListener.Instance;
    var tcs = new TaskCompletionSource<IYubiKeyDevice?>();

    // This event handler will complete our Task when a YubiKey arrives.
    void DeviceArrivedHandler(object? sender, YubiKeyDeviceEventArgs eventArgs)
    {
        yubiKeyDeviceListener.Arrived -= DeviceArrivedHandler;
        tcs.TrySetResult(eventArgs.Device);
    }

    yubiKeyDeviceListener.Arrived += DeviceArrivedHandler;

    // Await the event or a timeout, whichever comes first.
    Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));

    yubiKeyDeviceListener.Arrived -= DeviceArrivedHandler;

    if (completedTask == tcs.Task)
    {
        return await tcs.Task;
    }
    else
    {
        return null; // Timeout occurred
    }
}
```

### 2. Verify the Correct Device Was Re-inserted

**The Problem:** A user might accidentally insert a different YubiKey than the one they removed. Proceeding with the reset would be a destructive action on the wrong device.

**The Solution:** After detecting a re-inserted key, always compare its `SerialNumber` to the serial number of the key that was originally removed.

**Reference Implementation (`Fido2ResetService.cs`):**
```csharp
// Store the original serial number before starting the process.
int originalSerialNumber = yubiKey.SerialNumber;

// ... after WaitForYubiKeyArrival returns reinsertedKey ...

if (reinsertedKey is null)
{
    // Handle timeout...
    return;
}

// VERIFY THE SERIAL NUMBER
if (reinsertedKey.SerialNumber != originalSerialNumber)
{
    WriteError($"Incorrect YubiKey. Expected {originalSerialNumber}, but found {reinsertedKey.SerialNumber}.");
    return;
}
```

### 3. Inspect the Command Response to Guide the User

**The Problem:** Sending the `ResetCommand` does not mean the operation is complete. The command's response tells you what to do next. Ignoring the response can lead to a state where the key is blinking, but the user has not been told to touch it, causing the operation to time out and fail.

**The Solution:** Always capture the `ResetResponse` object returned from the `SendCommand` method. Check its `Status` property to determine the outcome.

**Reference Implementation (`Fido2ResetService.cs`):**
```csharp
using (IYubiKeyConnection fidoConnection = reinsertedKey.Connect(YubiKeyApplication.Fido2))
{
    var resetCommand = new ResetCommand();
    ResetResponse response = fidoConnection.SendCommand(resetCommand);

    // Check the status of the response to handle user touch.
    switch (response.Status)
    {
        case ResponseStatus.Success:
            WriteSuccess("FIDO2 application has been successfully reset.");
            break;

        case ResponseStatus.ConditionsNotSatisfied:
            WriteSuccess("Command sent. Please touch the flashing contact on your YubiKey NOW to complete the reset.");
            break;

        default:
            WriteError($"The FIDO2 reset failed. The YubiKey returned an error: {response.StatusMessage}");
            break;
    }
}
```

### 4. Provide Clear, Step-by-Step User Instructions

**The Problem:** The reset is a multi-step process (remove, re-insert, touch). A silent or unresponsive application will confuse the user.

**The Solution:** Guide the user through each phase of the operation.
* Tell the user when to remove the key.
* Use a visual indicator (like a spinner) to show the app is waiting.
* Tell the user when to re-insert the key.
* Give the final, explicit instruction to touch the key based on the command response.

By adhering to these best practices, your development team can build a FIDO2 reset feature that is not only functional but also robust, reliable, and user-friendly.

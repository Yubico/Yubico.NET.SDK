// Fido2ResetService.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Yubico.YubiKey;
using Yubico.YubiKey.DeviceExtensions; // Required for YubiKeyDeviceListener
using Yubico.YubiKey.Fido2;            // Required for ResponseStatus
using Yubico.YubiKey.Fido2.Commands;  // Required for ResetCommand

/// <summary>
/// A service to reliably perform a FIDO2 reset on a YubiKey,
/// demonstrating the best practice event-driven approach and response validation.
/// </summary>
public class Fido2ResetService
{
    /// <summary>
    /// Executes the full FIDO2 reset workflow, including response validation
    /// to handle the user touch requirement.
    /// </summary>
    public async Task PerformResetAsync()
    {
        Console.WriteLine("--- YubiKey FIDO2 Resilient Reset Tool ---");

        // Find the YubiKey to operate on.
        IYubiKeyDevice? yubiKey = Yubico.YubiKey.YubiKeyDevice.FindAll().FirstOrDefault();
        if (yubiKey is null)
        {
            WriteError("No YubiKey found. Please insert a YubiKey and try again.");
            return;
        }

        Console.WriteLine($"Found YubiKey: {yubiKey.SerialNumber} (Firmware: {yubiKey.FirmwareVersion})");

        // Get explicit user confirmation.
        WriteWarning("DANGER: This will permanently erase all FIDO2 credentials on this YubiKey.");
        /*Console.Write("Type 'reset' to confirm this action: ");
        if (Console.ReadLine()?.ToLower() != "reset")
        {
            Console.WriteLine("Action canceled.");
            return;
        }*/

        var spinner = new ConsoleSpinner();
        try
        {
            // Poll for removal, as this is a simple state change.
            Console.WriteLine("\nSTEP 1 of 2: Please REMOVE the YubiKey from the USB port.");
            Console.Write("Waiting for removal... ");
            spinner.Start();
            while (Yubico.YubiKey.YubiKeyDevice.FindAll().Any(k => k.SerialNumber == yubiKey.SerialNumber))
            {
                await Task.Delay(500);
            }
            spinner.Stop();
            WriteSuccess("YubiKey removed.");

            // Use an event listener for immediate device detection upon re-insertion.
            Console.WriteLine("\nSTEP 2 of 2: Please RE-INSERT the YubiKey.");
            Console.Write("Waiting for re-insertion... ");
            spinner.Start();

            IYubiKeyDevice? reinsertedKey = await WaitForYubiKeyArrival(TimeSpan.FromSeconds(20));

            spinner.Stop();

            if (reinsertedKey is null)
            {
                WriteError("Operation timed out. Could not detect the YubiKey after re-insertion.");
                return;
            }

            if (reinsertedKey.SerialNumber != yubiKey.SerialNumber)
            {
                WriteError($"Incorrect YubiKey inserted. Expected serial number {yubiKey.SerialNumber}, but found {reinsertedKey.SerialNumber}.");
                return;
            }

            WriteSuccess($"YubiKey {reinsertedKey.SerialNumber} detected.");
            Console.WriteLine("Sending reset command...");

            // Connect and send the ResetCommand, then validate the response.
            using (IYubiKeyConnection fidoConnection = reinsertedKey.Connect(YubiKeyApplication.Fido2))
            {
                var resetCommand = new ResetCommand();
                WriteWarning("\nSTEP 3 of 3: Touch the YubiKey's to complete the FIDO2 reset.");
                ResetResponse response = fidoConnection.SendCommand(resetCommand);

                // Check the status of the response to handle user touch.
                switch (response.Status)
                {
                    case ResponseStatus.Success:
                        WriteSuccess("FIDO2 application has been successfully reset.");
                        break;

                    case ResponseStatus.ConditionsNotSatisfied:
                        WriteSuccess("Command sent. Please touch the flashing contact on your YubiKey to complete the reset.");
                        break;

                    default:
                        WriteError($"The FIDO2 reset failed. The YubiKey returned an error: {response.StatusMessage}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            spinner.Stop();
            WriteError($"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Uses the YubiKeyDeviceListener to asynchronously wait for a YubiKey to be inserted.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for a device.</param>
    /// <returns>The IYubiKeyDevice that arrived, or null if the operation timed out.</returns>
    private async Task<IYubiKeyDevice?> WaitForYubiKeyArrival(TimeSpan timeout)
    {
        YubiKeyDeviceListener yubiKeyDeviceListener = YubiKeyDeviceListener.Instance;
        var tcs = new TaskCompletionSource<IYubiKeyDevice?>();

        // This event handler will complete our Task when a YubiKey arrives.
        void DeviceArrivedHandler(object? sender, YubiKeyDeviceEventArgs eventArgs)
        {
            // Unsubscribe immediately to prevent multiple completions.
            yubiKeyDeviceListener.Arrived -= DeviceArrivedHandler;
            tcs.TrySetResult(eventArgs.Device);
        }

        yubiKeyDeviceListener.Arrived += DeviceArrivedHandler;

        // Await the event or a timeout, whichever comes first.
        Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));

        // Clean up the event handler subscription.
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

    // Helper methods for colored console output.
    private void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
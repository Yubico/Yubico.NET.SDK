var info = await device.GetDeviceInfoAsync();
bool pivOverUsb = (info.UsbSupported & DeviceCapabilities.Piv) != 0;
bool oathEnabledOverNfc = (info.NfcEnabled & DeviceCapabilities.Oath) != 0;
Console.WriteLine($"Configuration locked: {info.IsLocked}");

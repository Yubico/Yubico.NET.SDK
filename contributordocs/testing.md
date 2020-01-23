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

# Testing

## Unit Tests

Project: `Yubico.YubiKey.UnitTests`

## Integration Tests

Project: `Yubico.YubiKey.IntegrationTests`

### Standard Test Devices

To make integration testing easier across the team, we have standardized the set of YubiKeys used in integration testing. These devices are enumerated in `Yubico.YubiKey.TestUtilities.StandardTestDevice`. As of April 26, 2021, these test devices are:

- Fw3
  - Major version 3, USB A keychain, not FIPS
- Fw4Fips
  - Major version 4, USB A keychain, FIPS
- Fw5
  - Major version 5, USB A keychain, not FIPS
- Fw5Fips
  - Major version 5, USB A keychain, FIPS
- Fw5ci
  - Major version 5, USB C Lightning, not FIPS

### Selecting Test Devices

The set of all available test devices can be retrieved using `Yubico.YubiKey.TestUtilities.IntegrationTestDeviceEnumeration`

To run a test against a specific test device, you can use `Yubico.YubiKey.TestUtilities.TestDeviceSelection` For example, `SelectRequiredTestDevice(StandardTestDevice)` will find the first valid device in a set of `IYubiKey` objects. If it cannot find a valid device, it throws an exception.

```csharp
[Theory]
[InlineData(StandardTestDevice.Fw5)]
[InlineData(StandardTestDevice.Fw5Fips)]
public void SetDeviceInfo_NoData_ResponseStatusSuccess(StandardTestDevice testDeviceType)
{
    IYubiKey testDevice =
        IntegrationTestDeviceEnumeration.GetTestDevices()
        .SelectRequiredTestDevice(testDeviceType);

    using IYubiKeyConnection connection = testDevice.Connect(YubiKeyApplication.Management);

    var setCommand = new SetDeviceInfoCommand();

    YubiKeyResponse setDeviceInfoResponse = connection.SendCommand(setCommand);
    Assert.Equal(ResponseStatus.Success, setDeviceInfoResponse.Status);
}
```

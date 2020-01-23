# Copyright 2021 Yubico AB
# 
# Licensed under the Apache License, Version 2.0 (the "License").
# You may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import clr
import pathlib
import sys

# Add directory of libraries to search path
# In this case, the same directory as the location of this file
fileDirectory = pathlib.Path(__file__).parent.absolute()
sys.path.append(fileDirectory)

# Required 
clr.AddReference('System.Collections.Immutable')

# Import the assembly. Don't need '.dll'
clr.AddReference('Yubico.Authenticators')

from Yubico.Authenticators import *
import Yubico.Authenticators.Commands.Piv as Piv
import Yubico.Authenticators.Commands.Otp as Otp


def main():
   # Enumerate all Yubikeys
   keys = YubiKeyEnumerator.GetYubiKeys()

   # Connect to each yubikey and print basic info
   i = 0
   for key in keys:
      print(f'YubiKey #{i} [ Path: {key.Path} HasSmartCard: {key.HasSmartCard} HasHidFido: {key.HasHidFido} HasHidKeyboard: {key.HasHidKeyboard} ]')

      # First, connect to the PIV application
      pivConnection = key.Connect(YubiKeyApplication.Piv)

      # Next, instantiate the command with any parameters.
      pivCommand = Piv.VersionCommand() # No parameters for this command.

      # Make sure we got a response, and do something with the result
      pivResponse = pivConnection.SendCommand[Piv.VersionResponse](pivCommand)
      pivResponse.ThrowIfFailed()

      version = pivResponse.GetData()
      print(f'\tFWV   : {version.Major}.{version.Minor}.{version.Patch}')

      # Now, try it for OTP.
      otpConnection = key.Connect(YubiKeyApplication.Otp)
      otpCommand = Otp.GetSerialNumberCommand()
      otpResponse = otpConnection.SendCommand[Otp.GetSerialNumberResponse](otpCommand)
      otpResponse.ThrowIfFailed()

      serial = otpResponse.GetData()
      print(f'\tSerial: {serial}\n')

      i += 1

if __name__ == "__main__":
   main()

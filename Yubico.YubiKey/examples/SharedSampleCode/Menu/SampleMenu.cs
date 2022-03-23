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
using System.Globalization;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Sample.SharedCode
{
    public class SampleMenu
    {
        private const int DefaultMaxInvalidCount = 3;
        private const int LowMaxInvalidCount = 1;
        private const int HighMaxInvalidCount = 5;

        private readonly int _maxInvalidCount;
        private int _invalidCount;
        private readonly string[] _mainMenuStrings;
        private readonly Array _mainMenuValues;
        private readonly int _exitValue;

        // Create a new instance of the SampleMenu class.
        // Provide the max invalid count, this is the number of times in a row
        // the class will allow the user to type an invalid response before
        // giving up.
        // For example, if the maxInvalidCount is 2, and in response to the main
        // menu the user types "5000", the class will indicate invalid response
        // and ask again. If on the second try, the user types "11111", the class
        // will indicate invalid response and exit.
        // The maxInvalidCount can be 1, 2, 3, 4, or 5. Any other input and the
        // class will set the max to 3. For example, if you instantiate this
        // class
        //   var sampleMenu = new SampleMenu(21).
        // the constructor will build an object and the maxInvalidCount will be 3.
        public SampleMenu(int maxInvalidCount, Type mainMenuEnumType, int exitValue)
        {
            _maxInvalidCount = DefaultMaxInvalidCount;
            if ((maxInvalidCount >= LowMaxInvalidCount) && (maxInvalidCount <= HighMaxInvalidCount))
            {
                _maxInvalidCount = maxInvalidCount;
            }

            _mainMenuStrings = Enum.GetNames(mainMenuEnumType);
            _mainMenuValues = Enum.GetValues(mainMenuEnumType);
            _exitValue = exitValue;
        }

        // Run a menu for the given menuItems.
        // Write out the title, then write out lines listing the choices, based
        // on the menuItems supported.
        // Collect the response, and if it is one of the valid responses, return
        // it.
        // If the user input is invalid, try again. If the user enters an invalid
        // response too many times in a row, stop trying and return Exit.
        public int RunMainMenu(string title)
        {
            int indexChosen = RunMenu(title, _mainMenuStrings);
            if ((indexChosen >= 0) && (indexChosen < _mainMenuStrings.Length))
            {
                object? returnValue = _mainMenuValues.GetValue(indexChosen);
                if (!(returnValue is null))
                {
                    return (int)returnValue;
                }
            }

            return _exitValue;
        }

        // Run a menu for the given strings.
        // Write out the title, then write out lines listing the number
        // associated with the string, then the string.
        // Collect the response, and if it is one of the valid responses, return
        // it. The response is the index into the menuItems list of the item
        // chosen.
        // If the user input is invalid, try again. If the user enters an invalid
        // response too many times in a row, stop trying and return -1.
        // This will print out choices starting with 1, but will return the index
        // into the array. That is, printed are numbers that are counted
        // beginning with 1, but the indices, including the return value, starts
        // counting at 0.
        public int RunMenu(string title, string[] menuItems)
        {
            if (menuItems is null)
            {
                throw new ArgumentNullException(nameof(menuItems));
            }

            _invalidCount = 0;

            do
            {
                WriteMessage(MessageType.Title, 0, title);
                for (int index = 0; index < menuItems.Length; index++)
                {
                    // When printing out the choices, begin counting at 1.
                    WriteMessage(MessageType.MenuLine, index + 1, menuItems[index]);
                }

                _ = ReadResponse(out int response);
                if ((response > 0) && (response <= menuItems.Length))
                {
                    // When writing the menu, counting begins at 1. So to get the
                    // index of the choice, subtract 1.
                    return response - 1;
                }

                WriteMessage(MessageType.Special, 0, "Invalid response for this menu.");
                _invalidCount++;

            } while (_invalidCount < _maxInvalidCount);

            WriteMessage(MessageType.Special, 0, "Too many invalid responses, exiting menu.");

            return -1;
        }

        // If the messageType is MessageType.Special, this is a special message.
        // Ignore the number and write out the message with a leading new line
        // and three dashes, alonge with trailing three dashes and new line. For
        // example,
        //
        //   ---Message here---
        //
        // If the messageType is MessageType.Title, this is a title. Ignore the
        // number and write out the message with no dashes and no extra new
        // lines. For example,
        //   Message here
        // If the messageType is MessageType.MenuLine, write out the line as
        //    num message".
        // For example,
        //     8 - Message here
        public static void WriteMessage(MessageType messageType, int numberToWrite, string message)
        {
            switch (messageType)
            {
                case MessageType.Special:
                    Console.WriteLine("\n---" + message + "---\n");
                    break;

                case MessageType.Title:
                    Console.WriteLine(message);
                    break;

                default:
                    Console.WriteLine("   " + numberToWrite.ToString("D1", CultureInfo.InvariantCulture) + " - " + message);
                    break;
            }
        }

        // Read the response the user entered. Return the the response converted
        // to an integer. Return the actual string response in the output arg
        // responseString.
        // If the response cannot be converted to an integer, return -1.
        public static int ReadResponse(out string responseString)
        {
            responseString = Console.ReadLine();
            if (int.TryParse(responseString, out int response))
            {
                return response;
            }

            return -1;
        }

        // Read the response the user entered. Return the char array entered.
        // Also, set the out arg to the response converted to an integer.
        // If the response cannot be converted to an integer, set responseAsInt
        // to -1.
        // Read the response the user entered. Return the the response converted
        // to an integer. Return the actual string response in the output arg
        // responseString.
        // If the response cannot be converted to an integer, return -1.
        public static char[] ReadResponse(out int responseAsInt)
        {
            char[] inputData = new char[8];
            int dataLength = 0;

            do
            {
                ConsoleKeyInfo currentKeyInfo = Console.ReadKey();
                if (currentKeyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }

                inputData = AppendChar(currentKeyInfo.KeyChar, inputData, ref dataLength);
            } while (true);

            char[] result = ResizeResult(inputData, dataLength);
            if (int.TryParse(result, out responseAsInt) == false)
            {
                responseAsInt = -1;
            }

            Console.WriteLine("\n");
            return result;
        }

        // Place the currentChar onto the end of inputData. The dataLength arg is
        // how long the data currently is (so it will be the next index). Update
        // this value to the new length.
        // If the buffer is not big enough, create a new buffer with more space,
        // copy the old data, overwrite the old buffer, and add the new char.
        // Return the new buffer.
        // If the buffer is big enough, return it.
        public static char[] AppendChar(char currentChar, char[] inputData, ref int dataLength)
        {
            if (inputData is null)
            {
                throw new ArgumentNullException(nameof(inputData));
            }

            char[] currentBuffer = inputData;
            if (dataLength >= inputData.Length)
            {
                char[] newBuffer = new char[inputData.Length + 8];
                Array.Copy(inputData, newBuffer, inputData.Length);
                Array.Clear(inputData, 0, inputData.Length);
                currentBuffer = newBuffer;
            }

            currentBuffer[dataLength] = currentChar;
            dataLength++;

            return currentBuffer;
        }

        // Return a buffer whose length is dataLength.
        // If inputData.Length is already dataLength, just return inputData.
        // If not, create a new buffer, copy the old, overwrite the old, and
        // return the new.
        public static char[] ResizeResult(char[] inputData, int dataLength)
        {
            if (inputData is null)
            {
                throw new ArgumentNullException(nameof(inputData));
            }

            if (inputData.Length == dataLength)
            {
                return inputData;
            }

            char[] returnValue = new char[dataLength];
            Array.Copy(inputData, returnValue, dataLength);
            Array.Clear(inputData, 0, inputData.Length);

            return returnValue;
        }
    }
}

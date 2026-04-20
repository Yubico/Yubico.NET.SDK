// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Collections.Generic;
using SharpFuzz;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.Core.Fuzz;

public delegate void FuzzTarget(ReadOnlySpan<byte> data);

public static class Program
{
    private const int MaxStackAllocSize = 1024;

    private static readonly Dictionary<string, FuzzTarget> Targets = new()
    {
        ["tlv-reader"] = FuzzTlvReader,
        ["tlv-object"] = FuzzTlvObject,
        ["tlv-decode-list"] = FuzzTlvDecodeList,
        ["base16"] = FuzzBase16,
        ["base32"] = FuzzBase32,
        ["modhex"] = FuzzModHex,
        ["response-apdu"] = FuzzResponseApdu,
    };

    public static void Main(string[] args)
    {
        if (args.Length == 0 || !Targets.ContainsKey(args[0]))
        {
            Console.Error.WriteLine($"Usage: Yubico.Core.Fuzz <target>");
            Console.Error.WriteLine($"Available targets: {string.Join(", ", Targets.Keys)}");
            return;
        }

        string target = args[0];
        FuzzTarget fuzzAction = Targets[target];

        Fuzzer.LibFuzzer.Run(span =>
        {
            fuzzAction(span);
        });
    }

    private static void FuzzTlvReader(ReadOnlySpan<byte> data)
    {
        try
        {
            var reader = new TlvReader(data.ToArray());
            while (reader.HasData)
            {
                int tag = reader.PeekTag();
                _ = reader.ReadValue(tag);
            }
        }
        catch (TlvException) { }
    }

    private static void FuzzTlvObject(ReadOnlySpan<byte> data)
    {
        try
        {
            using var tlvObject = TlvObject.Parse(data);
        }
        catch (TlvException) { }
        catch (ArgumentException) { }
    }

    private static void FuzzTlvDecodeList(ReadOnlySpan<byte> data)
    {
        try
        {
            _ = TlvObjects.DecodeList(data);
        }
        catch (TlvException) { }
        catch (ArgumentException) { }
    }

    private static void FuzzBase16(ReadOnlySpan<byte> data)
    {
        try
        {
            int len = data.Length;
            if (len == 0)
            {
                return;
            }

            Span<char> chars = len <= MaxStackAllocSize ? stackalloc char[len] : new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = (char)data[i];
            }

            Span<byte> output = len <= MaxStackAllocSize ? stackalloc byte[len] : new byte[len];
            Base16.DecodeText(chars, output);
        }
        catch (ArgumentException) { }
        catch (FormatException) { }
    }

    private static void FuzzBase32(ReadOnlySpan<byte> data)
    {
        try
        {
            int len = data.Length;
            if (len == 0)
            {
                return;
            }

            Span<char> chars = len <= MaxStackAllocSize ? stackalloc char[len] : new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = (char)data[i];
            }

            int decodedSize = Base32.GetDecodedSize(chars);
            var decoder = new Base32();
            Span<byte> output = decodedSize <= MaxStackAllocSize ? stackalloc byte[decodedSize] : new byte[decodedSize];
            decoder.Decode(chars, output);
        }
        catch (ArgumentException) { }
        catch (FormatException) { }
    }

    private static void FuzzModHex(ReadOnlySpan<byte> data)
    {
        try
        {
            int len = data.Length;
            if (len == 0)
            {
                return;
            }

            Span<char> chars = len <= MaxStackAllocSize ? stackalloc char[len] : new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = (char)data[i];
            }

            Span<byte> output = len <= MaxStackAllocSize ? stackalloc byte[len] : new byte[len];
            ModHex.DecodeText(chars, output);
        }
        catch (ArgumentException) { }
        catch (FormatException) { }
    }

    private static void FuzzResponseApdu(ReadOnlySpan<byte> data)
    {
        try
        {
            if (data.Length < 2)
            {
                return;
            }

            var apdu = new ResponseApdu(data.ToArray());
            _ = apdu.SW;
            _ = apdu.SW1;
            _ = apdu.SW2;
            _ = apdu.Data;
        }
        catch (ArgumentException) { }
    }
}

global using Coordinates = (double Latitude, double Longitude);

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable LocalizableElement

namespace Yubico.YubiKey;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public abstract class CSharpLangFeaturesExamples
{
    public static async Task Main()
    {
        InitExample();
        WithKeywordExample();
        _ = ExceptionNUllThrowExample1(null);
        _ = ExceptionNotNullThrowExample2("a string");
        RawStringLiteralExample();
        SequencePatternMatchingExample();

        // C12 Features
        // PrimaryConstructorExample();
        AnyTypeUsingExample();
        CollectionInitExample();
        SpreadElementExample();

        await AsyncEnumerableExample().ConfigureAwait(false);

        ByteRepresentation.Main();
        Console.WriteLine("Finished");
    }

    private static void AnyTypeUsingExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        _ = new Coordinates(item1: 1.0, item2: 2.0);
    }

    [SuppressMessage("ReSharper", "UnusedVariable")]
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value")]
    private static void CollectionInitExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        int[] numbers1 = new int[3] { 1, 2, 3 };
        int[] numbers2 = { 1, 2, 3 };

        int[] numbers3 = [1, 2, 3]; // New
        int[] emptyCollection = []; // New
    }

    private static void SpreadElementExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        int[] oneTwoThree = [1, 2, 3];
        int[] fourFiveSix = [4, 5, 6];

        int[] all = [.. fourFiveSix, 100, .. oneTwoThree];

        Console.WriteLine(string.Join(", ", all));
        Console.WriteLine($"Length: {all.Length}");

        // Outputs:
        //   4, 5, 6, 100, 1, 2, 3
        //   Length: 7
    }

    private static void PrimaryConstructorExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        var p = new PrimaryConstructor("myName");
        p.PrintPrivateMember();
    }

    private static void SequencePatternMatchingExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        Console.WriteLine("-----------------------------------------");
        int[] numbers = { 1, 2, 3 };
        Console.WriteLine(numbers is [1, 2, 3]); // True
        Console.WriteLine(numbers is [1, 2, 4]); // False
        Console.WriteLine(numbers is [1, 2, 3, 4]); // False
        Console.WriteLine(numbers is [0 or 1, <= 2, >= 3]); // True

        if (numbers is [var first, _, _])
        {
            Console.WriteLine($"The first element of a three-item list is {first}.");
        }

        // Output:
        // The first element of a three-item list is 1.
    }

    private static void RawStringLiteralExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        Console.WriteLine("-----------------------------------------");

        string longMessage = """
                             This is a long message.
                             It has several lines.
                                 Some are indented
                                         more than others.
                             Some should start at the first column.
                             Some have "quoted text" in them.
                             """;

        Console.WriteLine(longMessage);
    }

    private static string? ExceptionNUllThrowExample1(string? isNull)
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        Console.WriteLine("-----------------------------------------");

        string result = "";
        try
        {
            result = isNull ?? throw new NotImplementedException();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Console.WriteLine(result);
        return result;
    }

    private static string? ExceptionNotNullThrowExample2(string? isNull)
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);
        Console.WriteLine("-----------------------------------------");

        string result = "";
        try
        {
            result = isNull is not null
                ? isNull
                : throw new NotImplementedException();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Console.WriteLine(result);
        return result;
    }

    public static async Task AsyncEnumerableExample()
    {
        Console.WriteLine("AsyncEnumerableExample");

        await StartRaceAsync().ConfigureAwait(false);
    }

    public static void InitExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);

        // Using C# 9.0 feature: init-only setters
        var person = new Person { Name = "John", Age = 30 };
        Console.WriteLine($"Person: {person.Name}, {person.Age}");
    }

    public static void WithKeywordExample()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod().Name);

        var p1 = new NamedPoint("A", X: 0, Y: 0);
        Console.WriteLine($"{nameof(p1)}: {p1}"); // output: p1: NamedPoint { Name = A, X = 0, Y = 0 }

        NamedPoint p2 = p1 with { Name = "B", X = 5 };
        Console.WriteLine($"{nameof(p2)}: {p2}"); // output: p2: NamedPoint { Name = B, X = 5, Y = 0 }

        NamedPoint p3 = p1 with
        {
            Name = "C",
            Y = 4
        };

        Console.WriteLine($"{nameof(p3)}: {p3}"); // output: p3: NamedPoint { Name = C, X = 0, Y = 4 }

        Console.WriteLine($"{nameof(p1)}: {p1}"); // output: p1: NamedPoint { Name = A, X = 0, Y = 0 }

        var apples = new { Item = "Apples", Price = 1.19m };
        Console.WriteLine($"Original: {apples}"); // output: Original: { Item = Apples, Price = 1.19 }
        var saleApples = apples with { Price = 0.79m };
        Console.WriteLine($"Sale: {saleApples}"); // output: Sale: { Item = Apples, Price = 0.79 }
    }

    private static async Task StartRaceAsync()
    {
        await foreach (int i in CountDownAsync())
        {
            if (i > 0)
            {
                Console.WriteLine(i);
            }
            else
            {
                Console.WriteLine("GO!");
            }
        }
    }

    private static async IAsyncEnumerable<int> CountDownAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(100).ConfigureAwait(false);
            yield return i;
        }
    }
}

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
internal static class ByteRepresentation
{
    public static void Main()
    {
        for (int i = 0; i < 10; i++)
        {
            char digit = (char)('0' + i);

            byte asciiBytes = (byte)digit;
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(new[] { digit });
            byte[] utf16Bytes = Encoding.Unicode.GetBytes(new[] { digit });

            Console.WriteLine($"Digit: {digit}");
            Console.WriteLine($"ASCII:  0x{asciiBytes:X2}");
            Console.WriteLine(
                $"UTF-8:  0x{BitConverter.ToString(utf8Bytes).Replace(oldChar: '-', newChar: ' ')}");

            Console.WriteLine(
                $"UTF-16: 0x{BitConverter.ToString(utf16Bytes).Replace(oldChar: '-', newChar: ' ')}");

            Console.WriteLine();

            EncodingPin();
        }
    }

    public static void EncodingPin()
    {
        Console.Write("EncodingPin:");
        byte[] bytes = Encoding.UTF8.GetBytes("01234567");
        Console.WriteLine($"{BitConverter.ToString(bytes).Replace(oldChar: '-', newChar: ' ')}");
    }
}

internal class Person
{
    public required string Name { get; init; }
    public int Age { get; init; }
}

internal record struct RecordStruct
{
    public int A { get; set; }
}

internal record struct RecordStruct2(int A, int B, bool C)
{
}

internal struct Parent
{
}

internal class PrimaryConstructor(string name)
{
    public void PrintPrivateMember() => Console.WriteLine(name);
}

public record NamedPoint(string Name, int X, int Y);

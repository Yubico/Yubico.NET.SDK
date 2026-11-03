using System;
using System.Threading.Tasks;

namespace ManagementCheckerDiagnostics;

// Compiler-only calibration, not a documentation replacement or an executable test.
public static class AsyncWarningCalibration
{
    public static async Task NoAwaitMethod()
    {
        Console.WriteLine("Calibration source is compiled, never executed.");
    }

    public static Func<Task> NoAwaitLambda() => async () =>
    {
        Console.WriteLine("Calibration source is compiled, never executed.");
    };
}

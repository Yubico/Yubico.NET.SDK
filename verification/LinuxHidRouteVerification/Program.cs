using LinuxHidRouteVerification;
using System.Diagnostics;
using System.Globalization;

Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
if (!OperatingSystem.IsLinux())
{
    Console.Error.WriteLine("BLOCKED: Linux is required");
    return 2;
}

bool child = args.FirstOrDefault() == "--child";
string[] options = child ? args[1..] : args;
string? output = null;
if (options.Length >= 3 && options[^2] == "--output")
{
    output = options[^1];
    options = options[..^2];
}
if (options is not ["--native"] and not ["--list"] and not ["--sensitivity-blocking"]
    and not ["--sensitivity-quiescence"]
    and not ["--fido" or "--otp" or "--raw" or "--expert-io" or "--expert-feature" or "--smartcard" or "--touch" or "--removal" or "--active-cancel", "--serial", _])
{
    Console.Error.WriteLine("Usage: LinuxHidRouteVerification --native | --list | --sensitivity-blocking | --sensitivity-quiescence | (--fido|--otp|--raw|--expert-io|--expert-feature|--smartcard|--touch|--removal|--active-cancel) --serial POSITIVE_SERIAL [--output FILE]");
    return 2;
}
int serial = 0;
if (options.Length == 3 && (!int.TryParse(options[2], NumberStyles.None, CultureInfo.InvariantCulture, out serial) || serial <= 0))
    return 2;

if (!child)
{
    using var process = new Process();
    process.StartInfo = new(Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable path"))
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    process.StartInfo.ArgumentList.Add("--child");
    foreach (string arg in args) process.StartInfo.ArgumentList.Add(arg);
    _ = process.Start();
    Task stdout = process.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardOutput());
    Task stderr = process.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError());
    try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(options[0] == "--removal" ? 200 : 60)); }
    catch (TimeoutException)
    {
        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        await Task.WhenAll(stdout, stderr);
        Console.Error.WriteLine("FAIL watchdog expired: own-child kill is OS cleanup, NOT native drain proof");
        return 3;
    }
    await Task.WhenAll(stdout, stderr);
    return process.ExitCode;
}

var evidence = new Evidence(options[0]);
int result;
try
{
    result = options[0] switch
    {
        "--native" => await NativeScenarios.RunAsync(evidence),
        "--sensitivity-blocking" => await NativeScenarios.BlockingControlAsync(evidence),
        "--sensitivity-quiescence" => await NativeScenarios.QuiescenceControlAsync(evidence),
        "--smartcard" => await SmartCardScenario.RunAsync(serial),
        _ => await HardwareScenarios.RunAsync(options[0], serial, evidence)
    };
    if (result == 0 && options[0] == "--smartcard") evidence.Check(true, "pcsc-three-open-transaction-select-info-end-dispose-cycles");
    if (result == 0 && evidence.Checks.Count == 0) throw new InvalidOperationException("Zero named checks is not success");
    evidence.Outcome = result switch { 0 => "PASSED", 2 => "BLOCKED", _ => "FAILED" };
}
catch (Exception error)
{
    evidence.Failure = error.ToString();
    Console.Error.WriteLine($"FAIL {error}");
    result = 1;
}
evidence.Save(output);
Console.WriteLine($"RESULT: {evidence.Outcome}; checks={evidence.Checks.Count}; exit={result}");
return result;
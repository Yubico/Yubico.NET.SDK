// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.YubiOtp.Examples.OtpTool;
using Yubico.YubiKit.YubiOtp.Examples.OtpTool.Commands;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var options = CliOptions.Parse(args);

    YubiKeyManager.StartMonitoring();

    int exitCode = await RunCommandAsync(options, cts.Token);

    await YubiKeyManager.ShutdownAsync();
    return exitCode;
}
catch (ArgumentException ex)
{
    OutputHelper.WriteError(ex.Message);
    return 1;
}
catch (InvalidOperationException ex)
{
    OutputHelper.WriteError(ex.Message);
    return 1;
}
catch (OperationCanceledException)
{
    return 0;
}

static Task<int> RunCommandAsync(CliOptions options, CancellationToken ct) =>
    options.Command switch
    {
        "info" => InfoCommand.RunAsync(options, ct),
        "swap" => SwapCommand.RunAsync(options, ct),
        "delete" => DeleteCommand.RunAsync(options, ct),
        "chalresp" => ChalRespCommand.RunAsync(options, ct),
        "hotp" => HotpCommand.RunAsync(options, ct),
        "static" => StaticCommand.RunAsync(options, ct),
        "yubiotp" => YubiOtpCommand.RunAsync(options, ct),
        "calculate" => CalculateCommand.RunAsync(options, ct),
        "ndef" => NdefCommand.RunAsync(options, ct),
        "settings" => SettingsCommand.RunAsync(options, ct),
        _ => throw new ArgumentException(
            $"Unknown command: {options.Command}. Run 'OtpTool --help' for usage.")
    };
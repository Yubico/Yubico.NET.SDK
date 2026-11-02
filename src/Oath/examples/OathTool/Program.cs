// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Oath.Examples.OathTool;

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};
Console.CancelKeyPress += cancelHandler;
try
{
    return await OathToolApplication.RunAsync(args, OathToolApplication.ExecuteAsync, cancellation.Token);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
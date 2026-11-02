// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Oath.Examples.OathTool;

namespace Yubico.YubiKit.Oath.UnitTests;

public class OathToolDispatchTests
{
    [Theory]
    [InlineData("reset --help --force")]
    [InlineData("RESET --HELP --force")]
    [InlineData("reset --force -h")]
    [InlineData("accounts help")]
    [InlineData("access help")]
    [InlineData("--help reset --force")]
    [InlineData("help reset --force")]
    [InlineData("info --help")]
    [InlineData("access change --help --clear")]
    [InlineData("accounts delete --force --help name")]
    [InlineData("accounts add --unknown --help")]
    public async Task Help_NeverInvokesBootstrapOrMutation(string commandLine)
    {
        int calls = 0;
        int exitCode = await OathToolApplication.RunAsync(commandLine.Split(' '), (_, _) =>
        {
            calls++;
            return Task.FromResult(99);
        }, TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("accounts add --touch name SECRET --issuer issuer --force", "name,SECRET")]
    [InlineData("accounts add name --force SECRET --touch", "name,SECRET")]
    [InlineData("accounts add name SECRET --touch --force", "name,SECRET")]
    [InlineData("accounts delete --force name --password password", "name")]
    [InlineData("accounts rename --password password old --force new", "old,new")]
    [InlineData("accounts rename old --force new --password password", "old,new")]
    [InlineData("accounts uri --touch otpauth://totp/name?secret=MY --force", "otpauth://totp/name?secret=MY")]
    public async Task Options_ConsumeOnlyTheirDeclaredValues(string commandLine, string expected)
    {
        OathInvocation? invocation = null;
        int exitCode = await OathToolApplication.RunAsync(commandLine.Split(' '), (parsed, _) =>
        {
            invocation = parsed;
            return Task.FromResult(0);
        }, TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.NotNull(invocation);
        Assert.Equal(expected, string.Join(',', invocation.Positionals));
    }

    [Theory]
    [InlineData("reset --unknown --force")]
    [InlineData("reset help -f")]
    [InlineData("reset --password password")]
    [InlineData("accounts delete --password")]
    [InlineData("accounts delete --password --force name")]
    [InlineData("accounts rename old new extra --force")]
    [InlineData("accounts add name SECRET --digits nope")]
    [InlineData("accounts add name SECRET --period 999999999999999")]
    [InlineData("accounts add name SECRET --algorithm bogus")]
    [InlineData("accounts add name SECRET --issuer first --issuer second")]
    public async Task MalformedOptions_DoNotEnterBootstrap(string commandLine)
    {
        int calls = 0;
        int exitCode = await OathToolApplication.RunAsync(commandLine.Split(' '), (_, _) =>
        {
            calls++;
            return Task.FromResult(99);
        }, TestContext.Current.CancellationToken);

        Assert.Equal(1, exitCode);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ValueOptions_PreserveTheirValuesAndBooleanFlags()
    {
        OathInvocation? invocation = null;
        int exitCode = await OathToolApplication.RunAsync(
            ["accounts", "add", "--issuer", "issuer", "name", "--touch", "SECRET", "--digits", "8", "--force", "-p", "password"],
            (parsed, _) => { invocation = parsed; return Task.FromResult(0); }, TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.NotNull(invocation);
        Assert.Equal(["name", "SECRET"], invocation.Positionals);
        Assert.Equal("issuer", invocation.Value("--issuer"));
        Assert.Equal("password", invocation.Value("--password"));
        Assert.Equal(8, invocation.Number("--digits", 6));
        Assert.True(invocation.Flag("--touch"));
        Assert.True(invocation.Flag("--force"));
    }

    [Fact]
    public async Task EndOfOptions_AllowsDashPrefixedPositional()
    {
        OathInvocation? invocation = null;
        await OathToolApplication.RunAsync(["accounts", "delete", "--force", "--", "-name"],
            (parsed, _) => { invocation = parsed; return Task.FromResult(0); }, TestContext.Current.CancellationToken);
        Assert.NotNull(invocation);
        Assert.Equal(["-name"], invocation.Positionals);
        Assert.True(invocation.Flag("--force"));
    }

    [Theory]
    [InlineData("access change --password=MixedCaseSecret", "MixedCaseSecret")]
    [InlineData("access change --new-password=OtherSecret", "OtherSecret")]
    [InlineData("access change --password first --password=DuplicateSecret", "DuplicateSecret")]
    [InlineData("UnclassifiedCommandSecret", "UnclassifiedCommandSecret")]
    public async Task InvalidInput_DoesNotDiscloseUnclassifiedTokens(string commandLine, string secret)
    {
        using var output = new StringWriter();
        int calls = 0;
        int exitCode = await OathToolApplication.RunAsync(commandLine.Split(' '), (_, _) =>
        {
            calls++;
            return Task.FromResult(0);
        }, TestContext.Current.CancellationToken, output);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(0, calls);
        Assert.DoesNotContain(secret, output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("accounts code help", "help", null, null, null)]
    [InlineData("ACCOUNTS CODE --PASSWORD MiXeD help", "help", "MiXeD", null, null)]
    [InlineData("accounts delete help --force", "help", null, null, null)]
    [InlineData("accounts rename old help --force", "old,help", null, null, null)]
    [InlineData("accounts add help SECRET --force", "help,SECRET", null, null, null)]
    [InlineData("accounts code --password help query", "query", "help", null, null)]
    [InlineData("accounts add name SECRET --issuer help", "name,SECRET", null, "help", null)]
    [InlineData("access change --password help --new-password help", "", "help", null, "help")]
    [InlineData("accounts code -- --help", "--help", null, null, null)]
    [InlineData("accounts code -- -h", "-h", null, null, null)]
    [InlineData("accounts code -- help", "help", null, null, null)]
    public async Task HelpLikeData_ReachesCallbackUnchanged(string commandLine, string positionals,
        string? password, string? issuer, string? newPassword)
    {
        OathInvocation? invocation = null;
        int exitCode = await OathToolApplication.RunAsync(commandLine.Split(' '), (parsed, _) =>
        {
            invocation = parsed;
            return Task.FromResult(0);
        }, TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.NotNull(invocation);
        Assert.Equal(positionals, string.Join(',', invocation.Positionals));
        Assert.Equal(password, invocation.Value("--password"));
        Assert.Equal(issuer, invocation.Value("--issuer"));
        Assert.Equal(newPassword, invocation.Value("--new-password"));
    }

    [Theory]
    [InlineData("accounts code --password help --help")]
    [InlineData("accounts add --issuer help --unknown -h")]
    [InlineData("accounts code --password --help")]
    [InlineData("accounts add --issuer --help --force")]
    public async Task GenuineHelp_AfterValuesOrMalformedOptions_DoesNotBootstrap(string commandLine)
    {
        int calls = 0;
        int exitCode = await OathToolApplication.RunAsync(commandLine.Split(' '), (_, _) =>
        {
            calls++;
            return Task.FromResult(99);
        }, TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task RawAdd_SevenDigitsReachesCallback()
    {
        OathInvocation? invocation = null;
        int exitCode = await OathToolApplication.RunAsync(["accounts", "add", "name", "SECRET", "--digits", "7"],
            (parsed, _) => { invocation = parsed; return Task.FromResult(0); }, TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.NotNull(invocation);
        Assert.Equal(7, invocation.Number("--digits", 6));
    }
}
// ConsoleSpinner.cs
using System;
using System.Threading;

public class ConsoleSpinner : IDisposable
{
    private readonly Thread _spinnerThread;
    private bool _isActive;

    public ConsoleSpinner()
    {
        _spinnerThread = new Thread(Spin);
        _isActive = false;
    }

    public void Start()
    {
        _isActive = true;
        if (!_spinnerThread.IsAlive)
        {
            _spinnerThread.Start();
        }
    }

    public void Stop()
    {
        _isActive = false;
        Console.Write("\b \b"); // Erase the spinner character
    }

    private void Spin()
    {
        char[] spinner = { '|', '/', '-', '\\' };
        int counter = 0;
        while (_isActive)
        {
            Console.Write(spinner[counter % spinner.Length]);
            Thread.Sleep(150);
            Console.Write("\b \b");
            counter++;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
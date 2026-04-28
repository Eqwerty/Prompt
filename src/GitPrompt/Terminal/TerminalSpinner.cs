namespace GitPrompt.Terminal;

internal sealed class TerminalSpinner : IDisposable
{
    private readonly string _message;
    private readonly bool _interactive;
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _thread;
    private bool _completed;

    private TerminalSpinner(string message)
    {
        _message = message;
        _interactive = !Console.IsOutputRedirected;

        if (_interactive)
        {
            Console.Write($"{AnsiTerminal.HideCursor}{AnsiTerminal.Yellow}●{AnsiTerminal.Reset} {message}{".",-3}");
        }

        _thread = new Thread(SpinLoop) { IsBackground = true };
        _thread.Start();
    }

    internal static TerminalSpinner Start(string message) => new(message);

    internal void Complete()
    {
        _completed = true;
        _cts.Cancel();
        _thread.Join();

        if (_interactive)
        {
            Console.WriteLine($"\r{AnsiTerminal.ShowCursor}{AnsiTerminal.Green}✓{AnsiTerminal.Reset} {_message}...");
        }
        else
        {
            Console.WriteLine($"✓ {_message}...");
        }
    }

    public void Dispose()
    {
        if (!_completed)
        {
            _cts.Cancel();
            _thread.Join();

            if (_interactive)
            {
                Console.WriteLine(AnsiTerminal.ShowCursor);
            }
        }

        _cts.Dispose();
    }

    private void SpinLoop()
    {
        if (!_interactive)
        {
            return;
        }

        var i = 0;
        while (!_cts.IsCancellationRequested)
        {
            _cts.Token.WaitHandle.WaitOne(500);
            if (_cts.IsCancellationRequested)
            {
                break;
            }

            i++;
            var dots = (i % 3) switch
            {
                1 => "..",
                2 => "...",
                _ => "."
            };

            Console.Write($"\r{AnsiTerminal.Yellow}●{AnsiTerminal.Reset} {_message}{dots,-3}");
        }
    }
}

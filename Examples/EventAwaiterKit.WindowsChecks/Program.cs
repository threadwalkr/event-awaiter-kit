using EventAwaiterKit;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        // A real WinForms message loop, with a hidden marshaling control and no visible window.
        using var dispatcher = new Control();
        _ = dispatcher.Handle;
        using var loop = new ApplicationContext();
        using var context = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);
        var exitCode = 1;
        using var watchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine("Windows context checks exceeded 15 seconds.");
            Environment.Exit(1);
        }, null, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);
        dispatcher.BeginInvoke((Action)(async () =>
        {
            try
            {
                for (var outcome = 0; outcome < 3; outcome++)
                    await CheckAsync(outcome);
                await CheckControlEventAsync();
                Console.WriteLine("Windows message-loop checks passed: background event, timeout, cancellation, control event.");
                exitCode = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { loop.ExitThread(); }
        }));
        Application.Run(loop);
        return exitCode;
    }

    private static async Task CheckAsync(int outcome)
    {
        if (SynchronizationContext.Current is not WindowsFormsSynchronizationContext)
            throw new InvalidOperationException("Expected a real Windows Forms synchronization context.");
        var ownerThread = Environment.CurrentManagedThreadId;
        Action? callback = null;
        var removes = 0;
        using var cts = new CancellationTokenSource();
        var wait = EventAwaiter.WaitForEventAsync(h => callback = h, _ =>
        {
            if (Environment.CurrentManagedThreadId != ownerThread)
                throw new InvalidOperationException("Cleanup ran outside the UI thread.");
            removes++;
        }, outcome == 1 ? TimeSpan.FromMilliseconds(20) : Timeout.InfiniteTimeSpan, cts.Token);

        if (outcome == 0) await Task.Run(() => callback!());
        if (outcome == 2) await Task.Run(cts.Cancel);
        try
        {
            var occurred = await wait;
            if (outcome == 2 || occurred != (outcome == 0))
                throw new InvalidOperationException("Unexpected wait outcome.");
        }
        catch (OperationCanceledException ex) when (outcome == 2 && wait.IsCanceled && ex.CancellationToken == cts.Token) { }
        if (removes != 1)
            throw new InvalidOperationException("Expected one completed UI cleanup.");
    }

    private static async Task CheckControlEventAsync()
    {
        using var control = new Control();
        var ownerThread = Environment.CurrentManagedThreadId;
        var removes = 0;
        var wait = EventAwaiter.WaitForEventAsync(h => control.TextChanged += h, h =>
        {
            if (Environment.CurrentManagedThreadId != ownerThread)
                throw new InvalidOperationException("Control event cleanup ran outside the UI thread.");
            control.TextChanged -= h;
            removes++;
        }, TimeSpan.FromSeconds(5));
        control.Text = "ready";
        if (!await wait || removes != 1)
            throw new InvalidOperationException("Expected a completed control event wait with one removal.");
    }
}

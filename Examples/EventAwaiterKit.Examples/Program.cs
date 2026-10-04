using EventAwaiterKit;

await SynchronousCallbackAsync();
await CompetingEventsAsync();
await WatchFileAsync();
Console.WriteLine("All examples passed.");

static async Task SynchronousCallbackAsync()
{
    var sensor = new ExampleSensor();
    using var stopWaiting = new CancellationTokenSource();
    var wait = EventAwaiter.WaitForEventAsync<int>(
        h => sensor.Measured += h, h => sensor.Measured -= h,
        TimeSpan.FromSeconds(5), stopWaiting.Token);
    try
    {
        sensor.Measure(); // May raise the event before returning.
        var result = await wait;
        if (!result.Occurred || result.Value != 42)
            throw new InvalidOperationException("Expected the synchronous measurement.");
        Console.WriteLine($"Synchronous callback: {result.Value}");
    }
    finally
    {
        // Also runs if starting the underlying operation throws.
        stopWaiting.Cancel();
        try { await wait; }
        catch (OperationCanceledException) when (wait.IsCanceled) { }
    }
}

static async Task CompetingEventsAsync()
{
    var first = new ExampleSensor();
    var second = new ExampleSensor();
    using var stopWaiting = new CancellationTokenSource();
    var firstWait = EventAwaiter.WaitForEventAsync<int>(h => first.Measured += h, h => first.Measured -= h, stopWaiting.Token);
    var secondWait = EventAwaiter.WaitForEventAsync<int>(h => second.Measured += h, h => second.Measured -= h, stopWaiting.Token);
    var waits = new[] { firstWait, secondWait };
    try
    {
        first.Measure();
        var winner = await Task.WhenAny(waits);
        var result = await winner;
        if (!result.Occurred || result.Value != 42)
            throw new InvalidOperationException("Expected the first sensor.");
    }
    finally
    {
        stopWaiting.Cancel();
        // WhenAll observes both tasks, even if one faults during cleanup.
        var completion = Task.WhenAll(waits);
        try { await completion; }
        catch (OperationCanceledException) when (completion.IsCanceled) { }
    }
    if (first.HasSubscribers || second.HasSubscribers)
        throw new InvalidOperationException("A competing wait retained its subscription.");
    Console.WriteLine("Competing events: losing wait canceled and observed.");
}

static async Task WatchFileAsync()
{
    var directory = Path.Combine(Path.GetTempPath(), "event-awaiter-example-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var path = Path.Combine(directory, "signal.txt");
        using var watcher = new FileSystemWatcher(directory)
        {
            Filter = "signal.txt",
            NotifyFilter = NotifyFilters.FileName
        };
        using var stopWaiting = new CancellationTokenSource();
        FileSystemEventHandler? adapter = null;
        // FileSystemEventHandler is a distinct delegate type; retain the exact adapter for removal.
        var wait = EventAwaiter.WaitForEventAsync<FileSystemEventArgs>(
            (EventHandler<FileSystemEventArgs> h) =>
            {
                adapter = (sender, args) => h(sender, args);
                watcher.Created += adapter;
            },
            _ => watcher.Created -= adapter,
            TimeSpan.FromSeconds(10), stopWaiting.Token);
        try
        {
            watcher.EnableRaisingEvents = true;
            await File.WriteAllTextAsync(path, "ready");
            var result = await wait;
            if (!result.Occurred || result.Value.FullPath != path)
                throw new InvalidOperationException("Expected the file-created event.");
            Console.WriteLine($"File watcher: {result.Value.Name}");
        }
        finally
        {
            stopWaiting.Cancel();
            try { await wait; }
            catch (OperationCanceledException) when (wait.IsCanceled) { }
        }
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

internal sealed class ExampleSensor
{
    internal event Action<int>? Measured;
    internal bool HasSubscribers => Measured != null;
    internal void Measure() => Measured?.Invoke(42);
}

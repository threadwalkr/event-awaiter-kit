# EventAwaiterKit

A small, dependency-free .NET Standard 2.0 library for awaiting one future event, with timeout, cancellation, and cleanup.

Use it through a project reference:

```sh
dotnet add YourApp.csproj reference path/to/EventAwaiterKit/EventAwaiterKit.csproj
```

## Supported events

| Event shape | Result |
| --- | --- |
| `Action` | `Task<bool>` |
| `EventHandler` | `Task<bool>`; sender and arguments are discarded |
| `Action<T>` | `Task<EventWaitResult<T>>` |
| `EventHandler<TEventArgs>` | `Task<EventWaitResult<TEventArgs>>`; sender is discarded |

Each shape has an overload with `TimeSpan timeout` and one without a timeout, both of which accept an optional `CancellationToken`. Specify the type argument when subscribing to a typed event, as in `WaitForEventAsync<int>(...)`.

Untyped waits return `true` for an event and `false` for timeout. Typed results use `Occurred` to distinguish a timeout from an event carrying `null`, `0`, or another default value; `Value` throws `InvalidOperationException` after a timeout, including for a default-constructed result.

If cancellation wins and cleanup succeeds, awaiting the canceled task throws `OperationCanceledException`; the sensor, motor, or other operation that produces the event continues unless you stop it separately.

## Subscribe before starting work

`WaitForEventAsync` subscribes synchronously, so store the task before starting the operation and then await it. This ordering captures a completion event even when the operation raises it before its start method returns.

This excerpt is implemented in the [executable examples](Examples/EventAwaiterKit.Examples/Program.cs):

```csharp
using var stopWaiting = new CancellationTokenSource();
var wait = EventAwaiter.WaitForEventAsync<int>(
    h => sensor.Measured += h,
    h => sensor.Measured -= h,
    TimeSpan.FromSeconds(5),
    stopWaiting.Token);

try
{
    sensor.Measure();
    var result = await wait;
    if (result.Occurred)
        Console.WriteLine(result.Value);
}
finally
{
    // If starting the operation throws, detach the abandoned wait too.
    stopWaiting.Cancel();
    try { await wait; }
    catch (OperationCanceledException) when (wait.IsCanceled) { }
}
```

In this `finally` pattern, a cleanup failure can supersede an exception from starting the operation. Collect and report both exceptions explicitly if the application needs them.

For an existing non-generic event:

```csharp
bool occurred = await EventAwaiter.WaitForEventAsync(
    h => source.Completed += h,
    h => source.Completed -= h,
    TimeSpan.FromSeconds(5),
    cancellationToken);
```

This waits for a future event; it does not replay earlier events or check whether the underlying operation already finished.

## Racing or abandoning waits

`Task.WhenAny` leaves the other waits running. Give related waits a cancellation token, cancel it in `finally`, and await `Task.WhenAll` so every task is observed and cleanup finishes; suppress cancellation only when the combined task is actually canceled, leaving cleanup failures visible.

The executable examples use two event sources and check that both subscriptions were removed. Cancellation is especially important for an abandoned wait without a timeout.

## Custom delegates and file watching

The API accepts the four delegate shapes above; events with another delegate type need an adapter. For example, `FileSystemEventHandler` has the same signature as `EventHandler<FileSystemEventArgs>` but is a distinct type.

The [file-watcher example](Examples/EventAwaiterKit.Examples/Program.cs) retains one adapter instance while subscribing to `Created`, then enables the watcher and creates a temporary file. It checks the path reported by the filesystem event before disposing the watcher and removing the temporary directory.

## Cleanup, exceptions, and UI callers

- Event, timeout, and cancellation compete for one outcome; later signals do nothing.
- Cleanup waits for the add accessor to return or throw. Removal is attempted once for an attempted subscription, including after a failed add, so it must tolerate an absent handler.
- The returned task completes after cleanup finishes or fails, with accessor exceptions reported through that task rather than completion callbacks.
- Addition failure overrides an event signal, while cleanup failure overrides event, timeout, or cancellation. If both accessors fail, the task faults with an `AggregateException` containing both.
- When a `SynchronizationContext` is present, cleanup uses it. Call from the source's owning context, keep its message pump running, and use `await` rather than `.Wait()` or `.Result` on that thread.
- A failing remover or unavailable context can prevent detachment, so automatic cleanup cannot guarantee removal when the source refuses it.

Timeout accepts exactly `Timeout.InfiniteTimeSpan`, or zero through `Int32.MaxValue` milliseconds; other negative values are invalid. Zero returns timeout without subscribing unless the token is already canceled, in which case cancellation wins. Accessors must return promptly because a timeout cannot interrupt them.

Read the full [lifecycle contract](LIFECYCLE.md) for ordering, exception precedence, and context limitations. Public API XML documentation is generated beside the library assembly for editor help.

### Why supply add/remove operations instead of a delegate?

The helper creates a handler and needs to attach and detach that exact instance. A delegate alone does not identify the event accessors, so the two lambdas supply those operations for both ordinary events and custom callback registration APIs.

## Build, tests, and examples

Install the .NET 10 SDK, then run:

```sh
dotnet build EventAwaiterKit.sln -c Release
dotnet test EventAwaiterKit.sln -c Release
dotnet run --project Examples/EventAwaiterKit.Examples -c Release
```

On Windows, also run:

```sh
dotnet run --project Examples/EventAwaiterKit.WindowsChecks -c Release
```

The Windows check uses hidden controls and a real WinForms message loop to verify UI-thread cleanup after a background event, timeout, background cancellation, or control event. It does not open a visible window or check visual behavior, and its project sits outside the portable solution.

## Compatibility and scope

The library targets .NET Standard 2.0 without adding runtime dependencies, while the tests and executable examples use .NET 10. Windows Forms checks are Windows-specific, and the repository currently does not test against other .NET Standard 2.0 runtimes such as .NET Framework.

The library handles one event per call. Event streams, predicates, sender capture, device control, and automatic marshaling to an unknown source owner are outside its scope.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for checks and regression-test expectations, and keep the examples and documentation aligned with the code.

## Using and releasing

If you want to use EventAwaiterKit in your app, add a project reference to `EventAwaiterKit/EventAwaiterKit.csproj` or reference the built assembly. The version number is in `EventAwaiterKit/EventAwaiterKit.csproj`.

Before tagging a release, run the checks in [CONTRIBUTING.md](CONTRIBUTING.md), update the version number, and make sure the documentation and examples match the code.

## License

[MIT](LICENSE).

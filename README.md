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

Every shape has an overload with `TimeSpan timeout` and an overload without a timeout. All accept an optional `CancellationToken`. Specify the type argument for typed event subscriptions, for example `WaitForEventAsync<int>(...)`.

Untyped waits return `true` for an event and `false` for timeout. Typed results have `Occurred` and `Value`: `Occurred` can be true even when the payload is `null` or `0`. Reading `Value` after timeout throws `InvalidOperationException`. A default-constructed result represents timeout.

Cancellation produces a canceled task and throws `OperationCanceledException` when awaited, provided cleanup succeeds. Cancellation stops waiting; it does not stop a sensor, motor, or other underlying operation.

## Subscribe before starting work

Calling `WaitForEventAsync` subscribes synchronously. Store the task, start the operation, then await. This also handles an operation that raises its completion event before its start method returns.

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

A cleanup failure remains observable and can supersede an exception from starting the operation in this simple `finally` pattern. If an application needs both failures, collect and report both explicitly.

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

`Task.WhenAny` does not stop the losing waits. Give related waits a cancellation token, cancel it in `finally`, and await `Task.WhenAll` to observe every task and finish cleanup. Suppress cancellation only when that combined task is actually canceled; cleanup failures must remain visible.

The executable examples demonstrate this pattern with two event sources, including checking that both subscriptions were removed. Canceling an abandoned wait matters especially when it has no timeout.

## Custom delegates and file watching

The API accepts the four delegate shapes above. C# events with other delegate types need an adapter. `FileSystemEventHandler` is a distinct type, even though its signature resembles `EventHandler<FileSystemEventArgs>`.

The [file-watcher example](Examples/EventAwaiterKit.Examples/Program.cs) retains one adapter instance, subscribes to `Created`, enables the watcher, and creates a temporary file. It verifies the returned path, observes cleanup, disposes the watcher, and removes the temporary directory. It uses an actual filesystem event.

## Cleanup, exceptions, and UI callers

- Event, timeout, and cancellation compete for one outcome. Later signals do nothing.
- Cleanup waits for the add accessor to return or throw. Removal is attempted once for an attempted subscription, including after a failed add; it must tolerate an absent handler.
- The returned task completes after cleanup finishes or fails. Accessor exceptions reach that task instead of escaping through completion callbacks.
- Addition failure overrides an event signal. Cleanup failure overrides event/timeout/cancellation. If both accessors fail, the task faults with an `AggregateException` containing both.
- Cleanup uses the calling `SynchronizationContext` when present. Call from the source's owning context and keep its message pump running. Use `await`, not `.Wait()` or `.Result` on that thread.
- A failing remover or unavailable context can prevent detachment. Automatic cleanup cannot guarantee removal when the source refuses it.

Timeout accepts exactly `Timeout.InfiniteTimeSpan`, or zero through `Int32.MaxValue` milliseconds. Zero returns timeout without subscribing; an already-canceled token takes precedence over zero. Other negative values are invalid. Accessors cannot be interrupted by a timeout and must return promptly.

Read the full [lifecycle contract](LIFECYCLE.md) for ordering, exception precedence, and context limitations. Public API XML documentation is generated beside the library assembly for editor help.

### Why supply add/remove operations instead of a delegate?

The helper creates the handler and must attach and detach that exact instance. A delegate alone does not tell it which event accessors to invoke. The two lambdas provide those operations without reflection and work with both ordinary events and custom callback registration APIs.

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

The Windows check runs a real WinForms message loop with hidden controls. It verifies cleanup on the UI thread for a background event, timeout, background cancellation, and an actual control event. It does not open a visible window or validate visual UI behavior. The Windows-only project is kept outside the portable solution.

## Compatibility and scope

The library targets .NET Standard 2.0 and has no added runtime dependencies. Tests and executable examples target .NET 10. Validation for this change was performed on Windows with .NET 10, including the WinForms checks. Other compatible runtimes, including .NET Framework, have not been runtime-tested here; target compatibility alone is not a runtime test.

The library handles one event per call. Event streams, predicates, sender capture, device control, and automatic marshaling to an unknown source owner are outside its scope.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for checks and regression-test expectations. Keep behavior, examples, and documentation synchronized.

## Roadmap

- [ ] Publish EventAwaiterKit to NuGet

## License

[MIT](LICENSE).

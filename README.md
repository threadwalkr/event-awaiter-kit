# EventAwaiterKit

A small, zero-dependency .NET Standard 2.0 library for “awaiting” one future event, with timeout, cancellation, and cleanup.

Supply functions to add and remove a handler; EventAwaiterKit turns the next event into a task and unsubscribes when the wait ends.

## Supported events

| Event shape | Result |
| --- | --- |
| `Action` | `Task<bool>` |
| `EventHandler` | `Task<bool>` |
| `Action<T>` | `Task<EventWaitResult<T>>` |
| `EventHandler<TEventArgs>` | `Task<EventWaitResult<TEventArgs>>` |

Each shape supports an optional timeout and cancellation token. Untyped waits return `true` when the event fires and `false` on timeout; typed waits use `Occurred` to distinguish timeout from an event carrying `null` or another default value. Cancellation throws `OperationCanceledException` when awaited.

Events with other delegate types need an adapter. For example, `FileSystemWatcher.Created` uses `FileSystemEventHandler`; the [executable file-watcher example](Examples/EventAwaiterKit.Examples/Program.cs) shows how to adapt it.

## Subscribe before starting work

`WaitForEventAsync` subscribes before it returns, so create the wait task before starting the operation. This also catches an event raised synchronously by the start method:

```csharp
var wait = EventAwaiter.WaitForEventAsync<int>(
    h => sensor.Measured += h,
    h => sensor.Measured -= h,
    TimeSpan.FromSeconds(2),
    cancellationToken);

sensor.Measure();
var result = await wait;
if (result.Occurred)
    Console.WriteLine(result.Value);
```

If starting the operation can throw, cancel and await the pending wait so its handler is removed. The [runnable example](Examples/EventAwaiterKit.Examples/Program.cs) shows that cleanup. It also shows how to wait for either of two events: `Task.WhenAny` identifies the first, but you still need to cancel and observe the other wait.

For cleanup ordering, accessor failures, and UI-thread behavior, see the [lifecycle contract](LIFECYCLE.md).

## Why supply add/remove operations instead of a delegate?

The helper creates a handler and needs to attach and detach that exact instance. A delegate alone does not identify the event accessors, so the two lambdas supply those operations for both ordinary events and custom callback registration APIs.

## Build, tests, and examples

With the .NET 10 SDK installed, run:

```sh
dotnet build EventAwaiterKit.sln -c Release
dotnet test EventAwaiterKit.sln -c Release
dotnet run --project Examples/EventAwaiterKit.Examples -c Release
```

On Windows, run `dotnet run --project Examples/EventAwaiterKit.WindowsChecks -c Release` to check cleanup through a WinForms message loop.

- Version: [`EventAwaiterKit.csproj`](EventAwaiterKit/EventAwaiterKit.csproj)
- Contributing: [CONTRIBUTING.md](CONTRIBUTING.md)
- License: [MIT](LICENSE)

# EventAwaiterKit

A small, zero-dependency .NET Standard 2.0 library for “awaiting” one future event, with timeout, cancellation, and cleanup.

Use it when an API exposes events or callbacks and you want to await a button click, a device response, or the completion of an operation.

## Usage

Pass two functions to subscribe and unsubscribe from the event. For example, to wait up to ten seconds for a WinForms button click:

```csharp
using EventAwaiterKit;

bool clicked = await EventAwaiter.WaitForEventAsync(
    handler => button.Click += handler,
    handler => button.Click -= handler,
    TimeSpan.FromSeconds(10),
    cancellationToken);

Console.WriteLine(clicked ? "Button clicked." : "Timed out.");
```

The wait returns `true` when the event fires or `false` when the timeout expires, and removes its handler before completing. You can omit the timeout to wait indefinitely, or cancel the wait with a `CancellationToken`; awaiting a canceled wait throws `OperationCanceledException`.

## Subscribe before starting work

If you are starting an operation that will raise an event, create the wait task first. Otherwise, a fast response could arrive before the handler is attached.

For a sensor with an `Action<int>` event named `Measured`:

```csharp
var measurement = EventAwaiter.WaitForEventAsync<int>(
    handler => sensor.Measured += handler,
    handler => sensor.Measured -= handler,
    TimeSpan.FromSeconds(2),
    cancellationToken);

sensor.Measure();
var result = await measurement;

if (result.Occurred)
    Console.WriteLine($"Measurement: {result.Value}");
else
    Console.WriteLine("Measurement timed out.");
```

`WaitForEventAsync` subscribes before returning its task, so this works even if `Measure()` raises the event before it returns. The typed result uses `Occurred` to distinguish timeout from a valid payload such as `0` or `null`; read `Value` only when `Occurred` is true.

## Supported event types

| Event | Return type |
| --- | --- |
| `Action` | `Task<bool>` |
| `EventHandler` | `Task<bool>` |
| `Action<T>` | `Task<EventWaitResult<T>>` |
| `EventHandler<TEventArgs>` | `Task<EventWaitResult<TEventArgs>>` |

Events with another delegate type, such as `FileSystemEventHandler`, need an adapter; see the [file-watcher example](Examples/EventAwaiterKit.Examples/Program.cs).

For timeout limits, cleanup errors, and UI-thread behavior, see the [lifecycle contract](LIFECYCLE.md).

## Why supply add/remove operations instead of a delegate?

The helper creates a handler and needs to attach and detach that exact instance. A delegate alone does not identify the event accessors, so the two lambdas supply those operations for both ordinary events and custom callback registration APIs.

## Build, tests, and examples

To use the library in your app, add a project reference:

```sh
dotnet add YourApp.csproj reference path/to/EventAwaiterKit/EventAwaiterKit.csproj
```

To build the repository and run its tests and examples, use the .NET 10 SDK:

```sh
dotnet build EventAwaiterKit.sln -c Release
dotnet test EventAwaiterKit.sln -c Release
dotnet run --project Examples/EventAwaiterKit.Examples -c Release
```

The examples cover a synchronous callback, a file-watcher event, and waiting for either of two events while cleaning up the remaining wait. On Windows, you can also run the WinForms cleanup checks:

```sh
dotnet run --project Examples/EventAwaiterKit.WindowsChecks -c Release
```

The version number is in [EventAwaiterKit.csproj](EventAwaiterKit/EventAwaiterKit.csproj). See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.

## License

[MIT](LICENSE).

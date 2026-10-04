# Contributing

Use the .NET 10 SDK to build and test the library, which targets .NET Standard 2.0.

```sh
dotnet restore EventAwaiterKit.sln
dotnet build EventAwaiterKit.sln -c Debug --no-restore
dotnet test EventAwaiterKit.sln -c Debug --no-build
dotnet build EventAwaiterKit.sln -c Release --no-restore
dotnet test EventAwaiterKit.sln -c Release --no-build
dotnet run --project Examples/EventAwaiterKit.Examples -c Release --no-build
```

On Windows, run `dotnet run --project Examples/EventAwaiterKit.WindowsChecks -c Release` to check event, timeout, and cancellation cleanup through a hidden WinForms message loop. The command exits with a nonzero code if cleanup runs on the wrong thread, produces an incorrect result, or stalls.

Add regression tests for behavior changes, covering each affected event shape. For concurrency-sensitive cases, coordinate callbacks or use a controlled synchronization context so tests do not depend on arbitrary delays.

Preserve public signatures unless a breaking change is intentional and documented, and update the XML documentation, lifecycle contract, and executable examples when behavior changes. Follow the existing code style and add runtime dependencies only when needed.

# Contributing

Use the .NET 10 SDK. The library itself remains compatible with .NET Standard 2.0.

```sh
dotnet restore EventAwaiterKit.sln
dotnet build EventAwaiterKit.sln -c Debug --no-restore
dotnet test EventAwaiterKit.sln -c Debug --no-build
dotnet build EventAwaiterKit.sln -c Release --no-restore
dotnet test EventAwaiterKit.sln -c Release --no-build
dotnet run --project Examples/EventAwaiterKit.Examples -c Release --no-build
```

On Windows, also run `dotnet run --project Examples/EventAwaiterKit.WindowsChecks -c Release`. It starts a hidden WinForms message loop and exits with a nonzero code if cleanup uses the wrong thread, returns an incorrect outcome, or stalls.

For a lifecycle bug, add a bounded regression test that fails on the old implementation. Exercise the shared contract across all four event shapes. Prefer coordinated signals and controlled synchronization contexts over sleeps. Small bounded race loops supplement deterministic checks; timing alone is not proof of correctness.

Preserve existing public signatures unless a breaking change is intentional and documented. Update XML documentation, the lifecycle contract, and executable examples when behavior changes. Retain the existing style and avoid new runtime dependencies without a concrete need.

Run `git diff --check` before submitting. Explain the problem, resulting behavior, and validation in the pull request. For issues, include a minimal reproduction, runtime/OS, event shape, whether a synchronization context exists, and the full exception details.

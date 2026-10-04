using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventAwaiterKit
{
    /// <summary>Waits for one future event, with optional timeout and cancellation.</summary>
    public static class EventAwaiter
    {
        public static Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler, signal => () => signal(true), false, timeout, cancellationToken);

        public static Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);

        public static Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
            => EventWait.WaitAsync(addHandler, removeHandler, signal => (sender, args) => signal(true), false, timeout, cancellationToken);

        public static Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
